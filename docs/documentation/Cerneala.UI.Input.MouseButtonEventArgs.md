# MouseButtonEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/MouseButtonEventArgs.cs`

Provides routed mouse button event data, including the changed mouse button and click count.

```csharp
public sealed class MouseButtonEventArgs : MouseEventArgs
```

Inheritance:
`Object` -> `RoutedEventArgs` -> `MouseEventArgs` -> `MouseButtonEventArgs`

## Examples

Handle a routed mouse button event by reading the changed button, coordinates, and click count.

```csharp
using Cerneala.UI.Input;

void OnMouseDown(object? sender, MouseButtonEventArgs args)
{
    if (args.ChangedButton == InputMouseButton.Left && args.ClickCount > 0)
    {
        int x = args.X;
        int y = args.Y;
        args.Handled = true;
    }
}
```

Create mouse button event data directly when raising or testing routed input behavior.

```csharp
object originalSource = new object();

MouseButtonEventArgs args = new(
    InputEvents.MouseDownEvent,
    originalSource,
    InputMouseButton.Left,
    x: 32,
    y: 48,
    clickCount: 1);
```

## Remarks

`MouseButtonEventArgs` extends `MouseEventArgs` with `ChangedButton` and `ClickCount`. The inherited `X` and `Y` properties store the integer pointer coordinates supplied to the constructor.

The retained input bridge creates this event data for mouse down and mouse up routed event pairs. Mouse down events are created with a click count of `1`. Mouse up events receive the value produced by `ClickTracker.Release`: `1` when the press and release resolve to the same click target, or `0` otherwise.

`ClickCount` is stored exactly as supplied to the constructor. The class does not validate, normalize, or recalculate it.

Because `MouseButtonEventArgs` derives from `RoutedEventArgs`, handlers can use inherited routing state such as `RoutedEvent`, `OriginalSource`, `Source`, and `Handled`. The base constructor requires non-null `routedEvent` and `originalSource` values.

## Constructors

| Name | Description |
| --- | --- |
| `MouseButtonEventArgs(RoutedEvent routedEvent, object originalSource, InputMouseButton changedButton, int x, int y, int clickCount)` | Initializes routed mouse button event data for an event, original source, changed button, coordinates, and click count. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ChangedButton` | `InputMouseButton` | Gets the mouse button associated with the event. |
| `ClickCount` | `int` | Gets the click count supplied for the event. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `int` | Gets the mouse X coordinate associated with the event. |
| `Y` | `int` | Gets the mouse Y coordinate associated with the event. |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event represented by this event data. |
| `OriginalSource` | `object` | Gets the original source object supplied when the event data was created. |
| `Source` | `object` | Gets or sets the current routed source. It is initialized to `OriginalSource`. |
| `Handled` | `bool` | Gets or sets whether the routed event has been handled. |

## Related Routed Events

| Event Field | Routing Strategy | Description |
| --- | --- | --- |
| `InputEvents.PreviewMouseDownEvent` | `Tunnel` | Preview event for mouse button down input. |
| `InputEvents.MouseDownEvent` | `Bubble` | Bubble event for mouse button down input. |
| `InputEvents.PreviewMouseUpEvent` | `Tunnel` | Preview event for mouse button up input. |
| `InputEvents.MouseUpEvent` | `Bubble` | Bubble event for mouse button up input. |
| `InputEvents.PreviewMouseLeftButtonDownEvent` | `Direct` | Direct preview event for left mouse button down input. |
| `InputEvents.MouseLeftButtonDownEvent` | `Direct` | Direct event for left mouse button down input. |
| `InputEvents.PreviewMouseLeftButtonUpEvent` | `Direct` | Direct preview event for left mouse button up input. |
| `InputEvents.MouseLeftButtonUpEvent` | `Direct` | Direct event for left mouse button up input. |
| `InputEvents.PreviewMouseRightButtonDownEvent` | `Direct` | Direct preview event for right mouse button down input. |
| `InputEvents.MouseRightButtonDownEvent` | `Direct` | Direct event for right mouse button down input. |
| `InputEvents.PreviewMouseRightButtonUpEvent` | `Direct` | Direct preview event for right mouse button up input. |
| `InputEvents.MouseRightButtonUpEvent` | `Direct` | Direct event for right mouse button up input. |
| `InputEvents.PreviewMouseDoubleClickEvent` | `Direct` | Direct preview event for mouse double-click input. |
| `InputEvents.MouseDoubleClickEvent` | `Direct` | Direct event for mouse double-click input. |

## Applies to

`Cerneala` retained UI input routing.

## See also

- `InputEvents`
- `InputMouseButton`
- `MouseEventArgs`
- `RoutedEventArgs`
