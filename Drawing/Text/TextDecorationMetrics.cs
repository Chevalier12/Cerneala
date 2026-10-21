namespace Cerneala.Drawing.Text;

// Offsets are relative to the text baseline; a positive offset is below the baseline. The underline offset is the top edge
// of the underline stroke and the strikethrough offset is the bottom edge of the strikethrough stroke (the Skia
// definitions); a thickness is the height of its stroke.
public readonly record struct TextDecorationMetrics(
    float UnderlineOffset,
    float UnderlineThickness,
    float StrikethroughOffset,
    float StrikethroughThickness)
{
    // The metrics of a font that reports none, or that is not a Skia font.
    public static TextDecorationMetrics CreateFallback(float size)
    {
        float thickness = MathF.Max(1, size / 14);
        return new TextDecorationMetrics(size * 0.1f, thickness, -(size * 0.3f), thickness);
    }
}
