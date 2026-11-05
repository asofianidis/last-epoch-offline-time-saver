using HarmonyLib;
using Il2Cpp;

namespace OfflineTimeSaver;

internal static class KeylessDungeons
{
    internal static bool On => Mod.Active && Settings.KeylessDungeons.Value;
}

// The check behind entering a dungeon. A key left in the slot is still used up on entry;
// the game also clears the slot when it is empty, so going in without one is safe.
[HarmonyPatch(typeof(ItemContainersManager), nameof(ItemContainersManager.IsOccupiedWithValidDungeonKey))]
internal static class DungeonKeyCheckPatch
{
    private static void Postfix(ref bool __result)
    {
        if (!KeylessDungeons.On || __result) return;
        __result = true;
        Mod.FirstUse("KeylessDungeons", "entered without a key");
    }
}

// The panel's enter button only works while the key slot is filled. These are the two places that turn it off.
[HarmonyPatch(typeof(DungeonEnterPanelUI), nameof(DungeonEnterPanelUI.Open))]
internal static class DungeonPanelOpenPatch
{
    private static void Postfix(DungeonEnterPanelUI __instance)
    {
        if (KeylessDungeons.On) __instance.keysEnterDungeonButton.interactable = true;
    }
}

[HarmonyPatch(typeof(DungeonEnterPanelUI), nameof(DungeonEnterPanelUI.onKeyExit))]
internal static class DungeonPanelKeyRemovedPatch
{
    private static void Postfix(DungeonEnterPanelUI __instance)
    {
        if (KeylessDungeons.On) __instance.keysEnterDungeonButton.interactable = true;
    }
}
