# 执行报告：相机、运动控制器与机械臂抽象

执行日期：2026-08-23  
执行编号：02

## 执行目标

在阶段一仓库中增加相机、运动控制器和机械臂的厂商无关抽象，同时建立模拟设备和自动化测试，为 Hikrobot、MotionRT7 和后续机械臂驱动预留稳定边界。

## 执行步骤

1. 增加统一设备身份、连接生命周期和操作结果契约。
2. 设计相机配置、采集状态、异步帧流及图像缓冲区所有权。
3. 设计运动轴、运动参数、停止模式和位置比较触发计划。
4. 设计机械臂远程模式、运行状态、程序选择和控制握手。
5. 实现三类模拟设备，用于无硬件开发和故障前置验证。
6. 增加相机 ProductId/FrameId、运动互锁和机器人启动互锁测试。
7. 固化每次执行后生成 Markdown 报告和更新状态文档的规则。

## 文件修改

- 新增 `AGENTS.md`：规定中文注释、执行报告和状态更新要求。
- 新增 `DeviceContracts.cs`、`ICamera.cs`、`IMotionController.cs`、`IRobotController.cs`。
- 新增 `SimulatedCamera.cs`、`SimulatedMotionController.cs`、`SimulatedRobotController.cs`。
- 新增 `DeviceAbstractionTests.cs`。
- 新增 `docs/STATUS.md` 和本执行报告。

## 关键设计决定

- 图像帧携带 `CameraId + FrameId + ProductId`，并要求使用后释放池化缓冲区。
- 相机接口只接收采集结果，不使用 Windows 定时器产生高速触发。
- 位置比较以计划形式下发运动控制器，高速触发不在 C# 中调度。
- 软件全轴停止不等同于急停，硬件急停和 STO 仍归安全系统。
- 机器人启动要求远程模式、伺服开启和程序已选择；真实驱动还必须接入 PLC/控制柜互锁。

## 验证结果

- `dotnet build .\IndustrialInspection.sln --no-restore`：通过，0 警告、0 错误。
- `dotnet test .\IndustrialInspection.sln --no-build`：通过，5/5 测试成功。
- 覆盖内容：PLC 轮询与重连、相机帧关联、运动使能/回零互锁、机器人启动互锁。
- 首轮构建发现并修复模拟运动控制器 `AddOrUpdate` 重载推断二义性。

## 当前系统程度

设备抽象层已覆盖 PLC、Camera、Motion 和 Robot，具备模拟开发能力；真实硬件驱动目前只有 Siemens PLC 初版，其他三类仍是接口与模拟器阶段。整机尚未形成视觉、跟踪、判定和剔除闭环。

## 遗留问题

- Hikrobot MVS SDK、MotionRT7 SDK 的准确版本和开发包路径尚未确认。
- 机械臂品牌、型号、控制柜和通讯协议尚未确定。
- 相机丢帧策略、六相机 NIC 拓扑和图像队列容量需要硬件 PoC 标定。
- 运动单位、轴号、编码器分辨率、软限位和位置比较通道尚未冻结。

## 下一步建议

1. 完成 PLC 点表 V1 和真实 S7-1511 联调。
2. 探测 Hikrobot MVS SDK，先实现单相机硬触发 10 万帧 PoC。
3. 探测 MotionRT7 x64 SDK，完成回零、运动和位置比较 PoC。
4. 客户确认机械臂清单后再建立厂商适配项目。
