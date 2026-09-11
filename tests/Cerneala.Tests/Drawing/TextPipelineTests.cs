using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using SkiaSharp;

namespace Cerneala.Tests.Drawing;

public sealed class TextPipelineTests
{
    [Fact]
    public void ShapeCacheAdmissionDoesNotAcquireGlobalDictionaryLocksPerMiss()
    {
        using SKTypeface typeface = CreateIsolatedTextTypeface();
        SkiaFont font = new(typeface, typeface.FamilyName, 16);
        SkiaTextShaper shaper = new();
        for (int index = 0; index < 4_352; index++)
        {
            shaper.Shape(new DrawTextRun(font, $"warm-admission-{index:D4}", 16));
        }

        using DictionaryLockListener listener = new();
        System.Collections.Concurrent.ConcurrentDictionary<int, int> control = new();
        control.TryAdd(1, 1);
        listener.StartCapture();
        _ = control.Count;
        Assert.True(listener.StopCapture() > 0, "The runtime must expose the global dictionary-lock event used by this regression.");

        const int admissions = 256;
        listener.StartCapture();
        for (int index = 0; index < admissions; index++)
        {
            shaper.Shape(new DrawTextRun(font, $"new-admission-{index:D4}", 16));
        }
        int globalLocks = listener.StopCapture();

        // Occasional dictionary growth can lock all stripes; cache-size checks
        // must not do so once (or twice) for every admitted text run.
        Assert.True(globalLocks <= 16, $"{admissions} text admissions acquired all dictionary locks {globalLocks} times.");
    }

    [Fact]
    public void ShapeCacheConcurrentAdmissionsRemainBoundedAndPreserveHeldResults()
    {
        using SKTypeface typeface = CreateIsolatedTextTypeface();
        SkiaFont font = new(typeface, typeface.FamilyName, 16);
        SkiaTextShaper shaper = new();
        DrawTextRun recurring = new(font, "held shaped result", 16);
        TextShapeResult held = shaper.Shape(recurring);
        ushort[] glyphs = held.GlyphIds;
        DrawPoint[] positions = held.GlyphPositions;

        Parallel.For(0, 8_192, new ParallelOptions { MaxDegreeOfParallelism = 8 }, index =>
            shaper.Shape(new DrawTextRun(font, $"concurrent-shape-{index:D4}", 16)));

        const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        object caches = typeof(SkiaTextShaper).GetField("ShapesByTypeface",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.GetValue(null)!;
        object?[] arguments = [typeface, null];
        Assert.True((bool)caches.GetType().GetMethod("TryGetValue")!.Invoke(caches, arguments)!);
        object cache = arguments[1]!;
        object entries = cache.GetType().GetField("entries", fields)!.GetValue(cache)!;
        int count = (int)entries.GetType().GetProperty("Count")!.GetValue(entries)!;
        Assert.InRange(count, 1, 4_096);

        Assert.Equal(glyphs, held.GlyphIds);
        Assert.Equal(positions, held.GlyphPositions);
        TextShapeResult reshaped = shaper.Shape(recurring);
        Assert.Equal(glyphs, reshaped.GlyphIds);
        Assert.Equal(positions, reshaped.GlyphPositions);
        Assert.Equal(held.AdvanceWidth, reshaped.AdvanceWidth);
    }

    [Fact]
    public void TextShaperRejectsNullTextRun()
    {
        SkiaTextShaper shaper = new();

        Assert.Throws<ArgumentNullException>(() => shaper.Shape(null!));
    }

    [Fact]
    public void TextShaperReturnsGlyphCountForSystemFontTextRun()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Cerneala", 16);
        SkiaTextShaper shaper = new();

        TextShapeResult result = shaper.Shape(textRun);

        Assert.Equal("Cerneala", result.Text);
        Assert.True(result.GlyphCount > 0);
        Assert.Equal(result.GlyphCount, result.GlyphIds.Length);
        Assert.Equal(result.GlyphCount, result.GlyphPositions.Length);
    }

    [Fact]
    public void TextShapingDoesNotPopulateRasterBlobCache()
    {
        SkiaFont font = Assert.IsType<SkiaFont>(new SystemFontSource().LoadFont("Arial", 16));
        DrawTextRun textRun = new(font, $"shape-only-{Guid.NewGuid():N}", 16);
        SkiaTextShaper shaper = new();
        int entriesBefore = SkiaTextBlobCache.GetCachedEntryCount(font);

        shaper.Shape(textRun);

        Assert.Equal(entriesBefore, SkiaTextBlobCache.GetCachedEntryCount(font));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4_096)]
    public void TextBlobCacheAdmitsRecurringTextWithoutExceedingCapacity(int initialEntries)
    {
        const int capacity = 4_096;
        const int requests = 256;
        using SKTypeface typeface = CreateIsolatedTextTypeface();
        SkiaFont font = new(typeface, typeface.FamilyName, 16);
        Assert.Equal(0, SkiaTextBlobCache.GetCachedEntryCount(font));
        SkiaTextShaper shaper = new();

        for (int index = 0; index < initialEntries; index++)
        {
            TextShapeResult shape = shaper.Shape(new DrawTextRun(font, $"earlier-{index:D4}", 16));
            using SkiaTextBlobCache.Lease lease = SkiaTextBlobCache.Rent(font, 16, shape);
            Assert.InRange(SkiaTextBlobCache.GetCachedEntryCount(font), 0, capacity);
        }

        Assert.Equal(initialEntries, SkiaTextBlobCache.GetCachedEntryCount(font));
        TextShapeResult recurring = shaper.Shape(new DrawTextRun(font, "later-recurring-label", 16));
        Assert.True(recurring.GlyphCount > 0);
        SKTextBlob? previous = null;
        int reused = 0;
        // A bounded admission window, not an immediate-admission or LRU requirement.
        for (int index = 0; index < requests; index++)
        {
            using SkiaTextBlobCache.Lease lease = SkiaTextBlobCache.Rent(font, 16, recurring);
            if (ReferenceEquals(previous, lease.Value))
            {
                reused++;
            }

            // Compare managed identity only; released native handles can be recycled.
            previous = lease.Value;
            Assert.InRange(SkiaTextBlobCache.GetCachedEntryCount(font), 0, capacity);
        }

        Assert.True(reused > 0,
            $"Recurring text reused a cached blob on {reused}/{requests - 1} repeat requests " +
            $"after {initialEntries} earlier entries; cached count: {SkiaTextBlobCache.GetCachedEntryCount(font)}.");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TextBlobCacheEvictionDisposesOnlyAfterTheLastLease(bool keepLeasesActive)
    {
        using SKTypeface typeface = CreateIsolatedTextTypeface();
        SkiaFont font = new(typeface, typeface.FamilyName, 16);
        SkiaTextShaper shaper = new();
        TextShapeResult firstShape = shaper.Shape(new DrawTextRun(font, "first-label", 16));
        using SkiaTextBlobCache.Lease first = SkiaTextBlobCache.Rent(font, 16, firstShape);
        using SkiaTextBlobCache.Lease second = SkiaTextBlobCache.Rent(font, 16, firstShape);
        SKTextBlob blob = first.Value;
        Assert.Same(blob, second.Value);
        SKRect bounds = blob.Bounds;
        if (!keepLeasesActive)
        {
            first.Dispose();
            second.Dispose();
        }

        for (int index = 0; index < 4_096; index++)
        {
            TextShapeResult shape = shaper.Shape(new DrawTextRun(font, $"replacement-{index:D4}", 16));
            using SkiaTextBlobCache.Lease lease = SkiaTextBlobCache.Rent(font, 16, shape);
            Assert.InRange(SkiaTextBlobCache.GetCachedEntryCount(font), 0, 4_096);
        }

        if (keepLeasesActive)
        {
            Assert.NotEqual(IntPtr.Zero, blob.Handle);
            Assert.Equal(bounds, blob.Bounds);
            first.Dispose();
            first.Dispose(); // Returning one lease twice must not release the other lease.
            Assert.NotEqual(IntPtr.Zero, blob.Handle);
            Assert.Equal(bounds, second.Value.Bounds);
            second.Dispose();
        }

        Assert.Equal(IntPtr.Zero, blob.Handle);
        using SkiaTextBlobCache.Lease recreated = SkiaTextBlobCache.Rent(font, 16, firstShape);
        Assert.NotSame(blob, recreated.Value);
        Assert.Equal(bounds, recreated.Value.Bounds);
    }

    [Fact]
    public void TextBlobCacheConcurrentAdmissionsRespectCapacityAndActiveLease()
    {
        using SKTypeface typeface = CreateIsolatedTextTypeface();
        SkiaFont font = new(typeface, typeface.FamilyName, 16);
        SkiaTextShaper shaper = new();
        TextShapeResult firstShape = shaper.Shape(new DrawTextRun(font, "held-label", 16));
        using SkiaTextBlobCache.Lease held = SkiaTextBlobCache.Rent(font, 16, firstShape);
        SKTextBlob heldBlob = held.Value;
        SKRect bounds = heldBlob.Bounds;
        TextShapeResult[] shapes = Enumerable.Range(0, 8_192)
            .Select(index => shaper.Shape(new DrawTextRun(font, $"parallel-{index:D4}", 16)))
            .ToArray();

        Parallel.ForEach(shapes, new ParallelOptions { MaxDegreeOfParallelism = 8 }, shape =>
        {
            using SkiaTextBlobCache.Lease lease = SkiaTextBlobCache.Rent(font, 16, shape);
            Assert.NotEqual(IntPtr.Zero, lease.Value.Handle);
            Assert.InRange(SkiaTextBlobCache.GetCachedEntryCount(font), 0, 4_096);
        });

        Assert.Equal(4_096, SkiaTextBlobCache.GetCachedEntryCount(font));
        Assert.NotEqual(IntPtr.Zero, heldBlob.Handle);
        Assert.Equal(bounds, heldBlob.Bounds);
        held.Dispose();
        Assert.Equal(IntPtr.Zero, heldBlob.Handle);
    }

    [Fact]
    public void TextBlobCacheConcurrentRentalsOfTheSameTextShareOneBlob()
    {
        using SKTypeface typeface = CreateIsolatedTextTypeface();
        SkiaFont font = new(typeface, typeface.FamilyName, 16);
        TextShapeResult shape = new SkiaTextShaper().Shape(new DrawTextRun(font, "shared-label", 16));
        System.Collections.Concurrent.ConcurrentBag<SkiaTextBlobCache.Lease> leases = [];
        try
        {
            Parallel.For(0, 256, new ParallelOptions { MaxDegreeOfParallelism = 8 }, _ =>
                leases.Add(SkiaTextBlobCache.Rent(font, 16, shape)));

            SKTextBlob blob = leases.First().Value;
            Assert.All(leases, lease => Assert.Same(blob, lease.Value));
            Assert.Equal(1, SkiaTextBlobCache.GetCachedEntryCount(font));
        }
        finally
        {
            foreach (SkiaTextBlobCache.Lease lease in leases)
            {
                lease.Dispose();
            }
        }
    }

    [Fact]
    public void TextShapeResultDoesNotExposeRasterPlacement()
    {
        Assert.Null(typeof(TextShapeResult).GetProperty("OriginOffset"));
    }

    [Fact]
    public void OpenTypeFontDataIsReusedForTheSameTypeface()
    {
        SkiaFont font = Assert.IsType<SkiaFont>(new SystemFontSource().LoadFont("Arial", 16));

        OpenTypeFontData first = OpenTypeFontData.Read(font);
        OpenTypeFontData second = OpenTypeFontData.Read(font);

        Assert.Same(first.Bytes, second.Bytes);
        Assert.Equal(first.FaceIndex, second.FaceIndex);
    }

    [Theory]
    [InlineData("Calibri", 13.765625f)]
    [InlineData("Consolas", 13.25f)]
    public void TextShaperUsesOpenTypeHorizontalMetricsForBaseline(string familyName, float expectedBaseline)
    {
        IDrawFont font = new SystemFontSource().LoadFont(familyName, 16);
        DrawTextRun textRun = new(font, "Hello world!", 16);

        bool measured = TextShaper.Default.TryMeasureBaseline(textRun, out float baseline);

        Assert.True(measured);
        Assert.Equal(expectedBaseline, baseline);
    }

    [Fact]
    public void DrawTextRunRejectsSizeThatCannotBeShaped()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawTextRun(font, "Cerneala", float.MaxValue));
    }

    [Fact]
    public void DrawTextRunRejectsSizeAtHarfBuzzScaleOverflowBoundary()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawTextRun(font, "Cerneala", 33_554_432f));
    }

    [Fact]
    public void DrawTextRunRejectsSizeThatCannotBeRasterizedSafely()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);

        Assert.Throws<ArgumentOutOfRangeException>(() => new DrawTextRun(font, "Cerneala", 33_000_000f));
    }

    [Fact]
    public void TextRasterizerReturnsPixelsForSystemFontTextRun()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Cerneala", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(textRun, Color.White);

        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        Assert.NotEmpty(result.RgbaPixels);
        Assert.True(result.ShapeResult.GlyphCount > 0);
        Assert.Equal(result.ShapeResult.GlyphCount, result.ShapeResult.GlyphIds.Length);
    }

    [Fact]
    public void TextRasterizerMaskIsColorIndependentGlyphCoverage()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Cerneala", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText mask = rasterizer.RasterizeMask(textRun);
        RasterizedText white = rasterizer.Rasterize(textRun, Color.White);

        Assert.Equal(white.ShapeResult.Text, mask.ShapeResult.Text);
        Assert.Equal(white.ShapeResult.GlyphCount, mask.ShapeResult.GlyphCount);
        Assert.Equal(white.ShapeResult.AdvanceWidth, mask.ShapeResult.AdvanceWidth);
        Assert.Equal(white.ShapeResult.GlyphIds, mask.ShapeResult.GlyphIds);
        Assert.Equal(white.ShapeResult.GlyphPositions, mask.ShapeResult.GlyphPositions);
        Assert.Equal(white.OriginOffset, mask.OriginOffset);
        Assert.Equal(white.RgbaPixels, mask.RgbaPixels);
    }

    [Fact]
    public void TextRasterizerPreservesIndependentRgbSubpixelCoverageUnderDpiTransform()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Hello world!", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText[] layers = rasterizer.RasterizeSubpixel(
            textRun,
            Color.Black,
            coordinateScale: 1.25f,
            position: new DrawPoint(118.4f, 84.4f));

        Assert.Equal(3, layers.Length);
        Assert.Equal(layers[0].Width, layers[1].Width);
        Assert.Equal(layers[0].Height, layers[2].Height);
        Assert.True(HasDifferentAlphaCoverage(layers[0], layers[1]));
        Assert.True(HasDifferentAlphaCoverage(layers[1], layers[2]));
    }

    [Fact]
    public void SubpixelCoverageUsesTheForegroundColorForSkiaGammaCorrection()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Cascadia Mono", 10), "MOTION LAB / SECOND NATIVE WINDOW", 10);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText[] black = rasterizer.RasterizeSubpixel(
            textRun,
            Color.Black,
            coordinateScale: 1,
            position: new DrawPoint(601, 38));
        RasterizedText[] slate = rasterizer.RasterizeSubpixel(
            textRun,
            new Color(138, 147, 166),
            coordinateScale: 1,
            position: new DrawPoint(601, 38));

        Assert.True(HasDifferentAlphaCoverage(black[0], slate[0]));
        Assert.True(HasDifferentAlphaCoverage(black[1], slate[1]));
        Assert.True(HasDifferentAlphaCoverage(black[2], slate[2]));
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(1.5f)]
    [InlineData(2f)]
    public void CanonicalSubpixelRasterizationStaysCloseToIntermediatePositions(float coordinateScale)
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Motion baseline", 16);
        SkiaTextRasterizer rasterizer = new();
        DrawPoint position = new(20.049f / coordinateScale, 30.049f / coordinateScale);
        // The renderer's phase selection is covered by SdlGpuTextCacheContractTests.
        // This shared rasterizer contract compares the same canonical zero phase
        // with the unchanged intermediate physical position at every DPI.
        DrawPoint phase = new(0, 0);

        RasterizedText[] exact = rasterizer.RasterizeSubpixel(
            textRun,
            new Color(38, 72, 110),
            coordinateScale,
            position);
        RasterizedText[] canonical = rasterizer.RasterizeSubpixelAtPhase(
            textRun,
            new Color(38, 72, 110),
            coordinateScale,
            phase);

        Assert.Equal(exact[0].ShapeResult.GlyphIds, canonical[0].ShapeResult.GlyphIds);
        Assert.InRange(Math.Abs(exact[0].Width - canonical[0].Width), 0, 1);
        Assert.InRange(Math.Abs(exact[0].Height - canonical[0].Height), 0, 1);
        Assert.InRange(Math.Abs(exact[0].OriginOffset.X - canonical[0].OriginOffset.X), 0, 0.063f);
        Assert.InRange(Math.Abs(exact[0].OriginOffset.Y - canonical[0].OriginOffset.Y), 0, 0.063f);
        Assert.InRange(MeanAlphaDifference(exact, canonical), 0, 12);
    }

    [Fact]
    public void AdjacentCanonicalPhasesDoNotIntroduceLargeCoverageJump()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Motion baseline", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText[] left = rasterizer.RasterizeSubpixelAtPhase(
            textRun,
            Color.Black,
            1.25f,
            new DrawPoint(0, 0));
        RasterizedText[] right = rasterizer.RasterizeSubpixelAtPhase(
            textRun,
            Color.Black,
            1.25f,
            new DrawPoint(0.125f, 0.125f));

        Assert.InRange(MeanAlphaDifference(left, right), 0, 24);
        Assert.True(HasDifferentAlphaCoverage(left[0], right[0]));
    }

    [Fact]
    public void TextRasterizerResolvesBackendAgnosticFontsByFamily()
    {
        DrawTextRun textRun = new(new ContractFont("Arial", 16), "Cerneala", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(textRun, Color.Black);

        Assert.True(result.ShapeResult.GlyphCount > 0);
        Assert.Contains(result.RgbaPixels, value => value != 0);
    }

    [Fact]
    public void TextRasterizerUsesTextRunSize()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText small = rasterizer.Rasterize(new DrawTextRun(font, "MMMMMMMM", 16), Color.White);
        RasterizedText large = rasterizer.Rasterize(new DrawTextRun(font, "MMMMMMMM", 32), Color.White);

        Assert.True(large.Width > small.Width);
        Assert.True(large.Height > small.Height);
    }

    [Fact]
    public void TextRasterizerReportsCroppedTextureOriginOffset()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(new DrawTextRun(font, "hahahehe", 16), Color.White);

        Assert.True(result.ShapeResult.AdvanceWidth < result.Width);
        Assert.True(result.OriginOffset.X >= 0);
    }

    [Fact]
    public void TextRasterizerTrimsTransparentLeftPadding()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(new DrawTextRun(font, "a", 16), Color.White);

        Assert.Equal(0, FirstInkX(result));
    }

    [Fact]
    public void TextRasterizerHandlesEmptyText()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), string.Empty, 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(textRun, Color.White);

        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.Empty(result.ShapeResult.GlyphIds);
        Assert.Empty(result.ShapeResult.GlyphPositions);
    }

    [Fact]
    public void TextRasterizerCanRepeatedlyShapeSystemFontText()
    {
        SystemFontSource fonts = new();
        IDrawFont font = fonts.LoadFont("Arial", 16);
        SkiaTextRasterizer rasterizer = new();

        for (int i = 0; i < 100; i++)
        {
            RasterizedText result = rasterizer.Rasterize(new DrawTextRun(font, $"Cerneala {i}", 16), Color.White);

            Assert.True(result.ShapeResult.GlyphCount > 0);
            Assert.NotEmpty(result.RgbaPixels);
        }
    }

    [Fact]
    public void TextShapeResultRejectsMismatchedGlyphData()
    {
        ushort[] glyphIds = [1, 2];
        DrawPoint[] glyphPositions = [new DrawPoint(0, 0)];

        Assert.Throws<ArgumentException>(() => new TextShapeResult("Cerneala", 2, glyphIds, glyphPositions));
    }

    [Fact]
    public void TextShapeResultRejectsNegativeGlyphCount()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextShapeResult("Cerneala", -1));
    }

    [Fact]
    public void TextShapeResultRejectsGlyphCountThatCannotBeAllocated()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextShapeResult("Cerneala", int.MaxValue));
    }

    [Fact]
    public void TextShapeResultCountConstructorCreatesMatchingGlyphData()
    {
        TextShapeResult result = new("Cerneala", 2);

        Assert.Equal(2, result.GlyphCount);
        Assert.Equal(2, result.GlyphIds.Length);
        Assert.Equal(2, result.GlyphPositions.Length);
    }

    [Fact]
    public void TextShapeResultDefensivelyCopiesGlyphData()
    {
        ushort[] glyphIds = [1];
        DrawPoint[] glyphPositions = [new DrawPoint(2, 3)];

        TextShapeResult result = new("Cerneala", 1, glyphIds, glyphPositions);
        glyphIds[0] = 9;
        glyphPositions[0] = new DrawPoint(10, 11);

        Assert.Equal(1, result.GlyphIds[0]);
        Assert.Equal(new DrawPoint(2, 3), result.GlyphPositions[0]);
    }

    [Fact]
    public void TextShapeResultReturnsCopiesOfGlyphData()
    {
        TextShapeResult result = new("Cerneala", 1, [1], [new DrawPoint(2, 3)]);

        result.GlyphIds[0] = 9;
        result.GlyphPositions[0] = new DrawPoint(10, 11);

        Assert.Equal(1, result.GlyphIds[0]);
        Assert.Equal(new DrawPoint(2, 3), result.GlyphPositions[0]);
    }

    [Fact]
    public void DefaultTextShapeResultReturnsEmptyGlyphData()
    {
        TextShapeResult result = default;

        Assert.Equal(string.Empty, result.Text);
        Assert.Empty(result.GlyphIds);
        Assert.Empty(result.GlyphPositions);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-1, 1)]
    public void RasterizedTextRejectsInvalidDimensions(int width, int height)
    {
        TextShapeResult shapeResult = new(string.Empty, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => new RasterizedText(width, height, new byte[4], shapeResult));
    }

    [Fact]
    public void RasterizedTextRejectsMismatchedPixelBufferLength()
    {
        TextShapeResult shapeResult = new(string.Empty, 0);

        Assert.Throws<ArgumentException>(() => new RasterizedText(2, 2, new byte[4], shapeResult));
    }

    [Fact]
    public void RasterizedTextRejectsPixelBufferDimensionsThatOverflow()
    {
        TextShapeResult shapeResult = new(string.Empty, 0);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => new RasterizedText(int.MaxValue, 2, Array.Empty<byte>(), shapeResult));
    }

    [Fact]
    public void RasterizedTextDefensivelyCopiesPixels()
    {
        byte[] pixels = [1, 2, 3, 4];
        TextShapeResult shapeResult = new(string.Empty, 0);

        RasterizedText result = new(1, 1, pixels, shapeResult);
        pixels[0] = 99;

        Assert.Equal(1, result.RgbaPixels[0]);
    }

    [Fact]
    public void RasterizedTextReturnsCopyOfPixels()
    {
        RasterizedText result = new(1, 1, [1, 2, 3, 4], new TextShapeResult(string.Empty, 0));

        result.RgbaPixels[0] = 99;

        Assert.Equal(1, result.RgbaPixels[0]);
    }

    private static SKTypeface CreateIsolatedTextTypeface()
    {
        SkiaFont systemFont = Assert.IsType<SkiaFont>(new SystemFontSource().LoadFont("Arial", 16));
        OpenTypeFontData fontData = OpenTypeFontData.Read(systemFont);
        using SKData data = SKData.CreateCopy(fontData.Bytes);
        SKTypeface typeface = SKTypeface.FromData(data, checked((int)fontData.FaceIndex));
        Assert.NotNull(typeface);
        // Separate font identity prevents saturation from contaminating shared caches.
        Assert.Equal(0, SkiaTextBlobCache.GetCachedEntryCount(new SkiaFont(typeface, typeface.FamilyName, 16)));
        return typeface;
    }

    private static int FirstInkX(RasterizedText text)
    {
        byte[] pixels = text.RgbaPixels;
        for (int x = 0; x < text.Width; x++)
        {
            for (int y = 0; y < text.Height; y++)
            {
                int alphaIndex = (((y * text.Width) + x) * 4) + 3;
                if (pixels[alphaIndex] != 0)
                {
                    return x;
                }
            }
        }

        return -1;
    }

    private static bool HasDifferentAlphaCoverage(RasterizedText left, RasterizedText right)
    {
        byte[] leftPixels = left.RgbaPixels;
        byte[] rightPixels = right.RgbaPixels;
        for (int index = 3; index < leftPixels.Length; index += 4)
        {
            if (leftPixels[index] != rightPixels[index])
            {
                return true;
            }
        }

        return false;
    }

    private static double MeanAlphaDifference(
        IReadOnlyList<RasterizedText> left,
        IReadOnlyList<RasterizedText> right)
    {
        int width = Math.Min(left[0].Width, right[0].Width);
        int height = Math.Min(left[0].Height, right[0].Height);
        long difference = 0;
        int samples = width * height * left.Count;
        for (int layer = 0; layer < left.Count; layer++)
        {
            byte[] leftPixels = left[layer].RgbaPixels;
            byte[] rightPixels = right[layer].RgbaPixels;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int leftIndex = (((y * left[layer].Width) + x) * 4) + 3;
                    int rightIndex = (((y * right[layer].Width) + x) * 4) + 3;
                    difference += Math.Abs(leftPixels[leftIndex] - rightPixels[rightIndex]);
                }
            }
        }

        return difference / (double)samples;
    }

    private sealed record ContractFont(string FamilyName, float Size) : IDrawFont;

    private sealed class DictionaryLockListener : System.Diagnostics.Tracing.EventListener
    {
        private int capturedThread;
        private int globalLocks;

        public void StartCapture()
        {
            globalLocks = 0;
            capturedThread = Environment.CurrentManagedThreadId;
        }

        public int StopCapture()
        {
            capturedThread = 0;
            return globalLocks;
        }

        protected override void OnEventSourceCreated(System.Diagnostics.Tracing.EventSource source)
        {
            if (source.Name == "System.Collections.Concurrent.ConcurrentCollectionsEventSource")
            {
                EnableEvents(source, System.Diagnostics.Tracing.EventLevel.Verbose,
                    System.Diagnostics.Tracing.EventKeywords.All);
            }
        }

        protected override void OnEventWritten(System.Diagnostics.Tracing.EventWrittenEventArgs data)
        {
            if (capturedThread == Environment.CurrentManagedThreadId &&
                data.EventName == "ConcurrentDictionary_AcquiringAllLocks")
            {
                globalLocks++;
            }
        }
    }
}
