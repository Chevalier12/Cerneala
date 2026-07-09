namespace Cerneala.UI.Markup;

public sealed class UiMarkupTextContent : UiMarkupContent
{
    public UiMarkupTextContent(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            throw new ArgumentException("Markup text content cannot be empty.", nameof(text));
        }

        Text = text;
    }

    public string Text { get; }
}
