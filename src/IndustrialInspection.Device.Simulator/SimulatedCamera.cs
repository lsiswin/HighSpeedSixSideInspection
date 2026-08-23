using System.Buffers;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace IndustrialInspection.Device.Simulator;

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

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        State = DeviceConnectionState.Connected;
        AcquisitionState = CameraAcquisitionState.Open;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        AcquisitionState = CameraAcquisitionState.Closed;
        State = DeviceConnectionState.Disconnected;
        return Task.CompletedTask;
    }

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

    public Task StartGrabbingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        AcquisitionState = CameraAcquisitionState.Grabbing;
        return Task.CompletedTask;
    }

    public Task StopGrabbingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AcquisitionState = CameraAcquisitionState.Open;
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<CameraFrame> GetFramesAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var frame in _frames.Reader.ReadAllAsync(cancellationToken))
        {
            yield return frame;
        }
    }

    public bool InjectFrame(long frameId, string? productId, ReadOnlySpan<byte> pixels)
    {
        if (AcquisitionState != CameraAcquisitionState.Grabbing)
        {
            return false;
        }

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

    public async ValueTask DisposeAsync()
    {
        _frames.Writer.TryComplete();
        while (_frames.Reader.TryRead(out var frame))
        {
            frame.Dispose();
        }

        await DisconnectAsync(CancellationToken.None);
    }

    private void EnsureConnected()
    {
        if (State != DeviceConnectionState.Connected)
        {
            throw new InvalidOperationException("相机未连接。");
        }
    }
}

