using IndustrialInspection.Device.Plc.Siemens;
using Xunit;

namespace IndustrialInspection.Device.Tests;

public sealed class SiemensPlcOptionsValidatorTests
{
    /// <summary>验证默认 PoC 配置满足静态检查要求。</summary>
    [Fact]
    public void Default_options_are_valid()
    {
        SiemensPlcOptionsValidator.Validate(new SiemensPlcOptions());
    }

    /// <summary>验证不合法 IP 地址会在连接真实 PLC 前被拒绝。</summary>
    [Fact]
    public void Invalid_ip_is_rejected()
    {
        var options = new SiemensPlcOptions { IpAddress = "not-an-ip" };

        var exception = Assert.Throws<ArgumentException>(() => SiemensPlcOptionsValidator.Validate(options));

        Assert.Contains("IP", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>验证过短的命令脉冲会被静态检查拒绝。</summary>
    [Fact]
    public void Unsafe_command_pulse_is_rejected()
    {
        var options = new SiemensPlcOptions { CommandPulseMilliseconds = 1 };

        var exception = Assert.Throws<ArgumentException>(() => SiemensPlcOptionsValidator.Validate(options));

        Assert.Contains("脉冲", exception.Message, StringComparison.Ordinal);
    }
}
