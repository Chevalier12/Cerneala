namespace Cerneala.UI.Controls;

public class TextMeasurer
{
    public static TextMeasurer Default { get; } = new();

    public virtual TextMeasurement Measure(string text, string fontFamily, float fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(fontFamily);
        if (fontSize <= 0 || !float.IsFinite(fontSize))
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), "Font size must be positive and finite.");
        }

        float width = text.Length * fontSize * 0.5f;
        return new TextMeasurement(width, fontSize);
    }
}
