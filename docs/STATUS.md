# 项目状态

> 最后更新：2026-07-31 18:25 CST

## 当前阶段

阶段 2：正式查询 UI（首个可测试版本已编码，等待 Windows 编译与界面验证）。阶段 1 的主机—客户端查询闭环已通过。

## 已完成

- 收集并对照主机、联机加入者 SMAPI 日志。
- 确认双方游戏版本、SMAPI 版本与主要 Mod 环境。
- 确认独立主机—客户端查询方案。
- 完成功能边界、技术架构、通信协议草案。
- 完成开发阶段、测试矩阵与验收标准。
- 确认项目名、作者名、唯一 ID 和默认产品决策。
- 核对项目目录与 handoff 一致；当前目录不是 Git 仓库，因此没有分支或源提交可核对。
- 建立 `net6.0` 最小 SMAPI C# 工程、0.1.0 manifest 和 Mod 入口。
- 引入 `Pathoschild.Stardew.ModBuildConfig` 4.4.0；关闭自动部署，保留构建 zip。
- 加入只读控制台验证命令 `mnl_validate [NPC internal name]`，用于记录：
  - NPC 查找、实时地点内部名/显示名与格子坐标；
  - `NPC.Schedule` 的实际成员类型、运行时类型与条目；
  - 当前任务日志中 `ItemDeliveryQuest` 的实际可读字段。
- 探针不修改 NPC、任务或存档。
- 添加 `scripts/Build-Windows.ps1`：在 Windows 上检测 .NET 6、游戏与 SMAPI 程序集，构建 Release，并可在目标目录不存在时安全安装验证版。
- 添加 `docs/WINDOWS_VALIDATION.md`：记录 Windows 构建、加载、NPC/日程/任务验证及结果回传步骤。
- Windows 1.6.15 实际编译确认 `Game1.player.questLog` 的类型是 `Netcode.NetObjectList<Quest>`，不是 `List<Quest>`；探针已改为保留实际集合类型。
- 首次 Windows 构建已成功完成 NuGet 还原并进入 C# 编译；上述集合类型差异是当次唯一编译错误。
- .NET 6 SDK 会对 ModBuildConfig 4.4.0 分析器显示 `CS9057` 编译器版本警告；该警告不是当次失败原因，暂保留证据并等待构建通过后再评估是否需要更新构建 SDK。
- Windows 1.6.15 / SMAPI 4.5.2 已成功构建、安装并加载阶段 0 Mod；普通单人存档的验证命令完成且无本 Mod 异常。
- 普通日验证确认：
  - `Game1.getCharacterFromName` 可读取 Pam 与 Abigail，并返回内部名、本地化显示名、实时地点、地点显示名和格子坐标；
  - `NPC.Schedule` 是 `Dictionary<int, StardewValley.Pathfinding.SchedulePathDescription>`，Pam 有 4 个时间条目，Abigail 有 5 个时间条目；
  - 不存在 NPC 会返回空并安全记录，不抛异常。
- 当前任务日志中的剧情交付任务已确认属于 `ItemDeliveryQuest`：
  - “潘姆渴了”：`target=Pam`、`ItemId=(O)303`、`number=1`；
  - “作物研究”：`target=Demetrius`、`ItemId=(O)254`、`number=1`；
  - 两项任务在日志中存在且未完成，但 `accepted=False`，因此生产逻辑不得用 `accepted` 判断任务是否活动；应以任务日志成员关系及完成/销毁状态为主。
- 第二轮 Windows 验证确认 `SchedulePathDescription` 的稳定可读字段：`time`、`targetLocationName`、`targetTile`、`facingDirection`、`endOfRouteBehavior`、`endOfRouteMessage` 与 `route`。
- Pam 的普通日日程已验证为 Trailer 08:00 → JojaMart 12:00 → Saloon 16:00 → Trailer 24:00；Abigail 的 5 个条目也返回了对应地点与坐标。
- Windows 构建脚本新增 `-UpdateExisting`，只在现有 manifest UniqueID 匹配时更新本 Mod 文件。
- 阶段 1 已加入：
  - 协议版本 1、`ProtocolHello`、`NpcQueryRequest`、`NpcQueryResponse` 与唯一请求 ID；
  - 主机端实时位置与结构化标准日程读取服务；
  - 按 Mod ID 与玩家 ID 定向发送、主机安装检测、来源主机校验；
  - 主机总开关、位置/日程共享开关、每玩家滚动限频；
  - 5 秒超时、过期响应忽略、协议不兼容、NPC 不存在、断线与返回标题清理；
  - 临时 `mnl_query <NPC internal name>` 控制台命令，用于单机与联机闭环验证。
- 新增 `docs/PHASE1_VALIDATION.md`，记录双方安装、单机冒烟与联机验证步骤。
- 阶段 1 双方实际联机验证通过：
  - 非主机为 Pam、Abigail 和不存在 NPC 发送了三个不同请求 ID，并收到同一请求 ID 的主机定向响应；
  - 非主机获得了主机端实时地点、中文地点显示名、格子坐标和完整标准日程；
  - 主机本地 Pam 结果与非主机收到的结果逐项一致；
  - 不存在 NPC 返回 `NpcNotFound`，没有超时、错配响应或未处理异常。
- 阶段 2 首个可测试 UI 已加入：
  - `OpenMenuKey` 使用 `KeybindList`，默认 `F3`；
  - 单人模式可打开本地测试，联机主机保持静默，非主机可打开查询窗口；
  - NPC 列表、搜索框、点击查询、实时位置、今日标准日程、加载与失败状态；
  - NPC 列表和日程滚轮浏览、Esc/F3/右上角关闭；
  - 默认英文与简体中文 i18n；
  - 菜单查询不输出普通请求日志，控制台命令仍保留详细验证输出。
- 新增 `docs/PHASE2_VALIDATION.md`，记录单人和联机界面测试步骤。
- Windows 安装脚本已扩展为同步 `i18n` 翻译文件，阶段 2 更新不会遗漏中英文文本。
- 阶段 2 首次 Windows 编译确认 1.6.15 的 `IClickableMenu.cleanupBeforeExit()` 是 `protected`；菜单重写已从错误的 `public override` 修正为 `protected override`。该访问级别差异是当次唯一编译错误。
- 修正访问级别后，阶段 2 已在 Windows 1.6.15 / SMAPI 4.5.2 成功构建并进入游戏。
- 实际截图确认：F3 窗口可打开，英文 `Abi` 搜索可过滤到阿比盖尔，中文标题与分区正常显示，实时位置和 5 条标准日程均成功渲染；当前截图未见裁切、重叠或缺失翻译。
- 非主机联机档截图确认 F3 菜单查询使用了主机权威结果，阶段 2 的核心联机界面闭环通过。
- 截图也暴露两个绘制问题：菜单调用 `drawBackground` 后用蓝色山景替换游戏世界，左右内容区的半透明黑色覆盖造成纸张底色深浅分块。已删除背景替换与两块覆盖层，保留原游戏场景和统一纸张底色；选中行/悬停高亮继续保留。
- 去除覆盖层后的复测确认原游戏场景已正确保留，但 `drawTextureBox` 拉伸的大尺寸中心纹理仍形成三段横向明暗色带。现保留原版边框，并在边框内绘制不透明的统一浅羊皮纸色 `RGB(255,248,220)`；查询、布局和联机逻辑未改动。
- 统一 RGB 填充复测显示为偏白灰色，不符合 Lookup Anything 的视觉。核对 Lookup Anything 官方源码后确认其默认 `Parchment` 主题使用游戏自带 `LooseSprites/letterBG` 的 `(0,0,320,180)` 区域，并按 16:9 等比绘制。阶段 2 窗口已改为同一游戏资源与比例，不复制 Lookup Anything 资源、不依赖其 DLL；窗口后方仍保留实时游戏场景。

## 下一步

继续阶段 2：正式查询 UI 验证。

1. 主机与非主机安装同一阶段 2 构建，在非主机用 F3 查询远端 NPC，确认菜单结果来自主机。
2. 补测中文搜索、列表/日程滚轮、Esc/F3/关闭按钮和另一种 UI 缩放或分辨率。
3. 羊皮纸背景复测通过后接入 GMCM，并改善 NPC 显示名、列表过滤和基础手柄操作。
4. 补测只有客户端安装、主机关闭共享、请求超时和 NPC 暂时不存在状态。
5. 后续继续补雨天、节日/事件、公告栏随机任务和无标准日程边界。

## 当前阻塞项

- 当前开发机器为 Apple Silicon macOS；`dotnet` 不在 PATH，常见位置未发现 .NET SDK。
- 当前开发机器的常见 Steam 与 Applications 路径未发现 Stardew Valley、SMAPI 或对应程序集，因此尚不能执行实际编译和加载验证。
- 阶段 2 尚未完成非主机 F3 远端查询、不同 UI 缩放和完整输入验证。

## 风险提醒

- 快捷键冲突不能被可靠自动检测。
- 节日、事件或脚本控制 NPC 时可能不存在标准日程。
- 1.6.16+ 兼容性需要单独测试，但首版不得依赖 1.6.16 独有 API。
