# 任务交接：NPC 定位器 0.1.0 正式发布

生成时间：2026-08-01 01:42:54 CST（Asia/Shanghai，UTC+08:00）
项目：NPC Locator（NPC 定位器；原名 Multiplayer NPC Locator / 联机 NPC 定位器）
任务标识：`multiplayer-npc-locator-planning`
状态：进行中（Nexus Mods 正式发布准备）
源分支：`main`
源提交基线：`7e6251ac2f31a68d49802a62cacf2f13253ba2ee`

## 任务概述

开发一个独立 SMAPI Mod，让单人玩家、联机主机和农场助手查询 NPC 实时位置与今日标准日程，并追踪一名 NPC 或标准物品交付任务目标。主机为联机 NPC 数据权威源；任务仅在当地玩家任务日志中读取。

完整范围与验收标准见 `docs/DEVELOPMENT_PLAN.md`，当前进度见 `docs/STATUS.md`。

## 成功标准

- 主机和农场助手使用同一个 0.1.0 二进制包，可完成查询、日程显示、手动追踪和任务关联追踪。
- 主机端默认静默响应农场助手，主机自己也可用 F3 本地查询。
- 不修改 NPC、任务或存档；删除 Mod 后原存档正常加载。
- Stardew Valley 1.6.15 / SMAPI 4.5.2 / Windows 双方最终日志无本 Mod 未处理错误。
- 交付可直接解压到 `Mods` 的 `NpcLocator-0.1.0.zip`。

## 当前状态

- 阶段 0–4 已完成；用户已在 Windows 单人档和联机档多轮测试查询 UI、主机/农场助手查询、地点本地化、GMCM、追踪、送货任务分页和重新关联，最新反馈为“没有什么问题”。
- 阶段 5 发布资料和构建脚本已提交：`9f7efc5 chore: prepare the 0.1.0 private beta candidate`。
- 原候选源码包 `dist/MultiplayerNpcLocator-0.1.0-rc1-source.zip` 已被产品重命名取代，不再用于阶段 5 测试。
- rc2 源码包已在 Windows 成功构建、安装并生成二进制包；其单人档冒烟未发现问题。
- rc2 二进制包 SHA-256：`131ab6fb236c5819647a944be5b28e3e03cc40b1eec5495f4f2ebf58b61613e9`。
- rc2 单人测试后新增了追踪栏快速停止按钮，因此 rc2 不再作为最终阶段 5 候选。
- rc3 源码包 SHA-256：`f3bd7deaca70c8290d5b7b2a3adb01bb5fe09c4aee9a28bf2b41e6f841e46d08`；单人测试发现关闭按钮悬停色偏白，以及搜索输入 `e` 会触发默认菜单键关闭窗口。
- rc4 已将悬停色统一为棕金色，并在搜索框选中时阻止文本按键继续传给基础菜单；后续单人测试发现长中文下一站会越出追踪栏。
- rc4 源码包 SHA-256：`2765be29ca1e0482ff275902b731e3eafa815338f335637d8a5b92236b06c2bb`。
- rc5 对追踪栏详情使用游戏字体的本地化换行，并根据实际换行数动态增加高度，保留完整时间、地点名和坐标。
- rc5 源码包 SHA-256：`fd95191c3c1ab84ad720fb96140bc7e610278cc6df37ce4350118e1ed52cd911`。
- rc6 在中文游戏语言下按本地化人物名拼音排序，英文回退项置后。
- rc6 源码包 SHA-256：`ead4fe3b03ae3aeb053464521540a1618129088c4dbfd0767963229557eb4c71`。
- rc7 按用户选定的方案 A 将追踪栏改为“标签或时间 / 地点 / 坐标”三列，方向和距离独立成行，极端长地点用省略号及悬停全文兜底；当前候选源码包为 `dist/NpcLocator-0.1.0-rc7-source.zip`。
- rc7 源码包 SHA-256：`a48a6d3d41207b6de9a557b757ca00de671c784cf90da0306ac458772fef13a0`。
- rc8 将追踪栏首选宽度缩至 540 像素，移除坐标底块和方向行专属分隔线，保留深棕坐标并贴齐右侧；当前候选源码包为 `dist/NpcLocator-0.1.0-rc8-source.zip`。
- rc8 源码包 SHA-256：`d2cc67c0d00afe63371e30b0f7bddbaf9fb551873545acbd2c584854bdb79f10`。
- rc9 将追踪栏背景改为与 F3 查询窗口相同的 `LooseSprites/letterBG`，按卡片比例居中裁切以消除 `drawTextureBox` 的固定横向明暗带；当前候选源码包为 `dist/NpcLocator-0.1.0-rc9-source.zip`。
- rc9 源码包 SHA-256：`87a5314e00a8d3f345dbd31b1a9e8eabc92204376af5d4143450385b678e0697`。
- rc10 根据实机反馈改用原版背包/菜单风格：保留原生菜单纹理的九宫格边框和阴影，中心仅取同一纹理的稳定暖色像素铺开，避免 rc8 横向色带和 rc9 信纸裁切感；当前候选源码包为 `dist/NpcLocator-0.1.0-rc10-source.zip`。
- rc10 源码包 SHA-256：`a22b44fee477ef4887a4bba3761151d62b8e7977aa23170a986617985f1ee8e4`。
- rc11 将 rc10 的原生菜单面板提取为追踪栏与右下角任务提示共用组件；任务提示保持不透明，追踪与忽略按钮改用有主次关系的棕金色阶；当前候选源码包为 `dist/NpcLocator-0.1.0-rc11-source.zip`。
- rc11 源码包 SHA-256：`3eec63c9cfc79f8587ccbde21b33b32646c9ebf0f119e70a9c20d437a46d7212`。
- `phase4f-source.zip` 与 rc1 的游戏功能代码基本相同；rc2 统一产品技术标识，rc3 加入追踪栏右上角 `×`，rc4 修复首轮反馈，rc5 修复长地点名溢出，rc6 加入中文拼音排序，rc7 采用三列追踪布局，rc8 统一信息行并收窄卡片，rc9 尝试信纸背景，rc10 改为原生菜单混合背景，rc11 统一任务提示。后续只使用 rc11。
- 当前 Mac 没有 `dotnet`、PowerShell、Stardew Valley 或 SMAPI 程序集；构建与实机验证已在 Windows 完成，后续 manifest 变更仍需回到 Windows 重新打包。
- rc11 已在 Windows 构建并完成用户实机验证，用户确认未发现问题；回传最终候选 ZIP 的 SHA-256 为 `6d4432eb2be58d3e778a007923510bbc7ec95228cdefb590ac02005699a05d8d`。
- 用户确认 0.1.0 作为正式 Release，不使用 Beta 状态。
- Nexus Mods 未发布草稿已创建：`https://www.nexusmods.com/stardewvalley/mods/50217`；基本资料已保存，尚未上传媒体或文件，也未公开发布。
- manifest 已加入 `Nexus:50217`，因此此前通过实机验证的 ZIP 不能作为最终上传包，需要从最新源码重新构建。

## 已完成工作

- 已实现协议版本、请求 ID、主机权限、超时、限频、过期响应丢弃、断线与返回标题清理。
- F3 羊皮纸窗口包含“NPC 查询/送货任务”分页、本地化搜索、实时位置、坐标、标准日程、刷新和追踪控制。
- 同时追踪一名 NPC；角落 HUD 显示实时地点/坐标、下一站/日程坐标、同地图文字方向和大致距离。
- F3 打开时不重复绘制角落 HUD，而在窗口内即时显示手动/任务追踪来源和最新位置；重新打开 F3 会恢复对应 NPC 或任务分页。
- 只读当地 `ItemDeliveryQuest`，显示任务、目标、物品、需要/持有数和期限；提示去重，任务结束时只停止仍与该任务关联的追踪。
- GMCM 为可选依赖；已接入快捷键、任务识别、追踪栏、主机共享和限频设置。
- 已添加 `README.md`、`README.zh-CN.md`、`CHANGELOG.md`、`docs/KNOWN_LIMITATIONS.md` 和 `docs/PHASE5_VALIDATION.md`。
- `scripts/Build-Windows.ps1 -Package` 会拒绝 Debug，在独立临时目录组装最终包，生成 `dist/NpcLocator-0.1.0.zip` 并输出 SHA-256。
- 已在阶段 5 最终包测试前将产品改名为 `NPC Locator` / “NPC 定位器”；安装脚本发现旧 `Mods/MultiplayerNpcLocator` 时会停止并要求手动删除，避免两个 UniqueID 同时加载。
- 追踪栏右上角新增悬停 `×`：点击会停止当前手动或任务关联追踪并拦截输入；任务关联只解除，不修改任务。
- rc4 将 `×` 悬停背景改为当前界面的棕金配色，并修复英文搜索输入 `e` 时窗口被基础菜单关闭的问题。
- rc5 将追踪栏详情行改为按可用宽度自动换行，解决长本地化地点名越界。
- rc6 在中文游戏语言下使用固定 `zh-CN` 文化规则排列人物名；缺少中文显示名的英文回退项置后，同音时以内部分名稳定排序。
- rc7 采用三列结构化追踪栏，坐标独立右对齐，方向与距离单独成行，小视窗自动收窄，极端长地点悬停显示全文。
- rc8 统一追踪栏各行的羊皮纸背景，坐标不再使用独立底块，卡片首选宽度缩至 540 像素。
- rc9 使用与 F3 查询窗口一致的游戏原生羊皮纸纹理，并按追踪框比例居中裁切，避免旧背景固定的横向明暗带。
- rc10 使用原版菜单纹理的边框与阴影，并以同纹理中心色统一内层，保留背包式原生观感而不恢复横向色带。
- rc11 让送货任务提示复用同一原生菜单面板，按钮采用棕金主次色阶，不再使用白色悬停状态。

## 已确认决策

- 公开作者 March3tar；名称 `NPC Locator` / “NPC 定位器”；UniqueID 保持 `Mercury.NpcLocator`；既有 Git 作者 Mercury 不重写；首版 `0.1.0` 正式发布。
- 独立于 Lookup Anything，不引用、修改或硬依赖它。
- 单人、主机、农场助手均可 F3 主动查询；农场助手远程数据来自主机。
- 默认 F3，可通过 GMCM / `config.json` 改键；不尝试自动解决全局快捷键冲突。
- 同时只追踪一名 NPC；追踪不写存档，返回标题后清除。
- 任务内容只在当地读取，不发送给主机；首版只识别标准物品交付任务。
- UI 严格区分“实时位置”和“今日标准日程”；事件控制或无日程时不伪造预测。
- 未解锁姜岛时 `Leo` 仍保留在完整 NPC 名单中，但显示暂时无法定位；不过滤隐藏。
- F3 打开时使用窗口内追踪卡片，不同时在屏幕角落重复显示 HUD。
- 方向箭头不阻塞 0.1.0；首版保留文字方向与距离。

详细决策编号见 `docs/DECISIONS.md`。

## 重要发现

- Windows 1.6.15 / SMAPI 4.5.2 实测确认 `NPC.Schedule` 为 `Dictionary<int, StardewValley.Pathfinding.SchedulePathDescription>`，稳定字段已记录在 `docs/STATUS.md`。
- `Game1.player.questLog` 是 `Netcode.NetObjectList<Quest>`；`ItemDeliveryQuest.accepted` 在仍活动的剧情任务中可为 `False`，生产逻辑必须以任务日志成员关系及 `completed` / `destroy` 为主。
- `SebastianRoom` 类地图可缺少有效显示名；已使用客户端语言下的 NPC 本地化名 + 房间模板回退。
- Lookup Anything 羊皮纸视觉来自游戏自带 `LooseSprites/letterBG`；本 Mod 使用同一游戏资源，不复制资源、不依赖对方 DLL。
- .NET 6 SDK 可显示 ModBuildConfig 4.4.0 分析器 `CS9057` 警告；该警告不是已知构建失败原因。

## 已尝试但未采用的方案

- 曾尝试在列表中过滤当前存档无角色实例的 `Leo`；用户决定保留完整名单，该未提交修改已完整撤销。不要重新引入过滤。
- 早期 UI 使用 `drawBackground` 导致蓝色山景替换游戏背景，且半透明区块/拉伸九宫格导致纸张明暗不一；已改为游戏 `letterBG` 统一绘制，不要恢复旧方案。
- 曾考虑 F3 打开时仍在角落显示 HUD；用户接受“窗口内卡片，关闭后角落 HUD”方案，不要重复绘制。

## 当前修改和工作区状态

- 项目根目录：`repository root`
- 本 handoff 写入前，`main` 位于 `9f7efc5212e37de325a2c46c50bc45fc2f35d181`，工作区干净。
- 原 handoff 写入后只有本文件为未提交修改；2026-08-01 用户确认重命名后，项目、源码、脚本、翻译和文档均有待提交修改。
- `dist/` 被 `.gitignore` 忽略；候选源码 zip 只存在当前设备，不在 Git 提交中。
- 仓库没有配置 Git remote，所有 commit 仅在当前设备。

## 下一步

1. 将包含 `Nexus:50217` 的最新源码传到 Windows；若旧的 `Mods\MultiplayerNpcLocator` 测试目录仍存在，先手动删除。
2. 在源码目录运行：

   ```powershell
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Windows.ps1 -Install -UpdateExisting -Package
   ```

   若非默认 Steam 库，追加 `-GamePath 'D:\SteamLibrary\steamapps\common\Stardew Valley'`。
3. 核对 Release 编译成功，并生成新的 `dist\NpcLocator-0.1.0.zip` 与 SHA-256；确认包内 manifest 含 `Nexus:50217`。
4. 因功能代码未变，执行一次 SMAPI 启动冒烟，确认新包正常加载且无本 Mod 未处理错误。
5. 在 Nexus 草稿上传展示图和新的最终 ZIP；Requirements、Permissions 与 Donation Points 由用户确认，不能代为猜测。
6. 用户检查预览并明确确认后才点击 Publish；在此之前保持草稿未发布。

## 相关文件与资料

- `docs/STATUS.md`：最新进度、验证证据与阻塞。
- `docs/DEVELOPMENT_PLAN.md`：范围、阶段与验收标准。
- `docs/DECISIONS.md`：已确认产品/架构决策。
- `docs/PHASE5_VALIDATION.md`：下一轮必须使用的最终验收清单。
- `README.md` / `README.zh-CN.md`：英文/中文安装使用说明。
- `docs/KNOWN_LIMITATIONS.md`：双语已知限制。
- `scripts/Build-Windows.ps1`：Windows 构建、安全更新与最终打包。
- `dist/NpcLocator-0.1.0-rc11-source.zip`：当前设备候选源码包（Git 忽略）。

## 建议使用的 Skills

- Windows 构建或运行时出现失败、抛错或行为不符合预期时，使用 `diagnosing-bugs`。
- 需要再次暂停、换设备或换 agent 时，使用 `handoff` 更新本文件。

## 接手注意事项

- 先核对路径、`main`、源提交 `9f7efc5` 与工作区；不要根据旧 handoff 重新从阶段 0 开始。
- 当前没有尚未回答的产品问题；直接执行“下一步”。
- 不要修改 Lookup Anything，不引用其 DLL，不复制其资源。
- 不要将任务内容发送给主机，不要写存档，不要用日程坐标冒充实时位置。
- 不要过滤未解锁的 `Leo`，不要恢复替换游戏背景或纸张色块不一的旧 UI 绘制。
- 工作区可能只因本 handoff 而不干净；这是用户请求的未提交交接文档，不要误删或用旧版覆盖。
