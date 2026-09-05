using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using SkiaSharp;
using Xunit.Abstractions;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuDrawingBackendTests
{
    private readonly ITestOutputHelper output;

    public SdlGpuDrawingBackendTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void TextMissesBatchAtlasUploadAndPublishBackendTiming()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("text-timing", 160, 80, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 160, 80);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 16);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.DrawText(new DrawTextRun(font, "first", 16), new DrawPoint(4, 28), Color.White);
        drawing.DrawText(new DrawTextRun(font, "second", 16), new DrawPoint(4, 56), Color.White);

        Render(session, commands);

        Assert.Equal(
            1,
            api.GpuActions.Count(action =>
                action.StartsWith("upload-texture:", StringComparison.Ordinal)));
        IDrawingBackendFrameTimingSource timingSource =
            Assert.IsAssignableFrom<IDrawingBackendFrameTimingSource>(session.DrawingBackend);
        DrawingBackendFrameTiming timing = timingSource.LastFrameTiming;
        Assert.Equal(2, timing.TextRequestCount);
        Assert.True(timing.RasterizedPixelCount > 0);
        Assert.True(timing.TextRasterization > TimeSpan.Zero);
        Assert.True(timing.TextAtlasUpload > TimeSpan.Zero);
        Assert.True(timing.CommandRendering > TimeSpan.Zero);
    }

    [Fact]
    public void DynamicTextUploadsOnlyTheChangedAtlasRegion()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("dynamic-text-upload", 160, 80, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 160, 80);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 16);

        RenderText("score 1");
        api.GpuTextureUploads.Clear();
        RenderText("score 2");

        var upload = Assert.Single(api.GpuTextureUploads);
        Assert.True(
            upload.Destination.Width < 1024 && upload.Destination.Height < 1024,
            $"A small text change uploaded {upload.Destination.Width}x{upload.Destination.Height} pixels.");
        Assert.Equal(upload.Destination.Width, upload.Source.PixelsPerRow);
        Assert.Equal(upload.Destination.Height, upload.Source.RowsPerLayer);
        Assert.False(
            upload.Cycle,
            "Cycling a partial SDL_GPU texture upload leaves the rest of the atlas undefined.");

        void RenderText(string text)
        {
            DrawCommandList commands = new();
            new DrawingContext(commands).DrawText(
                new DrawTextRun(font, text, 16),
                new DrawPoint(4, 28),
                Color.White);
            Render(session, commands);
        }
    }

    [Fact]
    public void AdjacentGeometryBatchingHasLinearManagedAllocation()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("allocation", 256, 256, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 256, 256);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        for (int index = 0; index < 1_000; index++)
        {
            drawing.FillRectangle(
                new DrawRect(index % 250, index / 250, 1, 1),
                Color.White);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        Render(session, commands);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocated < 5_000_000,
            $"Rendering 1,000 adjacent rectangles allocated {allocated:N0} bytes.");
    }

    [Fact]
    public void WarmAdjacentGeometryBatchingReusesManagedStorage()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("allocation-reuse", 256, 256, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 256, 256);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        for (int index = 0; index < 1_000; index++)
        {
            drawing.FillRectangle(
                new DrawRect(index % 250, index / 250, 1, 1),
                Color.White);
        }
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        for (int warmupFrame = 0; warmupFrame < 3; warmupFrame++)
        {
            Render(session, commands, frame);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        Render(session, commands, frame);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.True(
            allocated < 50_000,
            $"A warm frame with 1,000 adjacent rectangles allocated {allocated:N0} bytes.");
    }

    [Fact]
    public void WarmCerberusWorkloadsRecordMeasuredAllocationAndStateBaselines()
    {
        WorkloadBaseline compatible = MeasureWarmWorkload(
            "1,000 compatible quads",
            drawCount: 1_000,
            alternateTextures: false);
        WorkloadBaseline alternating = MeasureWarmWorkload(
            "4,096 alternating textures",
            drawCount: 4_096,
            alternateTextures: true);
        WorkloadBaseline capacityGrowth = MeasureWarmWorkload(
            "257 alternating quads (exceeds all initial capacities)",
            drawCount: 257,
            alternateTextures: true);

        WriteBaseline(compatible);
        WriteBaseline(alternating);
        WriteBaseline(capacityGrowth);

        Assert.Equal(1, compatible.DrawCalls);
        Assert.Equal(4_096, alternating.DrawCalls);
        Assert.Equal(257, capacityGrowth.DrawCalls);
    }

    [Fact]
    public void GeometryFlushesUseFrameSlotBuffersAndAppendWithoutCycling()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("geometry-ring", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList warmup = new();
        new DrawingContext(warmup).FillRectangle(
            new DrawRect(0, 0, 1, 1),
            Color.White);
        Render(session, warmup);
        api.GpuActions.Clear();

        TestRenderSurface surface = new();
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.FillRectangle(
            new DrawRect(2, 2, 8, 8),
            Color.White);
        commands.Add(DrawCommand.RenderSurface2D(
            surface,
            new DrawRect(12, 2, 20, 16),
            Color.White));
        drawing.FillRectangle(
            new DrawRect(36, 2, 8, 8),
            Color.Coral);

        Render(session, commands);

        string[] vertexBindings = api.GpuActions
            .Where(action => action.StartsWith("bind-vertex:", StringComparison.Ordinal))
            .ToArray();
        string[] indexBindings = api.GpuActions
            .Where(action => action.StartsWith("bind-index:", StringComparison.Ordinal))
            .ToArray();
        string[] geometryUploads = api.GpuActions
            .Where(action => action.StartsWith("upload-buffer:", StringComparison.Ordinal))
            .ToArray();
        string[] transferMaps = api.GpuActions
            .Where(action => action.StartsWith("map-transfer:", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(3, vertexBindings.Length);
        Assert.Equal(3, indexBindings.Length);
        Assert.EndsWith(":0", vertexBindings[0], StringComparison.Ordinal);
        Assert.EndsWith(":0", indexBindings[0], StringComparison.Ordinal);
        Assert.False(vertexBindings[1].EndsWith(":0", StringComparison.Ordinal));
        Assert.False(indexBindings[1].EndsWith(":0", StringComparison.Ordinal));
        Assert.NotEqual(vertexBindings[1], vertexBindings[2]);
        Assert.NotEqual(indexBindings[1], indexBindings[2]);
        Assert.Equal(6, geometryUploads.Length);
        Assert.DoesNotContain(
            geometryUploads,
            action => action.EndsWith(":True", StringComparison.Ordinal));
        Assert.Equal(3, transferMaps.Length);
        Assert.DoesNotContain(
            transferMaps,
            action => action.EndsWith(":True", StringComparison.Ordinal));
        Assert.True(CountGpuActions(api, "bind-pipeline:") >= vertexBindings.Length);
        Assert.True(CountGpuActions(api, "bind-sampler:") >= vertexBindings.Length);
        Assert.True(CountGpuActions(api, "scissor:") >= vertexBindings.Length);
        Assert.True(CountGpuActions(api, "stencil-reference:") >= vertexBindings.Length);
    }

    [Fact]
    public void FailedFrameDoesNotPoisonPersistentCerberus()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("batch-recovery", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList failingCommands = new();
        DrawingContext failingDrawing = new(failingCommands);
        failingDrawing.FillRectangle(new DrawRect(1, 1, 8, 8), Color.White);
        failingDrawing.FillRectangle(
            new DrawRect(12, 1, 8, 8),
            new LinearGradientBrush(
                new DrawPoint(12, 1),
                new DrawPoint(20, 1),
                [new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)]));
        DrawingFrameContext failingFrame = new(
            new PrismFrameAnalyzer().Analyze(failingCommands));
        api.FailTextureCreationAt = api.TextureCreationCount + 2;

        session.BeginFrame(Color.Transparent);
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
            session.DrawingBackend.Render(failingCommands, in failingFrame));
        Assert.Contains("texture", failure.Message, StringComparison.OrdinalIgnoreCase);
        session.CompleteFrame(present: false);

        DrawCommandList recoveryCommands = new();
        new DrawingContext(recoveryCommands).FillRectangle(
            new DrawRect(2, 2, 6, 6),
            Color.Coral);

        Exception? recoveryFailure = Record.Exception(() => Render(session, recoveryCommands));

        Assert.Null(recoveryFailure);
    }

    [Fact]
    public void DrawingCommandCoverageTracksTheCompleteCoreEnum()
    {
        HashSet<DrawCommandKind> expected = Enum.GetValues<DrawCommandKind>().ToHashSet();

        Assert.True(
            expected.SetEquals(SdlGpuDrawingBackend.HandledCommandKinds),
            $"Missing: {string.Join(", ", expected.Except(SdlGpuDrawingBackend.HandledCommandKinds))}; " +
            $"unexpected: {string.Join(", ", SdlGpuDrawingBackend.HandledCommandKinds.Except(expected))}.");
    }

    [Fact]
    public void AdjacentCompatibleGeometryBatchesWithoutReorderingAndCachesAreSharedPerDevice()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("drawing-a", 64, 48, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("drawing-b", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = CreateSession(factory, api, firstWindow);
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, secondWindow);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.FillRectangle(new DrawRect(1, 1, 8, 8), Color.Red);
        drawing.FillRectangle(new DrawRect(12, 1, 8, 8), Color.Green);
        drawing.PushBlend(DrawBlendMode.Additive);
        drawing.FillRectangle(new DrawRect(23, 1, 8, 8), Color.Blue);
        drawing.PopBlend();

        Render(first, commands);

        Assert.Equal(2, api.GpuActions.Count(action => action.StartsWith("draw-indexed:", StringComparison.Ordinal)));
        Assert.Equal(2, CountGpuActions(api, "bind-pipeline:"));
        Assert.Equal(2, CountGpuActions(api, "bind-sampler:"));
        Assert.Equal(1, CountGpuActions(api, "scissor:"));
        Assert.Equal(1, CountGpuActions(api, "stencil-reference:"));
        Assert.Equal(2, first.DrawingResources.PipelineCount);
        Assert.Equal(1, first.DrawingResources.SamplerCount);
        int pipelineCount = first.DrawingResources.PipelineCount;
        int samplerCount = first.DrawingResources.SamplerCount;
        int textureCount = first.DrawingResources.CachedTextureCount;

        Render(second, commands);

        Assert.Same(first.DrawingResources, second.DrawingResources);
        Assert.Equal(pipelineCount, second.DrawingResources.PipelineCount);
        Assert.Equal(samplerCount, second.DrawingResources.SamplerCount);
        Assert.Equal(textureCount, second.DrawingResources.CachedTextureCount);
    }

    [Fact]
    public void TwoWindowBackendsKeepIndependentCerberusQueuesAndTargets()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("independent-a", 64, 48, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("independent-b", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = CreateSession(factory, api, firstWindow);
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, secondWindow, 80, 60);
        DrawCommandList firstCommands = new();
        new DrawingContext(firstCommands).FillRectangle(new DrawRect(1, 1, 8, 8), Color.Red);
        DrawCommandList secondCommands = new();
        new DrawingContext(secondCommands).FillRectangle(new DrawRect(2, 2, 10, 10), Color.Blue);
        DrawingFrameContext firstFrame = new(new PrismFrameAnalyzer().Analyze(firstCommands));
        DrawingFrameContext secondFrame = new(new PrismFrameAnalyzer().Analyze(secondCommands));

        first.BeginFrame(Color.Transparent);
        second.BeginFrame(Color.Transparent);
        first.DrawingBackend.Render(firstCommands, in firstFrame);
        second.DrawingBackend.Render(secondCommands, in secondFrame);
        first.CompleteFrame(present: false);
        second.CompleteFrame(present: false);

        Assert.NotSame(first.DrawingBackend, second.DrawingBackend);
        Assert.Equal(
            2,
            api.GpuActions.Count(action =>
                action.StartsWith("draw-indexed:", StringComparison.Ordinal)));
        Assert.True(api.RenderTargets.Select(target => target.Texture).Distinct().Count() >= 2);
    }

    [Fact]
    public void LayerTargetSwitchFlushesParentAndChildGeometryInPainterOrder()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("layer-target-switch", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.FillRectangle(new DrawRect(1, 1, 8, 8), Color.Red);
        drawing.PushLayer(new DrawLayerOptions(opacity: 0.75f));
        drawing.FillRectangle(new DrawRect(12, 1, 8, 8), Color.Green);
        drawing.PopLayer();
        drawing.FillRectangle(new DrawRect(23, 1, 8, 8), Color.Blue);

        Render(session, commands);

        nint[] textureOrder = api.FragmentSamplerBindings
            .Select(static binding => binding.Binding.Texture)
            .ToArray();
        Assert.Equal(4, textureOrder.Length);
        Assert.Equal(textureOrder[0], textureOrder[1]);
        Assert.NotEqual(textureOrder[1], textureOrder[2]);
        Assert.Equal(textureOrder[0], textureOrder[3]);
        Assert.Equal(2, api.RenderTargets.Select(target => target.Texture).Distinct().Count());
        Assert.Equal(4, CountGpuActions(api, "draw-indexed:"));
    }

    [Fact]
    public void PushOpacityCompositesOverlappingChildrenAsOneGroup()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("opacity-group", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushOpacity(0.5f);
        drawing.FillRectangle(new DrawRect(4, 4, 20, 20), Color.Red);
        drawing.FillRectangle(new DrawRect(12, 12, 20, 20), Color.Green);
        drawing.PopOpacity();

        Render(session, commands);

        nint[] textureOrder = api.FragmentSamplerBindings
            .Select(static binding => binding.Binding.Texture)
            .ToArray();
        Assert.Equal(2, api.RenderTargets.Select(target => target.Texture).Distinct().Count());
        Assert.Equal(2, CountGpuActions(api, "draw-indexed:"));
        Assert.Equal(2, textureOrder.Length);
        Assert.NotEqual(textureOrder[0], textureOrder[1]);
    }

    [Fact]
    public void AlternatingTexturesPreserveOrderAcrossFourThousandBatchBreaks()
    {
        const int drawCount = 4_096;
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("texture-alternation", 64, 64, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 64, 64);
        using SdlGpuImage first = new(1, 1, [255, 0, 0, 255]);
        using SdlGpuImage second = new(1, 1, [0, 0, 255, 255]);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        for (int index = 0; index < drawCount; index++)
        {
            drawing.DrawImage(
                (index & 1) == 0 ? first : second,
                new DrawRect(index % 64, index / 64, 1, 1),
                Color.White);
        }

        Render(session, commands);

        nint[] textureOrder = api.FragmentSamplerBindings
            .Select(static binding => binding.Binding.Texture)
            .ToArray();
        Assert.Equal(drawCount, textureOrder.Length);
        Assert.NotEqual(textureOrder[0], textureOrder[1]);
        for (int index = 0; index < textureOrder.Length; index++)
        {
            Assert.Equal(textureOrder[index & 1], textureOrder[index]);
        }
        Assert.Equal(
            drawCount,
            api.GpuActions.Count(action =>
                action.StartsWith("draw-indexed:", StringComparison.Ordinal)));
        Assert.Equal(
            1,
            api.GpuActions.Count(action =>
                action.StartsWith("bind-pipeline:", StringComparison.Ordinal)));
        Assert.Equal(
            drawCount,
            api.GpuActions.Count(action =>
                action.StartsWith("bind-sampler:", StringComparison.Ordinal)));
        Assert.Equal(
            1,
            api.GpuActions.Count(action =>
                action.StartsWith("scissor:", StringComparison.Ordinal)));
        Assert.Equal(
            1,
            api.GpuActions.Count(action =>
                action.StartsWith("stencil-reference:", StringComparison.Ordinal)));
        Assert.Equal(
            2,
            api.GpuActions.Count(action =>
                action.StartsWith("upload-texture:", StringComparison.Ordinal)));
        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            session.DrawingBackend);
        Assert.Equal(drawCount, backend.LastFrameCounters.SubmissionCount);
        Assert.Equal(0, backend.LastFrameCounters.MergedSubmissionCount);
        Assert.Equal(drawCount, backend.LastFrameCounters.DrawCallCount);
        Assert.Equal(1, backend.LastFrameCounters.PipelineBindCount);
        Assert.Equal(drawCount, backend.LastFrameCounters.SamplerBindCount);
        Assert.Equal(1, backend.LastFrameCounters.ScissorSetCount);
        Assert.Equal(1, backend.LastFrameCounters.StencilReferenceSetCount);
    }

    [Fact]
    public void NestedStateUsesScissorAndBalancedStencilOperationsThenRestoresDefaults()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("state", 64, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.PushClip(new DrawRect(2, 3, 20, 15));
        drawing.PushTransform(System.Numerics.Matrix3x2.CreateRotation(
            0.25f,
            new Vector2(10, 10)));
        drawing.PushClip(DrawPathFactory.Polygon(
            [new DrawPoint(2, 2), new DrawPoint(18, 3), new DrawPoint(9, 17)]));
        drawing.PushOpacity(0.5f);
        drawing.FillEllipse(new DrawRect(3, 3, 12, 10), Color.Coral);
        drawing.PopOpacity();
        drawing.PopClip();
        drawing.PopTransform();
        drawing.PopClip();
        drawing.FillRectangle(new DrawRect(30, 5, 8, 8), Color.White);

        Render(session, commands);

        Assert.Contains(api.GpuPipelines.Values, value => value.StencilMode == SdlGpuStencilMode.Increment);
        Assert.Contains(api.GpuPipelines.Values, value => value.StencilMode == SdlGpuStencilMode.Decrement);
        Assert.Contains(api.GpuPipelines.Values, value => value.StencilMode == SdlGpuStencilMode.Test);
        Assert.Contains(api.GpuPipelines.Values, value => value.StencilMode == SdlGpuStencilMode.Disabled);
        Assert.Contains(api.GpuActions, action => action.Contains(":2,3,20,15", StringComparison.Ordinal));
        Assert.EndsWith(":0", api.GpuActions.Last(action => action.StartsWith("stencil-reference:", StringComparison.Ordinal)));
    }

    [Fact]
    public void RasterImageLoaderUploadsOnceAndDisposalInvalidatesTheDeviceCache()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("image", 32, 24, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 32, 24);
        using MemoryStream encoded = CreatePng();
        SdlGpuImage image = Assert.IsType<SdlGpuImage>(session.ImageLoader!.Load(encoded));
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawImage(image, new DrawRect(1, 1, 8, 8), Color.White);

        Render(session, commands);
        int cachedAfterFirstFrame = session.DrawingResources.CachedTextureCount;
        Render(session, commands);

        Assert.Equal(cachedAfterFirstFrame, session.DrawingResources.CachedTextureCount);
        Assert.Equal(2, image.Width);
        Assert.Equal(2, image.Height);

        image.Dispose();

        Assert.Equal(cachedAfterFirstFrame - 1, session.DrawingResources.CachedTextureCount);
    }

    [Fact]
    public void ThousandsOfDistinctTexturesCanBeInvalidatedAndRecreatedWithoutResourceGrowth()
    {
        const int texturesPerCycle = 2_048;
        const int cycleCount = 2;
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("texture-churn", 64, 64, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 64, 64);
        DrawCommandList empty = new();
        using (SdlGpuImage warmupImage = new(1, 1, [255, 255, 255, 255]))
        {
            DrawCommandList warmup = new();
            new DrawingContext(warmup).DrawImage(
                warmupImage,
                new DrawRect(0, 0, 1, 1),
                Color.White);
            for (int warmupFrame = 0; warmupFrame < 3; warmupFrame++)
            {
                Render(session, warmup);
            }
        }
        Render(session, empty);
        int baselineGpuTextureCount = api.GpuTextures.Count;
        int baselineReleasedTextureCount = api.ReleasedGpuTextures.Count;
        (int Buffers, int Transfers, int Pipelines, int Samplers)? steadyNonTextureCounts = null;

        for (int cycle = 0; cycle < cycleCount; cycle++)
        {
            SdlGpuImage[] images = new SdlGpuImage[texturesPerCycle];
            DrawCommandList commands = new();
            DrawingContext drawing = new(commands);
            for (int index = 0; index < images.Length; index++)
            {
                byte value = unchecked((byte)index);
                images[index] = new SdlGpuImage(
                    1,
                    1,
                    [value, (byte)(value ^ 0x5A), (byte)(value ^ 0xA5), 255]);
                drawing.DrawImage(
                    images[index],
                    new DrawRect(index % 64, (index / 64) % 64, 1, 1),
                    Color.White);
            }

            int creationsBefore = api.TextureCreationCount;
            int uploadsBefore = api.GpuActions.Count(action =>
                action.StartsWith("upload-texture:", StringComparison.Ordinal));
            try
            {
                Render(session, commands);

                Assert.Equal(
                    texturesPerCycle,
                    api.TextureCreationCount - creationsBefore);
                Assert.Equal(
                    texturesPerCycle,
                    api.GpuActions.Count(action =>
                        action.StartsWith("upload-texture:", StringComparison.Ordinal)) - uploadsBefore);
                Assert.Equal(texturesPerCycle, session.DrawingResources.CachedTextureCount);
                Assert.Equal(
                    baselineGpuTextureCount + texturesPerCycle,
                    api.GpuTextures.Count);
            }
            finally
            {
                foreach (SdlGpuImage image in images)
                {
                    image?.Dispose();
                }
            }

            Assert.Equal(0, session.DrawingResources.CachedTextureCount);

            Render(session, empty);

            Assert.Equal(baselineGpuTextureCount, api.GpuTextures.Count);
            Assert.Equal(
                baselineReleasedTextureCount + ((cycle + 1) * texturesPerCycle),
                api.ReleasedGpuTextures.Count);
            (int Buffers, int Transfers, int Pipelines, int Samplers) currentNonTextureCounts =
                (api.GpuBuffers.Count, api.TransferBuffers.Count,
                    api.GpuPipelines.Count, api.GpuSamplers.Count);
            if (steadyNonTextureCounts is { } steady)
            {
                Assert.Equal(steady, currentNonTextureCounts);
            }
            else
            {
                steadyNonTextureCounts = currentNonTextureCounts;
            }
        }
    }

    [Fact]
    public void ShapedTextAtlasIsSharedPerDeviceAndSubpixelPhase()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint firstWindow = api.CreateWindow("text-a", 96, 48, SdlWindowOptions.Hidden);
        nint secondWindow = api.CreateWindow("text-b", 96, 48, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession first = CreateSession(factory, api, firstWindow, 96, 48);
        using SdlGpuWindowGraphicsSession second = CreateSession(factory, api, secondWindow, 96, 48);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 16);
        DrawTextRun run = new(font, "AV fi", 16);
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawText(run, new DrawPoint(4.25f, 24.5f), Color.White);

        Render(first, commands);
        int firstCount = first.DrawingResources.CachedTextureCount;
        Render(first, commands);

        DrawCommandList samePhaseCommands = new();
        new DrawingContext(samePhaseCommands).DrawText(
            run,
            new DrawPoint(4.26f, 24.49f),
            Color.White);
        Render(first, samePhaseCommands);
        Render(second, commands);

        Assert.Same(first.DrawingResources, second.DrawingResources);
        Assert.Equal(firstCount, first.DrawingResources.CachedTextureCount);
        Assert.Equal(1, first.DrawingResources.TextAtlasPageCount);
        Assert.Equal(3, first.DrawingResources.TextAtlasEntryCount);
        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.ColorWriteMask == SdlGpuColorWriteMask.Red);
        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.ColorWriteMask == SdlGpuColorWriteMask.Green);
        Assert.Contains(api.GpuPipelines.Values, pipeline =>
            pipeline.ColorWriteMask == SdlGpuColorWriteMask.Blue);
    }

    [Fact]
    public void DynamicTextReusesInactiveAtlasPagesInsteadOfRetainingHistoricalRuns()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("dynamic-text-atlas", 640, 160, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 640, 160);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 48);

        for (int frame = 0; frame < 80; frame++)
        {
            RenderDynamicTextFrame(session, font, frame);
        }

        Assert.InRange(session.DrawingResources.TextAtlasPageCount, 1, 2);

        static void RenderDynamicTextFrame(
            SdlGpuWindowGraphicsSession session,
            IDrawFont font,
            int frame)
        {
            DrawCommandList commands = new();
            DrawingContext drawing = new(commands);
            drawing.DrawText(
                new DrawTextRun(font, "SCORE", 48),
                new DrawPoint(4, 54),
                Color.White);
            drawing.DrawText(
                new DrawTextRun(font, $"value {frame:D8}", 48),
                new DrawPoint(4, 118),
                Color.White);
            Render(session, commands);
        }
    }

    [Fact]
    public void DynamicTextReusesAtlasPageStorageAcrossCompactionCycles()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("dynamic-text-atlas-allocation", 640, 160, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 640, 160);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 48);

        for (int frame = 0; frame < 5; frame++)
        {
            RenderDynamicTextFrame(frame);
        }

        long maximumFrameAllocation = 0;
        for (int frame = 5; frame < 240; frame++)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            RenderDynamicTextFrame(frame);
            maximumFrameAllocation = Math.Max(
                maximumFrameAllocation,
                GC.GetAllocatedBytesForCurrentThread() - before);
        }

        Assert.True(
            maximumFrameAllocation < 3_000_000,
            $"A warm dynamic-text frame allocated {maximumFrameAllocation:N0} bytes.");

        void RenderDynamicTextFrame(int frame)
        {
            DrawCommandList commands = new();
            DrawingContext drawing = new(commands);
            drawing.DrawText(
                new DrawTextRun(font, "SCORE", 48),
                new DrawPoint(4, 54),
                Color.White);
            drawing.DrawText(
                new DrawTextRun(font, $"value {frame:D8}", 48),
                new DrawPoint(4, 118),
                Color.White);
            Render(session, commands);
        }
    }

    [Fact]
    public void RenderSurfaceRetainsUnchangedFramesAndRecreatesOnlyAfterResize()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        TestRenderSurface source = new();

        Render(session, SurfaceCommands(source, 20, 16));
        Assert.Equal(1, source.RecordCount);
        int createdAfterFirst = api.TextureCreationCount;
        Render(session, SurfaceCommands(source, 20, 16));
        Assert.Equal(1, source.RecordCount);
        Assert.Equal(createdAfterFirst, api.TextureCreationCount);

        source.FrameVersion++;
        Render(session, SurfaceCommands(source, 20, 16));
        Assert.Equal(2, source.RecordCount);
        Assert.Equal(createdAfterFirst, api.TextureCreationCount);

        Render(session, SurfaceCommands(source, 24, 18));
        Assert.Equal(3, source.RecordCount);
        Assert.Equal(createdAfterFirst + 2, api.TextureCreationCount);
    }

    [Fact]
    public void RenderSurfaceExecutesNestedPrismScopes()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-prism", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        using SdlGpuImage sourceImage = new(1, 1, [255, 0, 0, 255]);
        using PrismImage prismImage = global::Cerneala.Drawing.Prism.Prism.Apply(
            sourceImage,
            new InvertFilter());
        TestRenderSurface source = new((drawing, bounds) =>
            drawing.DrawImage(prismImage, bounds, Color.White));

        Render(session, SurfaceCommands(source, 20, 16));

        Assert.Contains(
            api.FragmentSamplerBindings,
            binding => binding.Slot == 14);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedPrismPresentationRestoresBatchTargetForFollowingGeometry(bool inRenderSurface)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("nested-prism-sibling", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        DrawCommandList commands = new();
        if (inRenderSurface)
        {
            commands = SurfaceCommands(new TestRenderSurface(commandRecorder: RecordNested), 20, 16);
        }
        else { RecordNested(commands); }

        Render(session, commands);

        // Three ordinary quads, the nested presentation and the outer presentation;
        // the surface case adds its final host-window composite.
        Assert.Equal(inRenderSurface ? 6 : 5, CountGpuActions(api, "draw-indexed:"));

        static void RecordNested(DrawCommandList destination)
        {
            DrawRect bounds = new(0, 0, 20, 16);
            PrismCompositionDefinition definition = new("NestedSibling", [new PrismLayerDefinition(
                new PrismNodeId(1), "Invert", filters: [new PrismFilterDefinition(PrismFilterId.Invert)])]);
            destination.Add(DrawCommand.BeginPrism(new PrismDrawScope(new PrismInstance(definition),
                new PrismCacheOwnerToken(501), bounds, System.Numerics.Matrix3x2.Identity, 1, 1)));
            destination.Add(DrawCommand.FillRectangle(new(0, 0, 4, 4), Color.Red));
            destination.Add(DrawCommand.BeginPrism(new PrismDrawScope(new PrismInstance(definition),
                new PrismCacheOwnerToken(502), bounds, System.Numerics.Matrix3x2.Identity, 1, 1)));
            destination.Add(DrawCommand.FillRectangle(new(6, 0, 4, 4), Color.Green));
            destination.Add(DrawCommand.EndPrism());
            destination.Add(DrawCommand.FillRectangle(new(12, 0, 4, 4), Color.Blue));
            destination.Add(DrawCommand.EndPrism());
        }
    }

    [Fact]
    public void RenderSurfacePrismInvalidationsDoNotRetainHistoricalImageVersions()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-prism-invalidation", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        using SdlGpuImage sourceImage = new(1, 1, [255, 0, 0, 255]);
        BlurFilter blur = new() { Radius = 1 };
        using PrismImage prismImage = global::Cerneala.Drawing.Prism.Prism.Apply(
            sourceImage,
            blur);
        IDrawImageInvalidationSource invalidationSource = prismImage;
        EventHandler observer = (_, _) => { };
        invalidationSource.ContentChanged += observer;
        TestRenderSurface source = new((drawing, bounds) =>
            drawing.DrawImage(prismImage, bounds, Color.White));

        Render(session, SurfaceCommands(source, 20, 16));
        int warmedRetainedCount = session.DrawingResources.PrismResources.RetainedCount;

        for (int radius = 2; radius <= 10; radius++)
        {
            blur.Radius = radius;
            source.FrameVersion++;
            Render(session, SurfaceCommands(source, 20, 16));
            Assert.True(
                session.DrawingResources.PrismResources.RetainedCount <= warmedRetainedCount,
                "RenderSurface retained a historical PrismImage result after invalidation.");
        }

        invalidationSource.ContentChanged -= observer;
    }

    [Fact]
    public void RenderSurfaceExecutesComposedPrismImagesThroughDrawImageOptions()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-composed-prism", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        using SdlGpuImage sourceImage = new(1, 1, [255, 0, 0, 255]);
        using PrismImage inner = global::Cerneala.Drawing.Prism.Prism.Apply(
            sourceImage,
            new InvertFilter());
        using PrismImage outer = global::Cerneala.Drawing.Prism.Prism.Apply(
            inner,
            new InvertFilter());
        DrawCommandList commands = new();
        new DrawingContext(commands).DrawImage(
            outer,
            new DrawRect(4, 3, 20, 16),
            new DrawImageOptions(sampling: DrawSamplingMode.Linear));

        Render(session, commands);

        SdlGpuDrawingBackend backend = Assert.IsType<SdlGpuDrawingBackend>(
            session.DrawingBackend);
        Assert.True(backend.PrismDiagnostics.Counters.PassCount > 0);
        string report = backend.PrismDiagnostics.DumpExecutedGraph();
        Assert.Contains(" NestedPresent ", report, StringComparison.Ordinal);
    }

    [Fact]
    public void ComposedPrismImageCanBeDrawnTwiceInOneFrame()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("reused-composed-prism", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        using SdlGpuImage sourceImage = new(1, 1, [255, 0, 0, 255]);
        using PrismImage inner = global::Cerneala.Drawing.Prism.Prism.Apply(
            sourceImage,
            new InvertFilter());
        using PrismImage outer = global::Cerneala.Drawing.Prism.Prism.Apply(
            inner,
            new InvertFilter());
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        DrawImageOptions options = new(sampling: DrawSamplingMode.Linear);
        drawing.DrawImage(outer, new DrawRect(4, 3, 20, 16), options);
        drawing.DrawImage(outer, new DrawRect(28, 3, 20, 16), options);

        Render(session, commands);

        int prismPasses = api.FragmentSamplerBindings.Count(
            binding => binding.Slot == 14);
        Assert.True(
            prismPasses >= 4,
            $"Expected at least four composed Prism passes, observed {prismPasses}.");
    }

    [Fact]
    public void CachedRenderSurfaceRestoresParentBatchAfterPrecedingGeometry()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-parent-batch", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        TestRenderSurface source = new();
        DrawCommandList commands = new();
        commands.Add(DrawCommand.FillRectangle(
            new DrawRect(0, 0, 8, 8),
            Color.White));
        commands.Add(DrawCommand.RenderSurface2D(
            source,
            new DrawRect(4, 3, 20, 16),
            Color.White));

        Render(session, commands);
        Render(session, commands);

        Assert.Equal(1, source.RecordCount);
    }

    [Fact]
    public void MultisampledRenderSurfaceResolvesBeforeItIsSampled()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("surface-msaa", 80, 60, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: true);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 80, 60);
        TestRenderSurface source = new();

        Render(session, SurfaceCommands(source, 20, 16));

        KeyValuePair<nint, FakeSdlApi.FakeGpuTexture> resolve = Assert.Single(
            api.GpuTextures.Where(texture =>
                texture.Value.CreateInfo.Width == 20 &&
                texture.Value.CreateInfo.Height == 16 &&
                texture.Value.CreateInfo.SampleCount == SdlGpuSampleCount.One &&
                texture.Value.CreateInfo.Usage.HasFlag(SdlGpuTextureUsage.Sampler)));
        Assert.Contains(api.GpuTextures.Values, texture =>
            texture.CreateInfo.Width == 20 &&
            texture.CreateInfo.Height == 16 &&
            texture.CreateInfo.SampleCount == SdlGpuSampleCount.Four &&
            texture.CreateInfo.Usage == SdlGpuTextureUsage.ColorTarget);
        Assert.Contains(api.GpuActions, action =>
            action.StartsWith("bind-sampler:", StringComparison.Ordinal) &&
            action.Contains($":{resolve.Key}:", StringComparison.Ordinal));
    }

    [Fact]
    public void RepeatedDrawingFramesDoNotGrowGpuResourceCountsAndDisposeEverything()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("budget", 64, 48, SdlWindowOptions.Hidden);
        SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        drawing.FillRectangle(new DrawRect(2, 2, 30, 20), new LinearGradientBrush(
            new DrawPoint(2, 2),
            new DrawPoint(32, 2),
            [new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)]));
        drawing.DrawLine(new DrawPoint(1, 30), new DrawPoint(50, 31), Color.White, 2);
        IDrawFont font = new SystemFontSource().LoadFont("Arial", 12);
        drawing.DrawText(
            new DrawTextRun(font, "atlas budget", 12),
            new DrawPoint(2.25f, 44.5f),
            Color.White);

        for (int warmupFrame = 0; warmupFrame < 3; warmupFrame++)
        {
            Render(session, commands);
        }
        (int Textures, int Pipelines, int Samplers, int Buffers, int Transfers) baseline =
            (api.GpuTextures.Count, api.GpuPipelines.Count, api.GpuSamplers.Count,
                api.GpuBuffers.Count, api.TransferBuffers.Count);
        for (int iteration = 0; iteration < 30; iteration++)
        {
            Render(session, commands);
            Assert.Equal(baseline, (
                api.GpuTextures.Count,
                api.GpuPipelines.Count,
                api.GpuSamplers.Count,
                api.GpuBuffers.Count,
                api.TransferBuffers.Count));
        }

        session.Dispose();
        factory.Dispose();

        Assert.Empty(api.GpuTextures);
        Assert.Empty(api.GpuPipelines);
        Assert.Empty(api.GpuSamplers);
        Assert.Empty(api.GpuBuffers);
        Assert.Empty(api.TransferBuffers);
        Assert.Empty(api.GpuShaders);
    }

    private static SdlGpuWindowGraphicsSession CreateSession(
        SdlGpuWindowGraphicsSessionFactory factory,
        FakeSdlApi api,
        nint window,
        int width = 64,
        int height = 48) =>
        Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)),
            width,
            height,
            coordinateScale: 1));

    private WorkloadBaseline MeasureWarmWorkload(
        string name,
        int drawCount,
        bool alternateTextures)
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow(name, 256, 256, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = CreateSession(factory, api, window, 256, 256);
        using SdlGpuImage first = new(1, 1, [255, 0, 0, 255]);
        using SdlGpuImage second = new(1, 1, [0, 0, 255, 255]);
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        for (int index = 0; index < drawCount; index++)
        {
            DrawRect destination = new(index % 256, (index / 256) % 256, 1, 1);
            if (alternateTextures)
            {
                drawing.DrawImage((index & 1) == 0 ? first : second, destination, Color.White);
            }
            else
            {
                drawing.FillRectangle(destination, Color.White);
            }
        }

        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
        for (int warmupFrame = 0; warmupFrame < 3; warmupFrame++)
        {
            Render(session, commands, frame);
            ClearRecordedGpuActions(api);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        Render(session, commands, frame);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        return new WorkloadBaseline(
            name,
            allocated,
            CountGpuActions(api, "draw-indexed:"),
            CountGpuActions(api, "bind-pipeline:"),
            CountGpuActions(api, "bind-sampler:"),
            CountGpuActions(api, "scissor:"),
            CountGpuActions(api, "stencil-reference:"));
    }

    private void WriteBaseline(WorkloadBaseline baseline) => output.WriteLine(
        $"{baseline.Name}: allocated={baseline.AllocatedBytes}; " +
        $"draws={baseline.DrawCalls}; pipelines={baseline.PipelineBinds}; " +
        $"samplers={baseline.SamplerBinds}; scissors={baseline.ScissorSets}; " +
        $"stencilReferences={baseline.StencilReferenceSets}");

    private static int CountGpuActions(FakeSdlApi api, string prefix) =>
        api.GpuActions.Count(action => action.StartsWith(prefix, StringComparison.Ordinal));

    private static void ClearRecordedGpuActions(FakeSdlApi api)
    {
        api.GpuActions.Clear();
        api.FragmentSamplerBindings.Clear();
        api.RenderTargets.Clear();
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frame = new(analysis);
        Render(session, commands, frame);
    }

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands,
        DrawingFrameContext frame)
    {
        session.BeginFrame(Color.Transparent);
        session.DrawingBackend.Render(commands, in frame);
        session.CompleteFrame(present: false);
    }

    private static MemoryStream CreatePng()
    {
        using SKBitmap bitmap = new(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul);
        bitmap.SetPixel(0, 0, SKColors.Red);
        bitmap.SetPixel(1, 0, SKColors.Green);
        bitmap.SetPixel(0, 1, SKColors.Blue);
        bitmap.SetPixel(1, 1, SKColors.White);
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray(), writable: false);
    }

    private static DrawCommandList SurfaceCommands(
        IRenderSurface2DSource source,
        float width,
        float height)
    {
        DrawCommandList commands = new();
        commands.Add(DrawCommand.RenderSurface2D(
            source,
            new DrawRect(4, 3, width, height),
            Color.White));
        return commands;
    }

    private sealed class TestRenderSurface : IRenderSurface2DFrameSource
    {
        private readonly Dictionary<object, IRenderSurface2DBackendState?> states = [];
        private readonly Action<DrawingContext, DrawRect>? record;
        private readonly Action<DrawCommandList>? commandRecorder;

        public TestRenderSurface(Action<DrawingContext, DrawRect>? record = null, Action<DrawCommandList>? commandRecorder = null)
        {
            this.record = record;
            this.commandRecorder = commandRecorder;
        }

        public Color ClearColor => new(10, 20, 30, 255);

        public long FrameVersion { get; set; }

        public int RecordCount { get; private set; }

        public void RecordFrame(DrawCommandList commands, DrawRect bounds)
        {
            RecordCount++;
            if (commandRecorder is not null) { commandRecorder(commands); return; }
            DrawingContext drawing = new(commands);
            if (record is null)
            {
                drawing.FillRectangle(bounds, Color.Coral);
            }
            else
            {
                record(drawing, bounds);
            }
        }

        public IRenderSurface2DBackendState? GetBackendState(object owner) =>
            states.GetValueOrDefault(owner);

        public void SetBackendState(object owner, IRenderSurface2DBackendState? state)
        {
            if (state is null)
            {
                states.Remove(owner);
            }
            else
            {
                states[owner] = state;
            }
        }
    }

    private readonly record struct WorkloadBaseline(
        string Name,
        long AllocatedBytes,
        int DrawCalls,
        int PipelineBinds,
        int SamplerBinds,
        int ScissorSets,
        int StencilReferenceSets);
}
