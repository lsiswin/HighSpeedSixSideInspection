using IndustrialInspection.Domain;

namespace IndustrialInspection.Device;

public enum DeviceConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Faulted
}

public sealed record PlcDriverHealth(
    DeviceConnectionState State,
    DateTimeOffset? LastSuccessfulRead,
    long SuccessfulReads,
    long FailedReads,
    long Reconnects,
    string? LastError);

public interface IPlcDriver : IAsyncDisposable
{
    DeviceConnectionState State { get; }

    Task ConnectAsync(CancellationToken cancellationToken);

    Task DisconnectAsync(CancellationToken cancellationToken);

    Task<MachineStatus> ReadMachineStatusAsync(CancellationToken cancellationToken);

    // 命令写入必须由应用服务调用，UI 不得直接访问 PLC 驱动。
    Task WriteCommandAsync(PlcCommandRequest request, CancellationToken cancellationToken);
}

