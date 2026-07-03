namespace Cerneala.UI.Text;

public readonly record struct TextLine
{
    public TextLine(string text, float width)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (width < 0 || !float.IsFinite(width))
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Line width cannot be negative or non-finite.");
        }

        Text = text;
        Width = width;
    }

    public string Text { get; }

    public float Width { get; }
}
