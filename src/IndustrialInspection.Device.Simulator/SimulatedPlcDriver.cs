using IndustrialInspection.Domain;

namespace IndustrialInspection.Device.Simulator;

/// <summary>用于无硬件开发和断线故障注入的 PLC 模拟驱动。</summary>
public sealed class SimulatedPlcDriver : IPlcDriver
{
    private long _sequence;
    private long _commandAttempts;

    public DeviceIdentity Identity { get; } = new("PLC-SIM", "模拟 PLC", "Simulator", "S7-1500");
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;
    public int FailEveryReads { get; set; }
    public int FailEveryCommands { get; set; }
    public TimeSpan CommandProcessingDelay { get; set; }
    public short NextCommandStatusCode { get; set; }
    public long CommandAttempts => Interlocked.Read(ref _commandAttempts);
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

    /// <summary>模拟命令处理延时、业务拒绝和通讯失败，并记录成功执行的命令。</summary>
    public async Task WriteCommandAsync(PlcCommandRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (State != DeviceConnectionState.Connected)
        {
            throw new InvalidOperationException("模拟 PLC 未连接。");
        }

        var attempt = Interlocked.Increment(ref _commandAttempts);
        if (CommandProcessingDelay > TimeSpan.Zero)
        {
            await Task.Delay(CommandProcessingDelay, cancellationToken);
        }

        if (FailEveryCommands > 0 && attempt % FailEveryCommands == 0)
        {
            State = DeviceConnectionState.Faulted;
            throw new IOException("模拟 PLC 命令写入网络中断。");
        }

        if (NextCommandStatusCode != 0)
        {
            var statusCode = NextCommandStatusCode;
            NextCommandStatusCode = 0;
            throw new PlcCommandRejectedException(checked((int)attempt), statusCode);
        }

        Commands.Add(request);
    }

    /// <summary>模拟驱动没有非托管资源，因此直接完成释放。</summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
