using HarmonyLib;
using Il2Cpp;
using Il2CppClientAppState;
using Il2CppLE.UI.Login.UnityUI;
using MelonLoader;
using UnityEngine;

namespace OfflineTimeSaver;

/// <summary>
/// Presses "Play Offline" once the login screen has settled. Pressing it while the panel is
/// still enabling makes the game throw "Cannot transition to CharacterSelect while transition
/// to SystemLoading is in progress" and leaves an empty login screen.
/// </summary>
internal static class OfflineLogin
{
    private const float SettleSeconds = 1f, GiveUpSeconds = 30f;

    private static LandingZonePanel _panel;
    private static float _pressAt, _giveUpAt;

    internal static void Request(LandingZonePanel panel)
    {
        _panel = panel;
        _pressAt = Time.unscaledTime + SettleSeconds;
        _giveUpAt = Time.unscaledTime + GiveUpSeconds;
    }

    internal static void Update()
    {
        if (_panel == null) return;
        try
        {
            Advance();
        }
        catch (System.Exception ex)
        {
            // Stop trying: the normal login screen still works.
            _panel = null;
            Melon<Mod>.Logger.Warning($"Offline login skip stopped: {ex.GetBaseException().Message}");
        }
    }

    private static void Advance()
    {
        if (!_panel.isActiveAndEnabled)
        {
            _panel = null;
            return;
        }

        float now = Time.unscaledTime;
        if (now >= _giveUpAt)
        {
            _panel = null;
            Melon<Mod>.Logger.Warning("Login screen did not settle: leaving it for you to use normally.");
            return;
        }

        bool settled = ClientStateManager.CurrentClientAppStateType == ClientAppStateType.Login
            && _panel.playOfflineButton != null && _panel.playOfflineButton.interactable;
        if (!settled) _pressAt = now + SettleSeconds;
        if (now < _pressAt) return;

        var panel = _panel;
        _panel = null;
        Mod.FirstUse("SkipToOffline", "offline chosen automatically");
        panel.OnPlayOfflineClicked();
    }
}

[HarmonyPatch(typeof(LandingZonePanel), nameof(LandingZonePanel.OnOnEnable))]
internal static class SkipToOfflinePatch
{
    private static void Postfix(LandingZonePanel __instance)
    {
        if (Settings.OfflineOnlyLogin.Value) OfflineLogin.Request(__instance);
    }
}

[HarmonyPatch(typeof(LandingZonePanel), nameof(LandingZonePanel.OnPlayOnlineClicked))]
internal static class BlockOnlineButtonPatch
{
    private static bool Prefix() => !Settings.OfflineOnlyLogin.Value;
}

[HarmonyPatch(typeof(CharacterSelect), nameof(CharacterSelect.SwitchOnlineOffline))]
internal static class BlockOnlineSwitchPatch
{
    private static bool Prefix() => !Settings.OfflineOnlyLogin.Value;
}
