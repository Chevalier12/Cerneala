namespace Cerneala.UI.Text;

public readonly record struct TextCaret(int Position)
{
    public static TextCaret At(int position, int documentLength)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(documentLength);
        return new TextCaret(Math.Clamp(position, 0, documentLength));
    }
}
