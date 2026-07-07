using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Platform;

public sealed record PlatformServices(
    IClipboard? Clipboard = null,
    ICursorService? Cursor = null,
    IFileDialogService? FileDialogs = null,
    ITextInputPlatform? TextInput = null,
    IDpiProvider? Dpi = null,
    IAccessibilityPlatform? Accessibility = null,
    IReducedMotionSource? ReducedMotion = null) : IPlatformServices
{
    public static PlatformServices Empty { get; } = new();
}
