using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextMeasurerTests
{
    [Fact]
    public void EmptyTextMeasuresToZeroWidthWithLineHeight()
    {
        TextMeasurer measurer = new();

        TextMeasureResult result = measurer.Measure(string.Empty, new TextAspect("Default", 16), 100);

        Assert.Equal(0, result.Size.Width);
        Assert.True(result.Size.Height >= 16);
        Assert.Equal(1, result.LineCount);
    }

    [Fact]
    public void FontSizeChangesMeasurementAndCacheIdentity()
    {
        TextMeasurer measurer = new();

        TextMeasureResult first = measurer.Measure("Hello", new TextAspect("Default", 10), 100);
        TextMeasureResult second = measurer.Measure("Hello", new TextAspect("Default", 20), 100);

        Assert.NotEqual(first.Size, second.Size);
        Assert.NotEqual(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void WrappingWidthAffectsMeasurement()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult wide = measurer.Measure("HelloWorld", aspect, 100);
        TextMeasureResult narrow = measurer.Measure("HelloWorld", aspect, 10);

        Assert.Equal(1, wide.LineCount);
        Assert.True(narrow.LineCount > wide.LineCount);
        Assert.NotEqual(wide.CacheKey, narrow.CacheKey);
    }

    [Fact]
    public void WrappedMeasurementDoesNotSplitSurrogatePairsAcrossLines()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("a\U0001F600b", aspect, 10);

        Assert.Equal(["a", "\U0001F600", "b"], result.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public void WrappedMeasurementBreaksAtWordBoundariesBeforeHardWrapping()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("Alpha beta gamma", aspect, 45);

        Assert.Equal(["Alpha", "beta", "gamma"], result.Lines.Select(line => line.Text).ToArray());
        Assert.All(result.Lines, line => Assert.DoesNotMatch(@"\s$", line.Text));
    }

    [Fact]
    public void WrappedMeasurementPreservesExplicitLineBreaks()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("Alpha beta\r\ngamma", aspect, 100);

        Assert.Equal(["Alpha beta", "gamma"], result.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public void WrappedMeasurementPreservesWhitespaceOnlyParagraphHeight()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap);

        TextMeasureResult result = measurer.Measure("   \nAlpha", aspect, 100);

        Assert.Equal(["", "Alpha"], result.Lines.Select(line => line.Text).ToArray());
        Assert.True(result.Size.Height > aspect.FontSize);
    }

    [Fact]
    public void CharacterEllipsisUsesTheClosestGraphemeBoundary()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, trimming: TextTrimming.CharacterEllipsis);
        float width = measurer.Measure("Alpha be…", aspect, float.PositiveInfinity).Size.Width;

        TextMeasureResult result = measurer.Measure("Alpha beta gamma", aspect, width);

        TextLine line = Assert.Single(result.Lines);
        Assert.Equal("Alpha be…", line.Text);
        Assert.True(line.Width <= width);
    }

    [Fact]
    public void CharacterEllipsisDoesNotSplitSurrogatePairs()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, trimming: TextTrimming.CharacterEllipsis);
        float width = measurer.Measure("A…", aspect, float.PositiveInfinity).Size.Width;

        TextLine line = Assert.Single(measurer.Measure("A\U0001F600B", aspect, width).Lines);

        Assert.Equal("A…", line.Text);
    }

    [Fact]
    public void WordEllipsisUsesTheClosestCompleteWord()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, trimming: TextTrimming.WordEllipsis);
        float width = measurer.Measure("Alpha be…", aspect, float.PositiveInfinity).Size.Width;

        TextLine line = Assert.Single(measurer.Measure("Alpha beta gamma", aspect, width).Lines);

        Assert.Equal("Alpha…", line.Text);
        Assert.True(line.Width <= width);
    }

    [Fact]
    public void WordEllipsisFallsBackToCharacterBoundaryForAnOversizedFirstWord()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, trimming: TextTrimming.WordEllipsis);
        float width = measurer.Measure("Supe…", aspect, float.PositiveInfinity).Size.Width;

        TextLine line = Assert.Single(measurer.Measure("Supercalifragilistic", aspect, width).Lines);

        Assert.Equal("Supe…", line.Text);
        Assert.True(line.Width <= width);
    }

    [Fact]
    public void TrimmingLeavesFittingTextUnchanged()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, trimming: TextTrimming.CharacterEllipsis);

        TextLine line = Assert.Single(measurer.Measure("Alpha", aspect, 100).Lines);

        Assert.Equal("Alpha", line.Text);
    }

    [Fact]
    public void TrimmingReturnsAnEmptyLineWhenEvenTheEllipsisCannotFit()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, trimming: TextTrimming.CharacterEllipsis);

        TextLine line = Assert.Single(measurer.Measure("Alpha", aspect, 1).Lines);

        Assert.Equal(string.Empty, line.Text);
        Assert.Equal(0, line.Width);
    }

    [Fact]
    public void VerticalOverflowCollapsesTheLastVisibleWrappedLine()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap, TextTrimming.WordEllipsis);
        float lineHeight = measurer.Measure("A", aspect, 45).Size.Height;

        TextMeasureResult result = measurer.Measure(
            "Alpha beta gamma delta",
            aspect,
            new Cerneala.UI.Layout.LayoutSize(45, lineHeight * 2));

        Assert.Equal(["Alpha", "beta…"], result.Lines.Select(line => line.Text).ToArray());
        Assert.Equal(2, result.LineCount);
        Assert.Equal(lineHeight * 2, result.Size.Height, precision: 3);
    }

    [Fact]
    public void VerticalOverflowCollapsesExplicitLinesAtTheLastVisibleLine()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.NoWrap, TextTrimming.WordEllipsis);
        float lineHeight = measurer.Measure("A", aspect, 100).Size.Height;

        TextMeasureResult result = measurer.Measure(
            "Alpha\nbeta\ngamma",
            aspect,
            new Cerneala.UI.Layout.LayoutSize(100, lineHeight * 2));

        Assert.Equal(["Alpha", "beta…"], result.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public void VerticalOverflowRetainsTheFirstLineWhenHeightIsSmallerThanLineHeight()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap, TextTrimming.CharacterEllipsis);
        float lineHeight = measurer.Measure("A", aspect, 45).Size.Height;

        TextMeasureResult result = measurer.Measure(
            "Alpha beta gamma",
            aspect,
            new Cerneala.UI.Layout.LayoutSize(45, lineHeight / 2));

        TextLine line = Assert.Single(result.Lines);
        Assert.EndsWith("…", line.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void VisibleLineCountParticipatesInTheLayoutCacheIdentity()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap, TextTrimming.WordEllipsis);
        float lineHeight = measurer.Measure("A", aspect, 45).Size.Height;

        TextMeasureResult oneLine = measurer.Measure(
            "Alpha beta gamma delta",
            aspect,
            new Cerneala.UI.Layout.LayoutSize(45, lineHeight));
        TextMeasureResult twoLines = measurer.Measure(
            "Alpha beta gamma delta",
            aspect,
            new Cerneala.UI.Layout.LayoutSize(45, lineHeight * 2));

        Assert.NotEqual(oneLine.CacheKey, twoLines.CacheKey);
        Assert.Equal(1, oneLine.LineCount);
        Assert.Equal(2, twoLines.LineCount);
    }

    [Fact]
    public void NoneDoesNotCollapseLinesForFiniteLayoutBounds()
    {
        TextMeasurer measurer = new();
        TextAspect aspect = new("Default", 10, TextWrapping.Wrap, TextTrimming.None);
        float lineHeight = measurer.Measure("A", aspect, 45).Size.Height;

        TextMeasureResult result = measurer.Measure(
            "Alpha beta gamma",
            aspect,
            new Cerneala.UI.Layout.LayoutSize(45, lineHeight));

        Assert.Equal(["Alpha", "beta", "gamma"], result.Lines.Select(line => line.Text).ToArray());
    }

    [Fact]
    public void MeasureAllowsConcurrentAccessToSharedCache()
    {
        TextLayoutCache cache = new();
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, cache);
        TextAspect aspect = new("Default", 16);

        Parallel.For(
            0,
            64,
            _ => measurer.Measure("Hello", aspect, 100));

        Assert.Equal(1, cache.Misses);
        Assert.Equal(63, cache.Hits);
    }

    [Fact]
    public void TextAspectRejectsInvalidMetricInputs()
    {
        Assert.Throws<ArgumentException>(() => new TextAspect("", 16));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Default", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Default", 16, scale: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Default", 16_385));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextAspect("Default", 16, scale: 1_025));
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
