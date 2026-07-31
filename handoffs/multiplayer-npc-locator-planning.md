# 任务交接：联机 NPC 定位器规划与开发准备

生成时间：2026-07-31 16:09:41 CST（Asia/Shanghai，UTC+08:00）  
项目：Multiplayer NPC Locator（联机 NPC 定位器）  
任务标识：`multiplayer-npc-locator-planning`  
状态：进行中（规划已确认，尚未编码）  
版本控制：当前目录不是 Git 仓库，因此没有可核对的源分支或源提交

## 任务概述

开发一个独立的 SMAPI Mod，让《星露谷物语》联机中的非主机玩家通过主机权威数据查询 NPC 的实时位置与今日标准日程，并可追踪当前 NPC 或标准物品交付任务的目标 NPC。该 Mod 不修改 Lookup Anything，也不修改游戏存档。

完整范围、架构、通信协议、阶段划分和验收标准见 `docs/DEVELOPMENT_PLAN.md`，不要在接手时重新设计或复述整份计划。

## 成功标准

- 主机与查询者均安装本 Mod 后，非主机可查询原版 NPC 的实时位置和可用的今日标准日程。
- 查询者可使用默认 `F3`（可自定义）打开界面，并持续追踪一名 NPC。
- 可在本地识别标准物品交付任务，并由玩家选择是否追踪目标 NPC。
- 主机默认静默响应，不暂停游戏、不改变 NPC、任务或存档。
- 提供简体中文和英文，并在 Stardew Valley 1.6.15 / SMAPI 4.5.2 环境完成双方联机验证。

## 当前状态

需求、产品边界和主要技术方案已经确认，项目尚未创建 C# 工程，也没有 Mod 源代码。下一阶段是“阶段 0：构建与 API 验证”。当前开发机器上是否具备可用于编译的 Stardew Valley 与 SMAPI 程序集仍待验证。

## 已完成工作

- 对照了主机与联机加入者的 SMAPI 日志，确认双方的主要环境。
- 确认问题来自非主机可见的联机同步数据有限，并采用主机权威查询方案。
- 完成详细开发计划、设计决策记录和状态文档。
- 建立项目名称、作者、UniqueID、初版范围、交互原则和测试方向。

## 已确认决策

- 作者：Mercury；UniqueID：`Mercury.MultiplayerNpcLocator`；首版：`0.1.0` 私人内测。
- 本项目独立于 Lookup Anything；双方只需安装本 Mod，Lookup Anything 对双方均为可选。
- 默认按键为 `F3`，使用 `KeybindList` 并支持 GMCM / `config.json` 改键。
- 主机首版仅提供总开关，不实现逐玩家权限。
- 同时只追踪一名 NPC；手动追踪跨游戏日保留，但退出或重新载入后清除。
- 首版仅自动识别标准物品交付任务，采用非阻塞提示，不强制覆盖当前追踪目标。
- 首版先实现文字方向与距离，方向箭头后续增强；先做基础手柄按键兼容。
- UI 必须区分“实时位置”和“今日标准日程”；遇到节日、事件或脚本控制时不得伪造日程或预测位置。
- 最低目标为游戏 1.6.15，避免依赖 1.6.16 独有 API；首版正式支持原版 NPC。

以上决策的完整记录和变更规则见 `docs/DECISIONS.md`。

## 重要发现

- 已验证的双方日志环境：Windows 11、Stardew Valley 1.6.15 build 24356、SMAPI 4.5.2、Lookup Anything 1.55.0、GMCM 1.16.0，未发现大型 NPC/地图扩展。
- 联机加入者额外安装 MouseMoveMode，与本项目无直接关系。
- 主机日志的 RivaTuner 警告与 NPC 查询无直接关系；只有后续出现崩溃或渲染问题时再排查。
- 联机加入者日志曾出现 Pam 相关 NPC 事件错误。实现时必须把 NPC 暂时不存在、处于事件中或无标准日程视为正常可返回状态，不能因此崩溃。
- SMAPI 无法可靠检测所有第三方 Mod 的快捷键冲突，已决定通过自由改键解决。

## 当前修改和工作区状态

当前项目目录：`repository root`

现有文件：

- `README.md`
- `docs/DEVELOPMENT_PLAN.md`
- `docs/DECISIONS.md`
- `docs/STATUS.md`
- `handoffs/multiplayer-npc-locator-planning.md`

该目录不是 Git 仓库；以上文件没有 Git 提交或远程推送保障。不要假定另一台设备能够取得这些内容。

## 下一步

严格从阶段 0 开始，不要直接实现完整 UI：

1. 再次核对项目路径和现有三份文档，避免覆盖用户已确认的设计。
2. 验证本机 Stardew Valley、SMAPI 和 .NET 6 构建环境及程序集路径。
3. 创建最小 SMAPI C# 项目，引入 `Pathoschild.Stardew.ModBuildConfig`，设置作者 Mercury、版本 `0.1.0` 和 UniqueID。
4. 构建并确认最小 Mod 可由 SMAPI 加载。
5. 用小型验证代码确认 1.6.15 中 NPC 查找、实时地点与坐标、地点显示名、`NPC.Schedule` 的读取行为。
6. 验证标准物品交付任务的稳定读取字段，并记录事件、节日、NPC 缺席等边界结果。
7. 将验证结果更新到 `docs/STATUS.md`；若产生新的重要产品或架构决定，按编号追加到 `docs/DECISIONS.md`。
8. 阶段 0 通过后，再按计划顺序开发：联机通信 → 查询 UI → 持续追踪 → 任务识别 → 方向箭头。

## 相关文件与资料

- `docs/DEVELOPMENT_PLAN.md`：完整开发计划、协议草案、阶段与验收标准。
- `docs/DECISIONS.md`：全部已确认决定，后续不得无记录地推翻。
- `docs/STATUS.md`：当前进度、阻塞项和下一步。
- `README.md`：项目入口与定位。
- 联机加入者日志：<https://smapi.io/log/f97102cbcdff46d188a94e7f6d0d565b>
- 主机日志：<https://smapi.io/log/08225e5343144c6db3e142767d0a1995>
- SMAPI 联机 API：<https://wiki.stardewvalley.net/Modding%3AModder_Guide/APIs/Multiplayer>
- SMAPI Mod 入门与构建：<https://wiki.stardewvalley.net/Modding%3AModder_Guide/Get_Started>

## 接手注意事项

- 先验证再编码；不要把 Lookup Anything 作为依赖，也不要尝试修改其 DLL 或界面。
- 查询者和主机都必须安装本 Mod。主机无需主动操作，默认应静默处理请求。
- 玩家任务内容只在查询者本地解析，不发送给主机。
- 不写入游戏存档，不做自动寻路、自动交付或传送。
- 任何网络响应都要有请求 ID、协议版本、超时处理和明确的失败状态；客户端不能通过消息要求主机执行任意代码、读取文件或实例化任意类型。
- 若实际 API 行为与计划假设不一致，先记录证据与影响，再更新决策文档；不要悄悄改变产品语义。
