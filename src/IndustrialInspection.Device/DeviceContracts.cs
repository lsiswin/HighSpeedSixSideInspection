namespace IndustrialInspection.Device;

/// <summary>描述设备在系统中的稳定身份和厂商型号。</summary>
public sealed record DeviceIdentity(string Id, string Name, string Vendor, string Model);

/// <summary>定义所有设备驱动共同遵守的连接生命周期。</summary>
public interface IDeviceDriver : IAsyncDisposable
{
    DeviceIdentity Identity { get; }
    DeviceConnectionState State { get; }

    /// <summary>建立与设备的连接；重复调用必须保持幂等。</summary>
    /// <param name="cancellationToken">取消连接操作的令牌。</param>
    Task ConnectAsync(CancellationToken cancellationToken);

    /// <summary>断开设备连接并释放厂商 SDK 会话资源。</summary>
    /// <param name="cancellationToken">取消断开操作的令牌。</param>
    Task DisconnectAsync(CancellationToken cancellationToken);
}

/// <summary>表示不会通过异常表达的设备业务操作结果。</summary>
public sealed record DeviceOperationResult(bool Succeeded, string? ErrorCode = null, string? Message = null)
{
    public static DeviceOperationResult Success { get; } = new(true);

    /// <summary>创建包含稳定错误码和中文说明的失败结果。</summary>
    /// <param name="errorCode">供程序判断的稳定错误码。</param>
    /// <param name="message">供操作者阅读的中文错误说明。</param>
    /// <returns>失败的设备操作结果。</returns>
    public static DeviceOperationResult Failure(string errorCode, string message) =>
        new(false, errorCode, message);
}
