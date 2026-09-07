using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuSurfaceRetainedTests
{
    [Theory]
    [InlineData((int)SdlGpuSampleCount.Eight)]
    [InlineData((int)SdlGpuSampleCount.Four)]
    [InlineData((int)SdlGpuSampleCount.Two)]
    [InlineData((int)SdlGpuSampleCount.One)]
    public void SurfaceSelectsSupportedAntialiasingIndependentlyOfTheWindow(int supportedValue)
    {
        SdlGpuSampleCount supported = (SdlGpuSampleCount)supportedValue;
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        api.SupportedSampleCounts.Clear();
        api.SupportedSampleCounts.Add(SdlGpuSampleCount.One);
        api.SupportedSampleCounts.Add(supported);
        nint window = api.CreateWindow("surface-antialiasing", 32, 16, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 32, 16, 1));
        Assert.Equal(SdlGpuSampleCount.One, session.Diagnostics.SampleCount);
        HashSet<nint> existingTextures = api.GpuTextures.Keys.ToHashSet();
        using RecordedSurface surface = new((commands, bounds) => commands.Add(
            DrawCommand.FillEllipse(bounds, Color.White)), Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 16), Color.White));
        session.BeginFrame(Color.Black);
        try
        {
            DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
            session.DrawingBackend.Render(commands, in context);
        }
        finally { session.CompleteFrame(present: false); }

        Assert.Contains(api.GpuTextures, entry => !existingTextures.Contains(entry.Key) &&
            entry.Value.CreateInfo.Width == 32 && entry.Value.CreateInfo.Height == 16 &&
            entry.Value.CreateInfo.Format == session.Diagnostics.TextureFormat &&
            entry.Value.CreateInfo.SampleCount == supported);
        Assert.Equal(SdlGpuSampleCount.One, session.Diagnostics.SampleCount);
    }

    [SdlNativeFact]
    public void IdenticalRecordedFramesReuseTheRasterizedSurface()
    {
        using SdlDrawingFixture fixture = new(32, 16, useMultisampling: true);
        int recordCount = 0;
        using RecordedSurface surface = new((commands, _) =>
        {
            recordCount++;
            commands.Add(DrawCommand.FillRectangle(
                new DrawRect(2, 2, 8, 6), Color.CornflowerBlue));
        }, Color.Black);
        DrawCommandList presentation = new();
        presentation.Add(DrawCommand.RenderSurface2D(
            surface, new DrawRect(0, 0, 32, 16), Color.White));

        Color[] first = fixture.Render(presentation);
        Assert.Equal(2, fixture.Backend.LastFrameCounters.DrawCallCount);
        surface.FrameVersion++;
        Color[] second = fixture.Render(presentation);

        Assert.Equal(2, recordCount);
        Assert.Equal(first, second);
        // The unchanged surface needs only its presentation quad, not another
        // draw into the offscreen target. This measures the native submission.
        Assert.Equal(1, fixture.Backend.LastFrameCounters.DrawCallCount);
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ChangedDamageReplaysOnlyIntersectingCommandsInOrder(bool overlappingBackground)
    {
        using SdlDrawingFixture fixture = new(32, 16, useMultisampling: true);
        int movingX = 2;
        using RecordedSurface surface = new((commands, bounds) =>
        {
            if (overlappingBackground)
            {
                commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue));
            }
            commands.Add(DrawCommand.FillRectangle(new DrawRect(24, 2, 4, 4), Color.CornflowerBlue));
            commands.Add(DrawCommand.FillRectangle(new DrawRect(24, 10, 4, 4), Color.HotPink));
            commands.Add(DrawCommand.FillRectangle(new DrawRect(movingX, 2, 4, 4), Color.LimeGreen));
        }, Color.Black);
        DrawCommandList presentation = new();
        presentation.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 16), Color.White));
        _ = fixture.Render(presentation);
        movingX = 10;
        surface.FrameVersion++;
        Color[] pixels = fixture.Render(presentation);

        Assert.Equal(overlappingBackground ? Color.CornflowerBlue : Color.Black, pixels[(3 * 32) + 3]);
        Assert.Equal(Color.LimeGreen, pixels[(3 * 32) + 11]);
        Assert.Equal(Color.CornflowerBlue, pixels[(3 * 32) + 25]);
        Assert.Equal(Color.HotPink, pixels[(11 * 32) + 25]);
        // One damage-clear quad, the moving rectangle, the presentation quad,
        // and (when present) the intersecting background. Distant commands must
        // retain their pixels without being submitted again.
        Assert.Equal(overlappingBackground ? 16 : 12, fixture.Backend.LastFrameCounters.VertexCount);
    }

    [Fact]
    public void DamageScissorIsTheUnionOfOldAndNewCommandBounds()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-damage", 32, 16, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 32, 16, 1));
        int movingX = 2;
        using RecordedSurface surface = new((commands, _) => commands.Add(
            DrawCommand.FillRectangle(new DrawRect(movingX, 2, 4, 4), Color.White)), Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 16), Color.White));
        Render();
        movingX = 10;
        surface.FrameVersion++;
        int actionStart = api.GpuActions.Count;
        Render();

        Assert.Contains(api.GpuActions.Skip(actionStart), action =>
            action.StartsWith("scissor:", StringComparison.Ordinal) &&
            action.EndsWith(":2,2,12,4", StringComparison.Ordinal));

        void Render()
        {
            session.BeginFrame(Color.Black);
            try
            {
                DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands));
                session.DrawingBackend.Render(commands, in context);
            }
            finally { session.CompleteFrame(present: false); }
        }
    }

    [SdlNativeFact]
    public void UnchangedPrismImageReusesItsCachedResultWhenAnotherCommandMoves()
    {
        using SdlDrawingFixture fixture = new(32, 16, useMultisampling: true);
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(Color.LimeGreen);
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(source, new InvertFilter());
        int markerX = 20;
        using RecordedSurface surface = new((commands, bounds) =>
        {
            RenderSurface2DFrame frame = new(commands, bounds, TimeSpan.Zero);
            frame.DrawSprite(image, new DrawRect(0, 0, 16, 16), Color.White);
            frame.FillRectangle(new DrawRect(markerX, 2, 2, 2), Color.CornflowerBlue);
            frame.Complete();
        }, Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 16), Color.White));
        Color[] first = fixture.Render(commands);
        int initialPasses = fixture.Backend.LastFramePrismCounters.PassCount;
        Assert.True(initialPasses > 0);
        Assert.True(fixture.Session.DrawingResources.PrismResources.RetainedCount > 0);
        markerX = 24;
        surface.FrameVersion++;
        Color[] second = fixture.Render(commands);

        Assert.Equal(first[8 * 32 + 8], second[8 * 32 + 8]);
        Assert.True(fixture.Backend.LastFramePrismCounters.PassCount < initialPasses,
            "Replaying an unchanged Prism image must save GPU passes.");
    }

    [SdlNativeFact]
    public void DisposedPrismImageEvictsItsRetainedResults()
    {
        using SdlDrawingFixture fixture = new(32, 16, useMultisampling: true);
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(Color.LimeGreen);
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(source, new InvertFilter());
        bool drawImage = true;
        using RecordedSurface surface = new((commands, bounds) =>
        {
            RenderSurface2DFrame frame = new(commands, bounds, TimeSpan.Zero);
            if (drawImage) { frame.DrawSprite(image, new DrawRect(0, 0, 16, 16), Color.White); }
            frame.Complete();
        }, Color.Black);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 32, 16), Color.White));
        _ = fixture.Render(commands);
        Assert.True(fixture.Session.DrawingResources.PrismResources.RetainedCount > 0);
        drawImage = false;
        image.Dispose();
        surface.FrameVersion++;
        _ = fixture.Render(commands);

        Assert.Equal(0, fixture.Session.DrawingResources.PrismResources.RetainedCount);
    }

    [Fact]
    public void EmptyHostFrameStillConsumesPrismOwnerInvalidations()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("empty-host-invalidation", 32, 16, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 32, 16, 1));
        PrismCacheInvalidationQueue invalidations = new();
        using SdlGpuImage source = SdlDrawingFixture.SolidImage(Color.LimeGreen);
        using PrismImage image = global::Cerneala.Drawing.Prism.Prism.Apply(source, new InvertFilter());
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.DrawImage(image, new DrawRect(0, 0, 16, 16), Color.White);
        Render();
        Assert.True(session.DrawingResources.PrismResources.RetainedCount > 0);
        image.Dispose();
        commands.Clear();
        Render();

        Assert.Equal(0, session.DrawingResources.PrismResources.RetainedCount);
        Assert.Equal(0, invalidations.Count);

        void Render()
        {
            session.BeginFrame(Color.Black);
            try
            {
                DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(commands),
                    backdropLease: null, backdropSourceToken: default, invalidations);
                session.DrawingBackend.Render(commands, in context);
            }
            finally { session.CompleteFrame(present: false); }
        }
    }
}
