using System.Reflection;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Text;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SdlGpu;

// The allocation probe briefly reserves a process-wide no-GC region. Share
// SDL's serialized collection instead of perturbing concurrently running tests.
[Collection(SdlNativeTestCollection.Name)]
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

    [Theory]
    [InlineData("solid")]
    [InlineData("gradient")]
    [InlineData("image")]
    public void UnchangedCachedTextDoesNotRepeatCpuRasterization(string brushKind)
    {
        using Fixture fixture = new();
        using SdlGpuImage image = new(1, 1, [100, 50, 25, 255]);
        IDrawBrush brush = brushKind switch
        {
            "solid" => new SolidColorBrush(Color.White),
            "gradient" => new LinearGradientBrush(new DrawPoint(0, 0), new DrawPoint(80, 0),
                [new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)]),
            "image" => new ImageBrush(image),
            _ => throw new ArgumentOutOfRangeException(nameof(brushKind))
        };
        DrawCommandList commands = new();
        commands.Add(DrawCommand.DrawText(
            new DrawTextRun(new SystemFontSource().LoadFont("Arial", 16), "cached text", 16),
            new DrawPoint(4.125f, 28.375f), brush));

        // Warm the exact command, font, brush, scale and phase. No mutations,
        // eviction pressure or view switches occur during the measured replay.
        for (int frame = 0; frame < 3; frame++)
        {
            fixture.Render(commands);
        }
        int createdTextures = fixture.Api.TextureCreationCount;
        long rasterRequests = 0;
        long rasterizedPixels = 0;
        for (int frame = 0; frame < 16; frame++)
        {
            fixture.Render(commands);
            rasterRequests += fixture.Backend.LastFrameTiming.TextRequestCount;
            rasterizedPixels += fixture.Backend.LastFrameTiming.RasterizedPixelCount;
        }

        // Texture reuse alone is insufficient: the existing canonical-phase
        // test checks this but does not detect repeated CPU coverage generation.
        Assert.Equal(createdTextures, fixture.Api.TextureCreationCount);
        Assert.True(rasterRequests == 0 && rasterizedPixels == 0,
            $"Unchanged {brushKind} text reused its textures but performed {rasterRequests} " +
            $"rasterizations ({rasterizedPixels} layer pixels) across 16 warm frames; expected zero.");
    }

    [Fact]
    public void UnchangedSolidTextBeyondAtlasBudgetDoesNotRepeatCpuRasterization()
    {
        using Fixture fixture = new();
        long activeFrame = fixture.Resources.BeginTextAtlasFrame();
        try
        {
            // Keep eight full pages live, as queued draws in a large frame or
            // another window may do. The next label must use standalone layers.
            Assert.NotNull(fixture.Add(new KeySet("page group A"), activeFrame, 1022));
            Assert.NotNull(fixture.Add(new KeySet("page group B"), activeFrame, 1022));
            Assert.Null(fixture.Add(new KeySet("page group C"), activeFrame, 1022));
            Assert.Equal(8, fixture.Resources.TextAtlasPageCount);

            IDrawBrush brush = new SolidColorBrush(Color.White);
            int beforeFallback = fixture.Api.TextureCreationCount;
            fixture.Render("stable overflow label", new DrawPoint(4, 28), brush);
            Assert.Equal(1, fixture.Backend.LastFrameTiming.TextRequestCount);
            int createdTextures = fixture.Api.TextureCreationCount;
            Assert.Equal(beforeFallback + 3, createdTextures);

            fixture.Render("stable overflow label", new DrawPoint(4, 28), brush);

            Assert.Equal(createdTextures, fixture.Api.TextureCreationCount);
            Assert.True(fixture.Backend.LastFrameTiming.TextRequestCount == 0,
                $"An unchanged label reused its standalone textures but repeated " +
                $"{fixture.Backend.LastFrameTiming.TextRequestCount} CPU rasterizations " +
                $"({fixture.Backend.LastFrameTiming.RasterizedPixelCount} layer pixels).");
        }
        finally
        {
            fixture.Resources.EndTextAtlasFrame(activeFrame);
        }
    }

    [Fact]
    public void StandaloneSolidTextIsBoundedAndRetiredWhenNoLongerDrawn()
    {
        using Fixture fixture = new();
        long activeFrame = fixture.Resources.BeginTextAtlasFrame();
        try
        {
            Assert.NotNull(fixture.Add(new KeySet("page group A"), activeFrame, 1022));
            Assert.NotNull(fixture.Add(new KeySet("page group B"), activeFrame, 1022));
            Assert.Null(fixture.Add(new KeySet("page group C"), activeFrame, 1022));
            int baseline = fixture.Api.GpuTextures.Count;
            for (int frame = 0; frame < 16; frame++)
            {
                fixture.Render($"overflow label {frame}", new DrawPoint(4, 28), new SolidColorBrush(Color.White));
                // One current RGB set and at most one pending retirement set.
                Assert.InRange(fixture.Api.GpuTextures.Count, baseline, baseline + 6);
            }
            fixture.Render(new DrawCommandList());
            fixture.Render(new DrawCommandList());
            Assert.Equal(baseline, fixture.Api.GpuTextures.Count);
        }
        finally
        {
            fixture.Resources.EndTextAtlasFrame(activeFrame);
        }
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
        long allocated = MeasureAllocatedBytes(() =>
        {
            for (int iteration = 0; iteration < 1_000; iteration++)
            {
                if (!fixture.TryGet(variants[sequence[iteration % sequence.Length]], token, out _)) { misses++; }
            }
        });
        Assert.Equal(0, misses);
        Assert.InRange(allocated, 0, 1_024);
        fixture.Resources.EndTextAtlasFrame(token);
    }

    [Fact]
    public void AllocationMeasurementStillDetectsRealAllocations()
    {
        Assert.Equal(0, MeasureAllocatedBytes(static () => { }));
        long allocated = MeasureAllocatedBytes(static () => GC.KeepAlive(new byte[2_048]));
        Assert.InRange(allocated, 2_048, 4_096);
    }

    private static long MeasureAllocatedBytes(Action action)
    {
        // .NET 8 background GC can void a thread's unused allocation context
        // without subtracting it from this counter. A no-allocation spin loop
        // reproduced the same false positives as the atlas lookup test.
        // Isolate only the measured interval; keep its real allocation limit
        // and fail if isolation cannot be established or the region is broken.
        Assert.True(GC.TryStartNoGCRegion(1_048_576), "Could not isolate the allocation measurement from GC.");
        try
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
        finally
        {
            GC.EndNoGCRegion();
        }
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
    public void EvictionPressureDoesNotRehashPinnedEntries()
    {
        using Fixture fixture = new();
        CountingIdentity residentIdentity = new();
        long token = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(new KeySet("resident A", residentIdentity), token, 600));
        Assert.NotNull(fixture.Add(new KeySet("resident B", residentIdentity), token, 600));
        Assert.Null(fixture.Add(new KeySet("resident C", residentIdentity), token, 600));
        Assert.Equal(8, fixture.Resources.TextAtlasEntryCount);
        int before = residentIdentity.HashCalls;

        Assert.Null(fixture.Add(new KeySet("overflow"), token, 600));

        Assert.Equal(0, residentIdentity.HashCalls - before);
        fixture.Resources.EndTextAtlasFrame(token);
    }

    [Fact]
    public void SharedEntryRemainsPinnedUntilBothFramesFinishUnderEvictionPressure()
    {
        using Fixture fixture = new();
        KeySet shared = new("shared"), filler = new("filler"), overflow = new("overflow");
        long first = fixture.Resources.BeginTextAtlasFrame();
        SdlGpuTextAtlasEntries original = fixture.Add(shared, first, 600)!.Value;
        long second = fixture.Resources.BeginTextAtlasFrame();
        Assert.True(fixture.TryGet(shared, second, out _));
        Assert.NotNull(fixture.Add(filler, first, 600));
        Assert.Null(fixture.Add(overflow, first, 600));
        fixture.Resources.EndTextAtlasFrame(first);
        long third = fixture.Resources.BeginTextAtlasFrame();
        Assert.NotNull(fixture.Add(new KeySet("replacement"), third, 600));
        fixture.Resources.EndTextAtlasFrame(third);
        Assert.True(fixture.TryGet(shared, second, out SdlGpuTextAtlasEntries retained));
        for (int channel = 0; channel < 3; channel++)
        {
            Assert.Same(original[channel], retained[channel]);
        }
        fixture.Resources.EndTextAtlasFrame(second);
    }

    [Fact]
    public void WarmEntryPinsAcrossFrameLifetimesDoNotAllocate()
    {
        using Fixture fixture = new();
        KeySet[] keys = Enumerable.Range(0, 64).Select(i => new KeySet($"warm-{i}")).ToArray();
        long token = fixture.Resources.BeginTextAtlasFrame();
        foreach (KeySet key in keys) Assert.NotNull(fixture.Add(key, token, 8));
        fixture.Resources.EndTextAtlasFrame(token);
        Replay();
        Replay();
        long allocated = MeasureAllocatedBytes(() =>
        {
            for (int frame = 0; frame < 128; frame++) Replay();
        });
        Assert.InRange(allocated, 0, 1_024);

        void Replay()
        {
            long frame = fixture.Resources.BeginTextAtlasFrame();
            foreach (KeySet key in keys) Assert.True(fixture.TryGet(key, frame, out _));
            fixture.Resources.EndTextAtlasFrame(frame);
        }
    }

    [Fact]
    public void AtlasReclaimsInactiveEntriesBesideActivePixelsWithoutGrowingItsBudget()
    {
        using Fixture fixture = new();
        KeySet[] keys = Enumerable.Range(0, 11).Select(i => new KeySet($"resident-{i}")).ToArray();
        long oldFrame = fixture.Resources.BeginTextAtlasFrame();
        for (int index = 0; index < 10; index++)
        {
            Assert.NotNull(fixture.Add(keys[index], oldFrame, 510, (byte)(index + 1)));
        }
        // Each padded layer occupies one quarter-page. The last RGB request
        // fills the final two slots before taking the existing fallback.
        Assert.Null(fixture.Add(keys[10], oldFrame, 510, 11));
        fixture.Resources.EndTextAtlasFrame(oldFrame);
        Assert.Equal(8, fixture.Resources.TextAtlasPageCount);
        int created = fixture.Api.TextureCreationCount;

        long activeFrame = fixture.Resources.BeginTextAtlasFrame();
        List<(KeySet Key, SdlGpuTextAtlasEntries Entries, byte[][] Pixels)> active = [];
        foreach (int index in new[] { 0, 2, 3, 4, 6, 7, 9 })
        {
            Assert.True(fixture.TryGet(keys[index], activeFrame, out SdlGpuTextAtlasEntries entries));
            active.Add((keys[index], entries,
                Enumerable.Range(0, 3).Select(channel => ReadPaddedPixels(entries[channel])).ToArray()));
        }
        Assert.Equal(8, active.SelectMany(item => Enumerable.Range(0, 3)
            .Select(channel => item.Entries[channel].Page)).Distinct().Count());

        long replacementFrame = fixture.Resources.BeginTextAtlasFrame();
        SdlGpuTextAtlasEntries? replacement = fixture.Add(new KeySet("replacement"), replacementFrame, 300, 99);
        Assert.True(replacement.HasValue,
            "Inactive rectangles must be reusable even when every atlas page contains another frame's active pixels.");
        fixture.Resources.EndTextAtlasFrame(replacementFrame);
        foreach (var item in active)
        {
            Assert.True(fixture.TryGet(item.Key, activeFrame, out SdlGpuTextAtlasEntries retained));
            for (int channel = 0; channel < 3; channel++)
            {
                Assert.Same(item.Entries[channel], retained[channel]);
                Assert.Equal(item.Pixels[channel], ReadPaddedPixels(retained[channel]));
            }
        }
        for (int channel = 0; channel < 3; channel++)
        {
            byte[] pixels = ReadPaddedPixels(replacement!.Value[channel]);
            int stride = 302 * 4;
            Assert.All(pixels.Take(stride), value => Assert.Equal(0, value));
            Assert.All(pixels.TakeLast(stride), value => Assert.Equal(0, value));
            for (int row = 1; row <= 300; row++)
            {
                Assert.All(pixels.Skip(row * stride).Take(4), value => Assert.Equal(0, value));
                Assert.All(pixels.Skip(row * stride + 301 * 4).Take(4), value => Assert.Equal(0, value));
                Assert.Equal(99, pixels[row * stride + 4]);
            }
        }
        Assert.Equal(8, fixture.Resources.TextAtlasPageCount);
        Assert.Equal(created, fixture.Api.TextureCreationCount);
        fixture.Resources.EndTextAtlasFrame(activeFrame);
    }

    private static byte[] ReadPaddedPixels(SdlGpuTextAtlasEntry entry)
    {
        int dimension = entry.Texture.Width;
        int x = (int)(entry.TextureCoordinates.X * dimension) - 1;
        int y = (int)(entry.TextureCoordinates.Y * dimension) - 1;
        int rowBytes = (entry.Width + 2) * 4;
        byte[] result = new byte[rowBytes * (entry.Height + 2)];
        for (int row = 0; row < entry.Height + 2; row++)
        {
            entry.Page.Pixels.AsSpan(((y + row) * dimension + x) * 4, rowBytes)
                .CopyTo(result.AsSpan(row * rowBytes));
        }
        return result;
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
        internal KeySet(string text, object? fontIdentity = null)
        {
            SdlGpuTextRasterKey raster = new(fontIdentity ?? this, text, 16, 1, default);
            Red = new(raster, SdlGpuColorWriteMask.Red);
            Green = new(raster, SdlGpuColorWriteMask.Green);
            Blue = new(raster, SdlGpuColorWriteMask.Blue);
        }
        internal SdlGpuTextLayerTextureKey Red { get; }
        internal SdlGpuTextLayerTextureKey Green { get; }
        internal SdlGpuTextLayerTextureKey Blue { get; }
    }

    private sealed class CountingIdentity
    {
        internal int HashCalls { get; private set; }

        public override int GetHashCode()
        {
            HashCalls++;
            return base.GetHashCode();
        }
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

        internal SdlGpuTextAtlasEntries? Add(KeySet key, long token, int size, byte value = 0)
        {
            byte[] pixels = new byte[size * size * 4];
            Array.Fill(pixels, value);
            RasterizedText raster = new(size, size, pixels, new TextShapeResult("x", 1));
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
