using IndustrialInspection.Device;
using IndustrialInspection.Device.Plc.Siemens;
using IndustrialInspection.Device.Simulator;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .AddEnvironmentVariables("INSPECTION_")
    .Build();

var useSimulator = args.Contains("--simulator", StringComparer.OrdinalIgnoreCase);
var duration = ParseDuration(args) ?? TimeSpan.FromHours(8);
var options = configuration.GetSection(SiemensPlcOptions.SectionName).Get<SiemensPlcOptions>() ?? new();

await using IPlcDriver driver = useSimulator
    ? new SimulatedPlcDriver()
    : new SiemensPlcDriver(options);

var polling = new PlcPollingLoop(driver, TimeSpan.FromMilliseconds(50), TimeSpan.FromSeconds(2));
polling.StatusReceived += (_, status) =>
{
    if (status.Sequence % 20 == 0)
    {
        Console.WriteLine($"{status.Timestamp:O} seq={status.Sequence} ready={status.Ready} running={status.Running} speed={status.Speed:F1}");
    }
};

using var cancellation = new CancellationTokenSource(duration);
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

Console.WriteLine($"PLC PoC 启动：模式={(useSimulator ? "Simulator" : "S7-1500")}, 计划时长={duration}。");
await polling.RunAsync(cancellation.Token);
Console.WriteLine($"PLC PoC 结束：{polling.Health}");
return polling.Health.FailedReads == 0 ? 0 : 2;

static TimeSpan? ParseDuration(string[] arguments)
{
    var index = Array.FindIndex(arguments, value => value.Equals("--duration", StringComparison.OrdinalIgnoreCase));
    return index >= 0 && index + 1 < arguments.Length && TimeSpan.TryParse(arguments[index + 1], out var value)
        ? value
        : null;
}

