using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace OfflineTimeSaver;

/// <summary>
/// Adds to the game's own Movespeed stat, so the character sheet, animation and movement stay in
/// step. Stats are rebuilt per character and never saved, so this leaves nothing in the save file.
/// </summary>
internal static class MovementSpeed
{
    private const float CheckSeconds = 0.5f;

    private static IntPtr _stats;
    private static float _applied, _nextCheck;
    private static bool _logResult, _failed;

    internal static void Update()
    {
        if (_failed || Time.unscaledTime < _nextCheck) return;
        _nextCheck = Time.unscaledTime + CheckSeconds;

        try
        {
            Apply();
        }
        catch (Exception ex)
        {
            _failed = true;
            Melon<Mod>.Logger.Warning($"Movement speed stopped: {ex.GetBaseException().Message}");
        }
    }

    private static void Apply()
    {
        var player = PlayerFinder.getPlayerActor();
        var stats = player == null ? null : player.stats;
        if (stats == null)
        {
            _stats = IntPtr.Zero;
            _applied = 0f;
            return;
        }

        // A new stats object means a freshly built character without our bonus.
        if (stats.Pointer != _stats)
        {
            _stats = stats.Pointer;
            _applied = 0f;
        }

        if (_logResult)
        {
            _logResult = false;
            Mod.FirstUse("MovementSpeed", $"agent speed now {player.navMeshAgent.speed:0.00}");
        }

        float wanted = Mod.Active ? Settings.MovementSpeed - 1f : 0f;
        if (Math.Abs(wanted - _applied) < 0.001f) return;

        Melon<Mod>.Logger.Msg($"Movement speed bonus {_applied:P0} -> {wanted:P0} (agent speed {player.navMeshAgent.speed:0.00})");
        stats.ChangeStatModifier(SP.Movespeed, wanted - _applied, BaseStats.ModType.INCREASED, AT.None, 0, 0, false);
        stats.UpdateStats();
        _applied = wanted;
        _logResult = true;
    }
}
