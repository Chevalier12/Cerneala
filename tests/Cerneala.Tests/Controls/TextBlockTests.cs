using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class TextBlockTests
{
    [Fact]
    public void TextBlockMeasuresTextDeterministically()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello",
            FontSize = 20
        };

        LayoutSize desired = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(50, 20), desired);
    }

    [Fact]
    public void MeasureUsesInjectedTextMeasurerWithFontProperties()
    {
        RecordingTextMeasurer measurer = new(CreateMeasurement(12, 7));
        TextBlock textBlock = new()
        {
            Text = "Hello",
            FontFamily = "Serif",
            FontSize = 13,
            TextMeasurer = measurer
        };

        LayoutSize desired = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(12, 7), desired);
        Assert.Equal("Hello", measurer.Text);
        Assert.Equal("Serif", measurer.Style.FontFamily);
        Assert.Equal(13, measurer.Style.FontSize);
        Assert.Equal(100, measurer.AvailableWidth);
    }

    [Fact]
    public void TextBlockRejectsNullTextMeasurer()
    {
        TextBlock textBlock = new();

        Assert.Throws<ArgumentNullException>(() => textBlock.TextMeasurer = null!);
    }

    [Fact]
    public void TextBlockRejectsNullTextRenderer()
    {
        TextBlock textBlock = new();

        Assert.Throws<ArgumentNullException>(() => textBlock.TextRenderer = null!);
    }

    [Fact]
    public void TextBlockRendersTextCommand()
    {
        UIRoot root = new();
        TextBlock textBlock = new()
        {
            Text = "Hello",
            Foreground = DrawColor.White
        };
        root.VisualChildren.Add(textBlock);
        textBlock.Arrange(new ArrangeContext(new LayoutRect(2, 3, 50, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        Assert.Equal("Hello", commands[0].Text);
        Assert.Equal(new DrawPoint(2, 3), commands[0].Position);
    }

    [Fact]
    public void TextChangeInvalidatesMeasureAndRender()
    {
        TextBlock textBlock = new();

        textBlock.Text = "Hello";

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void TextPropertyCoercesNullToEmptyText()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello"
        };

        textBlock.SetValue(TextBlock.TextProperty, null!);

        Assert.Equal(string.Empty, textBlock.Text);
        Assert.Equal(new LayoutSize(0, 16), textBlock.Measure(new MeasureContext(new LayoutSize(100, 100))));
    }

    [Fact]
    public void ForegroundChangeInvalidatesRenderOnly()
    {
        TextBlock textBlock = new();

        textBlock.Foreground = DrawColor.White;

        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    private sealed class RecordingTextMeasurer : TextMeasurer
    {
        private readonly TextMeasureResult measurement;

        public RecordingTextMeasurer(TextMeasureResult measurement)
        {
            this.measurement = measurement;
        }

        public string? Text { get; private set; }

        public TextRunStyle Style { get; private set; }

        public float AvailableWidth { get; private set; }

        public override TextMeasureResult Measure(string text, TextRunStyle style, float availableWidth)
        {
            Text = text;
            Style = style;
            AvailableWidth = availableWidth;
            return measurement;
        }
    }

    private static TextMeasureResult CreateMeasurement(float width, float height)
    {
        TextLayoutKey key = new("Hello", "Serif:13", 13, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1);
        return new TextMeasureResult(new LayoutSize(width, height), 1, key, key.FontIdentity, [new TextLine("Hello", width)]);
    }
}
