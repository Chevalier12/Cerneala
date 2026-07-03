using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.Controls;

public sealed class TextBlockInvalidationTests
{
    [Fact]
    public void TextChangeInvalidatesMetricsAndRender()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello"
        };
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        string firstIdentity = textBlock.RenderDependencies.TextLayoutIdentity;

        textBlock.Text = "Hello!";
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.NotEqual(firstIdentity, textBlock.RenderDependencies.TextLayoutIdentity);
    }

    [Fact]
    public void FontChangeInvalidatesMetricsAndRender()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello"
        };

        textBlock.FontSize = 20;

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void ForegroundChangeDoesNotChangeTextLayoutIdentity()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello"
        };
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        string identity = textBlock.RenderDependencies.TextLayoutIdentity;
        textBlock.DirtyState.ClearAll();

        textBlock.Foreground = DrawColor.White;

        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.Equal(identity, textBlock.RenderDependencies.TextLayoutIdentity);
    }

    [Fact]
    public void ForegroundChangeDoesNotForceTextMeasurementRecompute()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextBlock textBlock = new()
        {
            Text = "Hello",
            TextMeasurer = measurer
        };

        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        textBlock.Foreground = DrawColor.White;
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(1, cache.Misses);
        Assert.Equal(0, cache.Hits);
    }

    [Fact]
    public void ChangingTextMeasurerInvalidatesCachedMeasurement()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello",
            TextMeasurer = new FixedTextMeasurer(10, 5)
        };
        LayoutSize first = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        textBlock.DirtyState.ClearAll();

        textBlock.TextMeasurer = new FixedTextMeasurer(30, 9);
        LayoutSize second = textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.Equal(new LayoutSize(10, 5), first);
        Assert.Equal(new LayoutSize(30, 9), second);
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void ChangingTextRendererInvalidatesRender()
    {
        TextBlock textBlock = new()
        {
            Text = "Hello"
        };
        textBlock.DirtyState.ClearAll();

        textBlock.TextRenderer = new TextRenderer();

        Assert.False(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    private sealed class FixedTextMeasurer : TextMeasurer
    {
        private readonly float width;
        private readonly float height;

        public FixedTextMeasurer(float width, float height)
        {
            this.width = width;
            this.height = height;
        }

        public override TextMeasureResult Measure(string text, TextRunStyle style, float availableWidth)
        {
            TextLayoutKey key = new(text, $"{style.FontFamily}:{width}x{height}", style.FontSize, style.Wrapping, availableWidth, style.Trimming, style.Scale);
            return new TextMeasureResult(new LayoutSize(width, height), 1, key, key.FontIdentity, [new TextLine(text, width)]);
        }
    }
}
