using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;

namespace OfflineTimeSaver;

// Parameter names below must match the game's (Harmony binds by name), so a renamed
// parameter fails that patch at startup and shows as [FAILED] in the log.

[HarmonyPatch(typeof(ExperienceTracker), nameof(ExperienceTracker.GainExp))]
internal static class ExperiencePatch
{
    private static void Prefix(ref long characterExp, ref long abilityExp, ref long expForFavourGain)
    {
        if (!Mod.Active) return;
        long baseXp = characterExp;
        characterExp = (long)(characterExp * (double)Settings.Xp);
        abilityExp = (long)(abilityExp * (double)Settings.AbilityXp);
        expForFavourGain = (long)(expForFavourGain * (double)Settings.Favor);
        Mod.FirstUse("Experience", $"{baseXp} -> {characterExp}");
    }
}

[HarmonyPatch]
internal static class ItemQuantityPatch
{
    // The static overload is the one that takes the final multiplier for a drop.
    private static MethodBase TargetMethod() =>
        AccessTools.GetDeclaredMethods(typeof(ItemDrop)).First(m => m.Name == nameof(ItemDrop.DropItem) && m.IsStatic);

    private static void Prefix(ref float itemMultiplier)
    {
        if (!Mod.Active) return;
        float before = itemMultiplier;
        itemMultiplier *= Settings.ItemQuantity;
        Mod.FirstUse("ItemQuantity", $"{before} -> {itemMultiplier}");
    }
}

[HarmonyPatch(typeof(GenerateItems), nameof(GenerateItems.RollRarity))]
internal static class UniqueSetRarityPatch
{
    private static void Prefix(ref float uniqueAndSetDropRateMultiplier)
    {
        if (!Mod.Active) return;
        float before = uniqueAndSetDropRateMultiplier;
        uniqueAndSetDropRateMultiplier *= Settings.UniqueSet;
        Mod.FirstUse("UniqueSetRarity", $"{before} -> {uniqueAndSetDropRateMultiplier}");
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropGoldForPlayer))]
internal static class GoldPatch
{
    private static bool Prefix(Actor player, ref int goldValue)
    {
        if (!Mod.Active) return true;
        int before = goldValue;
        goldValue = (int)Math.Min(goldValue * (double)Settings.Gold, int.MaxValue);
        Mod.FirstUse("Gold", $"{before} -> {goldValue}");

        var tracker = player == null ? null : player.goldTracker;
        if (!Settings.AutoGold.Value || tracker == null) return true;
        tracker.modifyGold(goldValue);
        Mod.FirstUse("AutoPickupGold", $"+{goldValue}");
        return false;
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropPotionForPlayer))]
internal static class PotionPatch
{
    private static bool Prefix(Actor player)
    {
        if (!Mod.Active || !Settings.AutoPotions.Value || player == null) return true;
        var potion = player.getHealthPotion();
        if (potion == null || potion.currentCharges >= potion.maxCharges) return true; // full: leave it on the ground
        potion.gainPotions(1);
        Mod.FirstUse("AutoPickupPotions", $"charges now {potion.currentCharges}");
        return false;
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropXPTomeForPlayer))]
internal static class XpTomePatch
{
    private static void Postfix(GroundItemManager __instance, Actor player)
    {
        if (!Mod.Active || !Settings.AutoXpTomes.Value) return;
        uint id = __instance.nextXpTomeId - 1;
        var list = __instance.activeXPTomes;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].id != id) continue;
            __instance.pickupXPTome(player, id, list[i]);
            Mod.FirstUse("AutoPickupXpTomes", $"tome {id}");
            return;
        }
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropFavorTomeForPlayer))]
internal static class FavorTomePatch
{
    private static void Postfix(GroundItemManager __instance, Actor player)
    {
        if (!Mod.Active || !Settings.AutoFavorTomes.Value) return;
        uint id = __instance.nextFavorTomeId - 1;
        var list = __instance.activeFavorTomes;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].id != id) continue;
            __instance.pickupFavorTome(player, id, list[i]);
            Mod.FirstUse("AutoPickupFavorTomes", $"tome {id}");
            return;
        }
    }
}

[HarmonyPatch(typeof(GroundItemManager), nameof(GroundItemManager.dropAncientBoneForPlayer))]
internal static class AncientBonePatch
{
    private static void Postfix(GroundItemManager __instance, Actor player)
    {
        if (!Mod.Active || !Settings.AutoAncientBones.Value) return;
        uint id = __instance.nextAncientBoneId - 1;
        var list = __instance.activeAncientBones;
        for (int i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].id != id) continue;
            __instance.pickupAncientBone(player, id, list[i]);
            Mod.FirstUse("AutoPickupAncientBones", $"bone {id}");
            return;
        }
    }
}
