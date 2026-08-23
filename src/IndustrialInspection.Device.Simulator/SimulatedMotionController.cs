using System.Collections.Concurrent;

namespace IndustrialInspection.Device.Simulator;

public sealed class SimulatedMotionController(DeviceIdentity identity) : IMotionController
{
    private readonly ConcurrentDictionary<AxisId, AxisState> _axes = new();

    public DeviceIdentity Identity { get; } = identity;
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;
    public PositionComparePlan? ComparePlan { get; private set; }

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        State = DeviceConnectionState.Connected;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public Task<DeviceOperationResult> EnableAxisAsync(AxisId axis, CancellationToken cancellationToken)
    {
        EnsureConnected();
        _axes.AddOrUpdate(axis, new AxisState(true, false, 0), (_, current) => current with { Enabled = true });
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> DisableAxisAsync(AxisId axis, CancellationToken cancellationToken)
    {
        _axes.AddOrUpdate(axis, new AxisState(false, false, 0), (_, current) => current with { Enabled = false });
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> HomeAsync(AxisId axis, CancellationToken cancellationToken)
    {
        if (!TryGetEnabled(axis, out var current))
        {
            return Task.FromResult(DeviceOperationResult.Failure("AXIS_DISABLED", $"{axis} 未使能。"));
        }

        _axes[axis] = current with { Homed = true, Position = 0 };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> MoveAbsoluteAsync(AxisId axis, MotionProfile profile, CancellationToken cancellationToken)
    {
        if (!TryGetEnabled(axis, out var current) || !current.Homed)
        {
            return Task.FromResult(DeviceOperationResult.Failure("AXIS_NOT_READY", $"{axis} 未使能或未回零。"));
        }

        if (profile.Velocity <= 0 || profile.Acceleration <= 0 || profile.Deceleration <= 0)
        {
            return Task.FromResult(DeviceOperationResult.Failure("PROFILE_INVALID", "速度、加速度和减速度必须大于零。"));
        }

        _axes[axis] = current with { Position = profile.Position };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> MoveVelocityAsync(AxisId axis, double velocity, string unit, CancellationToken cancellationToken) =>
        Task.FromResult(TryGetEnabled(axis, out _)
            ? DeviceOperationResult.Success
            : DeviceOperationResult.Failure("AXIS_DISABLED", $"{axis} 未使能。"));

    public Task<DeviceOperationResult> StopAsync(AxisId axis, MotionStopMode mode, CancellationToken cancellationToken) =>
        Task.FromResult(DeviceOperationResult.Success);

    public Task<double> ReadPositionAsync(AxisId axis, CancellationToken cancellationToken) =>
        Task.FromResult(_axes.TryGetValue(axis, out var state) ? state.Position : 0);

    public Task<DeviceOperationResult> ConfigurePositionCompareAsync(PositionComparePlan plan, CancellationToken cancellationToken)
    {
        if (plan.Points.Count == 0 || plan.Points.Any(point => point.PulseWidth <= TimeSpan.Zero || point.OutputChannel < 0))
        {
            return Task.FromResult(DeviceOperationResult.Failure("COMPARE_INVALID", "位置比较点、输出通道或脉宽不合法。"));
        }

        ComparePlan = plan;
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> RequestAllAxesStopAsync(MotionStopMode mode, CancellationToken cancellationToken) =>
        Task.FromResult(DeviceOperationResult.Success);

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private bool TryGetEnabled(AxisId axis, out AxisState state)
    {
        EnsureConnected();
        return _axes.TryGetValue(axis, out state!) && state.Enabled;
    }

    private void EnsureConnected()
    {
        if (State != DeviceConnectionState.Connected)
        {
            throw new InvalidOperationException("运动控制器未连接。");
        }
    }

    private sealed record AxisState(bool Enabled, bool Homed, double Position);
}
