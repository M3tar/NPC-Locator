# Windows 阶段 0 构建与验证

本轮产物只是阶段 0 验证版：它会加载一个只读控制台命令，不包含 F3 查询界面、联机通信或持续追踪。

## 一、准备环境

Windows 电脑需要具备：

- Stardew Valley 1.6.15；
- SMAPI 4.5.2，并确认可以通过 SMAPI 正常启动游戏；
- .NET 6 SDK x64（注意是 SDK，不只是 Runtime）；
- 完整的本项目文件夹。

安装 .NET SDK 后重新打开 PowerShell，执行：

```powershell
dotnet --list-sdks
```

输出中应至少有一行以 `6.` 开头。

## 二、自动构建并安装

在项目根目录打开 PowerShell，然后执行：

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\Build-Windows.ps1 -Install
```

首次安装时，脚本只会创建新的 `Mods\NpcLocator` 文件夹；如果同名目录已经存在且未明确要求更新，脚本会停止，不会覆盖。如果检测到重命名前的 `Mods\MultiplayerNpcLocator` 测试版，脚本也会停止，并要求先手动删除旧目录，避免两个 UniqueID 同时加载。

更新已经安装的本 Mod 时使用：

```powershell
.\scripts\Build-Windows.ps1 -Install -UpdateExisting
```

脚本会先确认现有 `manifest.json` 的 UniqueID 是 `Mercury.NpcLocator`，然后只更新本 Mod 的 DLL、PDB、manifest 和 i18n 翻译文件。

如果 Steam 不在默认目录，请指定包含 `Stardew Valley.dll` 和 `StardewModdingAPI.dll` 的游戏目录：

```powershell
.\scripts\Build-Windows.ps1 -GamePath "D:\SteamLibrary\steamapps\common\Stardew Valley" -Install
```

成功时应看到：

```text
Build succeeded: ...\NpcLocator.dll
Installed or updated validation build at: ...\Mods\NpcLocator
```

如果只想构建、不自动复制到 Mods，省略 `-Install`。Release DLL 位于：

```text
bin\Release\net6.0\NpcLocator.dll
```

## 三、确认 Mod 加载

1. 通过 SMAPI 启动游戏。
2. 启动日志的 Mod 列表中应出现 `NPC Locator 0.1.0 by March3tar`。
3. 不应出现红色的本 Mod 加载或编译错误。
4. 载入一个普通存档；本轮验证可先在单人模式完成。

## 四、执行只读验证

存档加载完成后，在 SMAPI 控制台执行：

```text
nl_validate Pam
```

再选择一个当前能在游戏中见到的原版 NPC，例如：

```text
nl_validate Abigail
```

最后验证不存在的目标：

```text
nl_validate DefinitelyMissingNpc
```

预期日志应分别包含：

- NPC 内部名与显示名；
- 实时地点内部名、地点显示名和格子坐标；
- `Schedule` 成员与当天条目，或明确的空/不可用状态；
- 不存在 NPC 返回警告，但不抛出异常；
- 当前任务总数和标准 `ItemDeliveryQuest` 数量。

为了验证物品交付任务字段，请先接取一个公告栏物品交付任务，不要完成，然后再次运行：

```text
nl_validate Pam
```

探针只读取内存中的 NPC 与任务信息，不修改 NPC、任务或存档。

## 五、需要回传的结果

请保留以下内容并发回开发端：

1. PowerShell 从版本检测到 `Build succeeded` 的完整输出；
2. 游戏启动后完整的 SMAPI 日志链接；
3. 上述三次 `nl_validate` 的输出；
4. 存在物品交付任务时的验证输出；
5. 当天是否为普通日、雨天、节日或剧情事件日。

拿到这些证据后，才能把实际稳定字段写入生产查询服务，并判断阶段 0 是否通过。

## 六、卸载验证版

退出游戏后，删除这个明确的目录即可：

```text
Stardew Valley\Mods\NpcLocator
```

本 Mod 不写入存档，阶段 0 探针也不改变游戏状态。
