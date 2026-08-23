using IndustrialInspection.Domain;
using S7.Net;
using S7Plc = S7.Net.Plc;

namespace IndustrialInspection.Device.Plc.Siemens;

public sealed class SiemensPlcDriver(SiemensPlcOptions options) : IPlcDriver
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private S7Plc? _plc;
    private long _sequence;

    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (State == DeviceConnectionState.Connected && _plc?.IsConnected == true)
            {
                return;
            }

            State = State == DeviceConnectionState.Disconnected
                ? DeviceConnectionState.Connecting
                : DeviceConnectionState.Reconnecting;
            _plc?.Close();
            _plc = new S7Plc(CpuType.S71500, options.IpAddress, options.Rack, options.Slot);
            await _plc.OpenAsync(cancellationToken);
            State = DeviceConnectionState.Connected;
        }
        catch
        {
            State = DeviceConnectionState.Faulted;
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _plc?.Close();
            _plc = null;
            State = DeviceConnectionState.Disconnected;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<MachineStatus> ReadMachineStatusAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var plc = RequireConnected();
            var points = options.Points;

            // PoC 首先保证语义正确；点表冻结后再合并为批量连续读取以降低报文数。
            return new(
                await ReadBoolAsync(plc, points.AutoMode, cancellationToken),
                await ReadBoolAsync(plc, points.ManualMode, cancellationToken),
                await ReadBoolAsync(plc, points.Running, cancellationToken),
                await ReadBoolAsync(plc, points.Ready, cancellationToken),
                await ReadBoolAsync(plc, points.Fault, cancellationToken),
                await ReadBoolAsync(plc, points.EStop, cancellationToken),
                await ReadBoolAsync(plc, points.SafetyDoor, cancellationToken),
                await ReadBoolAsync(plc, points.MaterialReady, cancellationToken),
                await ReadFloatAsync(plc, points.CycleTime, cancellationToken),
                await ReadFloatAsync(plc, points.Speed, cancellationToken),
                DateTimeOffset.UtcNow,
                Interlocked.Increment(ref _sequence));
        }
        catch
        {
            State = DeviceConnectionState.Faulted;
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task WriteCommandAsync(PlcCommandRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var plc = RequireConnected();
            if (request.Command == MachineCommand.RecipeChange)
            {
                if (request.RecipeId is null or < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(request), "配方切换必须提供非负 RecipeId。");
                }

                await plc.WriteAsync(options.Points.RecipeId, request.RecipeId.Value, cancellationToken);
            }

            var commandAddress = GetCommandAddress(request.Command);
            await plc.WriteAsync(commandAddress, true, cancellationToken);
            try
            {
                await Task.Delay(options.CommandPulseMilliseconds, cancellationToken);
            }
            finally
            {
                // 即使调用被取消，也尽最大努力复位命令位，避免命令永久保持。
                await plc.WriteAsync(commandAddress, false, CancellationToken.None);
            }
        }
        catch
        {
            State = DeviceConnectionState.Faulted;
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None);
        _gate.Dispose();
    }

    private S7Plc RequireConnected()
    {
        if (_plc is null || !_plc.IsConnected)
        {
            throw new InvalidOperationException("PLC 未连接。");
        }

        return _plc;
    }

    private string GetCommandAddress(MachineCommand command) => command switch
    {
        MachineCommand.Start => options.Points.Start,
        MachineCommand.Stop => options.Points.Stop,
        MachineCommand.Reset => options.Points.Reset,
        MachineCommand.Auto => options.Points.Auto,
        MachineCommand.Manual => options.Points.Manual,
        MachineCommand.RecipeChange => options.Points.RecipeChange,
        _ => throw new ArgumentOutOfRangeException(nameof(command), command, null)
    };

    private static async Task<bool> ReadBoolAsync(S7Plc plc, string address, CancellationToken cancellationToken) =>
        Convert.ToBoolean(await plc.ReadAsync(address, cancellationToken));

    private static async Task<float> ReadFloatAsync(S7Plc plc, string address, CancellationToken cancellationToken) =>
        Convert.ToSingle(await plc.ReadAsync(address, cancellationToken));
}
