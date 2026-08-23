# High-Speed Six-Side Inspection

高速六面视觉检测设备软件，采用 C# / .NET 8 / WPF，面向 Siemens S7-1500、正运动 XPCIE1028、Hikrobot GigE 相机和 MES 集成。

## 当前阶段

阶段一：PLC 通讯 PoC。

当前仓库先验证设备层，不提前开发完整 UI。Windows 上位机负责智能与管理；PLC 负责机器逻辑；运动控制器负责确定性实时控制；安全继电器或安全 PLC 负责人身和设备安全。

## 构建

```powershell
dotnet restore .\IndustrialInspection.sln
dotnet build .\IndustrialInspection.sln --no-restore
dotnet test .\IndustrialInspection.sln --no-build
```

阶段一测试说明见 [docs/phase-1-plc-poc.md](docs/phase-1-plc-poc.md)。

## 安全声明

- 上位机不得实现急停、安全门、STO 或高速位置触发。
- 所有 PLC 写入必须经过应用服务、权限、状态与互锁检查。
- 当前 DB 地址是 PoC 配置，不是冻结点表，连接真实 PLC 前必须由 PLC 工程师复核。
- MES、相机或上位机故障不得绕过 PLC 和硬件安全逻辑。
