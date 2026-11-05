using System;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace OfflineTimeSaver;

/// <summary>
/// Hits and mana costs still go through the game as normal, so on-hit and on-spend effects keep
/// working. The player is refilled afterwards and any hit that would be lethal is made survivable.
/// </summary>
internal static class UnlimitedResources
{
    private static BaseHealth _protected;
    private static IntPtr _health, _mana, _dying;
    private static bool _failed;

    internal static void Update()
    {
        if (_failed) return;
        try
        {
            bool health = Settings.UnlimitedHealth.Value, mana = Settings.UnlimitedMana.Value;
            var player = (health || mana) && Mod.Active ? PlayerFinder.getPlayerActor() : null;
            UpdateHealth(health ? player : null);
            UpdateMana(mana ? player : null);
        }
        catch (Exception ex)
        {
            _failed = true;
            _health = _mana = _dying = IntPtr.Zero;
            Melon<Mod>.Logger.Warning($"Unlimited health and mana stopped: {ex.GetBaseException().Message}");
        }
    }

    private static void UpdateHealth(Actor player)
    {
        var health = player == null ? null : player.health;
        IntPtr pointer = health == null ? IntPtr.Zero : health.Pointer;

        if (pointer != _health)
        {
            // The game counts these, and uses the same counter during scene transitions.
            if (_protected != null) _protected.RemoveSourceOfCannotDie();
            if (health != null) health.AddSourceOfCannotDie();
            _protected = health;
            _health = pointer;
            _dying = player == null || player.dying == null ? IntPtr.Zero : player.dying.Pointer;
        }

        // Turned on from the death screen, the respawn should still run as normal.
        if (health != null && !player.isDying() && health.currentHealth < health.maxHealth)
        {
            health.currentHealth = health.maxHealth;
        }
    }

    private static void UpdateMana(Actor player)
    {
        var mana = player == null ? null : player.mana;
        _mana = mana == null ? IntPtr.Zero : mana.Pointer;

        // Overcharge can put mana above the maximum, and that is left alone.
        if (mana != null && mana.currentMana < mana.maxMana) mana.currentMana = mana.maxMana;
    }

    internal static bool IsPlayerDying(Dying dying) => _dying != IntPtr.Zero && dying.Pointer == _dying;

    internal static bool IsPlayerMana(BaseMana mana) => _mana != IntPtr.Zero && mana.Pointer == _mana;

    internal static bool IsPlayerHealth(BaseHealth health) => _health != IntPtr.Zero && health.Pointer == _health;

    /// <summary>Leaves enough health for this hit to land without being lethal. The next frame refills it.</summary>
    internal static void SurviveHit(BaseHealth health, float damage)
    {
        if (health.currentHealth > damage) return;
        health.currentHealth = damage + 1f;
        Mod.FirstUse("UnlimitedHealth", $"survived a hit of {damage:0}");
    }
}

[HarmonyPatch(typeof(BaseHealth), nameof(BaseHealth.HealthDamage))]
internal static class HealthDamagePatch
{
    private static void Prefix(BaseHealth __instance, float damage)
    {
        if (UnlimitedResources.IsPlayerHealth(__instance)) UnlimitedResources.SurviveHit(__instance, damage);
    }
}

// Cull damage also kills once health falls under a threshold, however much is left.
[HarmonyPatch(typeof(BaseHealth), nameof(BaseHealth.HealthDamageCull))]
internal static class HealthDamageCullPatch
{
    private static void Prefix(BaseHealth __instance, float damage, ref float cullPercent, ref float flatCull)
    {
        if (!UnlimitedResources.IsPlayerHealth(__instance)) return;
        UnlimitedResources.SurviveHit(__instance, damage);
        cullPercent = 0f;
        flatCull = 0f;
    }
}

// A few effects and scripted events kill directly, without dealing any damage.
[HarmonyPatch(typeof(Dying), nameof(Dying.die))]
internal static class PlayerDiePatch
{
    private static bool Prefix(Dying __instance) => !UnlimitedResources.IsPlayerDying(__instance);
}

// Lets the player cast abilities that cost more than their maximum mana.
[HarmonyPatch(typeof(BaseMana), nameof(BaseMana.hasEnoughManaToUse))]
internal static class EnoughManaPatch
{
    private static void Postfix(BaseMana __instance, ref bool __result)
    {
        if (!__result && UnlimitedResources.IsPlayerMana(__instance)) __result = true;
    }
}
