using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuDeviceOwnerTests
{
    [Fact]
    public void OneDeviceRemainsAliveUntilTheLastSessionIsReleased()
    {
        FakeSdlApi api = new();
        SdlGpuDeviceOwner owner = new(api);
        SdlGpuDeviceLease first = owner.AcquireSession();
        SdlGpuDeviceLease second = owner.AcquireSession();

        Assert.Equal(first.Device, second.Device);
        Assert.Equal(1, api.CreateDeviceCount);
        owner.Dispose();
        Assert.Equal(0, api.DestroyDeviceCount);
        first.Dispose();
        Assert.Equal(0, api.DestroyDeviceCount);
        second.Dispose();
        second.Dispose();
        Assert.Equal(1, api.DestroyDeviceCount);
        Assert.Throws<ObjectDisposedException>(() => owner.AcquireSession());
    }

    [Fact]
    public void DeviceCreationRequestsEveryOfflineShaderFamilyAndUsesSupportedFormats()
    {
        FakeSdlApi api = new()
        {
            SupportedShaderFormats = SdlGpuShaderFormats.Dxil | SdlGpuShaderFormats.MetalLib
        };

        using SdlGpuDeviceOwner owner = new(api);

        Assert.Equal(SdlGpuDeviceOwner.RequestedShaderFormats, api.RequestedShaderFormats);
        Assert.Equal(SdlGpuDeviceOwner.DefaultDebugMode, api.RequestedDebugMode);
        Assert.Equal(api.SupportedShaderFormats, owner.ShaderFormats);
    }

    [Fact]
    public void DeviceCreationFailureIncludesTheSdlError()
    {
        FakeSdlApi api = new()
        {
            DeviceResult = 0,
            Error = "no compatible GPU driver"
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new SdlGpuDeviceOwner(api));

        Assert.Contains("SDL GPU device creation", exception.Message, StringComparison.Ordinal);
        Assert.Contains("no compatible GPU driver", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, api.DestroyDeviceCount);
    }

    [Fact]
    public void UnsupportedShaderFormatsDestroyThePartiallyCreatedDevice()
    {
        FakeSdlApi api = new()
        {
            SupportedShaderFormats = SdlGpuShaderFormats.None
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => new SdlGpuDeviceOwner(api));

        Assert.Contains("no supported offline shader format", exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, api.DestroyDeviceCount);
    }

    [Fact]
    public void DebugLabelsBalanceGroupsAndInsertMarkersWhenEnabled()
    {
        FakeSdlApi api = new();
        SdlGpuDebugLabels labels = new(api, enabled: true);

        IDisposable group = labels.Push((nint)0x55, "window frame");
        labels.Insert((nint)0x55, "opaque pass");
        group.Dispose();
        group.Dispose();

        Assert.Equal(
            ["push:85:window frame", "insert:85:opaque pass", "pop:85"],
            api.DebugLabelCalls);
    }

    [Fact]
    public void DebugLabelsAreNoOpsWhenDisabled()
    {
        FakeSdlApi api = new();
        SdlGpuDebugLabels labels = new(api, enabled: false);

        using IDisposable group = labels.Push((nint)0x55, "window frame");
        labels.Insert((nint)0x55, "opaque pass");

        Assert.Empty(api.DebugLabelCalls);
    }
}
