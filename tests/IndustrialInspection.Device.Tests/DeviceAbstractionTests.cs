using IndustrialInspection.Device.Simulator;
using Xunit;

namespace IndustrialInspection.Device.Tests;

public sealed class DeviceAbstractionTests
{
    [Fact]
    public async Task Camera_preserves_product_id_and_frame_id()
    {
        await using var camera = new SimulatedCamera(
            new("CAM01", "顶面相机", "Hikrobot", "MV-CA050-20GM"),
            new(4, 4, CameraPixelFormat.Mono8, 500, 3, CameraTriggerMode.HardwareLine0));
        await camera.ConnectAsync(CancellationToken.None);
        await camera.StartGrabbingAsync(CancellationToken.None);

        Assert.True(camera.InjectFrame(1001, "P-001", new byte[16]));
        await using var enumerator = camera.GetFramesAsync(CancellationToken.None).GetAsyncEnumerator();
        Assert.True(await enumerator.MoveNextAsync());
        using var frame = enumerator.Current;

        Assert.Equal(1001, frame.FrameId);
        Assert.Equal("P-001", frame.ProductId);
        Assert.Equal(16, frame.Data.Length);
    }

    [Fact]
    public async Task Motion_axis_requires_enable_and_home_before_absolute_move()
    {
        await using var motion = new SimulatedMotionController(new("MC01", "运动控制器", "ZMotion", "XPCIE1028"));
        var axis = new AxisId(0);
        await motion.ConnectAsync(CancellationToken.None);

        var denied = await motion.MoveAbsoluteAsync(axis, new(100, 10, 100, 100, 1000), CancellationToken.None);
        await motion.EnableAxisAsync(axis, CancellationToken.None);
        await motion.HomeAsync(axis, CancellationToken.None);
        var accepted = await motion.MoveAbsoluteAsync(axis, new(100, 10, 100, 100, 1000), CancellationToken.None);

        Assert.False(denied.Succeeded);
        Assert.True(accepted.Succeeded);
        Assert.Equal(100, await motion.ReadPositionAsync(axis, CancellationToken.None));
    }

    [Fact]
    public async Task Robot_requires_remote_mode_servo_and_program_before_start()
    {
        await using var robot = new SimulatedRobotController(new("ROBOT01", "机械臂", "Simulator", "R1"));
        await robot.ConnectAsync(CancellationToken.None);

        var denied = await robot.StartProgramAsync(CancellationToken.None);
        await robot.ServoOnAsync(CancellationToken.None);
        await robot.SelectProgramAsync(new("InspectAndPlace"), CancellationToken.None);
        var accepted = await robot.StartProgramAsync(CancellationToken.None);

        Assert.False(denied.Succeeded);
        Assert.True(accepted.Succeeded);
        Assert.Equal(RobotRunState.Running, (await robot.ReadStatusAsync(CancellationToken.None)).RunState);
    }
}

