# RoutingStrategy Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutingStrategy.cs`

Specifies which elements a routed event visits when it is raised.

```csharp
public enum RoutingStrategy
```

## Examples

Register one event per strategy and raise each from the child element:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(200, 200);
UIElement parent = new();
UIElement child = new();
root.VisualChildren.Add(parent);
parent.VisualChildren.Add(child);
root.ProcessFrame();

RoutedEvent ping = RoutedEventRegistry.Register("Ping", typeof(UIElement), RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
RoutedEvent pong = RoutedEventRegistry.Register("Pong", typeof(UIElement), RoutingStrategy.Bubble, typeof(RoutedEventArgs));
RoutedEvent poke = RoutedEventRegistry.Register("Poke", typeof(UIElement), RoutingStrategy.Direct, typeof(RoutedEventArgs));

List<string> calls = [];
foreach (RoutedEvent routedEvent in new[] { ping, pong, poke })
{
    parent.AddHandler(routedEvent, (_, _) => calls.Add(routedEvent.Name + ":parent"));
    child.AddHandler(routedEvent, (_, _) => calls.Add(routedEvent.Name + ":child"));
}

child.RaiseEvent(new RoutedEventArgs(ping, child));
child.RaiseEvent(new RoutedEventArgs(pong, child));
child.RaiseEvent(new RoutedEventArgs(poke, child));

Console.WriteLine(string.Join(", ", calls));
// Ping:parent, Ping:child, Pong:child, Pong:parent, Poke:child
```

## Remarks

The strategy is fixed when the event is created with `RoutedEventRegistry.Register` and is read back through `RoutedEvent.RoutingStrategy`. `Register` throws `ArgumentOutOfRangeException` for a value that is not one of the three members.

`RoutedEventRouter.Raise` builds the route from the target element up to the root of the input tree:

| Strategy | Elements visited, in order |
| --- | --- |
| `Direct` | Only the target element. |
| `Bubble` | The target element, then each ancestor up to the root. |
| `Tunnel` | The root, then each descendant down to the target element. |

`UIElement.RaiseEvent` sends a `Direct` event straight to the element's own handlers. It does the same for any strategy when the element has no root or no element id yet.

Setting `RoutedEventArgs.Handled` to `true` does not shorten the route. The remaining elements are still visited, and only handlers registered with `handledEventsToo: true` run on them.

Most built-in events follow a naming pattern. `Preview...` events, such as `InputEvents.PreviewMouseDownEvent` and `CommandEvents.PreviewExecutedEvent`, use `Tunnel`. Their paired events, such as `InputEvents.MouseDownEvent`, use `Bubble`. `UIElement.LoadedEvent`, `UnloadedEvent`, and `SizeChangedEvent` use `Direct`. So do the mouse, stylus, and touch enter and leave events, the left and right mouse button events (for example `MouseLeftButtonDownEvent`), and the double-click events. For these, the `Preview...` versions are `Direct` too, for example `PreviewMouseLeftButtonDownEvent`. The input bridge raises the button-specific and double-click pairs once on each element of the route, so ancestors still receive them even though each single raise is `Direct`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Direct` | `0` | Raises the event only on the target element. |
| `Bubble` | `1` | Raises the event on the target element, then on each ancestor up to the root. |
| `Tunnel` | `2` | Raises the event on the root first, then on each descendant down to the target element. |

## Applies to

Cerneala routed events registered with `RoutedEventRegistry`.

## See also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventRegistry`
- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Input.RoutedEventArgs`
