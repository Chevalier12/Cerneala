using Cerneala.Backends.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using SkiaSharp;
using System.Security.Cryptography;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeSdlLifetimeTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NativePlatformAndGpuDeviceHaveDeterministicLifetime()
    {
        NativeSdlApi api = new();
        using SdlPlatformLifetime platform = new(api);
        using SdlGpuDeviceOwner device = new(api);
        using SdlGpuDeviceLease session = device.AcquireSession();

        Assert.NotEqual(0, session.Device);
        Assert.NotEqual(SdlGpuShaderFormats.None, session.ShaderFormats);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NativeEventPumpKeepsTwoWindowsIndependent()
    {
        NativeSdlApi api = new();
        RecordingGraphicsFactory graphics = new();
        using SdlWindowPlatform platform = new(api, graphics);
        SdlPlatformWindow first = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window { Title = "SDL native A", Width = 320, Height = 200 }, new RecordingWindowCallbacks()));
        SdlPlatformWindow second = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(new Window { Title = "SDL native B", Width = 320, Height = 200 }, new RecordingWindowCallbacks()));

        first.Show();
        second.Show();
        platform.PumpEvents();

        Assert.NotEqual(first.WindowId, second.WindowId);
        first.Destroy();
        second.Activate();
        platform.PumpEvents();
        second.Destroy();
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NativeGpuCapturesTwoWindowsThroughWindowSaveScreenshot()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        Window first = new() { Title = "SDL GPU capture A", Width = 64, Height = 48 };
        Window second = new() { Title = "SDL GPU capture B", Width = 80, Height = 56 };
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"cerneala-sdlgpu-native-{Guid.NewGuid():N}");
        string firstPath = Path.Combine(directory, "first.png");
        string firstRepeatPath = Path.Combine(directory, "first-repeat.png");
        string secondPath = Path.Combine(directory, "second.png");
        string secondRepeatPath = Path.Combine(directory, "second-repeat.png");
        try
        {
            runtime.Show(first, modal: false);
            runtime.Show(second, modal: false);
            first.SaveScreenshot(firstPath);
            first.SaveScreenshot(firstRepeatPath);
            second.SaveScreenshot(secondPath);
            second.SaveScreenshot(secondRepeatPath);

            byte[] firstBytes = File.ReadAllBytes(firstPath);
            byte[] firstRepeatBytes = File.ReadAllBytes(firstRepeatPath);
            byte[] secondBytes = File.ReadAllBytes(secondPath);
            byte[] secondRepeatBytes = File.ReadAllBytes(secondRepeatPath);
            Assert.Equal(SHA256.HashData(firstBytes), SHA256.HashData(firstRepeatBytes));
            Assert.Equal(SHA256.HashData(secondBytes), SHA256.HashData(secondRepeatBytes));
            Assert.NotEqual(SHA256.HashData(firstBytes), SHA256.HashData(secondBytes));
            using SKBitmap firstBitmap = SKBitmap.Decode(firstBytes);
            using SKBitmap secondBitmap = SKBitmap.Decode(secondBytes);
            Assert.True(firstBitmap.Width > 0 && firstBitmap.Height > 0);
            Assert.True(secondBitmap.Width > 0 && secondBitmap.Height > 0);
            Assert.NotEqual(
                (firstBitmap.Width, firstBitmap.Height),
                (secondBitmap.Width, secondBitmap.Height));
        }
        finally
        {
            runtime.Close(first, force: true);
            runtime.Close(second, force: true);
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
