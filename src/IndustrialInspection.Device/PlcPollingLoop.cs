using IndustrialInspection.Domain;

namespace IndustrialInspection.Device;

/// <summary>提供可测试的 PLC 轮询、断线重连和运行统计。</summary>
public sealed class PlcPollingLoop(IPlcDriver driver, TimeSpan pollInterval, TimeSpan reconnectDelay)
{
    private long _successfulReads;
    private long _failedReads;
    private long _reconnects;
    private DateTimeOffset? _lastSuccessfulRead;
    private string? _lastError;

    public event EventHandler<MachineStatus>? StatusReceived;

    public PlcDriverHealth Health => new(
        driver.State,
        _lastSuccessfulRead,
        Interlocked.Read(ref _successfulReads),
        Interlocked.Read(ref _failedReads),
        Interlocked.Read(ref _reconnects),
        _lastError);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(pollInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (driver.State != DeviceConnectionState.Connected)
                {
                    await driver.ConnectAsync(cancellationToken);
                    Interlocked.Increment(ref _reconnects);
                }

                var status = await driver.ReadMachineStatusAsync(cancellationToken);
                _lastSuccessfulRead = DateTimeOffset.UtcNow;
                _lastError = null;
                Interlocked.Increment(ref _successfulReads);
                StatusReceived?.Invoke(this, status);
                await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _lastError = exception.Message;
                Interlocked.Increment(ref _failedReads);
                await SafeDisconnectAsync();
                try
                {
                    await Task.Delay(reconnectDelay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task SafeDisconnectAsync()
    {
        try
        {
            await driver.DisconnectAsync(CancellationToken.None);
        }
        catch
        {
            // 断线清理失败不能终止重连循环，具体异常由驱动日志记录。
        }
    }
}
