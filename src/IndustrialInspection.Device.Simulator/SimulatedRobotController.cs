namespace IndustrialInspection.Device.Simulator;

/// <summary>模拟机械臂远程握手和程序状态转换。</summary>
public sealed class SimulatedRobotController(DeviceIdentity identity) : IRobotController
{
    private RobotStatus _status = new(
        RobotOperatingMode.Remote, RobotRunState.Disconnected, true, false, null, null, DateTimeOffset.UtcNow);

    public DeviceIdentity Identity { get; } = identity;
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;

    /// <summary>连接模拟机械臂并进入就绪状态。</summary>
    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Connected;
        _status = _status with { RunState = RobotRunState.Ready, Timestamp = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    /// <summary>断开模拟机械臂并清除运行状态。</summary>
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Disconnected;
        _status = _status with { RunState = RobotRunState.Disconnected, Timestamp = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    /// <summary>读取模拟机械臂当前状态。</summary>
    public Task<RobotStatus> ReadStatusAsync(CancellationToken cancellationToken) => Task.FromResult(_status);

    /// <summary>清除模拟故障并恢复就绪状态。</summary>
    public Task<DeviceOperationResult> ResetFaultAsync(CancellationToken cancellationToken)
    {
        _status = _status with { RunState = RobotRunState.Ready, FaultCode = null, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>在机器人已连接且无故障时开启模拟伺服。</summary>
    public Task<DeviceOperationResult> ServoOnAsync(CancellationToken cancellationToken)
    {
        if (_status.RunState is RobotRunState.Disconnected or RobotRunState.Faulted)
        {
            return Task.FromResult(DeviceOperationResult.Failure("ROBOT_NOT_READY", "机器人未连接或存在故障。"));
        }

        _status = _status with { ServoOn = true, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>关闭模拟伺服并把机器人置为未就绪。</summary>
    public Task<DeviceOperationResult> ServoOffAsync(CancellationToken cancellationToken)
    {
        _status = _status with { ServoOn = false, RunState = RobotRunState.NotReady, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>选择非空的机器人程序。</summary>
    public Task<DeviceOperationResult> SelectProgramAsync(RobotProgramRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProgramName))
        {
            return Task.FromResult(DeviceOperationResult.Failure("PROGRAM_REQUIRED", "程序名称不能为空。"));
        }

        _status = _status with { ActiveProgram = request.ProgramName, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>校验远程模式、伺服和程序互锁后启动模拟程序。</summary>
    public Task<DeviceOperationResult> StartProgramAsync(CancellationToken cancellationToken)
    {
        // 启动互锁放在驱动层再次校验，避免上层状态快照过期后误启动。
        if (!_status.ServoOn || string.IsNullOrWhiteSpace(_status.ActiveProgram) || _status.Mode != RobotOperatingMode.Remote)
        {
            return Task.FromResult(DeviceOperationResult.Failure("START_INTERLOCK", "启动需要远程模式、伺服开启并已选择程序。"));
        }

        _status = _status with { RunState = RobotRunState.Running, AtHome = false, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>停止模拟程序并回到就绪状态。</summary>
    public Task<DeviceOperationResult> StopProgramAsync(CancellationToken cancellationToken)
    {
        _status = _status with { RunState = RobotRunState.Ready, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>在伺服开启时移动到模拟原点。</summary>
    public Task<DeviceOperationResult> MoveHomeAsync(CancellationToken cancellationToken)
    {
        if (!_status.ServoOn)
        {
            return Task.FromResult(DeviceOperationResult.Failure("SERVO_OFF", "伺服未开启。"));
        }

        _status = _status with { AtHome = true, RunState = RobotRunState.Ready, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    /// <summary>模拟器无非托管资源，直接完成释放。</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
