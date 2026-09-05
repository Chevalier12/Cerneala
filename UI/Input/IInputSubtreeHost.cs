using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

internal interface IInputSubtreeHost
{
    IEnumerable<UIElement> GetInputSubtreeChildren();
}
