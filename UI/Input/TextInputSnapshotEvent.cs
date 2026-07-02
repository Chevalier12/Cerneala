namespace Cerneala.UI.Input;

public sealed record TextInputSnapshotEvent(string Text)
{
    public string Text { get; } = string.IsNullOrEmpty(Text)
        ? throw new ArgumentException("Text input cannot be empty.", nameof(Text))
        : Text;
}
