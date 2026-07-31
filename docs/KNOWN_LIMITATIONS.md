# Known limitations / 已知限制

## English

- Remote farmhand queries require the host and querying farmhand to install a compatible version of this mod. A client-only installation can't recover world data the game didn't synchronize.
- Festivals, cutscenes, weddings, scripted events, or other mods can temporarily remove or control an NPC. The locator reports unavailable current state or schedule instead of predicting it.
- Locked characters may appear in the complete NPC list before their world instance exists. For example, Leo can be listed before Ginger Island progression but remains temporarily unavailable.
- Version 0.1.0 automatically recognizes only standard `ItemDeliveryQuest` entries. Special orders, multi-target quests, and custom quest types aren't inferred.
- The first release officially targets vanilla NPCs. Custom NPCs and maps may fall back to internal names or have no standard schedule.
- Only one NPC can be tracked at a time. Tracking is session-only and is cleared on return to title or reload.
- Tile coordinates are map-local. A next-stop coordinate is the schedule target, while the current coordinate is the NPC's live tile; neither is a route or arrival guarantee.
- Same-map direction and distance are approximate straight-line guidance, not pathfinding. The mod doesn't account for walls, doors, warps, or inaccessible areas.
- SMAPI can't reliably detect every key conflict. Change the default F3 binding through GMCM or `config.json` when needed.
- The 0.1.0 private beta is validated primarily on Windows, Stardew Valley 1.6.15, and SMAPI 4.5.2. Newer game or SMAPI versions need regression testing.

## 简体中文

- 农场助手进行远程查询时，主机与查询者必须安装兼容版本。只在客户端安装无法恢复游戏未同步的世界数据。
- 节日、过场、婚礼、脚本事件或其他 Mod 可能暂时移除或控制 NPC。定位器会如实显示实时状态/日程不可用，不会伪造预测。
- 未解锁角色可能在完整 NPC 名单中提前出现。例如姜岛进度前的 Leo 可被列出，但会显示暂时无法定位。
- 0.1.0 只自动识别标准 `ItemDeliveryQuest`。特殊订单、多目标任务和自定义任务类型不会被推断。
- 首版正式目标为原版 NPC。自定义 NPC/地图可能回退到英文内部名，或没有标准日程。
- 同时只追踪一名 NPC。追踪仅在当前会话中保留，返回标题或重新加载后清除。
- 格子坐标仅对当前地图有意义。“下一站”坐标是日程目标，“当前位置”坐标是 NPC 实时格子；两者都不是路线或到达保证。
- 同地图方向与距离是直线估算，不是寻路，不考虑墙壁、门、传送点或不可到达区域。
- SMAPI 无法可靠检测所有快捷键冲突。如有需要，通过 GMCM 或 `config.json` 修改默认 F3。
- 0.1.0 私人内测主要验证环境为 Windows、Stardew Valley 1.6.15 和 SMAPI 4.5.2。更新的游戏或 SMAPI 版本需要回归测试。
