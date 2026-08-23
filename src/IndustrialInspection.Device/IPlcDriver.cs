using IndustrialInspection.Domain;

namespace IndustrialInspection.Device;

/// <summary>设备连接生命周期状态。</summary>
public enum DeviceConnectionState
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Faulted
}

/// <summary>PLC 通讯循环的累计健康统计。</summary>
public sealed record PlcDriverHealth(
    DeviceConnectionState State,
    DateTimeOffset? LastSuccessfulRead,
    long SuccessfulReads,
    long FailedReads,
    long Reconnects,
    string? LastError);

/// <summary>表示 PLC 通讯正常，但因设备互锁或业务条件拒绝执行命令。</summary>
public sealed class PlcCommandRejectedException(int sequence, short statusCode)
    : InvalidOperationException($"PLC 拒绝命令，流水号={sequence}，状态码={statusCode}。")
{
    public int Sequence { get; } = sequence;
    public short StatusCode { get; } = statusCode;
}

/// <summary>定义 PLC 状态读取和受控命令写入能力。</summary>
public interface IPlcDriver : IDeviceDriver
{
    /// <summary>读取一份带时间戳和序号的完整设备状态快照。</summary>
    /// <param name="cancellationToken">取消读取操作的令牌。</param>
    /// <returns>PLC 当前设备状态。</returns>
    Task<MachineStatus> ReadMachineStatusAsync(CancellationToken cancellationToken);

    /// <summary>向 PLC 写入经过应用层校验的控制命令。</summary>
    /// <param name="request">命令类型及可选参数。</param>
    /// <param name="cancellationToken">取消写入操作的令牌。</param>
    /// <remarks>命令写入必须由应用服务调用，UI 不得直接访问 PLC 驱动。</remarks>
    Task WriteCommandAsync(PlcCommandRequest request, CancellationToken cancellationToken);
}
