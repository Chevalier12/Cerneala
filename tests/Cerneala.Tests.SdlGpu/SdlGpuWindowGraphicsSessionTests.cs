using System.Security.Cryptography;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuWindowGraphicsSessionTests
{
    [Fact]
    public void PrismWarmupRunsOnlyAfterTheFirstFrameIsSubmitted()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("prism warmup", 8, 6, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);

        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 8, 6);

        Assert.DoesNotContain(api.GpuPipelines.Values, pipeline =>
            pipeline.ColorFormat == SdlGpuTextureFormat.R16G16B16A16Float &&
            pipeline.DepthStencilFormat == SdlGpuTextureFormat.Invalid &&
            pipeline.SampleCount == SdlGpuSampleCount.One);
        Assert.Equal(0, api.SubmitCount);

        session.BeginFrame(Color.Black);
        session.CompleteFrame(present: true);

        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.ColorFormat == SdlGpuTextureFormat.R16G16B16A16Float &&
            pipeline.DepthStencilFormat == SdlGpuTextureFormat.Invalid &&
            pipeline.SampleCount == SdlGpuSampleCount.One);
        int submitIndex = api.GpuActions.FindIndex(action =>
            action.StartsWith("submit:", StringComparison.Ordinal));
        int prismPipelineIndex = api.GpuActions.FindIndex(action =>
            action.StartsWith("create-pipeline:", StringComparison.Ordinal));
        Assert.InRange(submitIndex, 0, prismPipelineIndex - 1);
    }

    [Fact]
    public void DefaultFactoryDisablesMultisampling()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("default msaa", 8, 6, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api);

        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 8, 6);

        Assert.Equal(SdlGpuSampleCount.One, session.Diagnostics.SampleCount);
        Assert.Equal(0, session.MultisampleTexture);
    }

    [Fact]
    public void PresentedFramesSubmitWithoutForcingACpuFenceWait()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("asynchronous", 8, 6, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 8, 6);

        session.BeginFrame(Color.Black);
        session.CompleteFrame(present: true);

        Assert.Equal(1, api.SubmitCount);
        Assert.DoesNotContain(api.GpuActions, action =>
            action.StartsWith("submit-fence:", StringComparison.Ordinal) ||
            action.StartsWith("wait-fence:", StringComparison.Ordinal));
        Assert.Contains(api.GpuActions, action =>
            action.StartsWith("submit:", StringComparison.Ordinal));
        Assert.Contains(api.RenderTargets, target => target.Cycle);
        Assert.Contains(api.DepthStencilTargets, target => target.Cycle);
    }

    [Fact]
    public void RenderTargetsCycleOnlyOnTheirFirstWritePerCommandBuffer()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow(
            "single-cycle-per-command-buffer",
            8,
            6,
            SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(
            api,
            useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(
            factory,
            api,
            window,
            8,
            6);

        session.BeginFrame(Color.Black);
        session.BeginRenderTarget(
            session.WindowRenderTarget,
            Color.Transparent,
            SdlGpuLoadOp.Clear);
        session.BeginRenderTarget(
            session.WindowRenderTarget,
            Color.Transparent,
            SdlGpuLoadOp.Clear);
        session.CompleteFrame(present: false);

        session.BeginFrame(Color.Black);
        session.CompleteFrame(present: false);

        SdlGpuColorTargetInfo[] writes = api.RenderTargets
            .Where(target =>
                target.Texture == session.WindowRenderTarget.ColorTexture)
            .ToArray();
        Assert.Equal(4, writes.Length);
        Assert.True(writes[0].Cycle);
        Assert.All(writes[1..3], target => Assert.False(target.Cycle));
        Assert.True(writes[3].Cycle);
    }

    [Fact]
    public void TwoWindowsShareOneDeviceButOwnIndependentSwapchainsAndTextures()
    {
        FakeSdlApi api = new();
        nint firstWindow = api.CreateWindow("A", 4, 3, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("B", 5, 2, SdlWindowOptions.Hidden);
        SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        SdlGpuWindowGraphicsSession first = CreateSession(factory, api, firstWindow, 4, 3);
        SdlGpuWindowGraphicsSession second = CreateSession(factory, api, secondWindow, 5, 2);

        Assert.Equal(1, api.CreateDeviceCount);
        Assert.NotEqual(first.FrameTexture, second.FrameTexture);
        Assert.Equal(2, api.ClaimedGpuWindows.Count);

        first.BeginFrame(new Color(10, 20, 30, 255));
        first.CompleteFrame(present: true);
        second.BeginFrame(new Color(80, 90, 100, 255));
        second.CompleteFrame(present: true);
        Assert.Equal(2, api.Blits.Count);

        first.Dispose();
        factory.Dispose();
        Assert.Equal(0, api.DestroyDeviceCount);
        Assert.Single(api.ClaimedGpuWindows);

        second.BeginFrame(Color.Black);
        second.CompleteFrame(present: true);
        second.Dispose();
        Assert.Equal(1, api.DestroyDeviceCount);
        Assert.Empty(api.ClaimedGpuWindows);
    }

    [Fact]
    public void UnsupportedPresentationChoicesFallBackWithDiagnostics()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("fallback", 8, 6, SdlWindowOptions.Hidden);
        SdlGpuPresentationOptions options = new(
            SdlGpuSwapchainComposition.Hdr10St2084,
            SdlGpuPresentMode.Immediate,
            SdlGpuSampleCount.Eight);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, options);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 8, 6);

        Assert.Equal(SdlGpuSwapchainComposition.Sdr, session.Diagnostics.Composition);
        Assert.Equal(SdlGpuPresentMode.VSync, session.Diagnostics.PresentMode);
        Assert.Equal(SdlGpuSampleCount.Four, session.Diagnostics.SampleCount);
        Assert.Equal(3, session.Diagnostics.Fallbacks.Count);
        Assert.Equal(SdlGpuSwapchainComposition.Sdr, api.ConfiguredComposition);
        Assert.Equal(SdlGpuPresentMode.VSync, api.ConfiguredPresentMode);
    }

    [Fact]
    public void EveryRenderPassSetsTheFullTargetViewportExplicitly()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("viewport", 11, 7, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 11, 7);

        session.BeginFrame(Color.Black);
        session.RunCopyPass(_ => { });
        session.CompleteFrame(present: false);

        string[] viewports = api.GpuActions
            .Where(action => action.StartsWith("viewport:", StringComparison.Ordinal))
            .ToArray();
        Assert.Equal(2, viewports.Length);
        Assert.All(viewports, action => Assert.EndsWith(":0,0,11,7,0,1", action));
    }

    [Theory]
    [InlineData((int)SdlGpuTextureFormat.R8G8B8A8Unorm)]
    [InlineData((int)SdlGpuTextureFormat.B8G8R8A8Unorm)]
    [InlineData((int)SdlGpuTextureFormat.R8G8B8A8UnormSrgb)]
    [InlineData((int)SdlGpuTextureFormat.B8G8R8A8UnormSrgb)]
    public void ReadbackNormalizesRowPitchAndChannelOrder(int formatValue)
    {
        SdlGpuTextureFormat format = (SdlGpuTextureFormat)formatValue;
        FakeSdlApi api = new() { SwapchainTextureFormat = format };
        nint window = api.CreateWindow("readback", 2, 2, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 2, 2);

        session.BeginFrame(new Color(10, 20, 30, 128));
        session.CompleteFrame(present: false);
        WindowPreviewFrame frame = session.CapturePresentedFrame();

        Assert.Equal(2, frame.PixelWidth);
        Assert.Equal(2, frame.PixelHeight);
        Assert.Equal(8, frame.Stride);
        Assert.Equal(16, frame.Pixels.Length);
        for (int offset = 0; offset < frame.Pixels.Length; offset += 4)
        {
            Assert.Equal(10, frame.Pixels[offset]);
            Assert.Equal(20, frame.Pixels[offset + 1]);
            Assert.Equal(30, frame.Pixels[offset + 2]);
            Assert.Equal(128, frame.Pixels[offset + 3]);
        }

        byte[] reusable = new byte[frame.Pixels.Length];
        Assert.Same(reusable, session.CapturePresentedFrame(reusable).Pixels);
        Assert.Empty(api.TransferBuffers);
    }

    [Fact]
    public void ResizeMinimizeRestoreAndNullSwapchainRemainRecoverable()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("resize", 8, 6, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 8, 6);
        nint originalTexture = session.FrameTexture;

        session.Resize(0, 0, 1);
        Assert.True(session.IsSuspended);
        Assert.Equal(0, session.FrameTexture);
        Assert.Contains(originalTexture, api.ReleasedGpuTextures);
        int submissionsBeforeSuspendedFrame = api.SubmitCount;
        session.BeginFrame(Color.Black);
        session.CompleteFrame(present: true);
        Assert.Equal(submissionsBeforeSuspendedFrame, api.SubmitCount);

        session.Resize(12, 7, 1.5f);
        Assert.False(session.IsSuspended);
        Assert.NotEqual(0, session.FrameTexture);
        Assert.NotEqual(originalTexture, session.FrameTexture);
        api.NullSwapchainTextureCount = 1;
        session.BeginFrame(Color.CornflowerBlue);
        session.CompleteFrame(present: true);
        Assert.Empty(api.Blits);

        WindowPreviewFrame frame = session.CapturePresentedFrame();
        Assert.Equal(12, frame.PixelWidth);
        Assert.Equal(7, frame.PixelHeight);
    }

    [Fact]
    public void SwapchainInvalidationIsReconfiguredAndRetriedOnce()
    {
        FakeSdlApi api = new() { FailSwapchainAcquireCount = 1 };
        nint window = api.CreateWindow("recover", 6, 4, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 6, 4);

        session.BeginFrame(Color.Red);
        session.CompleteFrame(present: true);

        Assert.Equal(2, api.SwapchainConfigurationCount);
        Assert.Equal(2, api.SubmitCount);
        Assert.Single(api.Blits);
        Assert.Equal(0, api.CancelCount);
    }

    [Fact]
    public void RepeatedResizePresentAndReadbackDoNotGrowOwnedResources()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("budget", 8, 6, SdlWindowOptions.Hidden);
        SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: true);
        SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 8, 6);

        for (int iteration = 0; iteration < 20; iteration++)
        {
            int width = 8 + iteration % 3;
            int height = 6 + iteration % 2;
            session.Resize(width, height, 1);
            session.BeginFrame(new Color((byte)iteration, 20, 30));
            session.CompleteFrame(present: true);
            _ = session.CapturePresentedFrame();

            Assert.Equal(3, api.GpuTextures.Count);
            Assert.Empty(api.TransferBuffers);
        }

        session.Dispose();
        factory.Dispose();
        Assert.Empty(api.GpuTextures);
        Assert.Empty(api.TransferBuffers);
        Assert.Empty(api.ClaimedGpuWindows);
        Assert.Equal(1, api.DestroyDeviceCount);
    }

    [Fact]
    public void PartialTextureCreationFailureReleasesClaimAndCreatedResources()
    {
        FakeSdlApi api = new() { FailTextureCreationAt = 3 };
        nint window = api.CreateWindow("failure", 6, 4, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: true);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => CreateSession(factory, api, window, 6, 4));

        Assert.Contains("multisample-texture creation", exception.Message, StringComparison.Ordinal);
        Assert.Empty(api.ClaimedGpuWindows);
        Assert.Single(api.ReleasedGpuWindows);
        Assert.Equal(2, api.ReleasedGpuTextures.Count);
        Assert.Empty(api.GpuTextures);
    }

    [Fact]
    public void DestroyReleasesGpuWindowBeforeDestroyingTheNativeWindow()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        SdlPlatformWindow window = Assert.IsType<SdlPlatformWindow>(
            platform.CreateWindow(
                new Window { Title = "ordered", Width = 20, Height = 10 },
                new RecordingWindowCallbacks()));

        window.Destroy();

        int releaseIndex = api.GpuActions.FindIndex(value => value == $"release-window:{window.Handle}");
        int destroyIndex = api.GpuActions.FindIndex(value => value == $"destroy-window:{window.Handle}");
        Assert.InRange(releaseIndex, 0, destroyIndex - 1);
        Assert.Equal(0, api.DestroyDeviceCount);

        platform.Dispose();

        int deviceIndex = api.GpuActions.FindIndex(value => value == "destroy-device");
        int quitIndex = api.GpuActions.FindIndex(value => value == "quit");
        Assert.InRange(deviceIndex, destroyIndex + 1, quitIndex - 1);
        Assert.Equal(1, api.DestroyDeviceCount);
    }

    [Fact]
    public void RenderPngIsDeterministicForTheSameGpuFrame()
    {
        FakeSdlApi api = new();
        nint window = api.CreateWindow("png", 9, 7, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 9, 7);

        byte[] first = RenderPng(session, Color.Coral);
        byte[] second = RenderPng(session, Color.Coral);

        Assert.Equal(SHA256.HashData(first), SHA256.HashData(second));
        using SKBitmap bitmap = SKBitmap.Decode(first);
        Assert.Equal(9, bitmap.Width);
        Assert.Equal(7, bitmap.Height);
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void RegionalPngUsesTheSharedPixelRegionContract(float scale)
    {
        const int dipWidth = 8;
        const int dipHeight = 6;
        int pixelWidth = (int)Math.Ceiling(dipWidth * scale);
        int pixelHeight = (int)Math.Ceiling(dipHeight * scale);
        FakeSdlApi api = new();
        nint window = api.CreateWindow("regional png", pixelWidth, pixelHeight, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)),
            pixelWidth,
            pixelHeight,
            scale));
        Assert.True(WindowScreenshotRegion.TryCreate(
            new Cerneala.UI.Layout.LayoutRect(1.2f, 1.4f, 3.2f, 2.2f),
            new Cerneala.UI.Hosting.UiViewport(dipWidth, dipHeight, scale),
            out WindowScreenshotRegion region));
        using MemoryStream fullOutput = new();
        ((IWindowScreenshotSource)session).RenderPng(
            fullOutput,
            new Color(10, 20, 30, 255),
            _ => { });
        using MemoryStream output = new();

        ((IWindowScreenshotSource)session).RenderPng(
            output,
            new Color(10, 20, 30, 255),
            region,
            _ => { });

        using SKBitmap full = SKBitmap.Decode(fullOutput.ToArray());
        using SKBitmap bitmap = SKBitmap.Decode(output.ToArray());
        Assert.Equal((pixelWidth, pixelHeight), (full.Width, full.Height));
        Assert.Equal((region.Width, region.Height), (bitmap.Width, bitmap.Height));
        Assert.Equal(new SKColor(10, 20, 30, 255), bitmap.GetPixel(0, 0));
    }

    [Fact]
    public void TwoWindowsCaptureThroughWindowSaveScreenshot()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: false);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        Window first = new() { Title = "screenshot A", Width = 17, Height = 11 };
        Window second = new() { Title = "screenshot B", Width = 19, Height = 13 };
        string directory = Path.Combine(Path.GetTempPath(), $"cerneala-sdlgpu-{Guid.NewGuid():N}");
        string firstPath = Path.Combine(directory, "first.png");
        string secondPath = Path.Combine(directory, "second.png");
        try
        {
            runtime.Show(first, modal: false);
            runtime.Show(second, modal: false);
            first.SaveScreenshot(firstPath);
            second.SaveScreenshot(secondPath);

            byte[] firstBytes = File.ReadAllBytes(firstPath);
            byte[] secondBytes = File.ReadAllBytes(secondPath);
            Assert.NotEqual(SHA256.HashData(firstBytes), SHA256.HashData(secondBytes));
            using SKBitmap firstBitmap = SKBitmap.Decode(firstBytes);
            using SKBitmap secondBitmap = SKBitmap.Decode(secondBytes);
            Assert.Equal((17, 11), (firstBitmap.Width, firstBitmap.Height));
            Assert.Equal((19, 13), (secondBitmap.Width, secondBitmap.Height));
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

    private static SdlGpuWindowGraphicsSession CreateSession(
        SdlGpuWindowGraphicsSessionFactory factory,
        FakeSdlApi api,
        nint window,
        int width,
        int height) =>
        Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)),
            width,
            height,
            coordinateScale: 1));

    private static byte[] RenderPng(SdlGpuWindowGraphicsSession session, Color color)
    {
        using MemoryStream output = new();
        session.RenderPng(output, color, _ => { });
        return output.ToArray();
    }
}
