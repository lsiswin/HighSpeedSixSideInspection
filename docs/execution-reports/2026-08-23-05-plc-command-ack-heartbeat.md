# 执行报告：PLC 命令应答与心跳

执行日期：2026-08-23

所属阶段：02 PLC Driver

执行状态：C# 协议层完成；TIA 在线编译 Gate 未通过

## 本次目标

继续完善 02 PLC Driver，把 DB100/DB101 中预留的协议版本、命令流水号应答和双向心跳落实到上位机代码与模拟测试，并严格区分 PLC 业务拒绝、用户取消和真实通信故障。

## 已执行步骤

1. 扩展 `SiemensPlcOptions` 和点位映射，加入协议版本、命令应答、心跳周期及超时配置。
2. 连接后校验 PLC 协议版本，写入上位机协议版本和 `PcReady`。
3. 实现命令流水号写入、Busy/Error/Status 批量读取、成功/业务拒绝/超时判定。
4. 实现 PLC 心跳冻结监控和按周期写入 PC 心跳，避免每 50 ms 轮询都写 PLC。
5. 修正异常分类：业务拒绝、参数错误和用户取消不再把健康连接误标记为通信故障。
6. 扩展模拟 PLC，支持命令处理延时、业务拒绝和确定性命令网络故障。
7. 新增协议等待、旧应答、Busy、业务拒绝、应答超时、心跳冻结及模拟器状态测试。
8. 同步 CSV/XLSX 点表的当前驱动状态，并重新渲染两张工作表核对。
9. 按新 TIA Openness 规范生成工作区 `.github/ProjectDescription.md`，包含架构、拓扑、调用树、数据流、状态机、DB 布局和队列图。
10. 读取 `.tia/cli.json` 并尝试连接 TIA CLI Bridge。

## 主要修改

- 新增 `src/IndustrialInspection.Device.Plc.Siemens/PlcCommunicationProtocol.cs`。
- 修改 `SiemensPlcDriver.cs`，加入协议初始化、应答等待和心跳监控。
- 修改 `SiemensPlcOptions.cs` 与 `appsettings.json`，加入 V1 协议配置和地址。
- 在设备抽象层新增通用 `PlcCommandRejectedException`。
- 扩展 `SimulatedPlcDriver` 命令异常注入能力。
- 新增 `PlcCommunicationProtocolTests.cs`，并扩展配置校验测试。
- 更新 PLC 点表 CSV、XLSX、PoC 指南、状态和路线图。

## 配置开关

- `EnableProtocolVersionCheck=true`：默认启用，PLC/PC 版本不一致时拒绝建立健康连接。
- `EnableCommandAcknowledgement=false`：等待 PLC 侧完成应答逻辑后启用。
- `EnableHeartbeatMonitoring=false`：等待 PLC 侧实现心跳递增后启用。
- 命令应答超时默认 `2000 ms`，轮询周期 `20 ms`。
- PC 心跳写入周期默认 `1000 ms`，PLC 心跳冻结超时默认 `5000 ms`。

## 验证结果

- 中文方法注释检查：通过，共扫描 19 个手写 C# 文件。
- `dotnet build .\IndustrialInspection.sln --no-restore`：通过，0 警告、0 错误。
- `dotnet test .\IndustrialInspection.sln --no-build`：通过，16/16 测试成功。
- `dotnet format .\IndustrialInspection.sln --verify-no-changes --no-restore`：通过。
- XLSX 公式错误扫描：无匹配项。
- XLSX 视觉检查：`点表V1` 和 `使用说明` 均无明显截断，条件启用点位使用蓝色状态标识。
- TIA Portal 进程及本地 `.ap18` 项目目录已检测到，但该目录属于用户/TIA 生成内容，本次未修改或提交。
- `tia_connect`：失败，本地 `.tia/cli.json` 所指端口拒绝连接，说明 CLI Bridge 会话未运行或状态文件已失效。
- `tia_compile`：已发起但无法送达桥接服务；尚未枚举到 PLC 设备，不能把 .NET 构建结果替代 PLC 编译结果。

## 系统当前程度

- 01 Device Abstraction：基础完成。
- 02 PLC Driver：约 70%，C# 协议层和离线测试进一步完成。
- 点表 V1：仍为草案，等待 TIA V18 编译核对。
- 03 Camera Driver 及后续模块：继续等待 02 PLC Driver Gate。

## 下一步

在 VS Code 打开 TIA Import CLI Bridge，并在当前 TIA V18 项目中添加 S7-1511 设备；随后连接桥接、枚举设备、导入两个 DB、创建 PLC 侧握手与心跳块、执行 `tia_compile` 和诊断修复循环。
