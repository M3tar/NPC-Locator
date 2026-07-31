# 联机 NPC 定位器

“联机 NPC 定位器”是 Mercury 为《星露谷物语》制作的 SMAPI Mod。单人玩家、联机主机和农场助手可以查询村民的实时位置、主机端今日标准日程，并追踪一名 NPC 或标准物品交付任务的目标。

本 Mod 独立于 Lookup Anything，不改变 NPC 行为，也不会将追踪状态写入存档。

## 运行要求

- Stardew Valley 1.6.15 或更新的 1.6.x 版本；
- SMAPI 4.5.2 或更高版本；
- 0.1.0 私人内测版已测试平台为 Windows；
- Generic Mod Config Menu（GMCM）为可选依赖。

农场助手如需查询远端地图中的 NPC，主机与该农场助手必须安装同一个兼容版本。单人玩家和主机会直接读取本地权威世界数据。

## 安装

1. 安装 SMAPI。
2. 将发布包解压到游戏的 `Mods` 文件夹。
3. 确认路径为 `Mods/MultiplayerNpcLocator/manifest.json`。
4. 通过 SMAPI 启动游戏。

更新时，只需用新包中的 `MultiplayerNpcLocator` 文件夹替换旧文件夹。

## 使用

- 按 `F3` 打开或关闭定位器。
- **NPC 查询**：可按内部名或本地化名搜索，查看实时位置、今日标准日程，手动刷新或开始手动追踪。
- **送货任务**：查看当前活动的标准物品交付任务，并追踪任务目标。手动改追其他 NPC 后，仍可返回此分页重新建立任务关联。
- F3 打开时，追踪状态会在窗口内显示；关闭后，紧凑追踪栏会回到配置的屏幕角落。
- 实时地点和下一站均显示格子坐标。玩家与 NPC 位于同一张地图时，还会显示文字方向和大致距离。

追踪仅在当前游戏会话中保留；返回标题后清除，不写入游戏存档。

## 配置

安装 GMCM 后，可在游戏内设置：

- 打开定位器的快捷键；
- 是否识别送货任务；
- 追踪栏显示、位置、透明度、下一站、方向和距离；
- 主机远程查询、实时位置、日程共享、通知与请求频率上限。

未安装 GMCM 时，先启动一次游戏，然后编辑 Mod 文件夹内自动生成的 `config.json`。SMAPI 无法可靠检测所有快捷键冲突；如其他 Mod 也使用 F3，请修改其中一个绑定。

## 联机与隐私

- NPC 位置和日程以主机为权威数据源。
- 农场助手只会发送所选 NPC 的稳定内部名和查询选项。
- 送货任务只在每名玩家的本地任务日志中读取，绝不会发送给主机。
- Mod 不接受客户端任意代码或文件路径，不传送玩家，不改写 NPC 日程，也不修改任务。

## 已知限制

详见[已知限制](docs/KNOWN_LIMITATIONS.md)。重要边界包括：事件控制的 NPC、不可用的标准日程、姜岛进度前尚未出现的 Leo，以及 0.1.0 仅支持标准物品交付任务。

## Windows 源码构建

在源码目录执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Windows.ps1 -Install -UpdateExisting -Package
```

如游戏不在 Steam 默认库，添加 `-GamePath 'D:\SteamLibrary\steamapps\common\Stardew Valley'`。`-Package` 会在 `dist` 中生成可安装压缩包。

## 文档

- [English README](README.md)
- [更新日志](CHANGELOG.md)
- [已知限制](docs/KNOWN_LIMITATIONS.md)
- [最终验收清单](docs/PHASE5_VALIDATION.md)
- [开发计划](docs/DEVELOPMENT_PLAN.md)
