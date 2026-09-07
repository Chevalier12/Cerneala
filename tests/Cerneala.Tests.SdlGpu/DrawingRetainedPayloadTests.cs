using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class DrawingRetainedPayloadTests
{
    [SdlNativeFact]
    public void OnDemandSpriteBatchTracksItsPrismImageAndOnlyRedrawsOnChange()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(Color.LimeGreen);
        BlurFilter blur = new() { Radius = 1 };
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(source, blur);
        RenderSurface2D surface = new() { RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.DrawSpriteBatch(new DrawSpriteBatch(image, [new DrawSprite2D(frame.Bounds)]));
        };
        DrawCommandList commands = Presentation(surface, 96, 64);
        _ = fixture.Render(commands);
        _ = fixture.Render(commands);
        blur.Radius = 3;
        _ = fixture.Render(commands);
        _ = fixture.Render(commands);

        Assert.Equal(2, drawCount);
        ((IRenderSurface2DFrameSource)surface).SetBackendState(fixture.Session.DrawingResources, null);
    }

    [Fact]
    public void ReusedPointBatchSkipsRasterizationAndNewVersionDamagesOldAndNewBounds()
    {
        using FakeSurfaceFixture fixture = new();
        DrawPointBatch batch = new([new DrawPoint(4, 4)], Color.White, 2);
        using RecordedSurface surface = new((commands, _) =>
            commands.Add(DrawCommand.DrawPointBatch(batch)), Color.Black);
        fixture.Render(surface);
        surface.FrameVersion++;
        fixture.Render(surface);
        Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);

        batch = new DrawPointBatch([new DrawPoint(12, 4)], Color.White, 2);
        surface.FrameVersion++;
        int start = fixture.Api.GpuActions.Count;
        fixture.Render(surface);

        Assert.True(fixture.Backend.LastFrameCounters.DrawCallCount > 1);
        fixture.AssertScissorAfter(start, "3,3,10,2");
    }

    [SdlNativeFact]
    public void AdvancedBatchesHaveOneSubmissionEachAndRepeatDeterministically()
    {
        using SdlDrawingFixture fixture = new(useMultisampling: true);
        using SdlGpuImage image = new(2, 2,
            [255, 0, 0, 255, 0, 128, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255]);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.DrawImageQuad(image,
            new DrawPoint(2.25f, 2.25f), new DrawPoint(22.25f, 2.25f),
            new DrawPoint(22.25f, 22.25f), new DrawPoint(2.25f, 22.25f),
            new DrawImageOptions(sampling: DrawSamplingMode.Point));
        drawing.DrawNineSlice(image, new DrawRect(26.25f, 2.25f, 20, 20), new DrawInsets(1),
            new DrawImageOptions(sampling: DrawSamplingMode.Point));
        drawing.DrawPointBatch(new DrawPointBatch(
            [new DrawPoint(8, 32), new DrawPoint(16, 32)], Color.White, 4));
        drawing.DrawLineBatch(new DrawLineBatch(
            [new DrawLineSegment2D(new DrawPoint(26, 32), new DrawPoint(46, 32), Color.White, 3)]));
        drawing.DrawSpriteBatch(new DrawSpriteBatch(image,
            [new DrawSprite2D(new DrawRect(52.25f, 2.25f, 20, 20),
                new DrawImageOptions(sampling: DrawSamplingMode.Point))]));

        Color[] first = fixture.Render(commands);
        SdlGpuDrawingFrameCounters firstCounters = fixture.Backend.LastFrameCounters;
        Color[] second = fixture.Render(commands);

        Assert.Equal(5, firstCounters.SubmissionCount);
        Assert.Equal(5, fixture.Backend.LastFrameCounters.SubmissionCount);
        // SDL may merge compatible submissions; it must not multiply the old
        // one-draw-per-batch bound just to reproduce a retired counter.
        Assert.InRange(firstCounters.DrawCallCount, 1, 5);
        Assert.Equal(firstCounters.DrawCallCount, fixture.Backend.LastFrameCounters.DrawCallCount);
        Assert.Equal(first, second);
        Assert.NotEqual(Color.Black, fixture.Sample(first, 8, 8));
        Assert.NotEqual(Color.Black, fixture.Sample(first, 8, 32));
        Assert.NotEqual(Color.Black, fixture.Sample(first, 34, 32));
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DisposedImageIsRejectedAndIdentifiedBeforeUpload(bool mesh)
    {
        using SdlDrawingFixture fixture = new(16, 16);
        using SdlGpuImage image = SdlDrawingFixture.SolidImage(Color.White);
        DrawCommandList commands = new();
        if (mesh)
        {
            commands.Add(DrawCommand.DrawMesh(Triangle(1, 1, image)));
        }
        else
        {
            commands.Add(DrawCommand.DrawImage(image, new DrawRect(0, 0, 8, 8), Color.White));
        }
        using RecordedSurface surface = new((recording, _) =>
        {
            foreach (DrawCommand command in commands) { recording.Add(command); }
        }, Color.Black);
        image.Dispose();
        int cachedBefore = fixture.Session.DrawingResources.CachedTextureCount;
        ObjectDisposedException exception = Assert.Throws<ObjectDisposedException>(() =>
            fixture.Render(Presentation(surface, 16, 16)));

        Assert.Contains(nameof(SdlGpuImage), exception.ToString(), StringComparison.Ordinal);
        Assert.Equal(cachedBefore, fixture.Session.DrawingResources.CachedTextureCount);
    }

    [Fact]
    public void MeshDamageReuseStressScopeFallbackAndDisposalRemainDeterministic()
    {
        using FakeSurfaceFixture fixture = new();
        DrawMesh2D mesh = Triangle(2, 2);
        bool scoped = false;
        using RecordedSurface surface = new((commands, _) =>
        {
            if (scoped) { commands.Add(DrawCommand.PushOpacity(0.75f)); }
            commands.Add(DrawCommand.DrawMesh(mesh));
            if (scoped) { commands.Add(DrawCommand.PopOpacity()); }
        }, Color.Black);
        HashSet<nint> before = fixture.Api.GpuTextures.Keys.ToHashSet();
        fixture.Render(surface);
        nint[] surfaceTextures = fixture.Api.GpuTextures.Keys.Where(key =>
            !before.Contains(key) && fixture.Api.GpuTextures[key].CreateInfo.Width == 32).ToArray();
        Assert.NotEmpty(surfaceTextures);
        surface.FrameVersion++;
        fixture.Render(surface);
        Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);
        mesh = Triangle(10, 2);
        surface.FrameVersion++;
        int actionStart = fixture.Api.GpuActions.Count;
        fixture.Render(surface);
        fixture.AssertScissorAfter(actionStart, "2,2,12,4");
        Assert.True(fixture.Backend.LastFrameCounters.DrawCallCount > 1);

        for (int frame = 0; frame < 128; frame++)
        {
            surface.FrameVersion++;
            fixture.Render(surface);
            Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);
        }

        scoped = true;
        surface.FrameVersion++;
        fixture.Render(surface);
        Assert.True(fixture.Backend.LastFrameCounters.DrawCallCount > 1);
        surface.Dispose();
        fixture.Render(new DrawCommandList()); // Flush the existing deferred GPU retirement queue.
        Assert.All(surfaceTextures, texture => Assert.Contains(texture, fixture.Api.ReleasedGpuTextures));
    }

    [Fact]
    public void OnDemandSurfaceReusesContentRecreatesOnResizeAndReleasesOnDetach()
    {
        using FakeSurfaceFixture fixture = new();
        UIRoot root = new(32, 32);
        RenderSurface2D surface = new() { RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        int drawCount = 0;
        surface.Draw += (_, frame) =>
        {
            drawCount++;
            frame.FillRectangle(frame.Bounds, Color.White);
        };
        root.VisualChildren.Add(surface);
        IRenderSurface2DFrameSource source = surface;
        fixture.Render(Presentation(surface, 8, 8));
        IRenderSurface2DBackendState? first = source.GetBackendState(fixture.Session.DrawingResources);
        nint[] firstTextures = fixture.Api.GpuTextures.Keys.Where(key =>
            fixture.Api.GpuTextures[key].CreateInfo.Width == 8).ToArray();
        Assert.NotEmpty(firstTextures);
        fixture.Render(Presentation(surface, 8, 8));
        Assert.Same(first, source.GetBackendState(fixture.Session.DrawingResources));
        fixture.Render(Presentation(surface, 16, 12));
        IRenderSurface2DBackendState? resized = source.GetBackendState(fixture.Session.DrawingResources);
        Assert.NotSame(first, resized);
        Assert.Equal(2, drawCount);
        Assert.All(firstTextures, texture => Assert.Contains(texture, fixture.Api.ReleasedGpuTextures));
        nint[] resizedTextures = fixture.Api.GpuTextures.Keys.Where(key =>
            fixture.Api.GpuTextures[key].CreateInfo.Width == 16 &&
            fixture.Api.GpuTextures[key].CreateInfo.Height == 12).ToArray();
        Assert.NotEmpty(resizedTextures);
        Assert.All(resizedTextures, texture => Assert.DoesNotContain(texture, fixture.Api.ReleasedGpuTextures));

        Assert.True(root.VisualChildren.Remove(surface));
        Assert.Null(source.GetBackendState(fixture.Session.DrawingResources));
        fixture.Render(new DrawCommandList());
        Assert.All(resizedTextures, texture => Assert.Contains(texture, fixture.Api.ReleasedGpuTextures));
    }

    [SdlNativeFact]
    public void ReusedTextLayoutSkipsRasterizationAndChangedLayoutUpdatesPixels()
    {
        using SdlDrawingFixture fixture = new(96, 40, useMultisampling: true);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 10);
        SolidColorBrush brush = new(Color.White);
        DrawTextLayout layout = Layout("stable", 36);
        using RecordedSurface surface = new((commands, _) => commands.Add(
            DrawCommand.DrawTextLayout(layout, new DrawPoint(2, 2))), Color.Black);
        DrawCommandList commands = Presentation(surface, 96, 40);
        Color[] first = fixture.Render(commands);
        surface.FrameVersion++;
        Assert.Equal(first, fixture.Render(commands));
        Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);
        layout = Layout("changed text", 60);
        surface.FrameVersion++;
        Color[] changed = fixture.Render(commands);

        Assert.True(fixture.Backend.LastFrameCounters.DrawCallCount > 1);
        Assert.False(first.SequenceEqual(changed));

        DrawTextLayout Layout(string text, float width) => new DrawTextLayoutBuilder()
            .AddSpan(text, font, 10, brush).Build(new DrawTextLayoutOptions(maxWidth: width));
    }

    private static DrawCommandList Presentation(IRenderSurface2DSource surface, int width, int height)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, width, height), Color.White));
        return commands;
    }

    private static DrawMesh2D Triangle(int x, int y, IDrawImage? image = null) => new(
        [new DrawVertex2D(new DrawPoint(x, y), Color.White),
         new DrawVertex2D(new DrawPoint(x + 4, y), Color.White),
         new DrawVertex2D(new DrawPoint(x, y + 4), Color.White)], [0, 1, 2], image: image);

    private sealed class FakeSurfaceFixture : IDisposable
    {
        private readonly SdlGpuWindowGraphicsSessionFactory factory;
        public FakeSurfaceFixture()
        {
            Api = new FakeSdlApi { WindowPixelDensity = 1 };
            nint window = Api.CreateWindow("retained-payload", 32, 16, SdlWindowOptions.Hidden);
            factory = new SdlGpuWindowGraphicsSessionFactory(Api, useMultisampling: false);
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, Api.GetWindowId(window)), 32, 16, 1));
        }
        public FakeSdlApi Api { get; }
        public SdlGpuWindowGraphicsSession Session { get; }
        public SdlGpuDrawingBackend Backend => Assert.IsType<SdlGpuDrawingBackend>(Session.DrawingBackend);
        public void Render(IRenderSurface2DSource surface) => Render(Presentation(surface, 32, 16));
        public void Render(DrawCommandList commands)
        {
            Session.BeginFrame(Color.Black);
            try
            {
                DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
                Session.DrawingBackend.Render(commands, in context);
            }
            finally { Session.CompleteFrame(present: false); }
        }
        public void AssertScissorAfter(int actionStart, string bounds) =>
            Assert.Contains(Api.GpuActions.Skip(actionStart), action =>
                action.StartsWith("scissor:", StringComparison.Ordinal) &&
                action.EndsWith(":" + bounds, StringComparison.Ordinal));
        public void Dispose()
        {
            Session.Dispose();
            factory.Dispose();
        }
    }
}
