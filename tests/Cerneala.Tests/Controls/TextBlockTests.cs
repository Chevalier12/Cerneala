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

        LayoutSize first = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        LayoutSize second = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(first, second);
        Assert.True(first.Width > 0);
        Assert.True(first.Height >= textBlock.FontSize);
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
        Assert.Equal("Serif", measurer.CapturedTextAspect.FontFamily);
        Assert.Equal(13, measurer.CapturedTextAspect.FontSize);
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
        root.ProcessFrame();
        textBlock.Arrange(new ArrangeContext(new LayoutRect(2, 3, 50, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(DrawCommandKind.DrawText, commands[0].Kind);
        Assert.Equal("Hello", commands[0].Text);
        Assert.Equal(2, commands[0].Position.X);
        Assert.True(commands[0].Position.Y > 3);
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
        LayoutSize desired = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        Assert.Equal(0, desired.Width);
        Assert.True(desired.Height >= textBlock.FontSize);
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

        public TextAspect CapturedTextAspect { get; private set; }

        public float AvailableWidth { get; private set; }

        public override TextMeasureResult Measure(string text, TextAspect aspect, float availableWidth)
        {
            Text = text;
            CapturedTextAspect = aspect;
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
