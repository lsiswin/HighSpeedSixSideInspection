namespace IndustrialInspection.Device.Plc.Siemens;

public sealed class SiemensPlcOptions
{
    public const string SectionName = "Devices:Plc";

    public string IpAddress { get; init; } = "192.168.10.20";
    public short Rack { get; init; }
    public short Slot { get; init; } = 1;
    public int CommandPulseMilliseconds { get; init; } = 100;
    public SiemensPlcPointMap Points { get; init; } = new();
}

/// <summary>地址为 PoC 占位，必须在 PLC 点表冻结后由自动化工程师确认。</summary>
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
    public string CycleTime { get; init; } = "DB101.DBD2";
    public string Speed { get; init; } = "DB101.DBD6";
    public string Start { get; init; } = "DB100.DBX0.0";
    public string Stop { get; init; } = "DB100.DBX0.1";
    public string Reset { get; init; } = "DB100.DBX0.2";
    public string Auto { get; init; } = "DB100.DBX0.3";
    public string Manual { get; init; } = "DB100.DBX0.4";
    public string RecipeChange { get; init; } = "DB100.DBX0.5";
    public string RecipeId { get; init; } = "DB100.DBD2";
}

