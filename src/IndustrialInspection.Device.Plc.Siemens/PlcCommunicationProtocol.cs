namespace IndustrialInspection.Device.Plc.Siemens;

/// <summary>表示 PLC 对某个命令流水号返回的处理结果。</summary>
public sealed record PlcCommandAcknowledgement(int Sequence, bool Busy, bool Error, short StatusCode);

/// <summary>等待 PLC 命令流水号应答，并区分成功、业务拒绝和通信超时。</summary>
public static class PlcCommandAcknowledgementWaiter
{
    /// <summary>循环读取 PLC 应答，直到目标流水号完成或超时。</summary>
    /// <param name="expectedSequence">本次命令期望的流水号。</param>
    /// <param name="readAcknowledgement">读取 PLC 应答快照的异步委托。</param>
    /// <param name="timeout">允许 PLC 完成命令的最长时间。</param>
    /// <param name="pollInterval">两次应答读取之间的等待时间。</param>
    /// <param name="cancellationToken">取消等待的令牌。</param>
    public static async Task WaitAsync(
        int expectedSequence,
        Func<CancellationToken, Task<PlcCommandAcknowledgement>> readAcknowledgement,
        TimeSpan timeout,
        TimeSpan pollInterval,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(readAcknowledgement);
        if (expectedSequence <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expectedSequence), "PLC 命令流水号必须大于 0。");
        }

        if (pollInterval <= TimeSpan.Zero || timeout <= pollInterval)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "应答超时必须大于零并且大于轮询周期。");
        }

        using var timeoutSource = new CancellationTokenSource(timeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

        try
        {
            while (true)
            {
                var acknowledgement = await readAcknowledgement(linkedSource.Token);
                if (acknowledgement.Sequence == expectedSequence && !acknowledgement.Busy)
                {
                    if (acknowledgement.Error || acknowledgement.StatusCode != 0)
                    {
                        throw new PlcCommandRejectedException(expectedSequence, acknowledgement.StatusCode);
                    }

                    return;
                }

                await Task.Delay(pollInterval, linkedSource.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException($"等待 PLC 命令应答超时，流水号={expectedSequence}，超时={timeout.TotalMilliseconds:F0} ms。");
        }
    }
}

/// <summary>监视 PLC 心跳值是否持续变化，以发现会话存在但 PLC 程序已停止的情况。</summary>
public sealed class PlcHeartbeatMonitor(TimeSpan timeout)
{
    private int? _lastValue;
    private DateTimeOffset? _lastChangedAt;

    /// <summary>记录一次 PLC 心跳采样，并在心跳冻结超过阈值时抛出超时异常。</summary>
    /// <param name="value">本次读取的 PLC 心跳计数。</param>
    /// <param name="timestamp">本次采样时间。</param>
    public void Observe(int value, DateTimeOffset timestamp)
    {
        if (_lastValue != value || _lastChangedAt is null)
        {
            _lastValue = value;
            _lastChangedAt = timestamp;
            return;
        }

        if (timestamp - _lastChangedAt > timeout)
        {
            throw new TimeoutException($"PLC 心跳已冻结超过 {timeout.TotalMilliseconds:F0} ms，当前值={value}。");
        }
    }

    /// <summary>在断线或重新连接时清除旧会话的心跳基准。</summary>
    public void Reset()
    {
        _lastValue = null;
        _lastChangedAt = null;
    }
}
