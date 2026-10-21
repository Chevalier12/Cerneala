using Cerneala.Drawing;
using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

// Issue #683, review round 2: measure, arrange and render must agree, and TextParagraphOptions validates its input.
public sealed class TextParagraphReviewTests
{
    private const string Family = "Segoe UI";

    private const string LongText =
        "The quick brown fox jumps over the lazy dog while the five boxing wizards jump quickly. " +
        "Pack my box with five dozen liquor jugs, and sphinx of black quartz, judge my vow! " +
        "AVAVA To Ty We Wa Yo Te fi fl ff -- (parentheses) [brackets] and some-hyphenated-words / slashes.";

    public static IEnumerable<object[]> WrappedTexts()
    {
        foreach (float width in new[] { 60f, 97.3f, 140f, 211.7f, 333f, 480.5f })
        {
            yield return new object[] { width, 400, FontStyle.Normal, false };
            yield return new object[] { width, 700, FontStyle.Normal, false };
            yield return new object[] { width, 400, FontStyle.Italic, false };
            yield return new object[] { width, 400, FontStyle.Normal, true };
        }
    }

    // 4. Measure -> arrange -> render must not re-wrap: the measured width is a width at which the same lines break.

    [Theory]
    [MemberData(nameof(WrappedTexts))]
    public void LayingTheTextOutAtItsMeasuredWidthGivesTheSameLines(float availableWidth, int weight, FontStyle style, bool mixed)
    {
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, new TextLayoutCache());
        TextAspect regular = new(Family, 16, TextWrapping.Wrap, fontWeight: (FontWeight)weight, fontStyle: style);
        TextAspect bold = new(Family, 16, TextWrapping.Wrap, fontWeight: FontWeight.Bold, fontStyle: style);
        int split = LongText.Length / 3;
        TextSpan[] spans = mixed
            ?
            [
                new TextSpan(LongText[..split], regular),
                new TextSpan(LongText[split..(split * 2)], bold),
                new TextSpan(LongText[(split * 2)..], regular)
            ]
            : [new TextSpan(LongText, regular)];

        TextParagraphLayout first = measurer.MeasureParagraph(spans, regular, new TextParagraphOptions(), new LayoutSize(availableWidth, float.PositiveInfinity));
        TextParagraphLayout second = measurer.MeasureParagraph(spans, regular, new TextParagraphOptions(), new LayoutSize(first.Size.Width, float.PositiveInfinity));

        Assert.Equal(first.Layout.Lines.Select(static line => line.Text), second.Layout.Lines.Select(static line => line.Text));
        Assert.Equal(first.LineCount, second.LineCount);
    }

    // 10. TextParagraphOptions is a validated class.

    [Fact]
    public void TextParagraphOptionsIsAValidatedReferenceType()
    {
        Assert.True(typeof(TextParagraphOptions).IsClass);
        Assert.True(typeof(TextParagraphOptions).IsSealed);
    }

    [Fact]
    public void DefaultOptionsHaveNoLineHeightAndTheDocumentedDefaults()
    {
        TextParagraphOptions options = new();

        Assert.Equal(TextAlignment.Left, options.Alignment);
        Assert.Equal(0, options.MaxLines);
        Assert.True(float.IsNaN(options.LineHeight));
        Assert.Equal(LineStackingStrategy.MaxHeight, options.LineStackingStrategy);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(-1f)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void LineHeightMustBeNaNOrPositiveAndFinite(float lineHeight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextParagraphOptions(LineHeight: lineHeight));
    }

    [Fact]
    public void OptionsRejectUndefinedEnumsAndNegativeMaxLines()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextParagraphOptions(Alignment: (TextAlignment)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextParagraphOptions(LineStackingStrategy: (LineStackingStrategy)9));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TextParagraphOptions(MaxLines: -1));
    }

    [Fact]
    public void OptionsWithTheSameValuesAreEqual()
    {
        Assert.Equal(new TextParagraphOptions(TextAlignment.Center, 2, 30), new TextParagraphOptions(TextAlignment.Center, 2, 30));
        Assert.NotEqual(new TextParagraphOptions(TextAlignment.Center, 2, 30), new TextParagraphOptions(TextAlignment.Center, 3, 30));
    }

    [Fact]
    public void MeasureAndRenderRejectNullOptions()
    {
        TextMeasurer measurer = new(FontResolver.Default, LineBreakService.Default, new TextLayoutCache());
        TextRenderer renderer = new(FontResolver.Default, measurer);
        TextAspect aspect = new(Family, 16);
        TextSpan[] spans = [new TextSpan("a", aspect)];
        object?[] measureArguments = [spans, aspect, null, new LayoutSize(100, 100)];
        object?[] renderArguments = [new DrawingContext(new DrawCommandList()), spans, aspect, null, new LayoutSize(100, 100), default(DrawPoint)];

        System.Reflection.TargetInvocationException measureFailure = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => typeof(TextMeasurer).GetMethod(nameof(TextMeasurer.MeasureParagraph))!.Invoke(measurer, measureArguments));
        System.Reflection.TargetInvocationException renderFailure = Assert.Throws<System.Reflection.TargetInvocationException>(
            () => typeof(TextRenderer).GetMethod(nameof(TextRenderer.RenderParagraph))!.Invoke(renderer, renderArguments));

        Assert.IsType<ArgumentNullException>(measureFailure.InnerException);
        Assert.IsType<ArgumentNullException>(renderFailure.InnerException);
    }
}
