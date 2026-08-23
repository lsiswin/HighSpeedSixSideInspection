using System.Collections.Concurrent;

namespace IndustrialInspection.Device.Simulator;

/// <summary>模拟轴使能、回零、定位和位置比较配置的运动控制器。</summary>
public sealed class SimulatedMotionController(DeviceIdentity identity) : IMotionController
{
    private readonly ConcurrentDictionary<AxisId, AxisState> _axes = new();

    public DeviceIdentity Identity { get; } = identity;
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;
    public PositionComparePlan? ComparePlan { get; private set; }

    /// <summary>连接模拟运动控制器。</summary>
    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        State = DeviceConnectionState.Connected;
        return Task.CompletedTask;
    }

    /// <summary>断开模拟运动控制器。</summary>
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    /// <summary>使能指定模拟轴。</summary>
    public Task<DeviceOperationResult> EnableAxisAsync(AxisId axis, CancellationToken cancellationToken)
    {
        EnsureConnected();
        _axes.AddOrUpdate(axis, new AxisState(true, false, 0), (_, current) => current with { Enabled = true });
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>禁用指定模拟轴。</summary>
    public Task<DeviceOperationResult> DisableAxisAsync(AxisId axis, CancellationToken cancellationToken)
    {
        _axes.AddOrUpdate(axis, new AxisState(false, false, 0), (_, current) => current with { Enabled = false });
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>完成模拟回零并把当前位置设置为零。</summary>
    public Task<DeviceOperationResult> HomeAsync(AxisId axis, CancellationToken cancellationToken)
    {
        if (!TryGetEnabled(axis, out var current))
        {
            return Task.FromResult(DeviceOperationResult.Failure("AXIS_DISABLED", $"{axis} 未使能。"));
        }

        _axes[axis] = current with { Homed = true, Position = 0 };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>在轴已使能且已回零时执行模拟绝对定位。</summary>
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

    /// <summary>在轴已使能时接受模拟速度运动请求。</summary>
    public Task<DeviceOperationResult> MoveVelocityAsync(AxisId axis, double velocity, string unit, CancellationToken cancellationToken) =>
        Task.FromResult(TryGetEnabled(axis, out _)
            ? DeviceOperationResult.Success
            : DeviceOperationResult.Failure("AXIS_DISABLED", $"{axis} 未使能。"));

    /// <summary>接受指定模拟轴的停止请求。</summary>
    public Task<DeviceOperationResult> StopAsync(AxisId axis, MotionStopMode mode, CancellationToken cancellationToken) =>
        Task.FromResult(DeviceOperationResult.Success);

    /// <summary>读取模拟轴当前位置。</summary>
    public Task<double> ReadPositionAsync(AxisId axis, CancellationToken cancellationToken) =>
        Task.FromResult(_axes.TryGetValue(axis, out var state) ? state.Position : 0);

    /// <summary>验证并保存硬件位置比较模拟计划。</summary>
    public Task<DeviceOperationResult> ConfigurePositionCompareAsync(PositionComparePlan plan, CancellationToken cancellationToken)
    {
        if (plan.Points.Count == 0 || plan.Points.Any(point => point.PulseWidth <= TimeSpan.Zero || point.OutputChannel < 0))
        {
            return Task.FromResult(DeviceOperationResult.Failure("COMPARE_INVALID", "位置比较点、输出通道或脉宽不合法。"));
        }

        // 比较计划整体保存，后续 Product Tracking/Trigger 模块可验证 ProductId 与触发点映射。
        ComparePlan = plan;
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>接受全部模拟轴的软件停止请求。</summary>
    public Task<DeviceOperationResult> RequestAllAxesStopAsync(MotionStopMode mode, CancellationToken cancellationToken) =>
        Task.FromResult(DeviceOperationResult.Success);

    /// <summary>模拟器无非托管资源，直接完成释放。</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    /// <summary>检查轴是否存在且已使能。</summary>
    private bool TryGetEnabled(AxisId axis, out AxisState state)
    {
        EnsureConnected();
        return _axes.TryGetValue(axis, out state!) && state.Enabled;
    }

    /// <summary>确保运动控制器处于连接状态。</summary>
    private void EnsureConnected()
    {
        if (State != DeviceConnectionState.Connected)
        {
            throw new InvalidOperationException("运动控制器未连接。");
        }
    }

    private sealed record AxisState(bool Enabled, bool Homed, double Position);
}
