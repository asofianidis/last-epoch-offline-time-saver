# Offline Time Saver

A MelonLoader mod for Last Epoch that cuts the grind in offline play: more XP, gold and loot, plus auto-pickup. Built for Season 5 (Unity 6000.4.8, MelonLoader 0.7.3).

All gameplay changes switch off if the game reports online play. Modifying the client is against Last Epoch's terms, so keep it offline.

## Settings

Press **F3** in game to open the settings menu. Changes apply immediately and are saved automatically; only the offline-only login setting waits until the next launch.

The Character XP and Skill XP sliders go up to 1000x, which is also their limit in the file. The others go up to 20x, or lower for settings with a smaller limit. For higher values, close the game and edit the `[OfflineTimeSaver]` section of:

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
| `UnlimitedHealth` | `false` | Health stays full and you cannot die. On-hit effects still trigger |
| `UnlimitedMana` | `false` | Mana stays full and abilities can always be cast. On-spend effects still trigger |

A multiplier of `1` means normal.

The menu also has these buttons, available only in offline play. They can't be undone:

- **Max character level** levels the character up to 100, granting the usual points and unlocks.
- **Max all skill levels** raises every specialised skill to level 20. Skills that aren't specialised have no level, so specialise first.
- **Finish campaign, go to End of Time** completes the main story and every quest that gives passive points or idol slots, granting their normal rewards, then unlocks every waypoint, empowers all Monolith timelines, completes on this character each timeline already completed on this stash (with its completion quest, but no blessings), joins the Forgotten Knights with the Harbinger reward item, and travels to the End of Time. It takes a second click to confirm and can be used again on a character that has already had it. Choose your mastery in the passive tree afterwards. Other side quests, faction intros and the Monolith tutorial stay open, and doors or events in old zones that are triggered by playing through them are not changed.
- **Timeline selector, +50 corruption and Fill stability** work on one of the character's Monolith timelines, picked with the `<` and `>` arrows (normal and empowered are separate). Corruption goes up by 50 per click, up to the timeline's limit if it has one, and stability fills to its maximum. They only work in the End of Time, so a timeline is never changed during one of its echoes, and only on timelines already started in the Monolith.

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
