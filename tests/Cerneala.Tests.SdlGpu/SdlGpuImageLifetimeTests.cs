using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuImageLifetimeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeviceImageInvalidationSurvivesTheUploadingWindow(bool directResourceUpload)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = CreateSession(factory, api, "upload-owner");
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, "remaining-window");
        using ImageResourceCache cache = new(new Loader());
        ImageResourceLease lease = cache.Acquire(new ImageResource("atlas"));
        SdlGpuImage image = Assert.IsType<SdlGpuImage>(lease.Image);
        first.BeginFrame(Color.Transparent);
        if (directResourceUpload)
        {
            // Prism also enters through the shared device resource owner.
            first.DrawingResources.GetOrCreateTexture(first, image, 1, 1, image.RgbaPixels.Span);
        }
        else { Draw(first, image); }
        first.CompleteFrame(present: false);
        Assert.Equal(1, second.DrawingResources.CachedTextureCount);
        first.Dispose();

        lease.Dispose();

        Assert.Equal(0, cache.ResidentCount);
        Assert.Equal(0, second.DrawingResources.CachedTextureCount);
        second.BeginFrame(Color.Transparent);
        second.CompleteFrame(present: false);
        Assert.DoesNotContain(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 1 && texture.CreateInfo.Height == 1);
    }

    [Fact]
    public void WorkerReleaseIsAppliedByTheDeviceThreadOnTheNextFrame()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, "worker-release");
        using ImageResourceCache cache = new(new Loader());
        ImageResourceLease lease = cache.Acquire(new ImageResource("atlas"));
        session.BeginFrame(Color.Transparent);
        Draw(session, lease.Image);
        session.CompleteFrame(present: false);
        Exception? failure = null;
        Thread worker = new(() =>
        {
            try { lease.Dispose(); }
            catch (Exception error) { failure = error; }
        });
        worker.Start();
        Assert.True(worker.Join(TimeSpan.FromSeconds(10)));
        Assert.Null(failure);
        Assert.Equal(0, cache.ResidentCount);
        Assert.Equal(1, session.DrawingResources.CachedTextureCount);

        session.BeginFrame(Color.Transparent);
        session.CompleteFrame(present: false);

        Assert.Equal(0, session.DrawingResources.CachedTextureCount);
        Assert.DoesNotContain(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 1 && texture.CreateInfo.Height == 1);
    }

    [Fact]
    public void ReleasingOneWindowsAcquisitionKeepsTheTextureAliveForTheOtherWindow()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = CreateSession(factory, api, "first-frame");
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, "second-frame");
        using ImageResourceCache cache = new(new Loader());
        ImageResourceLease lease = cache.Acquire(new ImageResource("atlas"));
        ImageResourceLease secondLease = lease.Retain();
        first.BeginFrame(Color.Transparent);
        Draw(first, lease.Image);
        nint texture = Assert.Single(api.GpuTextures.Where(pair =>
            pair.Value.CreateInfo.Width == 1 && pair.Value.CreateInfo.Height == 1)).Key;
        lease.Dispose();

        first.CompleteFrame(present: false);

        Assert.DoesNotContain(texture, api.ReleasedGpuTextures);
        second.BeginFrame(Color.Transparent);
        Draw(second, secondLease.Image);
        second.CompleteFrame(present: false);
        secondLease.Dispose();
        second.BeginFrame(Color.Transparent);
        second.CompleteFrame(present: false);
        Assert.Equal(1, api.ReleasedGpuTextures.Count(handle => handle == texture));
        Assert.Equal(0, first.DrawingResources.CachedTextureCount);
    }

    private static SdlGpuWindowGraphicsSession CreateSession(
        SdlGpuWindowGraphicsSessionFactory factory, FakeSdlApi api, string title)
    {
        nint window = api.CreateWindow(title, 16, 16, SdlWindowOptions.Hidden);
        return Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)), 16, 16, 1));
    }

    private static void Draw(SdlGpuWindowGraphicsSession session, IDrawImage image)
    {
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawImage(image, new DrawRect(0, 0, 8, 8), Color.White);
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        session.DrawingBackend.Render(commands, in frame);
    }

    private sealed class Loader : IImageLoader
    {
        public IDrawImage Load(string path) => new SdlGpuImage(1, 1, [255, 0, 0, 255]);
    }
}
