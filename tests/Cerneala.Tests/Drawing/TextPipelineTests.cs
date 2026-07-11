using Cerneala.Drawing;
using Cerneala.Drawing.Text;

namespace Cerneala.Tests.Drawing;

public sealed class TextPipelineTests
{
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

    private sealed record ContractFont(string FamilyName, float Size) : IDrawFont;
}
