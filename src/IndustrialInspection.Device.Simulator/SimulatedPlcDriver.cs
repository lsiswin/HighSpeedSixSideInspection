using IndustrialInspection.Domain;

namespace IndustrialInspection.Device.Simulator;

public sealed class SimulatedPlcDriver : IPlcDriver
{
    private long _sequence;

    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;
    public int FailEveryReads { get; set; }
    public IList<PlcCommandRequest> Commands { get; } = [];

    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        State = DeviceConnectionState.Connected;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public Task<MachineStatus> ReadMachineStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = Interlocked.Increment(ref _sequence);
        if (FailEveryReads > 0 && sequence % FailEveryReads == 0)
        {
            State = DeviceConnectionState.Faulted;
            throw new IOException("模拟 PLC 网络中断。");
        }

        State = DeviceConnectionState.Connected;
        return Task.FromResult(new MachineStatus(
            true, false, true, true, false, false, true, true,
            0.5f, 120f, DateTimeOffset.UtcNow, sequence));
    }

    public Task WriteCommandAsync(PlcCommandRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commands.Add(request);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

