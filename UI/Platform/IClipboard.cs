namespace Cerneala.UI.Platform;

public interface IClipboard
{
    bool HasText { get; }

    string? GetText();

    void SetText(string text);
}
