using System;
using System.Collections.Generic;
using HarmonyLib;
using Il2CppEHG.Multiplayer;
using MelonLoader;
using OfflineTimeSaver;

[assembly: MelonInfo(typeof(Mod), "Offline Time Saver", "1.0.0", "asofianidis")]
[assembly: MelonGame("Eleventh Hour Games", "Last Epoch")]
[assembly: HarmonyDontPatchAll]

namespace OfflineTimeSaver;

public sealed class Mod : MelonMod
{
    private static readonly HashSet<string> Fired = new();
    private static bool? _lastOnline;

    /// <summary>True only when the game itself reports offline play. Anything unexpected means off.</summary>
    internal static bool Active
    {
        get
        {
            try { return Settings.Enabled.Value && !GameplayEnvironment.IsOnlinePlay; }
            catch { return false; }
        }
    }

    /// <summary>Logs a hook's first use, which shows from the log which features really run in game.</summary>
    internal static void FirstUse(string hook, string detail)
    {
        if (Fired.Add(hook)) Melon<Mod>.Logger.Msg($"[fired] {hook}: {detail}");
    }

    public override void OnInitializeMelon()
    {
        Settings.Load();

        int ok = 0, failed = 0;
        foreach (var type in typeof(Mod).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
            try
            {
                HarmonyInstance.CreateClassProcessor(type).Patch();
                LoggerInstance.Msg($"[patched] {type.Name}");
                ok++;
            }
            catch (Exception ex)
            {
                // One broken hook (e.g. after a game update) only loses that feature.
                LoggerInstance.Warning($"[FAILED] {type.Name}: {ex.GetBaseException().Message}");
                failed++;
            }
        }
        LoggerInstance.Msg($"Ready: {ok} patches applied, {failed} failed.");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        try
        {
            bool online = GameplayEnvironment.IsOnlinePlay;
            if (_lastOnline != online)
            {
                _lastOnline = online;
                LoggerInstance.Msg(online ? "Online play detected: all gameplay changes are OFF." : "Offline play: gameplay changes are ON.");
            }
        }
        catch (Exception ex)
        {
            LoggerInstance.Warning($"Scene setup failed: {ex.GetBaseException().Message}");
        }
    }

    public override void OnUpdate()
    {
        Menu.Update();
        OfflineLogin.Update();
        MovementSpeed.Update();
    }

    public override void OnGUI() => Menu.Draw();
}
