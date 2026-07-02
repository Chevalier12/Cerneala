namespace Cerneala.Drawing;

public sealed class DrawTextRun
{
    public DrawTextRun(IDrawFont font, string text, float size)
    {
        Font = font ?? throw new ArgumentNullException(nameof(font));
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Size = size;
    }

    public IDrawFont Font { get; }

    public string Text { get; }

    public float Size { get; }
}
