using Cerneala.Drawing.Text;
using Cerneala.Drawing;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextCaretLayoutTests
{
    private const string VariableWidthText = "iiiiWWWW";
    private static readonly TextAspect TextAspect = new("Arial", 20);

    [Fact]
    public void GetCaretXUsesNonUniformShapedPrefixMetrics()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());

        float[] stops = Enumerable
            .Range(0, VariableWidthText.Length + 1)
            .Select(position => layout.GetCaretX(VariableWidthText, position, TextAspect, resolver))
            .ToArray();

        Assert.Equal(0, stops[0]);
        Assert.True(stops[^1] > 0);
        Assert.All(stops.Zip(stops.Skip(1)), pair => Assert.True(pair.Second >= pair.First));

        float firstAdvance = stops[1] - stops[0];
        Assert.Contains(stops.Zip(stops.Skip(1)), pair => Math.Abs(pair.Second - pair.First - firstAdvance) > 0.01f);
    }

    [Fact]
    public void GetCaretXMatchesRasterizedTextTopLeftCoordinateConvention()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        ResolvedTextFont font = resolver.Resolve(TextAspect);
        TextShapeResult shape = new SkiaTextShaper().Shape(TextAspect.ToDrawTextRun(font, VariableWidthText));

        float caretX = layout.GetCaretX(VariableWidthText, VariableWidthText.Length, TextAspect, resolver);

        Assert.Equal(shape.AdvanceWidth, caretX, precision: 2);
    }

    [Fact]
    public void GetCaretXClampsPositions()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());

        float start = layout.GetCaretX(VariableWidthText, 0, TextAspect, resolver);
        float end = layout.GetCaretX(VariableWidthText, VariableWidthText.Length, TextAspect, resolver);

        Assert.Equal(start, layout.GetCaretX(VariableWidthText, -10, TextAspect, resolver));
        Assert.Equal(end, layout.GetCaretX(VariableWidthText, VariableWidthText.Length + 10, TextAspect, resolver));
    }

    [Fact]
    public void GetCaretIndexAtXReturnsNearestCaretStop()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        float second = layout.GetCaretX(VariableWidthText, 2, TextAspect, resolver);
        float third = layout.GetCaretX(VariableWidthText, 3, TextAspect, resolver);

        Assert.Equal(0, layout.GetCaretIndexAtX(VariableWidthText, -10, TextAspect, resolver));
        Assert.Equal(VariableWidthText.Length, layout.GetCaretIndexAtX(VariableWidthText, 10_000, TextAspect, resolver));
        Assert.Equal(2, layout.GetCaretIndexAtX(VariableWidthText, second + ((third - second) * 0.25f), TextAspect, resolver));
        Assert.Equal(3, layout.GetCaretIndexAtX(VariableWidthText, second + ((third - second) * 0.75f), TextAspect, resolver));
    }

    [Fact]
    public void GetCaretIndexAtXDoesNotReturnMiddleOfSurrogatePair()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        string text = "a\U0001F600b";
        float middleOfSurrogatePair = layout.GetCaretX(text, 2, TextAspect, resolver);

        int index = layout.GetCaretIndexAtX(text, middleOfSurrogatePair, TextAspect, resolver);

        Assert.NotEqual(2, index);
    }

    [Fact]
    public void GetCaretIndexAtXMapsViewportCoordinatesThroughHorizontalOffset()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        float target = layout.GetCaretX(VariableWidthText, 5, TextAspect, resolver);
        float horizontalTextOffset = target - 2;

        int index = layout.GetCaretIndexAtX(VariableWidthText, 2, horizontalTextOffset, TextAspect, resolver);

        Assert.Equal(5, index);
    }

    [Fact]
    public void GetCaretLineHeightUsesFontMetricsWhenAvailable()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        ResolvedTextFont font = resolver.Resolve(TextAspect);
        Assert.True(TextShaper.Default.TryMeasureLineHeight(TextAspect.ToDrawTextRun(font, "Ag"), out float expected));

        float height = layout.GetCaretLineHeight(TextAspect, resolver);

        Assert.Equal(expected, height, precision: 2);
    }

    [Fact]
    public void GetCaretLineHeightFallsBackToAspectHeightWithoutRasterMetrics()
    {
        TextCaretLayout layout = TextCaretLayout.Default;

        float height = layout.GetCaretLineHeight(TextAspect, ApproximateResolver());

        Assert.Equal(TextAspect.FontSize * TextAspect.Scale, height);
    }

    [Fact]
    public void GetCaretVerticalMetricsUseFontLineMetricsWhenAvailable()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        ResolvedTextFont font = resolver.Resolve(TextAspect);
        Assert.True(TextShaper.Default.TryMeasureLineHeight(TextAspect.ToDrawTextRun(font, "Ag"), out float expected));

        TextCaretVerticalMetrics metrics = layout.GetCaretVerticalMetrics(TextAspect, resolver);

        Assert.Equal(0, metrics.OffsetY);
        Assert.Equal(expected, metrics.Height, precision: 2);
    }

    [Fact]
    public void GetCaretVerticalMetricsFallsBackToAspectHeightWithoutRasterMetrics()
    {
        TextCaretLayout layout = TextCaretLayout.Default;

        TextCaretVerticalMetrics metrics = layout.GetCaretVerticalMetrics(TextAspect, ApproximateResolver());

        Assert.Equal(0, metrics.OffsetY);
        Assert.Equal(TextAspect.FontSize * TextAspect.Scale, metrics.Height);
    }

    [Fact]
    public void FallsBackToApproximateMetricsWithoutSkiaFont()
    {
        TextCaretLayout layout = TextCaretLayout.Default;

        FontResolver resolver = ApproximateResolver();
        float x = layout.GetCaretX("abcd", 2, TextAspect, resolver);
        int index = layout.GetCaretIndexAtX("abcd", x + 1, TextAspect, resolver);

        Assert.True(x > 0);
        Assert.Equal(2, index);
    }

    private static FontResolver ApproximateResolver() => new(new ApproximateFontSource());

    private sealed class ApproximateFontSource : IFontSource
    {
        public IDrawFont LoadFont(string familyName, float size) => new ApproximateFont(familyName, size);
    }

    private sealed record ApproximateFont(string FamilyName, float Size) : IDrawFont;

}
