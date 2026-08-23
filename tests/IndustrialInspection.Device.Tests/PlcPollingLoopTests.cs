using IndustrialInspection.Device;
using IndustrialInspection.Device.Simulator;
using Xunit;

namespace IndustrialInspection.Device.Tests;

public sealed class PlcPollingLoopTests
{
    [Fact]
    public async Task Polling_loop_publishes_status()
    {
        await using var driver = new SimulatedPlcDriver();
        var loop = new PlcPollingLoop(driver, TimeSpan.FromMilliseconds(2), TimeSpan.FromMilliseconds(2));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        await loop.RunAsync(cancellation.Token);

        Assert.True(loop.Health.SuccessfulReads > 0);
        Assert.NotNull(loop.Health.LastSuccessfulRead);
    }

    [Fact]
    public async Task Polling_loop_recovers_after_transient_read_failure()
    {
        await using var driver = new SimulatedPlcDriver { FailEveryReads = 3 };
        var loop = new PlcPollingLoop(driver, TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await loop.RunAsync(cancellation.Token);

        Assert.True(loop.Health.FailedReads > 0);
        Assert.True(loop.Health.SuccessfulReads > loop.Health.FailedReads);
        Assert.True(loop.Health.Reconnects > 1);
    }
}
