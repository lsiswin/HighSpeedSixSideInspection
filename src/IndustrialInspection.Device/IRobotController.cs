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

public sealed record RobotStatus(
    RobotOperatingMode Mode,
    RobotRunState RunState,
    bool AtHome,
    bool ServoOn,
    string? ActiveProgram,
    string? FaultCode,
    DateTimeOffset Timestamp);

public sealed record RobotProgramRequest(string ProgramName, IReadOnlyDictionary<string, string>? Parameters = null);

public interface IRobotController : IDeviceDriver
{
    Task<RobotStatus> ReadStatusAsync(CancellationToken cancellationToken);
    Task<DeviceOperationResult> ResetFaultAsync(CancellationToken cancellationToken);
    Task<DeviceOperationResult> ServoOnAsync(CancellationToken cancellationToken);
    Task<DeviceOperationResult> ServoOffAsync(CancellationToken cancellationToken);
    Task<DeviceOperationResult> SelectProgramAsync(RobotProgramRequest request, CancellationToken cancellationToken);
    Task<DeviceOperationResult> StartProgramAsync(CancellationToken cancellationToken);
    Task<DeviceOperationResult> StopProgramAsync(CancellationToken cancellationToken);
    Task<DeviceOperationResult> MoveHomeAsync(CancellationToken cancellationToken);
}

