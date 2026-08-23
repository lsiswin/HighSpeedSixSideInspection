# 项目开发路线图（01→18）

更新时间：2026-08-23

## 执行原则

- 严格按照 01 到 18 的依赖顺序推进；前一阶段的基础契约未稳定前，不在后一阶段堆叠业务代码。
- “代码完成”不等于“阶段完成”；涉及真实设备的阶段必须通过对应硬件 PoC 和异常恢复测试。
- 每个阶段都必须有中文方法注释、自动化测试、执行报告、状态更新和可量化退出条件。

## 阶段状态

| 顺序 | 模块 | 状态 | 当前成果 | 退出条件 |
|---:|---|---|---|---|
| 01 | Device Abstraction | 基础完成 | 统一身份、连接生命周期、操作结果；PLC/Camera/Motion/Robot 接口 | 新驱动不泄漏厂商 SDK；契约测试覆盖生命周期 |
| 02 | PLC Driver | 进行中 | S7.NetPlus、批量读取、命令脉冲、重连、V1 协议版本、可选命令应答/心跳、DB100/DB101 草案 | PLC 侧握手/心跳；TIA 编译核对并冻结 V1；真实 S7-1511 8h/24h；断线和重启恢复 |
| 03 | Camera Driver | 等待 02 Gate | 相机抽象和模拟帧流已准备 | MVS SDK 接入；单相机 10 万帧；六相机丢帧指标 |
| 04 | Motion Driver | 等待 03 | 运动抽象和位置比较计划已准备 | MotionRT7 接入；回零/运动/位置比较；1000 次重复定位 |
| 05 | Device Simulator | 等待 04 | 已有四类基础模拟器 | 支持时序、断线、丢帧、轴故障、机器人故障脚本化注入 |
| 06 | Product Tracking | 未开始 | 已定义 ProductId 关联方向 | 100 件、5～10 件并发在线，六面串件率 0 |
| 07 | Trigger | 未开始 | PositionComparePlan 契约已定义 | 1 万件 Missing Trigger=0，触发完全位于运动控制器 |
| 08 | Vision Pipeline | 未开始 | CameraFrame 可承载 FrameId/ProductId | 六相机并行；单相机处理 <50 ms；超时和 Unknown 闭环 |
| 09 | Reject | 未开始 | 位置比较可承载 ProductId | 1 万件 WrongReject=0、MissReject=0 |
| 10 | Alarm | 未开始 | 无 | 报警等级、生命周期、确认、恢复和历史记录完成 |
| 11 | Recipe | 未开始 | PLC RecipeId 命令已预留 | Schema、版本、审批、设备参数下发和回滚完成 |
| 12 | Production | 未开始 | MachineStatus 初版 | 批次、产量、良率、节拍、停机原因和产品结果完成 |
| 13 | Database | 未开始 | 无 | 迁移、保留策略、图片路径、时序批写和恢复演练完成 |
| 14 | MES | 未开始 | 无 | RequestId 幂等、重试、离线队列和最终一致性完成 |
| 15 | UI | 未开始 | 无 | WPF MVVM、设备状态、手动页、趋势、报警、配方完成 |
| 16 | User Permission | 未开始 | 无 | Operator/Engineer/Admin 权限与高风险操作确认完成 |
| 17 | Audit | 未开始 | 无 | 登录、参数、手动操作、报警复位和权限变更全量审计 |
| 18 | Reports | 未开始 | 无 | 生产、质量、报警、设备和审计报表完成 |

## 当前停点

当前必须继续完成 02 PLC Driver。C# 已实现 DB100/DB101 V1 的协议版本检查、可选命令应答和心跳监控；TIA CLI Bridge 当前拒绝连接，下一项是在 TIA 中生成数据块、实现 PLC 侧握手/心跳、编译并核对实际偏移。真实 PLC Gate 未完成前，不把 03 Camera Driver 标记为进行中。

## 02 PLC Driver 下一批任务

1. 在 TIA Portal V18 导入 SCL，确认 DB100_HMI、DB101_Machine 的块号、符号、偏移和数据类型后冻结 V1。
2. 确认 S7-1511 固件、IP、Rack、Slot、优化访问及通讯权限。
3. 创建 PLC 侧命令上升沿、流水号应答、业务状态码和心跳递增逻辑，并通过 `tia_compile`。
4. 在安全测试模式验证 Read/Write/命令脉冲和 RecipeChange 握手。
5. 执行拔网线、PLC 重启、错误地址、应答超时、心跳冻结和恢复测试。
6. 执行 8 小时 PoC，达标后再执行 24 小时 Gate。
