using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.Tests.Drawing.SdlGpu;
using Cerneala.UI.Controls;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using System.Numerics;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class SdlGpuSurfaceRetainedTests
{
    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void InvalidatedSurfaceEffectsAndContentMatchFreshRendering(float ownerPixelScale)
    {
        using SdlDrawingFixture fixture = new(128, 128, useMultisampling: false);
        PrismLayerDefinition layer = new(new PrismNodeId(1), "Blur",
            filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)]);
        PrismInstance instance = new(new PrismCompositionDefinition("SurfaceBlur", [layer]));
        PrismFilterState blur = instance.GetLayerState(layer.Id).Filters[0];
        PrismCatalogParameterInfo distance = PrismCatalog.GetFilter(PrismFilterId.MotionBlur)
            .Parameters.Single(parameter => parameter.Name == "Distance");
        PrismCacheOwnerToken owner = new(773);
        PrismCacheInvalidationQueue invalidations = new();
        DrawRect bounds = new(32, 32, 48, 32);
        int contentVersion = 1;
        Color color = Color.CornflowerBlue;
        using RecordedSurface surface = new((commands, _) =>
        {
            commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
                instance, owner, bounds, Matrix3x2.Identity, ownerPixelScale,
                visualContentVersion: contentVersion)));
            commands.Add(DrawCommand.FillRectangle(bounds, color));
            commands.Add(DrawCommand.EndPrism());
        }, Color.Black);
        DrawCommandList presentation = new();
        presentation.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 128, 128), Color.White));

        blur.SetValue(distance, 5f);
        byte[] initial = Render();
        blur.SetValue(distance, 16f);
        surface.FrameVersion++;
        invalidations.EnqueueOwner(owner);
        byte[] effectChanged = Render();
        Assert.Equal(0, fixture.Backend.LastFramePrismCounters.CaptureCount);
        Assert.False(initial.AsSpan().SequenceEqual(effectChanged));
        // Drop the retained raster as well: an unchanged recorded surface can
        // otherwise present its prior pixels without invoking Prism at all.
        surface.SetBackendState(fixture.Session.DrawingResources, null);
        invalidations.EnqueueAll();
        surface.FrameVersion++;
        Assert.Equal(effectChanged, Render());
        Assert.Equal(1, fixture.Backend.LastFramePrismCounters.CaptureCount);

        color = Color.White;
        contentVersion++;
        surface.FrameVersion++;
        invalidations.EnqueueOwner(owner);
        byte[] contentChanged = Render();
        Assert.Equal(1, fixture.Backend.LastFramePrismCounters.CaptureCount);
        Assert.False(effectChanged.AsSpan().SequenceEqual(contentChanged));
        surface.SetBackendState(fixture.Session.DrawingResources, null);
        invalidations.EnqueueAll();
        surface.FrameVersion++;
        Assert.Equal(contentChanged, Render());

        byte[] Render()
        {
            fixture.Session.BeginFrame(Color.Black);
            try
            {
                DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(presentation),
                    backdropLease: null, backdropSourceToken: default, invalidations);
                fixture.Backend.Render(presentation, in context);
            }
            finally { fixture.Session.CompleteFrame(present: false); }
            return fixture.Session.CapturePresentedFrame().Pixels.ToArray();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SurfaceCaptureRetentionDistinguishesOwnerAndFullInvalidation(bool invalidateAll)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-owner-invalidation", 128, 128, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
            factory.Create(new SdlWindowSurface(window, api.GetWindowId(window)), 128, 128, 1));
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(session.DrawingBackend);
        PrismLayerDefinition layer = new(new PrismNodeId(1), "Blur",
            filters: [new PrismFilterDefinition(PrismFilterId.MotionBlur)]);
        PrismInstance instance = new(new PrismCompositionDefinition("SurfaceBlur", [layer]));
        PrismFilterState blur = instance.GetLayerState(layer.Id).Filters[0];
        PrismCatalogParameterInfo distance = PrismCatalog.GetFilter(PrismFilterId.MotionBlur)
            .Parameters.Single(parameter => parameter.Name == "Distance");
        PrismCacheOwnerToken owner = new(772);
        PrismCacheInvalidationQueue invalidations = new();
        DrawRect bounds = new(32, 32, 48, 32);
        using RecordedSurface surface = new((commands, _) =>
        {
            commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
                instance, owner, bounds, Matrix3x2.Identity, 1, visualContentVersion: 1)));
            commands.Add(DrawCommand.FillRectangle(bounds, Color.CornflowerBlue));
            commands.Add(DrawCommand.EndPrism());
        }, Color.Black);
        DrawCommandList presentation = new();
        presentation.Add(DrawCommand.RenderSurface2D(surface, new DrawRect(0, 0, 128, 128), Color.White));

        blur.SetValue(distance, 5f);
        Render();
        Assert.Equal(1, backend.LastFramePrismCounters.CaptureCount);
        blur.SetValue(distance, 6f);
        surface.FrameVersion++;
        if (invalidateAll) { invalidations.EnqueueAll(); }
        else { invalidations.EnqueueOwner(owner); }
        Render();

        Assert.Equal(invalidateAll ? 1 : 0, backend.LastFramePrismCounters.CaptureCount);
        Assert.Equal(0, invalidations.Count);
        // Removing the owner from the frame must still release its retained entries.
        presentation.Clear();
        invalidations.EnqueueOwner(owner);
        Render();
        Assert.Equal(0, session.DrawingResources.PrismResources.RetainedCount);

        void Render()
        {
            session.BeginFrame(Color.Black);
            try
            {
                DrawingFrameContext context = new(new PrismFrameAnalyzer().Analyze(presentation),
                    backdropLease: null, backdropSourceToken: default, invalidations);
                backend.Render(presentation, in context);
            }
            finally { session.CompleteFrame(present: false); }
        }
    }

    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    public void MovingPrismShapePreservesItsPixelsInsideAPixelSpaceSurface(float ownerPixelScale)
    {
        using SdlDrawingFixture fixture = new(256, 256, useMultisampling: false);
        PrismInstance instance = new(new PrismCompositionDefinition("MovingL",
        [
            new PrismLayerDefinition(new PrismNodeId(1), "BevelAndGlow", styles:
            [
                new PrismStyleDefinition(PrismStyleId.BevelEmboss),
                new PrismStyleDefinition(PrismStyleId.OuterGlow)
            ])
        ]));
        int positionY = 32;
        using RecordedSurface surface = new((commands, _) =>
        {
            commands.Add(DrawCommand.PushTransform(Matrix3x2.CreateTranslation(96, positionY)));
            // Scene scopes carry their owner's DPI, but RecordFrame geometry
            // is already in surface pixels (as in RenderSurface2DFrame.BeginPrism).
            commands.Add(DrawCommand.BeginPrism(new PrismDrawScope(
                instance, new PrismCacheOwnerToken(771), new DrawRect(0, 0, 48, 32),
                Matrix3x2.Identity, ownerPixelScale, visualContentVersion: positionY)));
            commands.Add(DrawCommand.FillRectangle(new DrawRect(0, 0, 16, 32), Color.CornflowerBlue));
            commands.Add(DrawCommand.FillRectangle(new DrawRect(16, 16, 32, 16), Color.CornflowerBlue));
            commands.Add(DrawCommand.EndPrism());
            commands.Add(DrawCommand.PopTransform());
        }, Color.Black);
        DrawCommandList presentation = new();
        presentation.Add(DrawCommand.RenderSurface2D(
            surface, new DrawRect(0, 0, 256, 256), Color.White));
        Color[]? reference = null;

        foreach (int nextY in new[] { 32, 96, 160 })
        {
            positionY = nextY;
            surface.FrameVersion++;
            Color[] pixels = fixture.Render(presentation);
            Assert.True(pixels[(nextY + 8) * 256 + 104].B > 100,
                $"The vertical arm disappeared at y={nextY}, DPI={ownerPixelScale}.");
            Color[] patch = new Color[80 * 64];
            for (int y = 0; y < 64; y++)
            {
                Array.Copy(pixels, (nextY - 16 + y) * 256 + 80, patch, y * 80, 80);
            }
            if (reference is not null)
            {
                Assert.Equal(reference, patch);
            }
            reference = patch;
        }
    }

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
