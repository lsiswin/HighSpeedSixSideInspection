using System.Buffers;

namespace IndustrialInspection.Device;

public enum CameraAcquisitionState
{
    Closed,
    Open,
    Grabbing,
    Faulted
}

public enum CameraTriggerMode
{
    Continuous,
    Software,
    HardwareLine0
}

public enum CameraPixelFormat
{
    Mono8,
    BayerRG8,
    Rgb8
}

public sealed record CameraConfiguration(
    int Width,
    int Height,
    CameraPixelFormat PixelFormat,
    double ExposureMicroseconds,
    double GainDb,
    CameraTriggerMode TriggerMode,
    int PacketSize = 9000);

public sealed class CameraFrame : IDisposable
{
    private IMemoryOwner<byte>? _owner;

    public CameraFrame(
        string cameraId,
        long frameId,
        string? productId,
        DateTimeOffset timestamp,
        int width,
        int height,
        CameraPixelFormat pixelFormat,
        IMemoryOwner<byte> owner,
        int length)
    {
        CameraId = cameraId;
        FrameId = frameId;
        ProductId = productId;
        Timestamp = timestamp;
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;
        _owner = owner;
        Length = length;
    }

    public string CameraId { get; }
    public long FrameId { get; }
    public string? ProductId { get; }
    public DateTimeOffset Timestamp { get; }
    public int Width { get; }
    public int Height { get; }
    public CameraPixelFormat PixelFormat { get; }
    public int Length { get; }

    public ReadOnlyMemory<byte> Data => _owner?.Memory[..Length]
        ?? throw new ObjectDisposedException(nameof(CameraFrame));

    // 图像处理完成后必须释放帧，真实相机驱动才能把缓冲区归还池中。
    public void Dispose()
    {
        Interlocked.Exchange(ref _owner, null)?.Dispose();
    }
}

public interface ICamera : IDeviceDriver
{
    CameraAcquisitionState AcquisitionState { get; }
    CameraConfiguration Configuration { get; }

    Task ConfigureAsync(CameraConfiguration configuration, CancellationToken cancellationToken);
    Task StartGrabbingAsync(CancellationToken cancellationToken);
    Task StopGrabbingAsync(CancellationToken cancellationToken);

    // 硬件触发由运动控制器产生；本接口只接收相机已经采集到的图像。
    IAsyncEnumerable<CameraFrame> GetFramesAsync(CancellationToken cancellationToken);
}

