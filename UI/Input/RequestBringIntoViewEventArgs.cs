using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Input;

// As WPF's RequestBringIntoViewEventArgs: the element that asked to be brought into view, and the part of it.
public sealed class RequestBringIntoViewEventArgs : RoutedEventArgs
{
    public RequestBringIntoViewEventArgs(RoutedEvent routedEvent, UIElement targetObject, LayoutRect targetRect)
        : base(routedEvent, targetObject)
    {
        TargetObject = targetObject;
        TargetRect = targetRect;
    }

    public UIElement TargetObject { get; }

    // The part of TargetObject to bring into view, in TargetObject's coordinates, where 0, 0 is its top left corner.
    // LayoutRect.Empty means the whole element.
    public LayoutRect TargetRect { get; }
}
