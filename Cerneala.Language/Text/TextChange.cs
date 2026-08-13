namespace Cerneala.Language.Text;

internal readonly struct TextChange
{
    public TextChange(TextSpan span, string newText)
    {
        Span = span;
        NewText = newText ?? throw new ArgumentNullException(nameof(newText));
    }

    public TextSpan Span { get; }

    public string NewText { get; }
}
