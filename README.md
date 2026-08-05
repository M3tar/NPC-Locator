# NPC Locator

NPC Locator is a SMAPI mod by Mercury for Stardew Valley. It lets solo players, multiplayer hosts, and farmhands search for villagers, view their current location and standard daily schedule, and track one NPC or a standard item-delivery quest target.

The mod began as a solution for farmhands who couldn't reliably look up NPCs in multiplayer, then grew into a complete locator and tracker for solo players, hosts, and farmhands alike.

The mod is independent from Lookup Anything, doesn't change NPC behavior, and doesn't write tracking data to the save.

## Requirements

- Stardew Valley 1.6.15 or later in the 1.6 line;
- SMAPI 4.5.2 or later;
- Windows is the tested platform for the 0.1.0 private beta;
- Generic Mod Config Menu is optional.

For a farmhand to query NPCs on remote locations, both the host and that farmhand must install the same compatible version. Solo players and hosts query their own local world directly.

## Installation

1. Install SMAPI.
2. Extract the release archive into the game's `Mods` folder.
3. Confirm the resulting path is `Mods/NpcLocator/manifest.json`.
4. Launch the game through SMAPI.

To update, replace only the existing `NpcLocator` folder with the folder from the new archive. If you installed a pre-release build named `MultiplayerNpcLocator`, optionally back up its `config.json`, delete that old folder, then install the renamed build so SMAPI doesn't load both UniqueIDs. You can copy the backed-up config into `NpcLocator` afterward.

## Use

- Press `F3` to open or close the locator.
- **NPCs**: search by internal or localized display name, inspect the current location and today's standard schedule, refresh, or start manual tracking. In Chinese, localized NPC names are ordered by pinyin and English fallback names follow them.
- **Delivery quests**: inspect active standard item-delivery quests and track their target. You can return to this tab later to reattach task tracking.
- While F3 is open, current tracking status appears inside the menu. After it closes, the compact tracker returns to the configured screen corner.
- Hover the tracker's top-right `×` and click it to stop the current tracking target without reopening F3.
- Current and next-stop locations use aligned label, location, and coordinate columns. Direction and approximate distance appear on their own row when the player and NPC are on the same map; exceptionally long custom locations show their full name on hover.

Tracking is session-only. It is cleared when returning to the title screen and isn't written to the game save.

## Configuration

With GMCM installed, the in-game settings include:

- the menu key;
- delivery quest detection;
- tracker visibility, position, opacity, next stop, direction, and distance;
- host controls for remote queries, current locations, daily schedules, notifications, and request rate limits.

Without GMCM, launch the game once and edit `config.json` in the mod folder. Key conflicts can't be detected reliably; change the default `F3` binding if another mod also uses it.

## Multiplayer and privacy

- The host is authoritative for NPC locations and schedules.
- A farmhand sends only the selected NPC's stable internal name and query options.
- Delivery quests are read exclusively from each local player's own quest log and are never sent to the host.
- The mod doesn't execute client-provided code, read arbitrary files, teleport players, alter schedules, or modify quests.

## Known limitations

See [Known limitations](docs/KNOWN_LIMITATIONS.md). Notable boundaries include event-controlled NPCs, unavailable standard schedules, locked characters such as Leo before Ginger Island progression, and support limited to standard item-delivery quests in 0.1.0.

## Building on Windows

From the source directory:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Windows.ps1 -Install -UpdateExisting -Package
```

Use `-GamePath 'D:\SteamLibrary\steamapps\common\Stardew Valley'` if the game isn't in the default Steam library. `-Package` creates the installable archive under `dist`.

## Documentation

- [Chinese README](README.zh-CN.md)
- [Changelog](CHANGELOG.md)
- [Known limitations](docs/KNOWN_LIMITATIONS.md)
- [Final validation checklist](docs/PHASE5_VALIDATION.md)
- [Development plan](docs/DEVELOPMENT_PLAN.md)
