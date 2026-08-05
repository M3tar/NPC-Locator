# 阶段 1：主机权威查询验证

本轮仍使用 SMAPI 控制台命令，不包含 F3 查询界面。目标是确认非主机能够从主机取得远端 NPC 的实时位置与今日标准日程。

## 准备

1. 主机与联机加入者必须安装同一次构建生成的 `NpcLocator`。
2. 两端均使用 Stardew Valley 1.6.15 与 SMAPI 4.5.2。
3. 完全退出游戏后再替换 DLL。
4. 主机首次加载后会生成 `config.json`；以下默认值应保持不变：

```json
{
  "AllowRemoteQueries": true,
  "ShareCurrentLocation": true,
  "ShareDailySchedule": true,
  "ShowHostNotifications": false,
  "MaxRequestsPerSecond": 4
}
```

## 先做单机冒烟测试

在任意单人档加载完成后执行：

```text
nl_query Pam
```

应输出 `status=Success`、实时地点和结构化日程。再执行：

```text
nl_query DefinitelyMissingNpc
```

应输出 `status=NpcNotFound`，且没有未处理异常。

## 联机闭环测试

1. 主机通过 SMAPI 开启联机存档。
2. 另一台电脑通过 SMAPI 加入。
3. 等待双方玩家完全进入游戏世界。
4. 在非主机的 SMAPI 控制台执行：

```text
nl_query Pam
```

5. 再查询一个不在非主机当前地图中的 NPC：

```text
nl_query Abigail
```

6. 最后执行：

```text
nl_query DefinitelyMissingNpc
```

非主机日志应依次显示：

- 已向主机发送带唯一请求 ID 的查询；
- 来自主机同一请求 ID 的响应；
- 实时地点内部名、显示名、坐标；
- 标准日程的时间、地点、坐标、朝向与行为；
- 不存在 NPC 返回 `NpcNotFound`；
- 没有超时或未处理异常。

主机默认不显示普通查询通知；这是 `ShowHostNotifications=false` 的预期行为。协议握手通常记录为 Trace，不一定显示在控制台，但会出现在完整日志中。

## 需要回传

- 主机完整 SMAPI 日志；
- 非主机完整 SMAPI 日志；
- 三次 `nl_query` 的非主机控制台输出；
- 查询时非主机所在地图，以及被查询 NPC 是否在同一地图。

阶段 1 的当前实现不修改 NPC、任务或存档，也不接受文件路径、类型名或可执行指令。
