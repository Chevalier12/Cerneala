using Cerneala.UI.Platform;

namespace Cerneala.Platforms.Sdl3;

internal sealed class SdlClipboard : IClipboard
{
    private readonly ISdlApi api;

    public SdlClipboard(ISdlApi api)
    {
        this.api = api;
    }

    public bool HasText => api.HasClipboardText();

    public string? GetText() => api.HasClipboardText() ? api.GetClipboardText() : null;

    // A refused write does nothing and does not throw, like a WPF TextBox copy: another program
    // holding the system clipboard must not crash the application.
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        api.SetClipboardText(text);
    }
}