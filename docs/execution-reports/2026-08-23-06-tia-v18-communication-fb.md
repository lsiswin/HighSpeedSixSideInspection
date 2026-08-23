# 执行报告：TIA V18 点表核对与通信 FB 接入

执行日期：2026-08-23

所属阶段：02 PLC Driver

执行状态：离线 TIA 集成完成；真实 PLC/PLCSIM 通信 Gate 待执行

## 本次目标

检查用户已创建的 DB100/DB101 点表，连接当前 TIA Portal V18 工程，继续实现 PLC 侧协议握手、心跳和命令应答，并把通信块接入 OB1。

## 已执行步骤

1. 启动 VS Code 中的 TIA Import CLI Bridge，并连接已打开的 `HighSpeedSixSideInspection_PLC`。
2. 枚举 `PLC_1`（S7-1511-1 PN）及程序块，确认 DB100/DB101 已存在。
3. 检查 TIA 导出结果：DB100 为 15 个成员、DB101 为 27 个成员，块号为 100/101，布局均为 Standard。
4. 对原始点表工程执行基线编译，结果为 0 错误、0 警告。
5. 新增并导入 `FB_UpperComputerCommunication`，实现协议版本、双向心跳、命令上升沿、流水号去重和完成后应答。
6. 根据 TIA V18 编译反馈修复外部源兼容问题：接口行尾注释、注释紧邻 `IF`、条件预计算和输出初始化。
7. 新增 `DB_UpperComputerCommunication` 专用实例 DB。
8. 将原空白 LAD `Main (OB1)` 替换为 SCL OB1，并调用通信实例；完成输入暂用 `FALSE/0`，不会驱动执行机构。
9. 刷新 TIA 工程树，确认 OB1、FB1、实例 DB1、DB100、DB101 共 5 个块。
10. 再次执行 PLC_1 完整编译，结果为 0 错误、0 警告，并通过 TIA Portal 普通保存操作保存工程。

## 主要修改

- 新增 `plc/tia-v18/FB_UpperComputerCommunication.scl`。
- 新增 `plc/tia-v18/DB_UpperComputerCommunication.scl`。
- 新增 `plc/tia-v18/Main.scl`。
- 保留用户创建的 `CommunicationDbV1.db`，并调整 `.gitignore` 允许提交该 PLC 外部源。
- 更新项目状态、路线图和 TIA 工程架构说明。

## PLC 逻辑边界

- PLC 每秒递增 `DB101_Machine.PlcHeartbeat`，并监控 PC 心跳冻结 5 秒。
- 只有协议版本一致、PC Ready 且已观察到 PC 心跳变化时才判定 `PcConnected`。
- 命令通过 R_TRIG 检测上升沿，并使用 `CommandSequence` 去重。
- 命令只在下游明确返回 `CommandDone` 后写入应答流水号；通信中断返回状态码 1001。
- 当前 OB1 的完成输入是安全占位值，因此命令可以被受理但不会被错误地确认成功，也不会直接动作设备。

## 验证结果

- TIA V18 点表基线编译：0 错误、0 警告。
- `FB_UpperComputerCommunication` 单块/完整软件编译：0 错误、0 警告。
- OB1 + 通信 FB + 实例 DB + DB100/DB101 完整编译：0 错误、0 警告。
- 工程树刷新后确认 5 个目标块均存在。
- 中文方法注释检查：通过，共扫描 19 个手写 C# 文件。
- `dotnet build .\IndustrialInspection.sln --no-restore`：通过，0 警告、0 错误。
- `dotnet test .\IndustrialInspection.sln --no-build`：通过，16/16 测试成功。
- `dotnet format .\IndustrialInspection.sln --verify-no-changes --no-restore`：通过。
- 未连接、未下载任何实体 PLC，未修改安全回路或执行机构输出。

## 系统当前程度

- 01 Device Abstraction：基础完成。
- 02 PLC Driver：约 82%，离线点表、C# 协议层、PLC 握手/心跳和 OB1 调用已完成。
- 03 Camera Driver：仍等待 02 的仿真/真机 Gate，不提前进入。

## 下一步

在 PLCSIM Advanced 或隔离测试 S7-1511 上核对 CPU 固件、IP、Rack/Slot、PUT/GET 权限；随后启用上位机命令应答和心跳开关，执行读写、业务拒绝、心跳冻结、拔网线、PLC 重启以及 8h/24h 稳定性测试。
