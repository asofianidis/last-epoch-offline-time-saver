using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppLE.Audio.Fmod.Components;
using MelonLoader;

namespace OfflineTimeSaver;

/// <summary>
/// One click from any point in the story to the End of Time with the campaign done. Quests go through
/// the game's own completion code, so their rewards, passive points, idol slots and timeline progress apply as normal.
/// </summary>
internal static class Campaign
{
    private const string EndOfTime = "EoT";

    // Flagged as main story, but replaced long ago: a finished character never has them.
    private static readonly HashSet<int> LegacyQuests = new()
    {
        15, // The End of Time
        18, // A Long Detour
    };

    /// <summary>True during the batch, so the quests don't each play a sound and open a reward panel.</summary>
    internal static bool Silent { get; private set; }

    internal static string SkipToEndGame()
    {
        if (!Mod.Active) return "Only available in offline play.";
        try
        {
            var actor = PlayerFinder.getPlayerActor();
            var data = PlayerFinder.getPlayerData();
            var quests = PlayerFinder.getLocalPlayerStatefulQuestList();
            if (actor == null || data == null || quests == null) return "No character loaded.";
            if (actor.isDying()) return "Can't do that while dying.";

            int completed = CompleteQuests(actor, quests);
            int waypoints = UnlockWaypoints(actor, data);
            data.PortalUnlocked = true;
            Leveling.Save();

            bool travelling = SceneList.getSceneName() != EndOfTime;
            if (travelling) PlayerSync.local.SendAttemptWaypoint(EndOfTime, 0);

            string result = $"{completed} quests done, {waypoints} waypoints unlocked."
                + (travelling ? " Travelling..." : "");
            Melon<Mod>.Logger.Msg(result);
            return result;
        }
        catch (Exception ex)
        {
            Melon<Mod>.Logger.Warning($"Skipping to end game failed: {ex.GetBaseException().Message}");
            return "Skipping to end game failed, see the log.";
        }
    }

    private static int CompleteQuests(Actor actor, StatefulQuestList quests)
    {
        var all = QuestList.get().quests;
        var selected = new List<Quest>();
        for (int i = 0; i < all.Count; i++)
        {
            if (ShouldComplete(all[i])) selected.Add(all[i]);
        }

        int completed = 0;
        Silent = true;
        try
        {
            // In story order, because quest XP is scaled to the character's level at the time.
            foreach (var quest in selected.OrderBy(q => q.chapter).ThenBy(q => q.id))
            {
                if (quests.getQuestState(quest) == QuestState.Completed) continue;
                try
                {
                    quest.completeQuest(actor, true);
                }
                catch (Exception ex)
                {
                    Melon<Mod>.Logger.Warning($"Quest {quest.id} failed: {ex.GetBaseException().Message}");
                }

                if (quests.getQuestState(quest) == QuestState.Completed) completed++;
                else Melon<Mod>.Logger.Warning($"Quest {quest.id} ({quest.displayName}) did not complete.");
            }
        }
        finally
        {
            Silent = false;
        }
        return completed;
    }

    private static bool ShouldComplete(Quest quest)
    {
        if (quest.testQuest || quest.questType != QuestType.Normal) return false;
        if (quest.passivePointsReward > 0 || quest.idolUnlockReward > 0) return true;

        // The game's own definition of the campaign, from its "complete all quests" achievement. Monolith
        // tutorials count as chapter Minilith and stay open, since they introduce the end game in the hub.
        return quest.mainLineQuest && quest.playerVisible
            && quest.chapter != ZoneChapterManager.Chapter.Minilith
            && !quest.ignoreForQuestAchievement
            && !LegacyQuests.Contains(quest.id);
    }

    private static int UnlockWaypoints(Actor actor, Il2CppLE.Data.CharacterData data)
    {
        // Unfiltered apart from menus, like the developers' own unlock-all. Scenes without a waypoint are never looked up.
        var scenes = SceneList.get().sceneDetailsCollection;
        var names = new Il2CppSystem.Collections.Generic.List<string>();
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneType != SceneType.NonGameplay && !string.IsNullOrEmpty(scenes[i].Name)) names.Add(scenes[i].Name);
        }

        int before = data.UnlockedWaypointScenes.Count;
        data.AddUnlockedWaypointsScenes(names);
        int added = data.UnlockedWaypointScenes.Count - before;

        // Saves and tells the map. The scene is already on the list, so this adds nothing.
        var sync = actor.GetActorSync().TryCast<PlayerActorSync>();
        if (sync != null) MonolithZoneManager.UnlockWaypointForPlayer(sync, EndOfTime);
        return added;
    }
}

[HarmonyPatch(typeof(QuestRewardPanel), nameof(QuestRewardPanel.open))]
internal static class QuestRewardPanelPatch
{
    private static bool Prefix() => !Campaign.Silent;
}

[HarmonyPatch(typeof(Notifications), nameof(Notifications.ShowQuestRewardNotification))]
internal static class QuestRewardNotificationPatch
{
    private static bool Prefix() => !Campaign.Silent;
}

[HarmonyPatch(typeof(QuestSoundPlayer), nameof(QuestSoundPlayer.playQuestSound))]
internal static class QuestSoundPatch
{
    private static bool Prefix() => !Campaign.Silent;
}
