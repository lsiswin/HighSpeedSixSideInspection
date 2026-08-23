using IndustrialInspection.Domain;
using S7.Net;
using S7.Net.Types;
using S7Plc = S7.Net.Plc;

namespace IndustrialInspection.Device.Plc.Siemens;

/// <summary>使用 S7.NetPlus 实现 Siemens S7-1500 通讯的 PLC 驱动。</summary>
public sealed class SiemensPlcDriver(SiemensPlcOptions options) : IPlcDriver
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private S7Plc? _plc;
    private long _sequence;

    public DeviceIdentity Identity { get; } = new(options.DeviceId, options.DeviceName, "Siemens", "S7-1500");
    public DeviceConnectionState State { get; private set; } = DeviceConnectionState.Disconnected;

    /// <summary>串行建立 PLC 连接，避免多个调用同时创建 S7 会话。</summary>
    /// <param name="cancellationToken">取消连接操作的令牌。</param>
    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        SiemensPlcOptionsValidator.Validate(options);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (State == DeviceConnectionState.Connected && _plc?.IsConnected == true)
            {
                return;
            }

            // 连接状态明确区分首次连接和故障重连，便于 HMI 显示和诊断统计。
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

    /// <summary>关闭 S7 会话并把驱动恢复为未连接状态。</summary>
    /// <param name="cancellationToken">取消断开操作的令牌。</param>
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

    /// <summary>通过一次批量变量请求读取完整设备状态快照。</summary>
    /// <param name="cancellationToken">取消读取操作的令牌。</param>
    /// <returns>带采集时间和递增序号的设备状态。</returns>
    public async Task<MachineStatus> ReadMachineStatusAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var plc = RequireConnected();
            var items = CreateStatusReadItems(options.Points);

            // 将同周期字段合并到一次 S7 多变量请求，避免 50 ms 周期内连续发送十个独立报文。
            var values = await plc.ReadMultipleVarsAsync(items, cancellationToken);
            return new(
                Convert.ToBoolean(values[0].Value),
                Convert.ToBoolean(values[1].Value),
                Convert.ToBoolean(values[2].Value),
                Convert.ToBoolean(values[3].Value),
                Convert.ToBoolean(values[4].Value),
                Convert.ToBoolean(values[5].Value),
                Convert.ToBoolean(values[6].Value),
                Convert.ToBoolean(values[7].Value),
                Convert.ToSingle(values[8].Value),
                Convert.ToSingle(values[9].Value),
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

    /// <summary>写入配方参数并产生一次受控命令脉冲。</summary>
    /// <param name="request">需要下发的 PLC 命令。</param>
    /// <param name="cancellationToken">取消写入操作的令牌。</param>
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
            // 同一信号的置位和复位必须处于同一互斥区，防止两个命令脉冲相互覆盖。
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

    /// <summary>关闭 PLC 会话并释放驱动内部的互斥资源。</summary>
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync(CancellationToken.None);
        _gate.Dispose();
    }

    /// <summary>返回已连接的 S7 会话，否则抛出明确的状态异常。</summary>
    /// <returns>当前有效的 S7.NetPlus PLC 对象。</returns>
    private S7Plc RequireConnected()
    {
        if (_plc is null || !_plc.IsConnected)
        {
            throw new InvalidOperationException("PLC 未连接。");
        }

        return _plc;
    }

    /// <summary>把领域命令映射到经过配置的 PLC 点位地址。</summary>
    /// <param name="command">领域层定义的设备命令。</param>
    /// <returns>对应的 S7 地址字符串。</returns>
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

    /// <summary>按固定字段顺序创建一次设备状态批量读取清单。</summary>
    /// <param name="points">已经冻结或处于 PoC 的 PLC 点位映射。</param>
    /// <returns>顺序与 <see cref="MachineStatus"/> 构造参数一致的 S7 数据项。</returns>
    private static List<DataItem> CreateStatusReadItems(SiemensPlcPointMap points) =>
    [
        DataItem.FromAddress(points.AutoMode),
        DataItem.FromAddress(points.ManualMode),
        DataItem.FromAddress(points.Running),
        DataItem.FromAddress(points.Ready),
        DataItem.FromAddress(points.Fault),
        DataItem.FromAddress(points.EStop),
        DataItem.FromAddress(points.SafetyDoor),
        DataItem.FromAddress(points.MaterialReady),
        DataItem.FromAddress(points.CycleTime),
        DataItem.FromAddress(points.Speed)
    ];
}
