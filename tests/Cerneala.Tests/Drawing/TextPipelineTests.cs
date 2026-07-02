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
    }

    [Fact]
    public void TextRasterizerReturnsPixelsForSystemFontTextRun()
    {
        SystemFontSource fonts = new();
        DrawTextRun textRun = new(fonts.LoadFont("Arial", 16), "Cerneala", 16);
        SkiaTextRasterizer rasterizer = new();

        RasterizedText result = rasterizer.Rasterize(textRun, DrawColor.White);

        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
        Assert.NotEmpty(result.RgbaPixels);
    }
}
