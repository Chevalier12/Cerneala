using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal enum KeyboardDispatchKind
{
    Pressed,
    Released
}

internal sealed record KeyboardDispatchResult(
    UIElement Target,
    InputKey Key,
    KeyboardDispatchKind Kind,
    bool Handled);
