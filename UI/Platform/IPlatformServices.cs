namespace Cerneala.UI.Platform;

public interface IPlatformServices
{
    IClipboard? Clipboard { get; }

    ICursorService? Cursor { get; }

    IFileDialogService? FileDialogs { get; }

    ITextInputPlatform? TextInput { get; }

    IDpiProvider? Dpi { get; }

    IAccessibilityPlatform? Accessibility { get; }
}
