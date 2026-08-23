namespace IndustrialInspection.Device;

public enum RobotOperatingMode
{
    Manual,
    Automatic,
    Remote
}

public enum RobotRunState
{
    Disconnected,
    NotReady,
    Ready,
    Running,
    Completed,
    Faulted
}

/// <summary>描述机械臂模式、运行状态、原点、伺服和故障快照。</summary>
public sealed record RobotStatus(
    RobotOperatingMode Mode,
    RobotRunState RunState,
    bool AtHome,
    bool ServoOn,
    string? ActiveProgram,
    string? FaultCode,
    DateTimeOffset Timestamp);

/// <summary>描述需要选择的机器人程序及其字符串参数。</summary>
public sealed record RobotProgramRequest(string ProgramName, IReadOnlyDictionary<string, string>? Parameters = null);

/// <summary>定义机械臂远程程序控制和状态读取能力。</summary>
public interface IRobotController : IDeviceDriver
{
    /// <summary>读取机械臂当前模式、运行状态和故障信息。</summary>
    Task<RobotStatus> ReadStatusAsync(CancellationToken cancellationToken);

    /// <summary>在安全条件满足时请求复位机器人故障。</summary>
    Task<DeviceOperationResult> ResetFaultAsync(CancellationToken cancellationToken);

    /// <summary>请求开启机器人伺服，真实实现必须校验控制柜互锁。</summary>
    Task<DeviceOperationResult> ServoOnAsync(CancellationToken cancellationToken);

    /// <summary>请求关闭机器人伺服。</summary>
    Task<DeviceOperationResult> ServoOffAsync(CancellationToken cancellationToken);

    /// <summary>选择允许远程执行的机器人程序和参数。</summary>
    Task<DeviceOperationResult> SelectProgramAsync(RobotProgramRequest request, CancellationToken cancellationToken);

    /// <summary>启动已选择的机器人程序。</summary>
    Task<DeviceOperationResult> StartProgramAsync(CancellationToken cancellationToken);

    /// <summary>请求停止当前机器人程序。</summary>
    Task<DeviceOperationResult> StopProgramAsync(CancellationToken cancellationToken);

    /// <summary>请求机器人移动到已配置的安全原点。</summary>
    Task<DeviceOperationResult> MoveHomeAsync(CancellationToken cancellationToken);
}
