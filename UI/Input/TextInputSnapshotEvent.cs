namespace Cerneala.UI.Input;

public sealed record TextInputSnapshotEvent(string Text)
{
    public string Text { get; } = Validate(Text);

    private static string Validate(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new ArgumentException("Text input cannot be empty.", nameof(Text));
        }

        return text;
    }
}
