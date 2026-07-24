namespace Cerneala.UI.Text;

internal sealed class TextDocument
{
    public TextDocument(string text = "")
    {
        Text = text ?? string.Empty;
    }

    public string Text { get; private set; }

    public int Length => Text.Length;

    public long Version { get; private set; }

    public string Replace(int start, int length, string text)
    {
        ValidateRange(start, length);
        string replacement = text ?? string.Empty;
        string oldText = Text;
        Text = Text.Remove(start, length).Insert(start, replacement);
        if (Text != oldText)
        {
            Version++;
        }

        return oldText;
    }

    public string SetText(string text)
    {
        string oldText = Text;
        string next = text ?? string.Empty;
        if (oldText == next)
        {
            return oldText;
        }

        Text = next;
        Version++;
        return oldText;
    }

    public void ValidateRange(int start, int length)
    {
        if (start < 0 || start > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Text range start must be inside the document.");
        }

        if (length < 0 || length > Length - start)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "Text range length must fit inside the document.");
        }
    }
}
