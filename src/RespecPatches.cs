using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace OfflineTimeSaver;

// Don't patch passiveRespecCost, passiveTimeGlassFragmentRespecCost or GetMemoryAmberRespecCostForWeaverTree.
// They are tiny enough that the game inlines them or shares their code with unrelated methods,
// so a patch would either do nothing or change those other methods too.

internal static class FreeRespec
{
    internal static bool On => Mod.Active && Settings.FreeRespecs.Value;

    internal static void Zero(ref int cost, string hook)
    {
        if (!On) return;
        if (cost > 0) Mod.FirstUse(hook, $"{cost} -> 0");
        cost = 0;
    }
}

// Confirming pending changes in the passive tree.
[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.CalculateRespecCostForBulkUpdate))]
internal static class PassiveRespecCostPatch
{
    private static void Postfix(ref int __result) => FreeRespec.Zero(ref __result, "FreeRespecPassive");
}

// Confirming pending changes in the Weaver tree.
[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.CalculateWeaverRespecCostForBulkUpdate))]
internal static class WeaverRespecCostPatch
{
    private static void Postfix(ref int __result) => FreeRespec.Zero(ref __result, "FreeRespecWeaver");
}

// The cost shown while changes are pending, in both trees.
[HarmonyPatch(typeof(TreeUIManager), nameof(TreeUIManager.CalculateTotalRespecCost))]
internal static class RespecPreviewCostPatch
{
    private static void Postfix(ref int __result) => FreeRespec.Zero(ref __result, "FreeRespecPreview");
}

[HarmonyPatch(typeof(TreeUIManager), nameof(TreeUIManager.RefundingPointCostsCurrency))]
internal static class RespecTooltipPatch
{
    private static void Postfix(ref bool __result)
    {
        if (FreeRespec.On) __result = false;
    }
}

[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.getGoldCostToRespecPassivePoint))]
internal static class RespecPointConfirmCostPatch
{
    private static void Postfix(ref int __result) => FreeRespec.Zero(ref __result, "FreeRespecPointPrompt");
}

// Respec all, which changing mastery also goes through.
[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.GetNodesToRespec))]
internal static class RespecAllCostPatch
{
    private static void Postfix(ref int totalGoldCost, ref bool canAfford, ref int totalTimeGlassCost)
    {
        if (!FreeRespec.On) return;
        FreeRespec.Zero(ref totalGoldCost, "FreeRespecAllGold");
        FreeRespec.Zero(ref totalTimeGlassCost, "FreeRespecAllTimeGlass");
        canAfford = true;
    }
}

[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.GetMasteryRespectGoldCostForLevel))]
internal static class MasteryRespecCostPatch
{
    private static void Postfix(ref int __result) => FreeRespec.Zero(ref __result, "FreeRespecMastery");
}

// Respec all spends its cost even when that is 0, and the game throws if the character has
// no time glass at that point, after the nodes have been cleared but before they are saved.
[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.spendTimeGlassFragments))]
internal static class SpendNoTimeGlassPatch
{
    private static bool Prefix(int amount) => !(FreeRespec.On && amount <= 0);
}

[HarmonyPatch(typeof(TreeUIManager), nameof(TreeUIManager.RespecAllWeaverTree))]
internal static class WeaverRefundAllPreviewPatch
{
    private static void Postfix(ref int totalCost) => FreeRespec.Zero(ref totalCost, "FreeRespecWeaverAll");
}

// The two commands below check Memory Amber against a fixed cost inside the game's code, so they
// would refuse a player without enough. The update they are replaced with charges the patched cost above.
[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.ReceiveRefundAllWeaverTreeCommand))]
internal static class WeaverRefundAllPatch
{
    private static bool Prefix(LocalTreeData __instance)
    {
        if (!FreeRespec.On) return true;
        // Nodes left out of the update go to 0 points.
        __instance.ReceiveBulkWeaverTreeUpdateCommand(new Il2CppSystem.Collections.Generic.Dictionary<byte, byte>());
        return false;
    }
}

[HarmonyPatch(typeof(LocalTreeData), nameof(LocalTreeData.ReceiveRespecWeaverPointsCommand), new[] { typeof(Il2CppStructArray<byte>) })]
internal static class WeaverRespecPointsPatch
{
    private static bool Prefix(LocalTreeData __instance, Il2CppStructArray<byte> nodeIDs, ref bool __result)
    {
        var nodes = __instance.weaverTree == null ? null : __instance.weaverTree.nodes;
        if (!FreeRespec.On || nodes == null || nodeIDs == null) return true;

        var allocation = new Il2CppSystem.Collections.Generic.Dictionary<byte, byte>();
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i].pointsAllocated > 0) allocation[nodes[i].id] = nodes[i].pointsAllocated;
        }

        foreach (byte id in nodeIDs)
        {
            if (!allocation.ContainsKey(id)) continue;
            if (allocation[id] > 1) allocation[id] = (byte)(allocation[id] - 1);
            else allocation.Remove(id);
        }

        int before = TotalPoints(nodes);
        __instance.ReceiveBulkWeaverTreeUpdateCommand(allocation);
        __result = TotalPoints(nodes) < before;
        return false;
    }

    private static int TotalPoints(Il2CppSystem.Collections.Generic.List<LocalTreeData.NodeData> nodes)
    {
        int total = 0;
        for (int i = 0; i < nodes.Count; i++) total += nodes[i].pointsAllocated;
        return total;
    }
}

// The game's own check and charge are a fixed amount inside this method, so it gets
// that much gold to spend and the balance is put back afterwards.
[HarmonyPatch(typeof(ItemContainersManager), nameof(ItemContainersManager.SwapBlessing))]
internal static class BlessingSwapPatch
{
    private const int GameCost = 10000;

    private static void Prefix(ItemContainersManager __instance, out int __state)
    {
        __state = -1;
        var gold = FreeRespec.On ? __instance.getGoldTracker() : null;
        if (gold == null) return;
        __state = gold.value;
        gold.modifyGold(GameCost);
    }

    private static void Postfix(ItemContainersManager __instance, int __state)
    {
        if (__state < 0) return;
        var gold = __instance.getGoldTracker();
        gold.modifyGold(__state - gold.value);
        Mod.FirstUse("FreeRespecBlessing", $"gold stays {gold.value}");
    }
}

// Without enough gold the panel hides its confirm button, and otherwise it quotes the normal price.
[HarmonyPatch(typeof(InventoryPanelUI), nameof(InventoryPanelUI.OpenConfirmRespecBlessingPanel))]
internal static class BlessingSwapPanelPatch
{
    private static void Postfix(InventoryPanelUI __instance)
    {
        if (!FreeRespec.On) return;
        __instance.confirmRespecButton.gameObject.SetActive(true);
        __instance.message.text = "Changing this Blessing is free.";
    }
}
