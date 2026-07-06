using Cerneala.Drawing.Text;
using System.Globalization;

namespace Cerneala.UI.Text;

public sealed class TextCaretLayout
{
    private readonly TextShaper shaper = TextShaper.Default;

    public static TextCaretLayout Default { get; } = new();

    public float GetCaretX(string text, int position, TextRunStyle style, FontResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(resolver);

        int clampedPosition = NormalizeCaretPosition(text, Math.Clamp(position, 0, text.Length));
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
        int[] positions = BuildCaretPositions(text);
        float[] stops = BuildCaretStops(text, positions, style, resolver);
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

        return positions[nearestIndex];
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

    private float[] BuildCaretStops(string text, int[] positions, TextRunStyle style, FontResolver resolver)
    {
        float[] stops = new float[positions.Length];
        for (int i = 1; i < stops.Length; i++)
        {
            stops[i] = MeasurePrefix(text[..positions[i]], style, resolver);
        }

        return stops;
    }

    private static int[] BuildCaretPositions(string text)
    {
        int[] starts = StringInfo.ParseCombiningCharacters(text);
        int[] positions = new int[starts.Length + 1];
        Array.Copy(starts, positions, starts.Length);
        positions[^1] = text.Length;
        return positions;
    }

    private static int NormalizeCaretPosition(string text, int position)
    {
        if (position <= 0)
        {
            return 0;
        }

        if (position >= text.Length)
        {
            return text.Length;
        }

        int previous = 0;
        foreach (int start in StringInfo.ParseCombiningCharacters(text))
        {
            if (start == position)
            {
                return position;
            }

            if (start > position)
            {
                return previous;
            }

            previous = start;
        }

        return previous;
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
