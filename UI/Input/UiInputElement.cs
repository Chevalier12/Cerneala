using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed record UiInputElement(UIElement Element, UIElement? Parent, bool IsEnabled);
