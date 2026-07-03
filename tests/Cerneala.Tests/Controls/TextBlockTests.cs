using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

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
        RecordingTextMeasurer measurer = new(new TextMeasurement(12, 7));
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
        Assert.Equal("Serif", measurer.FontFamily);
        Assert.Equal(13, measurer.FontSize);
    }

    [Fact]
    public void TextBlockRejectsNullTextMeasurer()
    {
        TextBlock textBlock = new();

        Assert.Throws<ArgumentNullException>(() => textBlock.TextMeasurer = null!);
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

        DrawCommandList commands = root.RetainedRenderer.Render(root);

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
        private readonly TextMeasurement measurement;

        public RecordingTextMeasurer(TextMeasurement measurement)
        {
            this.measurement = measurement;
        }

        public string? Text { get; private set; }

        public string? FontFamily { get; private set; }

        public float FontSize { get; private set; }

        public override TextMeasurement Measure(string text, string fontFamily, float fontSize)
        {
            Text = text;
            FontFamily = fontFamily;
            FontSize = fontSize;
            return measurement;
        }
    }
}
