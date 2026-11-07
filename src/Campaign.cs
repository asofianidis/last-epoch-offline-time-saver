using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Il2Cpp;
using Il2CppLE.Audio.Fmod.Components;
using Il2CppLE.Factions;
using Il2CppLE.Networking;
using Il2CppLE.Services;
using Il2CppLE.Services.Models;
using MelonLoader;
using Il2CppNullableAffixEffectiveness = Il2CppSystem.Nullable<Il2Cpp.ItemList.AffixEffectiveness>;
using Il2CppNullableFaction = Il2CppSystem.Nullable<Il2CppLE.Factions.FactionID>;
using Il2CppNullableInt = Il2CppSystem.Nullable<int>;
using Il2CppNullableSpecialAffix = Il2CppSystem.Nullable<Il2Cpp.AffixList.SpecialAffixType>;

namespace OfflineTimeSaver;

/// <summary>
/// One click from any point in the story to the End of Time with the campaign done. Quests go through
/// the game's own completion code, so their rewards, passive points, idol slots and timeline progress apply as normal.
/// </summary>
internal static class Campaign
{
    private const string EndOfTime = "EoT";
    private const byte Normal = 0, Empowered = 1;

    // Flagged as main story, but replaced long ago: a finished character never has them.
    private static readonly HashSet<int> LegacyQuests = new()
    {
        15, // The End of Time
        18, // A Long Detour
    };

    // The item a Harbinger kill drops.
    private const int HarbingerRewardBaseType = 104, HarbingerRewardSubType = 7;

    // "Timeline N Completion" quests. The quests themselves don't say which timeline they belong to.
    private static readonly Dictionary<TimelineID, int> CompletionQuests = new()
    {
        [TimelineID.UndeadAbom] = 83,
        [TimelineID.OsprixWithLance] = 84,
        [TimelineID.VoidRahyeh] = 85,
        [TimelineID.FrostLich] = 86,
        [TimelineID.Lagon] = 87,
        [TimelineID.UndeadVsVoid] = 88,
        [TimelineID.Dragons] = 89,
        [TimelineID.Gaspar] = 110,
        [TimelineID.Heorot] = 111,
        [TimelineID.Volcano] = 112,
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
            int empowered = EmpowerTimelines(actor);
            int timelines = CompleteStashTimelines(actor, data);
            GrantFirstHarbingerKill(actor);
            data.PortalUnlocked = true;
            Leveling.Save();

            bool travelling = SceneList.getSceneName() != EndOfTime;
            // Offline play has no PlayerSync.local; this is what the map's own waypoint buttons call offline.
            if (travelling) ServiceProvider.TransitionService.Waypoint(PlayerStore.LocalUserIdentity, TransitionFadeType.None, EndOfTime, 0);

            string result = $"{completed} quests, {waypoints} waypoints, {empowered} empowered, {timelines} completed."
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

    // What the game does on the first Harbinger kill, without that fight's rewards.
    private static int EmpowerTimelines(Actor actor)
    {
        var progress = PlayerFinder.getMonolithProgressManager();
        var runs = actor.monolithRunsManager;
        if (progress == null || runs == null) return 0;

        int empowered = 0;
        var timelines = TimelineIDExtensions.GetAllTimelines();
        Silent = true;
        try
        {
            for (int i = 0; i < timelines.Length; i++)
            {
                var timeline = timelines[i];
                if (timeline == TimelineID.None || progress.IsTimelineUnlockedForDifficulty(timeline, Empowered)) continue;

                progress.unlockTimeline(timeline, Normal);
                progress.unlockTimeline(timeline, Empowered);
                if (!runs.TryGetRun(timeline, Empowered, out _)) runs.getNewRun(timeline, Empowered);
                empowered++;
            }
        }
        finally
        {
            Silent = false;
        }

        if (empowered > 0) progress.loadProgress();
        return empowered;
    }

    // Completes on this character each timeline completed anywhere on this stash, with its completion quest.
    // Blessings aren't granted, because the game has the player choose them at the end of the boss fight.
    private static int CompleteStashTimelines(Actor actor, Il2CppLE.Data.CharacterData data)
    {
        var progress = PlayerFinder.getMonolithProgressManager();
        var runs = actor.monolithRunsManager;
        if (progress == null || runs == null) return 0;

        int done = 0;
        var timelines = TimelineIDExtensions.GetAllTimelines();
        Silent = true;
        try
        {
            for (int i = 0; i < timelines.Length; i++)
            {
                var timeline = timelines[i];
                if (timeline == TimelineID.None) continue;

                var onStash = progress.getTimelineBranchCompletion(timeline);
                if (onStash == null || onStash.Count == 0) continue;

                // An existing run stays, since a new one would replace it.
                if (!runs.TryGetRun(timeline, Normal, out _)) runs.getNewRun(timeline, Normal);

                // Each call adds to the character's conquered count, so never repeat one.
                if (!CompletedOnCharacter(data, timeline))
                {
                    progress.completeTimeline(timeline, 0, Normal);
                    done++;
                }

                if (CompletionQuests.TryGetValue(timeline, out int id)) QuestList.get().getQuest(id)?.completeQuest(actor, true);
            }
        }
        finally
        {
            Silent = false;
        }
        return done;
    }

    private static bool CompletedOnCharacter(Il2CppLE.Data.CharacterData data, TimelineID timeline)
    {
        var completion = data.TimelineCompletion;
        for (int i = 0; i < completion.Count; i++)
        {
            if (completion[i].TimelineID == (byte)timeline && completion[i].Progress.Length > 0) return true;
        }
        return false;
    }

    // Forgotten Knights progress and its quests belong to the stash, so only an empty stash gets the kill itself.
    // Joining and the item belong to the character, and joining is what marks them as already given.
    private static void GrantFirstHarbingerKill(Actor actor)
    {
        var faction = actor.FactionInfo;
        var harbingers = faction.FK().progress;

        Silent = true;
        try
        {
            if (harbingers.CurrentProgression == 0
                && harbingers.MarkTimelineHarbingerAsDefeated(HarbingersData.NoEmpoweredHarbingerTimelines[0], harbingers.HarbingerCorruptionThreshold))
            {
                var data = HarbingersData.Get();
                data.ProcessHarbingerKill(actor.Cast<StatefulQuestListHolderProvider>(), harbingers.CurrentProgression);
                data.harbingerProgressionQuests[0].completeQuest(actor, true);
            }
        }
        finally
        {
            Silent = false;
        }

        if (faction.IsMemberOf(FactionID.ForgottenKnights)) return;
        faction.JoinFaction(FactionID.ForgottenKnights);
        GiveHarbingerReward(actor);
    }

    // The game drops this on the ground, where travelling away would lose it.
    private static void GiveHarbingerReward(Actor actor)
    {
        var item = GenerateItems.instance.initialiseRandomItemData(
            giftable: false,
            ilvl: 100,
            requireClassCompatibility: false,
            baseType: HarbingerRewardBaseType,
            subType: HarbingerRewardSubType,
            rarity: 0,
            uniqueID: 0,
            forceExalted: false,
            minimumLegendaryPotential: 0,
            factionID: new Il2CppNullableFaction(),
            replacementClassID: new Il2CppNullableInt(),
            corruption: new Il2CppNullableInt(),
            requiredSubTypeAffixEffectiveness: new Il2CppNullableAffixEffectiveness(),
            specialAffix: new Il2CppNullableSpecialAffix(),
            minSpecialAffixTier: new Il2CppNullableInt(),
            overrideLegendaryPotential: new Il2CppNullableInt());

        if (item == null || !ItemContainersManager.Instance.attemptToPickupItem(item, actor.position()))
            Melon<Mod>.Logger.Warning("Couldn't add the Harbinger reward item to the inventory.");
    }

    private static int UnlockWaypoints(Actor actor, Il2CppLE.Data.CharacterData data)
    {
        // Unfiltered apart from menus, like the developers' own unlock-all.
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

[HarmonyPatch(typeof(Notifications), nameof(Notifications.TimelineUnlockedNotification))]
internal static class TimelineUnlockedNotificationPatch
{
    private static bool Prefix() => !Campaign.Silent;
}

[HarmonyPatch(typeof(QuestSoundPlayer), nameof(QuestSoundPlayer.playQuestSound))]
internal static class QuestSoundPatch
{
    private static bool Prefix() => !Campaign.Silent;
}
