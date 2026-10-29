namespace Cerneala.Drawing;

public sealed class DrawTextRun
{
    public DrawTextRun(IDrawFont font, string text, float size)
        : this(font, text, size, DrawTextDirection.Auto)
    {
    }

    internal DrawTextRun(IDrawFont font, string text, float size, DrawTextDirection direction)
    {
        DrawArgument.ThrowIfNotValidTextSize(size, nameof(size));

        Font = font ?? throw new ArgumentNullException(nameof(font));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Size = size;
        Direction = direction;
    }

    public IDrawFont Font { get; }

    public string Text { get; }

    public float Size { get; }

    // The direction a text layout resolved for the run (UAX #9), so that the shaper orders its glyphs that way: a number in
    // a right-to-left line is a left-to-right run, also when its digits are Arabic-Indic. Auto lets the shaper guess the
    // direction from the script of the text.
    internal DrawTextDirection Direction { get; }
}
