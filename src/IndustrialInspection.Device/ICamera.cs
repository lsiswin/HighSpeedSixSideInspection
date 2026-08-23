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

/// <summary>描述相机分辨率、像素格式、曝光、增益、触发和网络包配置。</summary>
public sealed record CameraConfiguration(
    int Width,
    int Height,
    CameraPixelFormat PixelFormat,
    double ExposureMicroseconds,
    double GainDb,
    CameraTriggerMode TriggerMode,
    int PacketSize = 9000);

/// <summary>表示一帧需要显式释放的工业相机图像。</summary>
public sealed class CameraFrame : IDisposable
{
    private IMemoryOwner<byte>? _owner;

    /// <summary>创建带相机、产品和帧序号关联信息的图像帧。</summary>
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

    /// <summary>释放图像所有权并把底层缓冲区归还内存池。</summary>
    public void Dispose()
    {
        Interlocked.Exchange(ref _owner, null)?.Dispose();
    }
}

/// <summary>定义工业相机配置和异步图像采集能力。</summary>
public interface ICamera : IDeviceDriver
{
    CameraAcquisitionState AcquisitionState { get; }
    CameraConfiguration Configuration { get; }

    /// <summary>在非采集状态下更新相机参数。</summary>
    /// <param name="configuration">曝光、增益、触发和像素格式等配置。</param>
    /// <param name="cancellationToken">取消配置操作的令牌。</param>
    Task ConfigureAsync(CameraConfiguration configuration, CancellationToken cancellationToken);

    /// <summary>启动相机取流并准备接收硬件触发图像。</summary>
    /// <param name="cancellationToken">取消启动操作的令牌。</param>
    Task StartGrabbingAsync(CancellationToken cancellationToken);

    /// <summary>停止相机取流但保持设备连接。</summary>
    /// <param name="cancellationToken">取消停止操作的令牌。</param>
    Task StopGrabbingAsync(CancellationToken cancellationToken);

    /// <summary>异步枚举相机已经采集到的图像帧。</summary>
    /// <param name="cancellationToken">停止枚举的令牌。</param>
    /// <returns>持续产生的图像帧流，每帧使用后必须释放。</returns>
    /// <remarks>硬件触发由运动控制器产生，本方法不得用 Windows 定时器模拟生产触发。</remarks>
    IAsyncEnumerable<CameraFrame> GetFramesAsync(CancellationToken cancellationToken);
}
