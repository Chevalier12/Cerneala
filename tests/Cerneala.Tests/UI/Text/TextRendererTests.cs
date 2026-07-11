using Cerneala.Drawing;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextRendererTests
{
    [Fact]
    public void RenderRecordsDrawTextCommand()
    {
        TextRenderer renderer = new();
        DrawCommandList commands = new();
        DrawingContext drawingContext = new(commands);

        TextMeasureResult result = renderer.Render(
            drawingContext,
            "Hello",
            new TextAspect("Default", 16),
            100,
            new DrawPoint(2, 3),
            Color.White);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        Assert.Equal("Hello", commands[0].Text);
        Assert.Equal(2, commands[0].Position.X);
        Assert.True(commands[0].Position.Y > 3, "DrawText commands use a font baseline, not the line-box top.");
        Assert.Equal("Default", commands[0].Font!.FamilyName);
        Assert.Equal(result.CacheKey, renderer.Render(
            new DrawingContext(new DrawCommandList()),
            "Hello",
            new TextAspect("Default", 16),
            100,
            new DrawPoint(0, 0),
            Color.Black).CacheKey);
    }

    [Fact]
    public void RenderReusesCachedLayoutForUnchangedMetrics()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);

        renderer.Render(new DrawingContext(new DrawCommandList()), "Hello", new TextAspect("Default", 16), 100, default, Color.Black);
        renderer.Render(new DrawingContext(new DrawCommandList()), "Hello", new TextAspect("Default", 16), 100, default, Color.White);

        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Hits);
    }

    [Fact]
    public void RenderReturnsMeasurementWithoutCommandForEmptyText()
    {
        TextRenderer renderer = new();
        DrawCommandList commands = new();

        TextMeasureResult result = renderer.Render(
            new DrawingContext(commands),
            string.Empty,
            new TextAspect("Default", 16),
            100,
            default,
            Color.White);

        Assert.Empty(commands);
        Assert.Equal(0, result.Size.Width);
        Assert.True(result.Size.Height >= 16);
        Assert.Equal(1, result.LineCount);
    }

    [Fact]
    public void RenderRecordsScaledAspectInDrawTextRun()
    {
        TextRenderer renderer = new();
        DrawCommandList commands = new();

        renderer.Render(
            new DrawingContext(commands),
            "Hello",
            new TextAspect("Serif", 12, scale: 2),
            100,
            default,
            Color.White);

        Assert.Single(commands);
        Assert.Equal("Serif", commands[0].TextRun!.Font.FamilyName);
        Assert.Equal(24, commands[0].TextRun!.Size);
    }

    [Fact]
    public void RenderUsesRequestedColor()
    {
        TextRenderer renderer = new();
        DrawCommandList commands = new();
        DrawingContext drawingContext = new(commands);
        Color styleColor = new(12, 34, 56);
        Color requestedColor = new(98, 76, 54);

        renderer.Render(
            drawingContext,
            "Hello",
            new TextAspect("Default", 16, color: styleColor),
            100,
            default,
            requestedColor);

        Assert.Single(commands);
        Assert.Equal(requestedColor, commands[0].Color);
    }
}
