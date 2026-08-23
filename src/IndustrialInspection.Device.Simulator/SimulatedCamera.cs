using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace IndustrialInspection.Device.Simulator;

/// <summary>提供有界帧队列和池化缓冲区行为的工业相机模拟器。</summary>
public sealed class SimulatedCamera(DeviceIdentity identity, CameraConfiguration configuration) : ICamera
{
    private readonly Channel<CameraFrame> _frames = Channel.CreateBounded<CameraFrame>(new BoundedChannelOptions(32)
    {
        FullMode = BoundedChannelFullMode.DropWrite,
        SingleWriter = false,
        SingleReader = false
    });

    public DeviceIdentity Identity { get; } = identity;
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;
    public CameraAcquisitionState AcquisitionState { get; private set; } = CameraAcquisitionState.Closed;
    public CameraConfiguration Configuration { get; private set; } = configuration;
    public long DroppedFrames { get; private set; }

    /// <summary>打开模拟相机并进入可配置状态。</summary>
    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        State = DeviceConnectionState.Connected;
        AcquisitionState = CameraAcquisitionState.Open;
        return Task.CompletedTask;
    }

    /// <summary>关闭模拟相机并结束采集状态。</summary>
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        AcquisitionState = CameraAcquisitionState.Closed;
        State = DeviceConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    /// <summary>验证并更新模拟相机配置，采集中拒绝修改。</summary>
    public Task ConfigureAsync(CameraConfiguration configuration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (AcquisitionState == CameraAcquisitionState.Grabbing)
        {
            throw new InvalidOperationException("采集中不能修改相机配置。");
        }

        if (configuration.Width <= 0 || configuration.Height <= 0 || configuration.ExposureMicroseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(configuration), "相机尺寸和曝光必须大于零。");
        }

        Configuration = configuration;
        return Task.CompletedTask;
    }

    /// <summary>启动模拟相机取流。</summary>
    public Task StartGrabbingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        AcquisitionState = CameraAcquisitionState.Grabbing;
        return Task.CompletedTask;
    }

    /// <summary>停止模拟相机取流但保持连接。</summary>
    public Task StopGrabbingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AcquisitionState = CameraAcquisitionState.Open;
        return Task.CompletedTask;
    }

    /// <summary>异步读取模拟器产生的图像帧。</summary>
    public async IAsyncEnumerable<CameraFrame> GetFramesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var frame in _frames.Reader.ReadAllAsync(cancellationToken))
        {
            yield return frame;
        }
    }

    /// <summary>向采集队列注入一帧测试图像。</summary>
    /// <param name="frameId">相机侧递增帧号。</param>
    /// <param name="productId">与图像绑定的产品唯一编号。</param>
    /// <param name="pixels">需要复制到池化缓冲区的像素数据。</param>
    /// <returns>成功入队返回 true；未采集或队列已满返回 false。</returns>
    public bool InjectFrame(long frameId, string? productId, ReadOnlySpan<byte> pixels)
    {
        if (AcquisitionState != CameraAcquisitionState.Grabbing)
        {
            return false;
        }

        // 模拟器也使用池化内存，以便尽早验证真实相机的资源释放纪律。
        var owner = MemoryPool<byte>.Shared.Rent(pixels.Length);
        pixels.CopyTo(owner.Memory.Span);
        var frame = new CameraFrame(
            Identity.Id, frameId, productId, DateTimeOffset.UtcNow,
            Configuration.Width, Configuration.Height, Configuration.PixelFormat,
            owner, pixels.Length);

        if (_frames.Writer.TryWrite(frame))
        {
            return true;
        }

        frame.Dispose();
        DroppedFrames++;
        return false;
    }

    /// <summary>完成帧通道并释放所有尚未被消费的图像缓冲区。</summary>
    public async ValueTask DisposeAsync()
    {
        _frames.Writer.TryComplete();
        while (_frames.Reader.TryRead(out var frame))
        {
            frame.Dispose();
        }

        await DisconnectAsync(CancellationToken.None);
    }

    /// <summary>确保相机已连接，避免在关闭状态开始采集。</summary>
    private void EnsureConnected()
    {
        if (State != DeviceConnectionState.Connected)
        {
            throw new InvalidOperationException("相机未连接。");
        }
    }
}
