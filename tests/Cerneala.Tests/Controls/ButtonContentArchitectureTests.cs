using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class ButtonContentArchitectureTests
{
    [Fact]
    public void StringContentMeasurementUsesSharedTextMeasurer()
    {
        RecordingTextMeasurer measurer = new(new TextMeasureResult(
            new LayoutSize(25, 9),
            1,
            new TextLayoutKey("Go", "Default", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1),
            "Default",
            [new TextLine("Go", 25)]));
        Button button = new()
        {
            Content = "Go",
            TextMeasurer = measurer
        };

        LayoutSize desired = button.Measure(new MeasureContext(new LayoutSize(100, 50)));

        Assert.Equal(1, measurer.Calls);
        Assert.Equal("Go", measurer.LastText);
        Assert.Equal(100, measurer.LastAvailableWidth);
        Assert.Equal(new LayoutSize(25, 9), desired);
    }

    [Fact]
    public void StringContentRenderingUsesSharedTextRenderer()
    {
        RecordingTextRenderer renderer = new();
        Button button = new()
        {
            Content = "Go",
            Foreground = DrawColor.White,
            TextRenderer = renderer
        };
        DrawCommandList commands = new();

        button.Arrange(new ArrangeContext(new LayoutRect(3, 4, 40, 20)));
        button.Render(new RenderContext(button, new DrawingContext(commands), button.ArrangedBounds, RenderLayer.Default, new RenderCounters()));

        Assert.Equal(1, renderer.Calls);
        Assert.Equal("Go", renderer.LastText);
        Assert.Equal(new DrawPoint(3, 4), renderer.LastPosition);
        Assert.Equal(40, renderer.LastAvailableWidth);
    }

    private sealed class RecordingTextMeasurer(TextMeasureResult result) : TextMeasurer
    {
        public int Calls { get; private set; }

        public string? LastText { get; private set; }

        public float LastAvailableWidth { get; private set; }

        public override TextMeasureResult Measure(string text, TextRunStyle style, float availableWidth)
        {
            Calls++;
            LastText = text;
            LastAvailableWidth = availableWidth;
            return result;
        }
    }

    private sealed class RecordingTextRenderer : TextRenderer
    {
        public int Calls { get; private set; }

        public string? LastText { get; private set; }

        public float LastAvailableWidth { get; private set; }

        public DrawPoint LastPosition { get; private set; }

        public override TextMeasureResult Render(
            DrawingContext drawingContext,
            string text,
            TextRunStyle style,
            float availableWidth,
            DrawPoint position,
            DrawColor color)
        {
            Calls++;
            LastText = text;
            LastAvailableWidth = availableWidth;
            LastPosition = position;
            return new TextMeasureResult(
                new LayoutSize(12, 16),
                1,
                new TextLayoutKey(text, "Default", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1),
                "Default",
                [new TextLine(text, 12)]);
        }
    }
}
