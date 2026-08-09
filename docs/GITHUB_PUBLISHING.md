# GitHub publishing copy

Reusable copy for the repository sidebar and the `v0.1.0` GitHub Release. Update the version-specific details before publishing a later release.

## Repository About

### Description

Find and track Stardew Valley NPC locations, daily schedules, and delivery quest targets in solo and multiplayer games.

### Suggested topics

`stardew-valley` `smapi` `stardew-valley-mod` `multiplayer` `npc-locator` `npc-tracker` `csharp`

## GitHub Release — English

### Title

NPC Locator 0.1.0

### Body

NPC Locator helps solo players, multiplayer hosts, and farmhands find villagers without guessing where they went. Search for an NPC, see their current location and standard schedule for today, or keep one target visible in a compact tracker while you play.

### Highlights

- Search NPCs by internal or localized display name.
- View current locations, tile coordinates, and standard daily schedules.
- Track one NPC with their next stop and same-map direction and approximate distance.
- Browse active standard item-delivery quests and track their targets.
- Query remote multiplayer locations using host-authoritative results.
- Configure the menu, tracker, and host sharing controls through optional GMCM support.
- Use the mod in English or Simplified Chinese, including pinyin sorting for Chinese NPC names.

### Install

1. Install SMAPI 4.5.2 or later.
2. Download and extract `NpcLocator-0.1.0.zip` into Stardew Valley's `Mods` folder.
3. Confirm the path is `Mods/NpcLocator/manifest.json`.
4. Launch the game through SMAPI and press `F3`.

Farmhands need the host to install the same compatible version before the mod can retrieve NPC data from remote maps.

If you used a pre-release folder named `MultiplayerNpcLocator`, back up its `config.json` if needed and remove that old folder before installing this release. Don't keep both versions installed.

### Current limitations

- The tested environment is Windows, Stardew Valley 1.6.15, and SMAPI 4.5.2.
- Version 0.1.0 recognizes standard item-delivery quests only.
- Festivals, cutscenes, scripted events, and other mods can make an NPC or schedule temporarily unavailable.
- Custom NPCs and maps may fall back to internal names or lack a standard schedule.
- Direction and distance are straight-line estimates, not pathfinding.

Tracking lasts only for the current game session. NPC Locator doesn't teleport players, change NPC schedules or quests, or write tracking data to the save.

See the repository README and [complete known limitations](KNOWN_LIMITATIONS.md) before testing.

## GitHub Release — 简体中文

### 标题

NPC 定位器 0.1.0

### 正文

“NPC 定位器”可以帮助单人玩家、联机主机和非主机玩家快速找到村民。搜索 NPC 即可查看其实时地点和今日标准日程，也可以在游玩过程中通过紧凑追踪栏持续关注一个目标。

### 版本亮点

- 按内部名或本地化显示名搜索 NPC；
- 查看实时地点、格子坐标和今日标准日程；
- 追踪一名 NPC，并显示下一站、同地图方向和大致距离；
- 浏览活动中的标准物品交付任务并追踪任务目标；
- 通过主机权威数据查询联机远端地图中的 NPC；
- 通过可选 GMCM 支持配置菜单、追踪栏和主机共享权限；
- 提供英文和简体中文界面，中文人物名支持拼音排序。

### 安装

1. 安装 SMAPI 4.5.2 或更高版本；
2. 下载 `NpcLocator-0.1.0.zip` 并解压到《星露谷物语》的 `Mods` 文件夹；
3. 确认路径为 `Mods/NpcLocator/manifest.json`；
4. 通过 SMAPI 启动游戏，然后按 `F3`。

非主机玩家如需查询远端地图中的 NPC，主机必须安装同一个兼容版本。

如果使用过名为 `MultiplayerNpcLocator` 的更名前测试版，可先备份其中的 `config.json`，再删除旧文件夹并安装此版本。请勿同时保留两个版本。

### 当前限制

- 已验证环境为 Windows、Stardew Valley 1.6.15 和 SMAPI 4.5.2；
- 0.1.0 仅识别标准物品交付任务；
- 节日、过场、脚本事件或其他 Mod 可能使 NPC 或日程暂时不可用；
- 自定义 NPC 和地图可能回退到内部名，或没有标准日程；
- 方向和距离为直线估算，不是寻路。

追踪仅在当前游戏会话中保留。“NPC 定位器”不会传送玩家、改写 NPC 日程或任务，也不会把追踪数据写入存档。

测试前请阅读仓库 README 和[完整已知限制](KNOWN_LIMITATIONS.md)。
