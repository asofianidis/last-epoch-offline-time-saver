using System;
using Il2Cpp;
using Il2CppEHG.Multiplayer;
using MelonLoader;
using UnityEngine;

namespace OfflineTimeSaver;

/// <summary>Settings panel, toggled with F3. Edits the same preferences as MelonPreferences.cfg.</summary>
internal static class Menu
{
    private const float Width = 440f, RowHeight = 26f, Padding = 14f, DesignHeight = 1080f;
    private const float SliderMax = 20f;

    private static readonly Rect Panel = new(40f, 120f, Width, 678f);

    private static bool _visible, _pointerOver, _dirty, _inputBlocked, _drawErrorLogged;

    internal static void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3) && !IsTyping())
        {
            _visible = !_visible;
            if (!_visible) Close();
        }

        BlockGameInput(_visible && (_pointerOver || GUIUtility.hotControl != 0));

        // Wait for the mouse release so dragging a slider doesn't write the file every frame.
        if (_dirty && GUIUtility.hotControl == 0) Save();
    }

    internal static void Draw()
    {
        if (!_visible) return;
        try
        {
            float scale = Math.Max(Screen.height / DesignHeight, 1f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _pointerOver = Panel.Contains(Event.current.mousePosition);
            DrawPanel();
        }
        catch (Exception ex)
        {
            if (_drawErrorLogged) return;
            _drawErrorLogged = true;
            Melon<Mod>.Logger.Error($"Menu failed to draw: {ex.GetBaseException().Message}");
        }
    }

    private static void DrawPanel()
    {
        var previous = GUI.color;
        GUI.color = new Color(0.05f, 0.06f, 0.08f, 0.96f);
        GUI.DrawTexture(Panel, Texture2D.whiteTexture);
        GUI.color = previous;

        float x = Panel.x + Padding, y = Panel.y + 10f, w = Width - Padding * 2f;

        GUI.Label(new Rect(x, y, w - 30f, RowHeight), "Offline Time Saver");
        if (GUI.Button(new Rect(x + w - 26f, y, 26f, 24f), "X"))
        {
            _visible = false;
            Close();
            return;
        }
        y += RowHeight;

        GUI.Label(new Rect(x, y, w, RowHeight), Status());
        y += RowHeight + 6f;

        Toggle(x, ref y, w, "Enable all gameplay changes", Settings.Enabled);
        y += 8f;

        Slider(x, ref y, w, "Character XP", Settings.XpEntry, 1f, SliderMax, 0.5f);
        Slider(x, ref y, w, "Skill XP", Settings.AbilityXpEntry, 1f, SliderMax, 0.5f);
        Slider(x, ref y, w, "Faction favor", Settings.FavorEntry, 1f, SliderMax, 0.5f);
        Slider(x, ref y, w, "Gold", Settings.GoldEntry, 1f, SliderMax, 0.5f);
        Slider(x, ref y, w, "Item drops", Settings.ItemQuantityEntry, 1f, Settings.ItemQuantityMax, 0.5f);
        Slider(x, ref y, w, "Unique / Set chance", Settings.UniqueSetEntry, 1f, SliderMax, 0.5f);
        Slider(x, ref y, w, "Ancient Bones", Settings.AncientBonesEntry, 1f, SliderMax, 0.5f);
        Slider(x, ref y, w, "Forging Potential", Settings.ForgingPotentialEntry, 1f, Settings.ForgingPotentialMax, 0.25f);
        Slider(x, ref y, w, "Movement speed", Settings.MovementSpeedEntry, 1f, Settings.MovementSpeedMax, 0.25f);
        Slider(x, ref y, w, "Monster density", Settings.DensityEntry, 1f, Settings.DensityMax, 0.25f);
        y += 8f;

        Toggle(x, ref y, w, "Auto-pickup gold", Settings.AutoGold);
        Toggle(x, ref y, w, "Auto-pickup potions", Settings.AutoPotions);
        Toggle(x, ref y, w, "Auto-pickup XP tomes", Settings.AutoXpTomes);
        Toggle(x, ref y, w, "Auto-pickup favor tomes", Settings.AutoFavorTomes);
        Toggle(x, ref y, w, "Auto-pickup ancient bones", Settings.AutoAncientBones);
        Toggle(x, ref y, w, "Auto-pickup materials", Settings.AutoMaterials);
        Toggle(x, ref y, w, "Send auto-picked materials to storage", Settings.StoreMaterials);
        Toggle(x, ref y, w, "Auto-pickup gear your loot filter shows", Settings.AutoFilteredItems);
        Toggle(x, ref y, w, "Free respecs", Settings.FreeRespecs);
        Toggle(x, ref y, w, "Offline-only login (next launch)", Settings.OfflineOnlyLogin);
        y += 8f;

        if (GUI.Button(new Rect(x, y, 150f, 26f), "Reset to defaults"))
        {
            Settings.ResetToDefaults();
            Changed();
        }
        GUI.Label(new Rect(x + 160f, y, w - 160f, RowHeight), "F3 closes  |  1x = normal");
    }

    private static string Status()
    {
        if (!Settings.Enabled.Value) return "Status: turned off";
        return GameplayEnvironment.IsOnlinePlay ? "Status: waiting for offline play" : "Status: active (offline)";
    }

    private static void Toggle(float x, ref float y, float w, string label, MelonPreferences_Entry<bool> entry)
    {
        bool value = GUI.Toggle(new Rect(x, y, w, RowHeight - 2f), entry.Value, " " + label);
        if (value != entry.Value)
        {
            entry.Value = value;
            Changed();
        }
        y += RowHeight;
    }

    private static void Slider(float x, ref float y, float w, string label, MelonPreferences_Entry<float> entry,
        float min, float max, float step, string suffix = "x")
    {
        GUI.Label(new Rect(x, y, 170f, RowHeight), $"{label}: {entry.Value:0.##}{suffix}");

        // A value above the slider's range (set in the file) stays as it is until the slider is moved.
        float shown = Math.Clamp(entry.Value, min, max);
        float moved = GUI.HorizontalSlider(new Rect(x + 175f, y + 7f, w - 175f, 16f), shown, min, max);
        float snapped = Mathf.Round(moved / step) * step;
        if (Math.Abs(snapped - shown) > 0.001f)
        {
            entry.Value = snapped;
            Changed();
        }
        y += RowHeight;
    }

    private static void Changed() => _dirty = true;

    private static void Close()
    {
        _pointerOver = false;
        BlockGameInput(false);
        if (_dirty) Save();
    }

    private static void Save()
    {
        _dirty = false;
        Settings.Save();
    }

    private static bool IsTyping()
    {
        try
        {
            var input = EpochInputManager.instance;
            return input != null && input.IsInputFieldActive;
        }
        catch { return false; }
    }

    // Without this, clicks on the panel also move the character. Re-applied while wanted
    // because a scene change creates a new input manager with the flag reset.
    private static void BlockGameInput(bool block)
    {
        try
        {
            var input = EpochInputManager.instance;
            if (input == null)
            {
                _inputBlocked = false;
                return;
            }
            if (block)
            {
                input.forceDisableInput = true;
                _inputBlocked = true;
            }
            else if (_inputBlocked)
            {
                input.forceDisableInput = false;
                _inputBlocked = false;
            }
        }
        catch { /* never let a menu problem interrupt the game */ }
    }
}
