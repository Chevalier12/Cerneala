using Cerneala.Drawing.Text;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextCaretLayoutTests
{
    private const string VariableWidthText = "iiiiWWWW";
    private static readonly TextRunStyle Style = new("Arial", 20);

    [Fact]
    public void GetCaretXUsesNonUniformShapedPrefixMetrics()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());

        float[] stops = Enumerable
            .Range(0, VariableWidthText.Length + 1)
            .Select(position => layout.GetCaretX(VariableWidthText, position, Style, resolver))
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
        ResolvedTextFont font = resolver.Resolve(Style);
        TextShapeResult shape = new SkiaTextShaper().Shape(Style.ToDrawTextRun(font, VariableWidthText));

        float caretX = layout.GetCaretX(VariableWidthText, VariableWidthText.Length, Style, resolver);

        Assert.Equal(shape.AdvanceWidth, caretX, precision: 2);
    }

    [Fact]
    public void GetCaretXClampsPositions()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());

        float start = layout.GetCaretX(VariableWidthText, 0, Style, resolver);
        float end = layout.GetCaretX(VariableWidthText, VariableWidthText.Length, Style, resolver);

        Assert.Equal(start, layout.GetCaretX(VariableWidthText, -10, Style, resolver));
        Assert.Equal(end, layout.GetCaretX(VariableWidthText, VariableWidthText.Length + 10, Style, resolver));
    }

    [Fact]
    public void GetCaretIndexAtXReturnsNearestCaretStop()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        float second = layout.GetCaretX(VariableWidthText, 2, Style, resolver);
        float third = layout.GetCaretX(VariableWidthText, 3, Style, resolver);

        Assert.Equal(0, layout.GetCaretIndexAtX(VariableWidthText, -10, Style, resolver));
        Assert.Equal(VariableWidthText.Length, layout.GetCaretIndexAtX(VariableWidthText, 10_000, Style, resolver));
        Assert.Equal(2, layout.GetCaretIndexAtX(VariableWidthText, second + ((third - second) * 0.25f), Style, resolver));
        Assert.Equal(3, layout.GetCaretIndexAtX(VariableWidthText, second + ((third - second) * 0.75f), Style, resolver));
    }

    [Fact]
    public void GetCaretIndexAtXMapsViewportCoordinatesThroughHorizontalOffset()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        float target = layout.GetCaretX(VariableWidthText, 5, Style, resolver);
        float horizontalTextOffset = target - 2;

        int index = layout.GetCaretIndexAtX(VariableWidthText, 2, horizontalTextOffset, Style, resolver);

        Assert.Equal(5, index);
    }

    [Fact]
    public void GetCaretLineHeightUsesRasterizedLineBoundsWhenAvailable()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        ResolvedTextFont font = resolver.Resolve(Style);
        float expected = new SkiaTextRasterizer().Rasterize(Style.ToDrawTextRun(font, "Ag"), Cerneala.Drawing.DrawColor.White).Height;

        float height = layout.GetCaretLineHeight(Style, resolver);

        Assert.Equal(expected, height, precision: 2);
    }

    [Fact]
    public void GetCaretLineHeightFallsBackToStyleHeightWithoutRasterMetrics()
    {
        TextCaretLayout layout = TextCaretLayout.Default;

        float height = layout.GetCaretLineHeight(Style, FontResolver.Default);

        Assert.Equal(Style.FontSize * Style.Scale, height);
    }

    [Fact]
    public void GetCaretVerticalMetricsSpansRasterizedLineBoundsWhenAvailable()
    {
        TextCaretLayout layout = TextCaretLayout.Default;
        FontResolver resolver = new(new SystemFontSource());
        ResolvedTextFont font = resolver.Resolve(Style);
        RasterizedText rasterizedLine = new SkiaTextRasterizer().Rasterize(
            Style.ToDrawTextRun(font, "Ag"),
            Cerneala.Drawing.DrawColor.White);

        TextCaretVerticalMetrics metrics = layout.GetCaretVerticalMetrics(Style, resolver);

        Assert.Equal(0, metrics.OffsetY);
        Assert.Equal(rasterizedLine.Height, metrics.Height, precision: 2);
    }

    [Fact]
    public void GetCaretVerticalMetricsFallsBackToStyleHeightWithoutRasterMetrics()
    {
        TextCaretLayout layout = TextCaretLayout.Default;

        TextCaretVerticalMetrics metrics = layout.GetCaretVerticalMetrics(Style, FontResolver.Default);

        Assert.Equal(0, metrics.OffsetY);
        Assert.Equal(Style.FontSize * Style.Scale, metrics.Height);
    }

    [Fact]
    public void FallsBackToApproximateMetricsWithoutSkiaFont()
    {
        TextCaretLayout layout = TextCaretLayout.Default;

        float x = layout.GetCaretX("abcd", 2, Style, FontResolver.Default);
        int index = layout.GetCaretIndexAtX("abcd", x + 1, Style, FontResolver.Default);

        Assert.True(x > 0);
        Assert.Equal(2, index);
    }

}
