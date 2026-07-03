namespace Cerneala.UI.Platform;

public interface ITextInputPlatform
{
    IClipboard? Clipboard => null;

    bool SupportsIme { get; }
}
