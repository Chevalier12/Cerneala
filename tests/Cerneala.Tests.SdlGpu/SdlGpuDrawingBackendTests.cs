using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Media;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuDrawingBackendTests
{
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

        Render(session, commands);
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

    private static void Render(
        SdlGpuWindowGraphicsSession session,
        DrawCommandList commands)
    {
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frame = new(analysis);
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

        public Color ClearColor => new(10, 20, 30, 255);

        public long FrameVersion { get; set; }

        public int RecordCount { get; private set; }

        public void RecordFrame(DrawCommandList commands, DrawRect bounds)
        {
            RecordCount++;
            new DrawingContext(commands).FillRectangle(bounds, Color.Coral);
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
}
