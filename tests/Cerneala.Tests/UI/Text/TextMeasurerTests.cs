using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextMeasurerTests
{
    [Fact]
    public void EmptyTextMeasuresToZeroWidthWithLineHeight()
    {
        TextMeasurer measurer = new();

        TextMeasureResult result = measurer.Measure(string.Empty, new TextRunStyle("Default", 16), 100);

        Assert.Equal(0, result.Size.Width);
        Assert.Equal(16, result.Size.Height);
        Assert.Equal(1, result.LineCount);
    }

    [Fact]
    public void FontSizeChangesMeasurementAndCacheIdentity()
    {
        TextMeasurer measurer = new();

        TextMeasureResult first = measurer.Measure("Hello", new TextRunStyle("Default", 10), 100);
        TextMeasureResult second = measurer.Measure("Hello", new TextRunStyle("Default", 20), 100);

        Assert.NotEqual(first.Size, second.Size);
        Assert.NotEqual(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void WrappingWidthAffectsMeasurement()
    {
        TextMeasurer measurer = new();
        TextRunStyle style = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult wide = measurer.Measure("HelloWorld", style, 100);
        TextMeasureResult narrow = measurer.Measure("HelloWorld", style, 10);

        Assert.Equal(1, wide.LineCount);
        Assert.True(narrow.LineCount > wide.LineCount);
        Assert.NotEqual(wide.CacheKey, narrow.CacheKey);
    }

    [Fact]
    public void WrappedMeasurementDoesNotSplitSurrogatePairsAcrossLines()
    {
        TextMeasurer measurer = new();
        TextRunStyle style = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("a\U0001F600b", style, 10);

        Assert.Equal(["a", "\U0001F600", "b"], result.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public void WrappedMeasurementBreaksAtWordBoundariesBeforeHardWrapping()
    {
        TextMeasurer measurer = new();
        TextRunStyle style = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("Alpha beta gamma", style, 45);

        Assert.Equal(["Alpha", "beta", "gamma"], result.Lines.Select(line => line.Text).ToArray());
        Assert.All(result.Lines, line => Assert.DoesNotMatch(@"\s$", line.Text));
    }

    [Fact]
    public void WrappedMeasurementPreservesExplicitLineBreaks()
    {
        TextMeasurer measurer = new();
        TextRunStyle style = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("Alpha beta\r\ngamma", style, 100);

        Assert.Equal(["Alpha beta", "gamma"], result.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public void WrappedMeasurementPreservesWhitespaceOnlyParagraphHeight()
    {
        TextMeasurer measurer = new();
        TextRunStyle style = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("   \nAlpha", style, 100);

        Assert.Equal(["", "Alpha"], result.Lines.Select(line => line.Text).ToArray());
        Assert.True(result.Size.Height > style.FontSize);
    }

    [Fact]
    public void MeasureAllowsConcurrentAccessToSharedCache()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextRunStyle style = new("Default", 16);

        Parallel.For(
            0,
            64,
            _ => measurer.Measure("Hello", style, 100));

        Assert.Equal(1, cache.Misses);
        Assert.Equal(63, cache.Hits);
    }

    [Fact]
    public void TextStyleRejectsInvalidMetricInputs()
    {
        Assert.Throws<ArgumentException>(() => new TextRunStyle("", 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRunStyle("Default", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRunStyle("Default", 16, scale: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRunStyle("Default", 16_385));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextRunStyle("Default", 16, scale: 1_025));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void TextLineRejectsInvalidWidth(float width)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextLine("Hello", width));
    }

    [Fact]
    public void TextLineRejectsNullText()
    {
        Assert.Throws<ArgumentNullException>(() => new TextLine(null!, 1));
    }
}
