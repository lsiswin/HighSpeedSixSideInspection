namespace IndustrialInspection.Device.Simulator;

public sealed class SimulatedRobotController(DeviceIdentity identity) : IRobotController
{
    private RobotStatus _status = new(
        RobotOperatingMode.Remote, RobotRunState.Disconnected, true, false, null, null, DateTimeOffset.UtcNow);

    public DeviceIdentity Identity { get; } = identity;
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Connected;
        _status = _status with { RunState = RobotRunState.Ready, Timestamp = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Disconnected;
        _status = _status with { RunState = RobotRunState.Disconnected, Timestamp = DateTimeOffset.UtcNow };
        return Task.CompletedTask;
    }

    public Task<RobotStatus> ReadStatusAsync(CancellationToken cancellationToken) => Task.FromResult(_status);

    public Task<DeviceOperationResult> ResetFaultAsync(CancellationToken cancellationToken)
    {
        _status = _status with { RunState = RobotRunState.Ready, FaultCode = null, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> ServoOnAsync(CancellationToken cancellationToken)
    {
        if (_status.RunState is RobotRunState.Disconnected or RobotRunState.Faulted)
        {
            return Task.FromResult(DeviceOperationResult.Failure("ROBOT_NOT_READY", "机器人未连接或存在故障。"));
        }

        _status = _status with { ServoOn = true, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> ServoOffAsync(CancellationToken cancellationToken)
    {
        _status = _status with { ServoOn = false, RunState = RobotRunState.NotReady, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> SelectProgramAsync(RobotProgramRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ProgramName))
        {
            return Task.FromResult(DeviceOperationResult.Failure("PROGRAM_REQUIRED", "程序名称不能为空。"));
        }

        _status = _status with { ActiveProgram = request.ProgramName, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> StartProgramAsync(CancellationToken cancellationToken)
    {
        if (!_status.ServoOn || string.IsNullOrWhiteSpace(_status.ActiveProgram) || _status.Mode != RobotOperatingMode.Remote)
        {
            return Task.FromResult(DeviceOperationResult.Failure("START_INTERLOCK", "启动需要远程模式、伺服开启并已选择程序。"));
        }

        _status = _status with { RunState = RobotRunState.Running, AtHome = false, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> StopProgramAsync(CancellationToken cancellationToken)
    {
        _status = _status with { RunState = RobotRunState.Ready, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public Task<DeviceOperationResult> MoveHomeAsync(CancellationToken cancellationToken)
    {
        if (!_status.ServoOn)
        {
            return Task.FromResult(DeviceOperationResult.Failure("SERVO_OFF", "伺服未开启。"));
        }

        _status = _status with { AtHome = true, RunState = RobotRunState.Ready, Timestamp = DateTimeOffset.UtcNow };
        return Task.FromResult(DeviceOperationResult.Success);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

