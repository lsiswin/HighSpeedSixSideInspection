# 执行报告：TIA V18 通信点表 V1

执行日期：2026-08-23

所属阶段：02 PLC Driver

执行状态：点表草案及建块材料完成，等待 TIA V18 编译核对

## 本次目标

在尚未创建 TIA 设备的情况下，先定义可供 S7-1511 和 C# 上位机共同遵循的 DB100/DB101 通信契约，并给出可操作的 TIA V18 创建、导出与核对流程。

## 已执行步骤

1. 依据现有 `MachineStatus` 和 PLC 命令接口整理 DB100/DB101 点位。
2. 重新规划四字节数据的边界，在布尔区后加入显式保留字段。
3. 生成中文 CSV 点表，标注方向、权限、初始值、单位和当前驱动使用状态。
4. 生成 TIA V18 SCL 外部源，两个通信 DB 均声明为非优化访问。
5. 编写 TIA V18 建设备、导入 SCL、核对偏移、通信权限、导出和安全检查指南。
6. 将 C# 及 PoC 配置中的默认地址同步到 V1。
7. 更新项目状态和 01→18 路线图停点。
8. 生成带筛选、冻结窗格、状态颜色和使用说明页的 XLSX 现场交付表，并完成两张工作表的渲染检查。

## 修改内容

- 新增 `docs/plc/PLC-POINT-LIST-V1.csv`。
- 新增 `docs/plc/TIA-V18-POINT-LIST-GUIDE.md`。
- 新增 `plc/tia-v18/CommunicationDbV1.db`。
- 新增 `outputs/2026-08-23-tia-v18-point-list-v1/PLC-POINT-LIST-V1.xlsx`。
- `RecipeId` 从 `DB100.DBD2` 调整为 `DB100.DBD4`。
- `CycleTime` 从 `DB101.DBD2` 调整为 `DB101.DBD4`。
- `Speed` 从 `DB101.DBD6` 调整为 `DB101.DBD8`。
- 更新 `docs/STATUS.md` 和 `docs/ROADMAP.md`。

## 关键设计说明

- 当前点表是 PC/PLC 通信点表，不是物理 I/O 点表。
- 绝对地址通信要求 DB100/DB101 使用标准访问，并在 TIA 编译后逐项核对偏移。
- 命令流水号、应答、心跳和协议版本已预留，但完整握手尚未在 C# 和 PLC 逻辑中启用。
- 急停、安全门和 STO 仍由安全硬件完成，上位机只读取状态镜像。

## 系统当前程度

- 01 Device Abstraction：基础完成。
- 02 PLC Driver：进行中，完成度约 62%。
- 点表 V1：已生成草案，尚未经过 TIA V18 实际编译和真实 PLC 验证，因此还不能标记为正式冻结。
- 03 Camera Driver 及后续模块：继续等待 02 PLC Driver Gate。

## 验证结果

- 中文方法注释检查：通过，共扫描 17 个手写 C# 文件。
- `dotnet build .\IndustrialInspection.sln --no-restore`：通过，0 警告、0 错误。
- `dotnet test .\IndustrialInspection.sln --no-build`：通过，8/8 测试成功。
- XLSX 内容检查：点表 42 个点位（含固定偏移所需保留字段）完整，公式错误扫描无匹配项。
- XLSX 视觉检查：`点表V1` 和 `使用说明` 两张工作表均已渲染，中文、列宽、状态颜色和主要说明无明显截断。
- TIA SCL 编译：尚未执行，需要用户在已安装的 TIA Portal V18 中导入验证。

## 下一步

用户在 TIA V18 中添加准确型号的 CPU，导入 SCL 并回传编译后的块号和偏移；随后执行只读连接、写脉冲、异常恢复、8 小时和 24 小时稳定性验证。
