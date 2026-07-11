using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;
using Cerneala.UI.Media;

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
        TextAspect aspect = new("Default", 16, TextWrapping.Wrap, foreground: new SolidColorBrush(Color.White));

        TextMeasureResult measurement = renderer.Render(
            new DrawingContext(commands),
            "ABCD",
            aspect,
            16,
            new DrawPoint(4, 6));

        Assert.Equal(measurement.LineCount, commands.Count);
        Assert.True(commands.Count > 1);
        Assert.Equal(4, commands[0].Position.X);
        Assert.True(commands[0].Position.Y > 6);
        Assert.All(commands, command => Assert.Equal(DrawCommandKind.DrawText, command.Kind));
        Assert.All(commands.Zip(commands.Skip(1)), pair => Assert.True(pair.Second.Position.Y > pair.First.Position.Y));
    }

    [Fact]
    public void RenderAdvancesWrappedLinesByFontLineHeightForSystemFonts()
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
        TextAspect aspect = new("Default", fontSize, TextWrapping.Wrap, foreground: new SolidColorBrush(Color.White), fontResourceId: id);
        Assert.True(TextShaper.Default.TryMeasureLineHeight(new DrawTextRun(font, "Ag", fontSize), out float expected));

        renderer.Render(
            new DrawingContext(commands),
            "ABCD",
            aspect,
            14,
            new DrawPoint(4, 6));

        Assert.True(commands.Count >= 2, "Expected wrapping to produce at least two draw commands.");
        float lineAdvance = commands[1].Position.Y - commands[0].Position.Y;
        Assert.Equal(expected, lineAdvance, precision: 3);
    }

    [Fact]
    public void RenderUsesSameLayoutCacheForMeasurementAndLineDrawing()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);
        TextAspect aspect = new("Default", 16, TextWrapping.Wrap, foreground: new SolidColorBrush(Color.White));

        renderer.Render(new DrawingContext(new DrawCommandList()), "ABCD", aspect, 16, default);
        renderer.Render(new DrawingContext(new DrawCommandList()), "ABCD", aspect, 16, default);

        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Hits);
    }
}
