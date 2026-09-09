using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

public sealed class SdlGpuTextCacheContractTests
{
    // Exercise the renderer's existing functions, not a recreated diagnostic facade.
    private static readonly Func<DrawPoint, float, DrawPoint> CanonicalPhase =
        typeof(SdlGpuDrawingBackend).GetMethod("CanonicalPhase", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<DrawPoint, float, DrawPoint>>();

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.0624f, 0f)]
    [InlineData(0.0626f, 0.125f)]
    [InlineData(0.9999f, 0f)]
    [InlineData(-0.0001f, 0f)]
    [InlineData(-0.0624f, 0f)]
    [InlineData(-0.0626f, 0.875f)]
    public void CanonicalPixelPhaseHandlesNegativeAndBoundaryPositions(float position, float expected)
    {
        Assert.Equal(new DrawPoint(expected, expected), CanonicalPhase(new DrawPoint(position, position), 1));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void IntermediateRasterizerPositionSelectsZeroCanonicalPhase(float scale)
    {
        DrawPoint position = new(20.049f / scale, 30.049f / scale);
        Assert.Equal(new DrawPoint(0, 0), CanonicalPhase(position, scale));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void LongTranslationProducesAtMostEightPhasesPerAxis(float scale)
    {
        HashSet<DrawPoint> phases = [];
        int hits = 0;
        for (int step = -4_096; step <= 4_096; step++)
        {
            if (!phases.Add(CanonicalPhase(new DrawPoint(step / 97f, step / 89f), scale))) { hits++; }
        }
        Assert.InRange(phases.Count, 2, 64);
        Assert.True(hits > 8_000);
    }

    [Fact]
    public void TextTextureOriginMapsBaselineToTightTextureOrigin()
    {
        using Fixture fixture = new();
        var map = typeof(SdlGpuDrawingBackend)
            .GetMethod("CreateTextDestination", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Func<DrawPoint, DrawPoint, int, int, DrawRect>>(fixture.Backend);
        DrawRect destination = map(new DrawPoint(20, 30), new DrawPoint(-10.5f, -16.25f), 8, 8);
        Assert.Equal(new DrawRect(10, 14, 8, 8), destination);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SolidAndBrushTextReuseTheSameCanonicalGeometryPhase(bool useBrush)
    {
        using Fixture fixture = new();
        fixture.Session.Resize(160, 80, 1.25f);
        IDrawBrush brush = useBrush
            ? new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(80, 0),
                [new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)])
            : new SolidColorBrush(Color.White);
        fixture.Render("animated", new DrawPoint(10.01f, -2.01f), brush);
        int created = fixture.Api.TextureCreationCount;
        fixture.Render("animated", new DrawPoint(10.02f, -2.02f), brush);
        Assert.Equal(created, fixture.Api.TextureCreationCount);
    }

    [Fact]
    public void CacheReturnsToAWithoutRasterMissAfterABASwitchAndColorChange()
    {
        using Fixture fixture = new();
        fixture.Render("A", default, new SolidColorBrush(Color.Black));
        fixture.Render("B", default, new SolidColorBrush(Color.Black));
        int entries = fixture.Resources.TextAtlasEntryCount;
        fixture.Render("A", default, new SolidColorBrush(Color.White));
        Assert.Equal(entries, fixture.Resources.TextAtlasEntryCount);
        Assert.Equal(6, entries);
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);
    }

    [Fact]
    public void CacheRetainsLargeVariantsAcrossABASwitchesWithinItsPageBudget()
    {
        using Fixture fixture = new();
        KeySet first = new("large A"), second = new("large B");
        long firstFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(first, firstFrame, 400));
        fixture.Resources.EndTextAtlasFrame(firstFrame);

        long secondFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(second, secondFrame, 400));
        fixture.Resources.EndTextAtlasFrame(secondFrame);

        long thirdFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.True(fixture.TryGet(first, thirdFrame, out _),
            "Closing a frame must not discard a fitting cached variant just because it was not used in that frame.");
        Assert.True(fixture.TryGet(second, thirdFrame, out _));
        Assert.InRange(fixture.Resources.TextAtlasPageCount, 1, 2);
        fixture.Resources.EndTextAtlasFrame(thirdFrame);
    }

    [Fact]
    public void AtlasUsesAvailablePageBudgetBeforeEvictingInactiveVariants()
    {
        using Fixture fixture = new();
        KeySet first = new("first"), second = new("second");
        long firstFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(first, firstFrame, 600));
        fixture.Resources.EndTextAtlasFrame(firstFrame);

        long secondFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(second, secondFrame, 600));
        Assert.Equal(6, fixture.Resources.TextAtlasPageCount);
        Assert.True(fixture.TryGet(first, secondFrame, out _),
            "Inactive variants must not be evicted while the eight-page budget still has room.");
        fixture.Resources.EndTextAtlasFrame(secondFrame);
    }

    [Fact]
    public void CompletingAnAtlasFrameDoesNotAllocatePixelSnapshots()
    {
        using Fixture fixture = new();
        KeySet first = new("first"), second = new("second");
        long firstFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(first, firstFrame, 400));
        fixture.Resources.EndTextAtlasFrame(firstFrame);

        long secondFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(second, secondFrame, 400));
        long before = GC.GetAllocatedBytesForCurrentThread();
        fixture.Resources.EndTextAtlasFrame(secondFrame);
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.InRange(allocated, 0, 4_096);
        long thirdFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.True(fixture.TryGet(second, thirdFrame, out _));
        fixture.Resources.EndTextAtlasFrame(thirdFrame);
    }

    [Fact]
    public void TextRasterKeySeparatesGeometryAndExcludesForegroundColor()
    {
        object firstFont = new(), secondFont = new();
        SdlGpuTextRasterKey key = new(firstFont, "key", 16, 1, new DrawPoint(0.125f, 0.125f));
        Assert.NotEqual(key, key with { FontIdentity = secondFont });
        Assert.NotEqual(key, key with { Size = 17 });
        Assert.NotEqual(key, key with { CoordinateScale = 1.25f });
        Assert.NotEqual(key, key with { Text = "changed" });
        Assert.DoesNotContain(typeof(SdlGpuTextRasterKey).GetProperties(), property => property.Name.Contains("Color"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(64)]
    [InlineData(2)]
    public void WarmStaticAnimatedAndABALookupsDoNotAllocateOrMiss(int variantCount)
    {
        using Fixture fixture = new();
        long token = fixture.Resources.BeginTextAtlasFrame();
        KeySet[] variants = Enumerable.Range(0, variantCount).Select(i => new KeySet($"variant-{i}")).ToArray();
        foreach (KeySet key in variants)
        {
            Assert.NotNull(fixture.Add(key, token, 8));
            Assert.True(fixture.TryGet(key, token, out _));
        }
        int[] sequence = variantCount == 2 ? [0, 1, 0] : Enumerable.Range(0, variantCount).ToArray();
        int misses = 0;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int iteration = 0; iteration < 1_000; iteration++)
        {
            if (!fixture.TryGet(variants[sequence[iteration % sequence.Length]], token, out _)) { misses++; }
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(0, misses);
        Assert.InRange(allocated, 0, 1_024);
        fixture.Resources.EndTextAtlasFrame(token);
    }

    [Fact]
    public void CompletingOneFrameCannotEvictOrMoveAnotherActiveFramesAtlasEntries()
    {
        using Fixture fixture = new();
        long firstFrame = fixture.Resources.BeginTextAtlasFrame();
        long secondFrame = fixture.Resources.BeginTextAtlasFrame();
        KeySet first = new("first"), second = new("second");
        Assert.NotNull(fixture.Add(first, firstFrame, 600));
        SdlGpuTextAtlasEntries active = fixture.Add(second, secondFrame, 600)!.Value;
        fixture.Resources.EndTextAtlasFrame(firstFrame);
        Assert.True(fixture.TryGet(second, secondFrame, out SdlGpuTextAtlasEntries retained));
        for (int channel = 0; channel < 3; channel++)
        {
            Assert.Same(active[channel].Texture, retained[channel].Texture);
            Assert.Equal(active[channel].TextureCoordinates, retained[channel].TextureCoordinates);
            Assert.Contains(active[channel].Texture.Handle, fixture.Api.GpuTextures.Keys);
        }
        fixture.Resources.EndTextAtlasFrame(secondFrame);
    }

    [Fact]
    public void AtlasOverflowEvictsLeastRecentlyUsedInactivePagesWithoutReleasingActivePages()
    {
        using Fixture fixture = new();
        long firstFrame = fixture.Resources.BeginTextAtlasFrame();
        KeySet first = new("A"), second = new("B"), overflow = new("C"), replacement = new("D");
        Assert.NotNull(fixture.Add(first, firstFrame, 600));
        Assert.NotNull(fixture.Add(second, firstFrame, 600));
        // Three channels require three pages at this size. The final request
        // reaches SDL's fixed eight-page cap and takes the existing fallback.
        Assert.Null(fixture.Add(overflow, firstFrame, 600));
        fixture.Resources.EndTextAtlasFrame(firstFrame);
        Assert.Equal(8, fixture.Resources.TextAtlasPageCount);
        int created = fixture.Api.TextureCreationCount;

        long secondFrame = fixture.Resources.BeginTextAtlasFrame();
        Assert.True(fixture.TryGet(first, secondFrame, out SdlGpuTextAtlasEntries active));
        Assert.NotNull(fixture.Add(replacement, secondFrame, 600));
        Assert.False(fixture.TryGet(second, secondFrame, out _));
        Assert.True(fixture.TryGet(first, secondFrame, out SdlGpuTextAtlasEntries retained));
        Assert.True(fixture.TryGet(replacement, secondFrame, out _));
        Assert.Equal(8, fixture.Resources.TextAtlasPageCount);
        Assert.Equal(created, fixture.Api.TextureCreationCount);
        for (int channel = 0; channel < 3; channel++)
        {
            Assert.Same(active[channel].Texture, retained[channel].Texture);
            Assert.Contains(active[channel].Texture.Handle, fixture.Api.GpuTextures.Keys);
        }
        fixture.Resources.EndTextAtlasFrame(secondFrame);
    }

    [Fact]
    public void CoordinateScaleChangeCannotReuseCoverageAtTheOldScale()
    {
        using Fixture fixture = new();
        IDrawBrush brush = new SolidColorBrush(Color.White);
        fixture.Render("scale", default, brush);
        fixture.Session.Resize(160, 80, 1.25f);
        fixture.Render("scale", default, brush);
        Assert.Equal(1, fixture.Backend.LastFrameTiming.TextRequestCount);
        fixture.Render("scale", default, brush);
        Assert.Equal(0, fixture.Backend.LastFrameTiming.TextRequestCount);
    }

    [Fact]
    public void ResourceOwnerDisposalClearsCachedTextAndIsIdempotent()
    {
        using Fixture fixture = new();
        SdlGpuDrawingResources resources = fixture.Resources;
        fixture.Render("dispose", default, new SolidColorBrush(Color.White));
        Assert.True(resources.TextAtlasEntryCount > 0);
        fixture.Dispose();
        fixture.Dispose();
        Assert.Equal(0, resources.TextAtlasEntryCount);
        Assert.Equal(0, resources.CachedTextureCount);
        Assert.Empty(fixture.Api.GpuTextures);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DynamicBrushCapturesAndDependentTextMasksAreBoundedAndRetired(bool text)
    {
        using Fixture fixture = new();
        DrawRect bounds = new(0, 0, 20, 10);
        DrawCommandList warmup = new();
        warmup.Add(DrawCommand.FillRectangle(bounds, Color.White));
        fixture.Render(warmup);
        int baseline = fixture.Api.GpuTextures.Count;
        using SdlGpuImage image = new(1, 1, [100, 50, 25, 128]);
        for (int frame = 0; frame < 64; frame++)
        {
            if (text)
            {
                fixture.Render($"label {frame}", new DrawPoint(4, 24), new ImageBrush(image));
            }
            else
            {
                DrawingBrush brush = new([DrawCommand.FillRectangle(bounds, new Color((byte)frame, 100, 200))], bounds);
                DrawCommandList commands = new();
                commands.Add(DrawCommand.FillRectangle(bounds, brush));
                fixture.Render(commands);
            }
            // CompleteFrame flushes retirement before EndFrame enqueues newly
            // unused captures. Count one live and one pending capture (+ masks
            // and the shared source image for text), not synchronous destruction.
            Assert.InRange(fixture.Api.GpuTextures.Count, baseline, baseline + (text ? 7 : 4));
        }
        image.Dispose();
        fixture.Render(new DrawCommandList());
        fixture.Render(new DrawCommandList());
        Assert.Equal(baseline, fixture.Api.GpuTextures.Count);
    }

    private sealed class KeySet
    {
        internal KeySet(string text)
        {
            SdlGpuTextRasterKey raster = new(this, text, 16, 1, default);
            Red = new(raster, SdlGpuColorWriteMask.Red);
            Green = new(raster, SdlGpuColorWriteMask.Green);
            Blue = new(raster, SdlGpuColorWriteMask.Blue);
        }
        internal SdlGpuTextLayerTextureKey Red { get; }
        internal SdlGpuTextLayerTextureKey Green { get; }
        internal SdlGpuTextLayerTextureKey Blue { get; }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly FakeSdlApi Api = new() { WindowPixelDensity = 1 };
        private readonly SdlGpuWindowGraphicsSessionFactory factory;
        private readonly IDrawFont font = new SystemFontSource().LoadFont("Arial", 16);
        internal readonly SdlGpuWindowGraphicsSession Session;
        internal SdlGpuDrawingResources Resources => Session.DrawingResources;
        internal SdlGpuDrawingBackend Backend => Assert.IsType<SdlGpuDrawingBackend>(Session.DrawingBackend);

        internal Fixture()
        {
            nint window = Api.CreateWindow("text cache contract", 160, 80, SdlWindowOptions.Hidden);
            factory = new(Api, useMultisampling: false);
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, Api.GetWindowId(window)), 160, 80, 1));
        }

        internal void Render(string text, DrawPoint position, IDrawBrush brush)
        {
            DrawCommandList commands = new();
            commands.Add(DrawCommand.DrawText(new DrawTextRun(font, text, 16), position, brush));
            Render(commands);
        }

        internal void Render(DrawCommandList commands)
        {
            DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));
            Session.BeginFrame(Color.Transparent);
            try { Session.DrawingBackend.Render(commands, frame); }
            finally { Session.CompleteFrame(present: false); }
        }

        internal SdlGpuTextAtlasEntries? Add(KeySet key, long token, int size)
        {
            RasterizedText raster = new(size, size, new byte[size * size * 4], new TextShapeResult("x", 1));
            return Resources.GetOrCreateTextAtlasEntries(Session, key.Red, key.Green, key.Blue,
                [raster, raster, raster], token);
        }

        internal bool TryGet(KeySet key, long token, out SdlGpuTextAtlasEntries entries) =>
            Resources.TryGetTextAtlasEntries(key.Red, key.Green, key.Blue, token, out entries);

        public void Dispose()
        {
            Session.Dispose();
            factory.Dispose();
        }
    }
}
