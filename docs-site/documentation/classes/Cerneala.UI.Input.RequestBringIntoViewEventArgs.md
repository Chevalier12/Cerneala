# RequestBringIntoViewEventArgs Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RequestBringIntoViewEventArgs.cs`

Routed event arguments for `UIElement.RequestBringIntoViewEvent`: the element that asked to be brought into view, and the part of it.

```csharp
public sealed class RequestBringIntoViewEventArgs : RoutedEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs` -> `RequestBringIntoViewEventArgs`

## Examples
A handler on a panel inside a `ScrollViewer` reads the request and stops it, so the `ScrollViewer` does not scroll:

```csharp
Button button = new() { Height = 20 };
StackPanel stack = new();
stack.VisualChildren.Add(new Border { Height = 300 });
stack.VisualChildren.Add(button);
ScrollViewer viewer = new() { Content = stack, Height = 100 };

stack.RequestBringIntoView += (_, args) =>
{
    // args.TargetObject is button; args.TargetRect is (0, 0, 10, 5).
    args.Handled = true;
};

button.BringIntoView(new LayoutRect(0, 0, 10, 5));
// viewer.Presenter.VerticalOffset stays 0.
```

## Remarks
As in WPF, `UIElement.BringIntoView` raises `RequestBringIntoViewEvent` from the element, and the event bubbles. Each `ScrollViewer` whose content holds `TargetObject` handles it: its `ScrollContentPresenter.MakeVisible` scrolls, and the `ScrollViewer` raises a new request for its own visible part, so the `ScrollViewer`s around it scroll too.

`TargetRect` is in the coordinates of `TargetObject`, where `0, 0` is its top left corner. `LayoutRect.Empty` means the whole element; `BringIntoView()` without an argument sends `LayoutRect.Empty`.

A handler that sets `Handled` to `true` before the event reaches a `ScrollViewer` stops the scroll, because the `ScrollViewer` handler does not run for handled events.

The event routes through the element input route, like the other bubbling events of Cerneala. An element that is not in that route, such as a disabled element, raises it only to its own handlers.

## Constructors
| Name | Description |
| --- | --- |
| `RequestBringIntoViewEventArgs(RoutedEvent, UIElement, LayoutRect)` | Creates the args for the routed event, the target element, and the target rectangle. The target is also `OriginalSource`. |

## Properties
| Name | Description |
| --- | --- |
| `TargetObject` | The `UIElement` that asked to be brought into view. |
| `TargetRect` | The part of `TargetObject` to bring into view, in its own coordinates; `LayoutRect.Empty` for the whole element. |

## Applies to
`UIElement.RequestBringIntoView`, `ScrollViewer`, `ScrollContentPresenter.MakeVisible`.

## See also
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Controls.ScrollViewer`
- `Cerneala.UI.Controls.ScrollContentPresenter`
