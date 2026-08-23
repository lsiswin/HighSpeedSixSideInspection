# 阶段一：PLC 通讯 PoC

## 目标

验证 Siemens S7-1511-1 PN 与 .NET 8 x64 上位机之间的 S7 通讯，在真实 PLC 上连续运行 8 小时，并为正式 Gate 扩展到 24 小时。

## 已实现范围

- `IPlcDriver` 厂商隔离接口。
- `SiemensPlcDriver`：S7-1500 连接、状态读取、命令脉冲写入。
- `PlcPollingLoop`：50 ms 周期、断线清理、2 秒重连、运行统计。
- `SimulatedPlcDriver`：无需硬件即可做故障注入。
- PLC 地址通过 `appsettings.json` 配置，等待点表冻结 V1。
- PoC 控制台支持真实 PLC 与模拟器模式及指定运行时长。

## 运行

```powershell
# 30 秒模拟验证
dotnet run --project .\src\IndustrialInspection.Poc.Plc -- --simulator --duration 00:00:30

# 真实 PLC 8 小时验证
dotnet run --project .\src\IndustrialInspection.Poc.Plc -- --duration 08:00:00
```

## 测试矩阵

| 编号 | 场景 | 操作 | 通过标准 |
|---|---|---|---|
| PLC-001 | 基础读取 | 50 ms 读取状态 | 状态刷新，无异常退出 |
| PLC-002 | 命令写入 | Start/Stop/Reset 脉冲 | PLC 仅响应一次，命令位复位 |
| PLC-003 | 网线断开 | 断网 60 秒后恢复 | 进程不退出，恢复后自动重连 |
| PLC-004 | PLC 重启 | 运行中重启 CPU | 进程不退出，CPU 恢复后重连 |
| PLC-005 | 地址错误 | 配置一个无效 DB 地址 | 明确错误，重连循环不崩溃 |
| PLC-006 | 8 小时耐久 | 连续运行 | Crash=0，统计完整 |
| PLC-007 | 24 小时 Gate | 连续运行 | Crash=0，无资源持续增长 |

## 开始真实 PLC 测试前必须确认

- PLC IP、Rack、Slot。
- S7 通信是否允许，PUT/GET 或所选访问方式是否已启用。
- DB100/DB101 的真实偏移、数据类型和优化访问设置。
- Start/Stop/Reset 是脉冲、保持还是请求/应答握手。
- 写入测试在离线设备或安全测试模式进行，并由 PLC 工程师在场确认。

## 阶段一退出条件

- 自动重连覆盖网线断开和 PLC 重启。
- 8 小时 PoC 无崩溃，24 小时 Gate 无崩溃。
- 点表冻结为 V1，并补充批量连续读取，避免每周期多次独立报文。
- 输出成功读取数、失败读取数、重连次数、最长中断时间和资源曲线。

