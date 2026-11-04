using System;
using HarmonyLib;
using Il2Cpp;
using Il2CppItemFiltering;
using UnityEngine;

namespace OfflineTimeSaver;

[HarmonyPatch(typeof(Spawner), nameof(Spawner.GenerateEntitiesInternal))]
internal static class DensityPatch
{
    private const float SingleSpawnLimit = 1.5f, MaxPackRoll = 128f;

    private static void Prefix(Spawner __instance, out float __state)
    {
        __state = float.NaN;
        if (!Mod.Active || Settings.Density <= 1f || !IsNormalPack(__instance)) return;

        float count = __instance.numberToSpawn;
        float ceiling = MaxPackRoll / (1f + Math.Clamp(__instance.percentVariance, 0f, 1f));
        if (count >= ceiling) return;

        __state = count;
        __instance.numberToSpawn = Math.Min(count * Settings.Density, ceiling);
        Mod.FirstUse("MonsterDensity", $"pack {count} -> {__instance.numberToSpawn}");
    }

    // The game rolls the pack inside this one call, so the spawner keeps its original number afterwards.
    private static void Postfix(Spawner __instance, float __state)
    {
        if (!float.IsNaN(__state)) __instance.numberToSpawn = __state;
    }

    private static bool IsNormalPack(Spawner spawner)
    {
        if (spawner.numberToSpawn <= SingleSpawnLimit || spawner.isChampion || spawner.forceGood) return false;
        if (spawner.spawnerContext != SpawnerContext.Default && spawner.spawnerContext != SpawnerContext.WaveSpawner) return false;

        var data = spawner.actorData;
        return data != null && data.actorType == ActorData.Type.Normal
            && !data.IsLoneBoss && !data.isChampion && !data.isOmen && !data.isNemesis && !data.isHarbinger;
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropAncientBoneForPlayer))]
internal static class AncientBoneMultiplierPatch
{
    private static void Prefix(ref int amount)
    {
        if (!Mod.Active) return;
        int before = amount;
        amount = (int)Math.Min(amount * (double)Settings.AncientBones, int.MaxValue);
        Mod.FirstUse("AncientBones", $"{before} -> {amount}");
    }
}

[HarmonyPatch(typeof(GenerateItems), nameof(GenerateItems.rollForgingPotential))]
internal static class ForgingPotentialPatch
{
    private const int GameMaximum = 63;

    private static void Postfix(GenerateItems.GenerationContext generationContext, ref int __result)
    {
        if (!Mod.Active || __result <= 0) return;
        if (generationContext is GenerateItems.GenerationContext.Shop or GenerateItems.GenerationContext.Gambler
            or GenerateItems.GenerationContext.SoulGambler) return;

        int before = __result;
        __result = Math.Max(before, Math.Min((int)Math.Round(before * (double)Settings.ForgingPotential), GameMaximum));
        Mod.FirstUse("ForgingPotential", $"{before} -> {__result}");
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropItemForPlayer))]
internal static class ItemAutoPickupPatch
{
    // Item types below this are equipment and idols; crafting materials are identified by the game.
    private const int FirstNonEquipmentType = 34;

    private static bool Prefix(Actor player, ItemData itemData)
    {
        if (!Mod.Active || player == null || itemData == null || itemData.isTutorialItem()) return true;

        var containers = ItemContainersManager.Instance;
        if (containers == null) return true;

        bool material = Settings.AutoMaterials.Value && ItemList.isCraftingItem(itemData.itemType);
        bool wanted = !material && Settings.AutoFilteredItems.Value && itemData.itemType < FirstNonEquipmentType && FilterShows(itemData);
        if (!material && !wanted) return true;

        // A full inventory returns false, and the item drops on the ground as normal.
        if (!containers.attemptToPickupItem(itemData, player.position())) return true;

        if (material && Settings.StoreMaterials.Value) containers.TryStoreMaterials(false);
        Mod.FirstUse(material ? "AutoPickupMaterials" : "AutoPickupFilteredItems", $"item type {itemData.itemType}");
        return false;
    }

    // Only an item matched by a visible rule counts, so playing without a filter picks up no gear.
    private static bool FilterShows(ItemData itemData)
    {
        var filter = ItemFilterManager.Instance == null ? null : ItemFilterManager.Instance.Filter;
        var item = itemData.TryCast<Il2Cpp.ItemDataUnpacked>();
        if (filter == null || item == null) return false;

        var outcome = filter.Match(item, out _, out _, out int rule, out _, out _, out _, out _, out Color _);
        return rule >= 0 && outcome != Rule.RuleOutcome.HIDE;
    }
}
