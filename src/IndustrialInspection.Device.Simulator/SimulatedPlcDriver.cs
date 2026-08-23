using IndustrialInspection.Domain;

namespace IndustrialInspection.Device.Simulator;

/// <summary>用于无硬件开发和断线故障注入的 PLC 模拟驱动。</summary>
public sealed class SimulatedPlcDriver : IPlcDriver
{
    private long _sequence;

    public DeviceIdentity Identity { get; } = new("PLC-SIM", "模拟 PLC", "Simulator", "S7-1500");
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;
    public int FailEveryReads { get; set; }
    public IList<PlcCommandRequest> Commands { get; } = [];

    /// <summary>把模拟 PLC 切换到已连接状态。</summary>
    public Task ConnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        State = DeviceConnectionState.Connected;
        return Task.CompletedTask;
    }

    /// <summary>把模拟 PLC 切换到未连接状态。</summary>
    public Task DisconnectAsync(CancellationToken cancellationToken)
    {
        State = DeviceConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    /// <summary>生成设备状态快照，并按配置周期注入网络异常。</summary>
    public Task<MachineStatus> ReadMachineStatusAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sequence = Interlocked.Increment(ref _sequence);
        // 使用确定性的第 N 次失败，确保重连测试可以稳定复现而不是依赖随机数。
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

    /// <summary>记录模拟命令，供应用层和单元测试验证调用顺序。</summary>
    public Task WriteCommandAsync(PlcCommandRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Commands.Add(request);
        return Task.CompletedTask;
    }

    /// <summary>模拟驱动没有非托管资源，因此直接完成释放。</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
