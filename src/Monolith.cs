using System;
using Il2Cpp;
using MelonLoader;

namespace OfflineTimeSaver;

/// <summary>Raises corruption and fills stability on one of the character's Monolith timelines, from the End of Time.</summary>
internal static class Monolith
{
    private const string EndOfTime = "EoT";
    internal const int CorruptionStep = 50;

    private static int _selected;

    /// <summary>The selected run's name, and its corruption and stability, for the menu.</summary>
    internal static void Selected(out string name, out string values)
    {
        name = "No timelines started";
        values = "Open one in the Monolith first";
        try
        {
            var run = SelectedRun(out _);
            if (run == null) return;
            name = Name(run);
            values = $"Corruption {run.web?.corruption ?? 0}     Stability {run.stability} / {run.GetMaxStability()}";
        }
        catch { /* shown as no timelines */ }
    }

    internal static void Cycle(int direction)
    {
        var runs = Runs();
        if (runs == null || runs.Count == 0) return;
        _selected = ((_selected + direction) % runs.Count + runs.Count) % runs.Count;
    }

    internal static string AddCorruption()
    {
        if (!CanChange(out var actor, out var run, out string error)) return error;
        try
        {
            var web = run.web;
            if (web == null) return "That timeline has no echo web yet. Open it in the Monolith first.";

            int target = web.corruption + CorruptionStep;
            var difficulty = Difficulty(run);
            if (difficulty != null && difficulty.hasMaxCorruption) target = Math.Min(target, difficulty.maximumCorruption);
            if (target <= web.corruption) return $"{Name(run)} is already at its maximum corruption.";

            web.corruption = target;
            run.updateCorruptionMod();
            run.calculateIncreasedRarityAndExperienceFromMods();
            PlayerFinder.getMonolithProgressManager()?.UpdateCorruption(run.timelineID, target);
            actor.monolithRunsManager.updateHighestCorruptionOfAnyRun();
            actor.monolithRunsManager.saveRuns();

            string result = $"{Name(run)} corruption is now {target}.";
            Melon<Mod>.Logger.Msg(result);
            return result;
        }
        catch (Exception ex)
        {
            Melon<Mod>.Logger.Warning($"Adding corruption failed: {ex.GetBaseException().Message}");
            return "Adding corruption failed, see the log.";
        }
    }

    internal static string FillStability()
    {
        if (!CanChange(out var actor, out var run, out string error)) return error;
        try
        {
            int max = run.GetMaxStability();
            if (run.stability >= max) return $"{Name(run)} stability is already full.";

            run.AddStability(max - run.stability);
            run.capStabilityToMaximum();
            actor.monolithRunsManager.saveRuns();

            string result = $"{Name(run)} stability is now {run.stability}/{max}.";
            Melon<Mod>.Logger.Msg(result);
            return result;
        }
        catch (Exception ex)
        {
            Melon<Mod>.Logger.Warning($"Filling stability failed: {ex.GetBaseException().Message}");
            return "Filling stability failed, see the log.";
        }
    }

    // Only in the End of Time, so a run is never changed while one of its echoes is being played.
    private static bool CanChange(out Actor actor, out MonolithRun run, out string error)
    {
        actor = null;
        run = null;
        error = null;
        if (!Mod.Active) error = "Only available in offline play.";
        else if (SceneList.getSceneName() != EndOfTime) error = "Go to the End of Time first.";
        else if ((run = SelectedRun(out actor)) == null) error = "No timelines started. Open one in the Monolith first.";
        return error == null;
    }

    private static MonolithRun SelectedRun(out Actor actor)
    {
        actor = PlayerFinder.getPlayerActor();
        var runs = actor?.monolithRunsManager?.runs;
        if (runs == null || runs.Count == 0) return null;
        if (_selected >= runs.Count) _selected = 0;
        return runs[_selected];
    }

    private static Il2CppSystem.Collections.Generic.List<MonolithRun> Runs()
        => PlayerFinder.getPlayerActor()?.monolithRunsManager?.runs;

    private static MonolithTimeline.Difficulty Difficulty(MonolithRun run)
    {
        var difficulties = run.timeline?.difficulties;
        return difficulties != null && run.difficultyIndex < difficulties.Count ? difficulties[run.difficultyIndex] : null;
    }

    private static string Name(MonolithRun run)
    {
        string name = run.timeline?.displayName;
        if (string.IsNullOrEmpty(name)) name = run.timelineID.ToString();
        return run.difficultyIndex > 0 ? name + " (Empowered)" : name;
    }
}
