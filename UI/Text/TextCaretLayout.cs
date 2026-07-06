using Cerneala.Drawing.Text;

namespace Cerneala.UI.Text;

public sealed class TextCaretLayout
{
    private readonly TextShaper shaper = TextShaper.Default;

    public static TextCaretLayout Default { get; } = new();

    public float GetCaretX(string text, int position, TextRunStyle style, FontResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(resolver);

        int clampedPosition = Math.Clamp(position, 0, text.Length);
        if (clampedPosition == 0)
        {
            return 0;
        }

        return MeasurePrefix(text[..clampedPosition], style, resolver);
    }

    public int GetCaretIndexAtX(string text, float x, TextRunStyle style, FontResolver resolver)
    {
        return GetCaretIndexAtX(text, x, 0, style, resolver);
    }

    public int GetCaretIndexAtX(string text, float x, float horizontalTextOffset, TextRunStyle style, FontResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(resolver);

        if (text.Length == 0)
        {
            return 0;
        }

        float textX = x + horizontalTextOffset;
        float[] stops = BuildCaretStops(text, style, resolver);
        if (textX <= stops[0])
        {
            return 0;
        }

        if (textX >= stops[^1])
        {
            return text.Length;
        }

        int nearestIndex = 0;
        float nearestDistance = Math.Abs(textX - stops[0]);
        for (int i = 1; i < stops.Length; i++)
        {
            float distance = Math.Abs(textX - stops[i]);
            if (distance < nearestDistance)
            {
                nearestIndex = i;
                nearestDistance = distance;
            }
        }

        return nearestIndex;
    }

    public float GetCaretLineHeight(TextRunStyle style, FontResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        ResolvedTextFont font = resolver.Resolve(style);
        if (shaper.TryMeasureLineHeight(style.ToDrawTextRun(font, "Ag"), out float lineHeight))
        {
            return lineHeight;
        }

        return style.FontSize * style.Scale;
    }

    public TextCaretVerticalMetrics GetCaretVerticalMetrics(TextRunStyle style, FontResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);

        ResolvedTextFont font = resolver.Resolve(style);
        if (shaper.TryMeasureCaretVerticalMetrics(style.ToDrawTextRun(font, "Ag"), out TextCaretVerticalMetrics metrics))
        {
            return metrics;
        }

        return new TextCaretVerticalMetrics(0, style.FontSize * style.Scale);
    }

    private float[] BuildCaretStops(string text, TextRunStyle style, FontResolver resolver)
    {
        float[] stops = new float[text.Length + 1];
        for (int i = 1; i < stops.Length; i++)
        {
            stops[i] = MeasurePrefix(text[..i], style, resolver);
        }

        return stops;
    }

    private float MeasurePrefix(string prefix, TextRunStyle style, FontResolver resolver)
    {
        if (prefix.Length == 0)
        {
            return 0;
        }

        ResolvedTextFont font = resolver.Resolve(style);
        if (shaper.TryShape(style.ToDrawTextRun(font, prefix), out TextShapeResult shape))
        {
            return shape.AdvanceWidth;
        }

        return TextMeasurer.Default.Measure(prefix, style, float.PositiveInfinity).Size.Width;
    }
}
