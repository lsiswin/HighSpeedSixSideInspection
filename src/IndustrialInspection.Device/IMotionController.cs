namespace IndustrialInspection.Device;

public readonly record struct AxisId(int Value)
{
    public override string ToString() => $"Axis{Value}";
}

public enum MotionStopMode
{
    Controlled,
    Immediate
}

public sealed record MotionProfile(
    double Position,
    double Velocity,
    double Acceleration,
    double Deceleration,
    double Jerk,
    string Unit = "mm");

public sealed record PositionComparePoint(
    string Id,
    double Position,
    int OutputChannel,
    TimeSpan PulseWidth,
    string? ProductId = null);

public sealed record PositionComparePlan(
    AxisId EncoderAxis,
    IReadOnlyList<PositionComparePoint> Points,
    bool OneShot = true);

public interface IMotionController : IDeviceDriver
{
    Task<DeviceOperationResult> EnableAxisAsync(AxisId axis, CancellationToken cancellationToken);
    Task<DeviceOperationResult> DisableAxisAsync(AxisId axis, CancellationToken cancellationToken);
    Task<DeviceOperationResult> HomeAsync(AxisId axis, CancellationToken cancellationToken);
    Task<DeviceOperationResult> MoveAbsoluteAsync(AxisId axis, MotionProfile profile, CancellationToken cancellationToken);
    Task<DeviceOperationResult> MoveVelocityAsync(AxisId axis, double velocity, string unit, CancellationToken cancellationToken);
    Task<DeviceOperationResult> StopAsync(AxisId axis, MotionStopMode mode, CancellationToken cancellationToken);
    Task<double> ReadPositionAsync(AxisId axis, CancellationToken cancellationToken);
    Task<DeviceOperationResult> ConfigurePositionCompareAsync(PositionComparePlan plan, CancellationToken cancellationToken);

    // 这是软件停机请求，不构成功能安全；急停、STO 必须由硬件安全回路完成。
    Task<DeviceOperationResult> RequestAllAxesStopAsync(MotionStopMode mode, CancellationToken cancellationToken);
}

