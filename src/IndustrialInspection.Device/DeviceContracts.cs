namespace IndustrialInspection.Device;

public sealed record DeviceIdentity(string Id, string Name, string Vendor, string Model);

public interface IDeviceDriver : IAsyncDisposable
{
    DeviceIdentity Identity { get; }
    DeviceConnectionState State { get; }

    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}

public sealed record DeviceOperationResult(bool Succeeded, string? ErrorCode = null, string? Message = null)
{
    public static DeviceOperationResult Success { get; } = new(true);

    public static DeviceOperationResult Failure(string errorCode, string message) =>
        new(false, errorCode, message);
}

