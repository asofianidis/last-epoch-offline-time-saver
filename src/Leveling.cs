using System;
using Il2Cpp;
using MelonLoader;

namespace OfflineTimeSaver;

/// <summary>One-click levelling, using the game's own level-up code so unlocks and points are granted as normal.</summary>
internal static class Leveling
{
    // Both caps are hard-coded inside the game's level-up methods.
    private const int MaxCharacterLevel = 100, MaxSkillLevel = 20;

    internal static string MaxCharacter() => Run(() =>
    {
        var tracker = PlayerFinder.getExperienceTracker();
        var stats = tracker == null ? null : tracker.stats;
        if (stats == null) return "No character loaded.";
        if (stats.level >= MaxCharacterLevel) return $"Already level {MaxCharacterLevel}.";
        if (tracker.getActor().isDying()) return "Can't level up while dying.";

        int start = stats.level;
        while (stats.level < MaxCharacterLevel)
        {
            int before = stats.level;
            tracker.LevelUp();
            if (stats.level == before) break;
        }

        Save();
        return $"Character level {start} -> {stats.level}.";
    });

    internal static string MaxSkills() => Run(() =>
    {
        var trees = PlayerFinder.getLocalTreeData();
        var skills = trees == null ? null : trees.specialisedSkillTrees;
        if (skills == null || skills.Count == 0) return "No specialised skills to level.";

        int below = CountBelowMax(skills);
        if (below == 0) return $"All specialised skills are already level {MaxSkillLevel}.";

        // Exactly the XP for the cap: the game adds this to each skill's XP in an int before capping it.
        trees.ApplyAbilityXp(SpecialisedAbilityManager.getAbilityXPLevelRequirement(MaxSkillLevel), false);

        Save();
        return $"{below - CountBelowMax(skills)} specialised skill(s) raised to level {MaxSkillLevel}.";
    });

    private static int CountBelowMax(Il2CppSystem.Collections.Generic.List<LocalTreeData.SkillTreeData> skills)
    {
        int count = 0;
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i].level < MaxSkillLevel) count++;
        }
        return count;
    }

    private static void Save()
    {
        var data = PlayerFinder.getPlayerDataTracker();
        if (data != null) data.MarkCharacterDirty(true);
    }

    private static string Run(Func<string> action)
    {
        if (!Mod.Active) return "Only available in offline play.";
        try
        {
            string result = action();
            Melon<Mod>.Logger.Msg(result);
            return result;
        }
        catch (Exception ex)
        {
            Melon<Mod>.Logger.Warning($"Levelling failed: {ex.GetBaseException().Message}");
            return "Levelling failed, see the log.";
        }
    }
}
