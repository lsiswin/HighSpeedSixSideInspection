namespace IndustrialInspection.Device;

/// <summary>表示运动控制器中的稳定轴编号。</summary>
public readonly record struct AxisId(int Value)
{
    /// <summary>返回便于日志和诊断显示的轴名称。</summary>
    /// <returns>格式为 Axis0、Axis1 的轴名称。</returns>
    public override string ToString() => $"Axis{Value}";
}

public enum MotionStopMode
{
    Controlled,
    Immediate
}

/// <summary>描述一次定位运动的目标位置和动力学参数。</summary>
public sealed record MotionProfile(
    double Position,
    double Velocity,
    double Acceleration,
    double Deceleration,
    double Jerk,
    string Unit = "mm");

/// <summary>描述编码器位置到硬件输出脉冲的单个比较点。</summary>
public sealed record PositionComparePoint(
    string Id,
    double Position,
    int OutputChannel,
    TimeSpan PulseWidth,
    string? ProductId = null);

/// <summary>描述一组由运动控制器确定性执行的位置比较点。</summary>
public sealed record PositionComparePlan(
    AxisId EncoderAxis,
    IReadOnlyList<PositionComparePoint> Points,
    bool OneShot = true);

/// <summary>定义低频运动管理和硬件位置比较配置能力。</summary>
public interface IMotionController : IDeviceDriver
{
    /// <summary>使能指定运动轴。</summary>
    Task<DeviceOperationResult> EnableAxisAsync(AxisId axis, CancellationToken cancellationToken);

    /// <summary>禁用指定运动轴。</summary>
    Task<DeviceOperationResult> DisableAxisAsync(AxisId axis, CancellationToken cancellationToken);

    /// <summary>执行指定轴的回零流程。</summary>
    Task<DeviceOperationResult> HomeAsync(AxisId axis, CancellationToken cancellationToken);

    /// <summary>向运动控制器下发绝对位置运动参数。</summary>
    Task<DeviceOperationResult> MoveAbsoluteAsync(AxisId axis, MotionProfile profile, CancellationToken cancellationToken);

    /// <summary>向运动控制器下发速度运动参数。</summary>
    Task<DeviceOperationResult> MoveVelocityAsync(AxisId axis, double velocity, string unit, CancellationToken cancellationToken);

    /// <summary>请求指定轴按所选模式停止。</summary>
    Task<DeviceOperationResult> StopAsync(AxisId axis, MotionStopMode mode, CancellationToken cancellationToken);

    /// <summary>读取运动控制器维护的轴位置。</summary>
    Task<double> ReadPositionAsync(AxisId axis, CancellationToken cancellationToken);

    /// <summary>把相机触发或剔除触发计划下发到硬件位置比较模块。</summary>
    Task<DeviceOperationResult> ConfigurePositionCompareAsync(PositionComparePlan plan, CancellationToken cancellationToken);

    /// <summary>请求所有轴停止，但不替代急停和 STO。</summary>
    /// <remarks>这是软件停机请求，不构成功能安全；急停、STO 必须由硬件安全回路完成。</remarks>
    Task<DeviceOperationResult> RequestAllAxesStopAsync(MotionStopMode mode, CancellationToken cancellationToken);
}
