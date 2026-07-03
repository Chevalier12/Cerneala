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
            new TextRunStyle("Default", 16),
            100,
            new DrawPoint(2, 3),
            DrawColor.White);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        Assert.Equal("Hello", commands[0].Text);
        Assert.Equal(new DrawPoint(2, 3), commands[0].Position);
        Assert.Equal("Default", commands[0].Font!.FamilyName);
        Assert.Equal(result.CacheKey, renderer.Render(
            new DrawingContext(new DrawCommandList()),
            "Hello",
            new TextRunStyle("Default", 16),
            100,
            new DrawPoint(0, 0),
            DrawColor.Black).CacheKey);
    }

    [Fact]
    public void RenderReusesCachedLayoutForUnchangedMetrics()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRenderer renderer = new(FontResolver.Default, measurer);

        renderer.Render(new DrawingContext(new DrawCommandList()), "Hello", new TextRunStyle("Default", 16), 100, default, DrawColor.Black);
        renderer.Render(new DrawingContext(new DrawCommandList()), "Hello", new TextRunStyle("Default", 16), 100, default, DrawColor.White);

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
            new TextRunStyle("Default", 16),
            100,
            default,
            DrawColor.White);

        Assert.Empty(commands);
        Assert.Equal(0, result.Size.Width);
        Assert.Equal(16, result.Size.Height);
        Assert.Equal(1, result.LineCount);
    }

    [Fact]
    public void RenderRecordsScaledStyleInDrawTextRun()
    {
        TextRenderer renderer = new();
        DrawCommandList commands = new();

        renderer.Render(
            new DrawingContext(commands),
            "Hello",
            new TextRunStyle("Serif", 12, scale: 2),
            100,
            default,
            DrawColor.White);

        Assert.Single(commands);
        Assert.Equal("Serif", commands[0].TextRun!.Font.FamilyName);
        Assert.Equal(24, commands[0].TextRun!.Size);
    }

    [Fact]
    public void RenderRecordsStyleColorInDrawCommand()
    {
        TextRenderer renderer = new();
        DrawCommandList commands = new();
        DrawingContext drawingContext = new(commands);
        DrawColor styleColor = new(12, 34, 56);

        renderer.Render(
            drawingContext,
            "Hello",
            new TextRunStyle("Default", 16, color: styleColor),
            100,
            default,
            DrawColor.White);

        Assert.Single(commands);
        Assert.Equal(styleColor, commands[0].Color);
    }
}
