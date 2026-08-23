namespace IndustrialInspection.Device.Plc.Siemens;

/// <summary>定义 Siemens S7-1500 连接参数和点位映射。</summary>
public sealed class SiemensPlcOptions
{
    public const string SectionName = "Devices:Plc";

    public string DeviceId { get; init; } = "PLC01";
    public string DeviceName { get; init; } = "主控制 PLC";
    public string IpAddress { get; init; } = "192.168.10.20";
    public short Rack { get; init; }
    public short Slot { get; init; } = 1;
    public int CommandPulseMilliseconds { get; init; } = 100;
    public bool EnableProtocolVersionCheck { get; init; } = true;
    public ushort ProtocolVersion { get; init; } = 1;
    public bool EnableCommandAcknowledgement { get; init; }
    public int CommandAcknowledgementTimeoutMilliseconds { get; init; } = 2_000;
    public int CommandAcknowledgementPollMilliseconds { get; init; } = 20;
    public bool EnableHeartbeatMonitoring { get; init; }
    public int HeartbeatWriteIntervalMilliseconds { get; init; } = 1_000;
    public int HeartbeatTimeoutMilliseconds { get; init; } = 5_000;
    public SiemensPlcPointMap Points { get; init; } = new();
}

/// <summary>定义 DB100/DB101 V1 通信点表；现场下载前仍需在 TIA Portal 中核对实际偏移。</summary>
public sealed class SiemensPlcPointMap
{
    public string AutoMode { get; init; } = "DB101.DBX0.0";
    public string ManualMode { get; init; } = "DB101.DBX0.1";
    public string Running { get; init; } = "DB101.DBX0.2";
    public string Ready { get; init; } = "DB101.DBX0.3";
    public string Fault { get; init; } = "DB101.DBX0.4";
    public string EStop { get; init; } = "DB101.DBX0.5";
    public string SafetyDoor { get; init; } = "DB101.DBX0.6";
    public string MaterialReady { get; init; } = "DB101.DBX0.7";
    public string CycleTime { get; init; } = "DB101.DBD4";
    public string Speed { get; init; } = "DB101.DBD8";
    public string Start { get; init; } = "DB100.DBX0.0";
    public string Stop { get; init; } = "DB100.DBX0.1";
    public string Reset { get; init; } = "DB100.DBX0.2";
    public string Auto { get; init; } = "DB100.DBX0.3";
    public string Manual { get; init; } = "DB100.DBX0.4";
    public string RecipeChange { get; init; } = "DB100.DBX0.5";
    public string RecipeId { get; init; } = "DB100.DBD4";
    public string CommandSequence { get; init; } = "DB100.DBD8";
    public string PcHeartbeat { get; init; } = "DB100.DBD12";
    public string ProtocolVersionToPlc { get; init; } = "DB100.DBW16";
    public string PcReady { get; init; } = "DB100.DBX18.0";
    public string CommandAckSequence { get; init; } = "DB101.DBD12";
    public string PlcHeartbeat { get; init; } = "DB101.DBD24";
    public string ProtocolVersionFromPlc { get; init; } = "DB101.DBW28";
    public string CommandBusy { get; init; } = "DB101.DBX30.0";
    public string CommandError { get; init; } = "DB101.DBX30.1";
    public string LastCommandStatus { get; init; } = "DB101.DBW32";
}

/// <summary>在建立真实 PLC 连接前验证连接参数和点位映射。</summary>
public static class SiemensPlcOptionsValidator
{
    /// <summary>验证 IP、机架、槽位、脉冲时长以及所有点位地址。</summary>
    /// <param name="options">需要验证的 Siemens PLC 配置。</param>
    /// <exception cref="ArgumentException">配置存在空值、重复地址或越界参数时抛出。</exception>
    public static void Validate(SiemensPlcOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!System.Net.IPAddress.TryParse(options.IpAddress, out _))
        {
            throw new ArgumentException("PLC IP 地址格式不合法。", nameof(options));
        }

        if (options.Rack < 0 || options.Slot < 0)
        {
            throw new ArgumentException("PLC Rack 和 Slot 不能为负数。", nameof(options));
        }

        if (options.CommandPulseMilliseconds is < 20 or > 2_000)
        {
            throw new ArgumentException("PLC 命令脉冲宽度必须在 20～2000 ms 之间。", nameof(options));
        }

        if (options.ProtocolVersion == 0)
        {
            throw new ArgumentException("PLC 通信协议版本不能为 0。", nameof(options));
        }

        if (options.CommandAcknowledgementPollMilliseconds is < 5 or > 1_000 ||
            options.CommandAcknowledgementTimeoutMilliseconds <= options.CommandAcknowledgementPollMilliseconds)
        {
            throw new ArgumentException("PLC 命令应答轮询周期或超时时间不合法。", nameof(options));
        }

        if (options.HeartbeatWriteIntervalMilliseconds is < 100 or > 60_000 ||
            options.HeartbeatTimeoutMilliseconds <= options.HeartbeatWriteIntervalMilliseconds * 2)
        {
            throw new ArgumentException("PLC 心跳写入周期或超时时间不合法。", nameof(options));
        }

        var addresses = GetAddresses(options.Points).ToArray();
        if (addresses.Any(item => string.IsNullOrWhiteSpace(item.Address)))
        {
            throw new ArgumentException("PLC 点位地址不能为空。", nameof(options));
        }

        var duplicate = addresses
            .GroupBy(item => item.Address, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new ArgumentException($"PLC 点位地址重复：{duplicate.Key}。", nameof(options));
        }
    }

    /// <summary>按配置字段名称枚举全部状态和命令点位。</summary>
    /// <param name="points">PLC 点位映射。</param>
    /// <returns>字段名称和 S7 地址组成的序列。</returns>
    private static IEnumerable<(string Name, string Address)> GetAddresses(SiemensPlcPointMap points)
    {
        yield return (nameof(points.AutoMode), points.AutoMode);
        yield return (nameof(points.ManualMode), points.ManualMode);
        yield return (nameof(points.Running), points.Running);
        yield return (nameof(points.Ready), points.Ready);
        yield return (nameof(points.Fault), points.Fault);
        yield return (nameof(points.EStop), points.EStop);
        yield return (nameof(points.SafetyDoor), points.SafetyDoor);
        yield return (nameof(points.MaterialReady), points.MaterialReady);
        yield return (nameof(points.CycleTime), points.CycleTime);
        yield return (nameof(points.Speed), points.Speed);
        yield return (nameof(points.Start), points.Start);
        yield return (nameof(points.Stop), points.Stop);
        yield return (nameof(points.Reset), points.Reset);
        yield return (nameof(points.Auto), points.Auto);
        yield return (nameof(points.Manual), points.Manual);
        yield return (nameof(points.RecipeChange), points.RecipeChange);
        yield return (nameof(points.RecipeId), points.RecipeId);
        yield return (nameof(points.CommandSequence), points.CommandSequence);
        yield return (nameof(points.PcHeartbeat), points.PcHeartbeat);
        yield return (nameof(points.ProtocolVersionToPlc), points.ProtocolVersionToPlc);
        yield return (nameof(points.PcReady), points.PcReady);
        yield return (nameof(points.CommandAckSequence), points.CommandAckSequence);
        yield return (nameof(points.PlcHeartbeat), points.PlcHeartbeat);
        yield return (nameof(points.ProtocolVersionFromPlc), points.ProtocolVersionFromPlc);
        yield return (nameof(points.CommandBusy), points.CommandBusy);
        yield return (nameof(points.CommandError), points.CommandError);
        yield return (nameof(points.LastCommandStatus), points.LastCommandStatus);
    }
}
