using System;
using MelonLoader;

namespace OfflineTimeSaver;

/// <summary>Values live in UserData/MelonPreferences.cfg under [OfflineTimeSaver], and the F3 menu edits them. 1.0 = normal game.</summary>
internal static class Settings
{
    // Capped lower than the rest: each extra item is a real ground object, so huge values freeze the game.
    internal const float ItemQuantityMax = 20f;
    private const float MultiplierMax = 1000f;

    private static MelonPreferences_Category _category;

    internal const float MovementSpeedMax = 5f, DensityMax = 5f, ForgingPotentialMax = 10f;

    internal static MelonPreferences_Entry<float> XpEntry, AbilityXpEntry, FavorEntry, GoldEntry, ItemQuantityEntry, UniqueSetEntry;
    internal static MelonPreferences_Entry<float> MovementSpeedEntry, DensityEntry, AncientBonesEntry, ForgingPotentialEntry;
    internal static MelonPreferences_Entry<bool> Enabled, OfflineOnlyLogin;
    internal static MelonPreferences_Entry<bool> AutoGold, AutoPotions, AutoXpTomes, AutoFavorTomes, AutoAncientBones;
    internal static MelonPreferences_Entry<bool> AutoMaterials, StoreMaterials, AutoFilteredItems, FreeRespecs, KeylessDungeons;

    internal static float MovementSpeed => Multiplier(MovementSpeedEntry, MovementSpeedMax);
    internal static float Density => Multiplier(DensityEntry, DensityMax);
    internal static float AncientBones => Multiplier(AncientBonesEntry, MultiplierMax);
    internal static float ForgingPotential => Multiplier(ForgingPotentialEntry, ForgingPotentialMax);

    internal static float Xp => Multiplier(XpEntry, MultiplierMax);
    internal static float AbilityXp => Multiplier(AbilityXpEntry, MultiplierMax);
    internal static float Favor => Multiplier(FavorEntry, MultiplierMax);
    internal static float Gold => Multiplier(GoldEntry, MultiplierMax);
    internal static float ItemQuantity => Multiplier(ItemQuantityEntry, ItemQuantityMax);
    internal static float UniqueSet => Multiplier(UniqueSetEntry, MultiplierMax);

    private static float Multiplier(MelonPreferences_Entry<float> entry, float max) => Math.Clamp(entry.Value, 1f, max);

    internal static void Load()
    {
        var c = _category = MelonPreferences.CreateCategory("OfflineTimeSaver", "Offline Time Saver");

        Enabled = c.CreateEntry("Enabled", true, description: "Master switch for all gameplay changes.");
        OfflineOnlyLogin = c.CreateEntry("OfflineOnlyLogin", true, description: "Skip the login screen straight to offline play, and disable the online buttons.");

        XpEntry = c.CreateEntry("ExperienceMultiplier", 3f, description: "Character XP. 1 = normal.");
        AbilityXpEntry = c.CreateEntry("AbilityExperienceMultiplier", 3f, description: "Skill XP. 1 = normal.");
        FavorEntry = c.CreateEntry("FavorMultiplier", 3f, description: "Faction favor earned from XP. 1 = normal.");
        GoldEntry = c.CreateEntry("GoldMultiplier", 3f, description: "Gold per pile. 1 = normal.");
        ItemQuantityEntry = c.CreateEntry("ItemQuantityMultiplier", 2f, description: "Item drops (max 20). 1 = normal. High values can lag big fights.");
        UniqueSetEntry = c.CreateEntry("UniqueAndSetRarityMultiplier", 3f, description: "Chance that a drop is a Unique or Set. 1 = normal.");

        AutoGold = c.CreateEntry("AutoPickupGold", true, description: "Gold goes straight to you.");
        AutoPotions = c.CreateEntry("AutoPickupPotions", true, description: "Potions refill you unless you are full.");
        AutoXpTomes = c.CreateEntry("AutoPickupXpTomes", true);
        AutoFavorTomes = c.CreateEntry("AutoPickupFavorTomes", true);
        AutoAncientBones = c.CreateEntry("AutoPickupAncientBones", true);
        MovementSpeedEntry = c.CreateEntry("MovementSpeedMultiplier", 1f, description: "Movement speed (max 5). 1 = normal.");
        DensityEntry = c.CreateEntry("MonsterDensityMultiplier", 1f, description: "Monsters per normal pack (max 5), from the next zone load. Bosses and single spawns stay as they are. 1 = normal.");
        AncientBonesEntry = c.CreateEntry("AncientBonesMultiplier", 3f, description: "Ancient Bones per drop. 1 = normal.");
        ForgingPotentialEntry = c.CreateEntry("ForgingPotentialMultiplier", 2f, description: "Forging Potential on new items (max 10, item limit 63). Shops and gamblers stay normal. 1 = normal.");
        AutoMaterials = c.CreateEntry("AutoPickupMaterials", true, description: "Shards, runes and glyphs are picked up when they drop.");
        StoreMaterials = c.CreateEntry("SendMaterialsToStorage", true, description: "Auto-picked materials go straight to material storage.");
        AutoFilteredItems = c.CreateEntry("AutoPickupFilteredItems", true, description: "Pick up gear matched by a visible rule in your active loot filter. Needs a loot filter.");
        FreeRespecs = c.CreateEntry("FreeRespecs", true, description: "Respeccing passives, masteries, the Weaver tree and blessings costs nothing.");
        KeylessDungeons = c.CreateEntry("KeylessDungeons", true, description: "Enter dungeons without a key.");

        Save();
    }

    internal static void Save() => _category.SaveToFile(false);

    internal static void ResetToDefaults()
    {
        foreach (var entry in _category.Entries) entry.ResetToDefault();
    }
}
