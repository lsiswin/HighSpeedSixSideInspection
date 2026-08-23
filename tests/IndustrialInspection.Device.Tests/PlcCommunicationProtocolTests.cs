using IndustrialInspection.Device.Plc.Siemens;
using IndustrialInspection.Device.Simulator;
using IndustrialInspection.Domain;
using Xunit;

namespace IndustrialInspection.Device.Tests;

public sealed class PlcCommunicationProtocolTests
{
    /// <summary>验证应答等待器会忽略旧流水号和处理中状态，直到目标命令完成。</summary>
    [Fact]
    public async Task Command_waiter_ignores_stale_and_busy_acknowledgements()
    {
        var readCount = 0;

        await PlcCommandAcknowledgementWaiter.WaitAsync(
            7,
            _ => Task.FromResult(++readCount switch
            {
                1 => new PlcCommandAcknowledgement(6, false, false, 0),
                2 => new PlcCommandAcknowledgement(7, true, false, 0),
                _ => new PlcCommandAcknowledgement(7, false, false, 0)
            }),
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromMilliseconds(1),
            CancellationToken.None);

        Assert.Equal(3, readCount);
    }

    /// <summary>验证 PLC 返回非零结果码时抛出业务拒绝，并保留流水号和状态码。</summary>
    [Fact]
    public async Task Command_waiter_reports_business_rejection()
    {
        var exception = await Assert.ThrowsAsync<PlcCommandRejectedException>(() =>
            PlcCommandAcknowledgementWaiter.WaitAsync(
                9,
                _ => Task.FromResult(new PlcCommandAcknowledgement(9, false, true, 23)),
                TimeSpan.FromMilliseconds(200),
                TimeSpan.FromMilliseconds(1),
                CancellationToken.None));

        Assert.Equal(9, exception.Sequence);
        Assert.Equal(23, exception.StatusCode);
    }

    /// <summary>验证 PLC 一直不返回目标流水号时产生明确的应答超时。</summary>
    [Fact]
    public async Task Command_waiter_times_out_for_missing_acknowledgement()
    {
        await Assert.ThrowsAsync<TimeoutException>(() =>
            PlcCommandAcknowledgementWaiter.WaitAsync(
                11,
                _ => Task.FromResult(new PlcCommandAcknowledgement(10, false, false, 0)),
                TimeSpan.FromMilliseconds(20),
                TimeSpan.FromMilliseconds(1),
                CancellationToken.None));
    }

    /// <summary>验证 PLC 心跳冻结超过阈值后被识别，数值变化后重新开始计时。</summary>
    [Fact]
    public void Heartbeat_monitor_detects_frozen_plc_program()
    {
        var monitor = new PlcHeartbeatMonitor(TimeSpan.FromSeconds(5));
        var start = new DateTimeOffset(2026, 8, 23, 0, 0, 0, TimeSpan.Zero);

        monitor.Observe(100, start);
        monitor.Observe(100, start.AddSeconds(5));
        Assert.Throws<TimeoutException>(() => monitor.Observe(100, start.AddSeconds(5).AddMilliseconds(1)));

        monitor.Observe(101, start.AddSeconds(6));
        monitor.Observe(101, start.AddSeconds(10));
    }

    /// <summary>验证模拟 PLC 的业务拒绝不会把健康连接误标记为通讯故障。</summary>
    [Fact]
    public async Task Simulator_business_rejection_keeps_connection_healthy()
    {
        await using var driver = new SimulatedPlcDriver { NextCommandStatusCode = 31 };
        await driver.ConnectAsync(CancellationToken.None);

        var exception = await Assert.ThrowsAsync<PlcCommandRejectedException>(() =>
            driver.WriteCommandAsync(new PlcCommandRequest(MachineCommand.Start), CancellationToken.None));

        Assert.Equal(31, exception.StatusCode);
        Assert.Equal(DeviceConnectionState.Connected, driver.State);
        Assert.Empty(driver.Commands);
    }

    /// <summary>验证模拟 PLC 的命令网络故障会进入故障状态，供上层执行重连。</summary>
    [Fact]
    public async Task Simulator_command_network_failure_marks_driver_faulted()
    {
        await using var driver = new SimulatedPlcDriver { FailEveryCommands = 1 };
        await driver.ConnectAsync(CancellationToken.None);

        await Assert.ThrowsAsync<IOException>(() =>
            driver.WriteCommandAsync(new PlcCommandRequest(MachineCommand.Stop), CancellationToken.None));

        Assert.Equal(DeviceConnectionState.Faulted, driver.State);
    }
}
