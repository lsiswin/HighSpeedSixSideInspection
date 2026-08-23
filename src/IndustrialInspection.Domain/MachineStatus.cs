namespace IndustrialInspection.Domain;

/// <summary>PLC 发布给上位机的设备状态快照。</summary>
public sealed record MachineStatus(
    bool AutoMode,
    bool ManualMode,
    bool Running,
    bool Ready,
    bool Fault,
    bool EStop,
    bool SafetyDoor,
    bool MaterialReady,
    float CycleTime,
    float Speed,
    DateTimeOffset Timestamp,
    long Sequence);

public enum MachineCommand
{
    Start,
    Stop,
    Reset,
    Auto,
    Manual,
    RecipeChange
}

public sealed record PlcCommandRequest(MachineCommand Command, int? RecipeId = null);

