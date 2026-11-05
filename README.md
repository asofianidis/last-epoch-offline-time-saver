# Offline Time Saver

A MelonLoader mod for Last Epoch that cuts the grind in offline play: more XP, gold and loot, plus auto-pickup. Built for Season 5 (Unity 6000.4.8, MelonLoader 0.7.3).

All gameplay changes switch off if the game reports online play. Modifying the client is against Last Epoch's terms, so keep it offline.

## Settings

Press **F3** in game to open the settings menu. Changes apply immediately and are saved automatically; only the offline-only login setting waits until the next launch.

The Character XP and Skill XP sliders go up to 100x. The others go up to 20x, or lower for settings with a smaller limit. For higher values, close the game and edit the `[OfflineTimeSaver]` section of:

`F:\SteamLibrary\steamapps\common\Last Epoch\UserData\MelonPreferences.cfg`

| Setting | Default | What it does |
| --- | --- | --- |
| `Enabled` | `true` | Master switch for gameplay changes |
| `OfflineOnlyLogin` | `true` | Skips the login screen to offline play and disables the online buttons |
| `ExperienceMultiplier` | `3` | Character XP |
| `AbilityExperienceMultiplier` | `3` | Skill XP |
| `FavorMultiplier` | `3` | Faction favor earned from XP |
| `GoldMultiplier` | `3` | Gold per pile |
| `ItemQuantityMultiplier` | `2` | Item drops (max 20; high values can lag big fights) |
| `UniqueAndSetRarityMultiplier` | `3` | Chance a drop is a Unique or Set |
| `AutoPickupGold` / `Potions` / `XpTomes` / `FavorTomes` / `AncientBones` | `true` | Collects these automatically (potions only when not full) |
| `MovementSpeedMultiplier` | `1` | Movement speed (max 5), through the game's Movespeed stat. Off by default until tested in play |
| `MonsterDensityMultiplier` | `1` | Monsters per normal pack (max 5), from the next zone load. Bosses and single spawns are unchanged |
| `AncientBonesMultiplier` | `3` | Ancient Bones per drop |
| `ForgingPotentialMultiplier` | `2` | Forging Potential on new items (max 10, item limit 63). Shops and gamblers are unchanged |
| `AutoPickupMaterials` | `true` | Picks up shards, runes and glyphs when they drop |
| `SendMaterialsToStorage` | `true` | Sends auto-picked materials straight to material storage |
| `AutoPickupFilteredItems` | `true` | Picks up gear matched by a visible rule in your active loot filter. Does nothing without a filter |
| `FreeRespecs` | `true` | Respeccing passives, masteries, the Weaver tree and blessings costs nothing. Skill trees are already free |
| `KeylessDungeons` | `true` | Enter dungeons without a key. A key left in the slot is still used up |

A multiplier of `1` means normal.

## Checking that it works

Each feature logs one `[fired]` line the first time it runs, and patches that could not be applied show as `[FAILED]`. See:

`F:\SteamLibrary\steamapps\common\Last Epoch\MelonLoader\Latest.log`

## Building

Requires the .NET SDK and a game that has run once with MelonLoader installed, which generates the assemblies the mod compiles against.

```
dotnet build -c Release
```

This copies the DLL into the game's `Mods` folder. If the game is installed elsewhere, add `-p:GameDir="<path>"`.

## After a game update

MelonLoader regenerates its assemblies on the next launch. Check the log for `[FAILED]`, then rebuild. A hook that no longer matches disables only that feature.

## Uninstalling

- **Mod only:** delete `Mods\OfflineTimeSaver.dll` from the game folder.
- **Everything:** also delete `version.dll`, `MelonLoader\`, `Mods\` and `UserData\` from the game folder, then verify the game files in Steam.

Save backups from before the mod was installed are in `save-backups\pre-mod-2026-11-04\`.
