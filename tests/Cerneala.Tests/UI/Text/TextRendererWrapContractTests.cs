using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextRendererWrapContractTests
{
    [Fact]
    public void RenderDrawsOneCommandPerMeasuredWrappedLine()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);
        DrawCommandList commands = new();
        TextRunStyle style = new("Default", 16, TextWrapping.Wrap);

        TextMeasureResult measurement = renderer.Render(
            new DrawingContext(commands),
            "ABCD",
            style,
            16,
            new DrawPoint(4, 6),
            DrawColor.White);

        Assert.Equal(2, measurement.LineCount);
        Assert.Collection(
            measurement.Lines,
            line => Assert.Equal("AB", line.Text),
            line => Assert.Equal("CD", line.Text));
        Assert.Equal(2, commands.Count);
        Assert.Equal("AB", commands[0].Text);
        Assert.Equal(new DrawPoint(4, 6), commands[0].Position);
        Assert.Equal("CD", commands[1].Text);
        Assert.Equal(new DrawPoint(4, 22), commands[1].Position);
    }

    [Fact]
    public void RenderAdvancesWrappedLinesByRasterizedLineHeightForSystemFonts()
    {
        const float fontSize = 14;
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        IDrawFont font = new SystemFontSource().LoadFont("Arial", fontSize);
        store.SetResource(id, new FontResource(font));
        TextLayoutCache cache = new();
        FontResolver resolver = new(store);
        TextMeasurer measurer = new(resolver, LineBreakService.Default, cache);
        TextRenderer renderer = new(resolver, measurer);
        DrawCommandList commands = new();
        TextRunStyle style = new("Default", fontSize, TextWrapping.Wrap, fontResourceId: id);
        RasterizedText rasterizedLine = new SkiaTextRasterizer().Rasterize(
            new DrawTextRun(font, "Ag", fontSize),
            DrawColor.White);

        renderer.Render(
            new DrawingContext(commands),
            "ABCD",
            style,
            14,
            new DrawPoint(4, 6),
            DrawColor.White);

        Assert.True(commands.Count >= 2, "Expected wrapping to produce at least two draw commands.");
        float lineAdvance = commands[1].Position.Y - commands[0].Position.Y;
        Assert.True(
            lineAdvance >= rasterizedLine.Height,
            $"Expected renderer line advance to be at least rasterized line height {rasterizedLine.Height}, but got {lineAdvance}.");
    }

    [Fact]
    public void RenderUsesSameLayoutCacheForMeasurementAndLineDrawing()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);
        TextRunStyle style = new("Default", 16, TextWrapping.Wrap);

        renderer.Render(new DrawingContext(new DrawCommandList()), "ABCD", style, 16, default, DrawColor.White);
        renderer.Render(new DrawingContext(new DrawCommandList()), "ABCD", style, 16, default, DrawColor.White);

        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Hits);
    }
}
