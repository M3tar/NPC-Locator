# 阶段 5：0.1.0 最终验收与打包

本清单使用同一个最终二进制包验收，不再混用历史阶段包。

## 1. 生成最终包

在 Windows 源码目录中执行：

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\scripts\Build-Windows.ps1 -Install -UpdateExisting -Package
```

非默认 Steam 库追加：

```powershell
-GamePath 'D:\SteamLibrary\steamapps\common\Stardew Valley'
```

预期结果：

- Release 编译成功；
- 当前电脑的 `Mods\MultiplayerNpcLocator` 安全更新；
- `dist\MultiplayerNpcLocator-0.1.0.zip` 生成；
- zip 顶层只有一个 `MultiplayerNpcLocator` 文件夹，其中包含 DLL、manifest、i18n 与说明。

## 2. 同版双方冒烟

1. 主机和农场助手删除旧的本 Mod 文件夹，各自解压同一个最终包到 `Mods`。
2. 确认双方 SMAPI 均识别 `Multiplayer NPC Locator 0.1.0`。
3. 主机与农场助手分别打开 F3，查询同一 NPC，对比实时地点、坐标与今日标准日程。
4. 农场助手追踪一名远端室内 NPC，等待至少两个游戏时间刻度，确认定时刷新。
5. 主机开始自己的手动/任务追踪，农场助手的查询不应弹出主机窗口或改变主机追踪。

## 3. 安装组合边界

| 场景 | 预期 |
|---|---|
| 双方同版 | 完整查询和追踪 |
| 只有农场助手安装 | F3 可打开，远程查询明确提示主机不可用，不持续发送 |
| 只有主机安装 | 主机本地功能正常；未安装的农场助手无本 Mod 界面 |
| 协议不兼容 | 显示版本/协议不兼容，不使用过期结果 |

## 4. 主机共享与断线

1. GMCM 中分别关闭“允许远程查询”、“共享实时位置”和“共享今日标准日程”，农场助手应得到区分明确的拒绝状态。
2. 主机关闭远程共享时，主机自己的 F3 本地查询仍正常。
3. 农场助手追踪期间让主机退出，应停止远程等待/追踪，不得冻结或持续报错。
4. 重新连接后可重新手动选择追踪，旧请求的过期响应不得覆盖新结果。

## 5. 游戏状态与 UI 边界

- UI 缩放：75%、100%、125%（若游戏选项可用）；
- 视窗：窗口化与全屏；
- 手柄：方向键、A、X、Y、B 与左右肩键切换分页；
- 普通日、雨天、节日/事件、NPC 暂时不存在、无标准日程；
- 未解锁姜岛的 Leo：可列出，但应安全显示暂时无法定位；
- 任务：剧情交付、公告栏随机交付、同时多任务、完成、取消和过期。

## 6. 日志与卸载

1. 检查双方 SMAPI 日志，确认没有由 `Multiplayer NPC Locator` 产生的红色未处理错误。
2. 正常查询/追踪不应逐帧或每次刷新刷屏。
3. 备份存档后删除 `Mods\MultiplayerNpcLocator`，游戏与原存档应仍可正常加载。

## 回传

- `dist\MultiplayerNpcLocator-0.1.0.zip` 的 SHA-256；
- 主机和农场助手的最终 SMAPI 日志；
- 未通过项的操作步骤、截图或完整错误文本。
