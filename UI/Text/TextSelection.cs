namespace Cerneala.UI.Text;

public readonly record struct TextSelection(int Anchor, int Active)
{
    public int Start => Math.Min(Anchor, Active);

    public int End => Math.Max(Anchor, Active);

    public int Length => End - Start;

    public bool IsEmpty => Length == 0;

    public static TextSelection Caret(int position)
    {
        return new TextSelection(position, position);
    }

    public TextSelection Clamp(int documentLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(documentLength);
        return new TextSelection(Math.Clamp(Anchor, 0, documentLength), Math.Clamp(Active, 0, documentLength));
    }
}
