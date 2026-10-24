# RoutedEventRouter Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RoutedEventRouter.cs`

Routes `RoutedEventArgs` through a `UiInputTree` according to the event's `RoutingStrategy`.

```csharp
public static class RoutedEventRouter
```

Inheritance:
`object` -> `RoutedEventRouter`

## Examples

Raise a bubbling event from a target element to its ancestors.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

UiInputTree tree = new();
Border root = new();
Border panel = new();
Border button = new();
List<string> calls = new();

tree.Add(root, parent: null);
tree.Add(panel, parent: root);
tree.Add(button, parent: panel);

tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));
tree.AddHandler(panel, InputEvents.MouseDownEvent, (_, _) => calls.Add("panel"));
tree.AddHandler(button, InputEvents.MouseDownEvent, (_, _) => calls.Add("button"));

MouseButtonEventArgs args = new(
    InputEvents.MouseDownEvent,
    button,
    InputMouseButton.Left,
    x: 0,
    y: 0,
    clickCount: 1);

RoutedEventRouter.Raise(tree, button, args);

// calls is ["button", "panel", "root"].
```

Raise a preview/bubble pair. When a preview handler marks the event as handled, the bubble event is still raised, but with `Handled` already `true`, so only bubble handlers registered with `handledEventsToo: true` run.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

UiInputTree tree = new();
Border root = new();
Border button = new();

tree.Add(root, parent: null);
tree.Add(button, parent: root);
tree.AddHandler(root, InputEvents.PreviewMouseDownEvent, (_, args) => args.Handled = true);

MouseButtonEventArgs previewArgs = new(
    InputEvents.PreviewMouseDownEvent,
    button,
    InputMouseButton.Left,
    x: 0,
    y: 0,
    clickCount: 1);

MouseButtonEventArgs bubbleArgs = new(
    InputEvents.MouseDownEvent,
    button,
    InputMouseButton.Left,
    x: 0,
    y: 0,
    clickCount: 1);

RoutedEventRouter.RaisePair(tree, button, previewArgs, bubbleArgs);
// bubbleArgs.Handled is true: the bubble route was walked, but no ordinary handler ran.
```

## Remarks

`RoutedEventRouter` is the retained input routing dispatcher. It does not store handlers itself; it reads the route and handler lists from the supplied `UiInputTree`.

`Raise` first checks that `args` is an instance of `args.RoutedEvent.ArgsType`, the same check as `UIElement.RaiseEvent`. A wrong args object fails here with `ArgumentException`, before any handler runs. This is what lets a `RoutedEventHandler<TArgs>` receive its `TArgs` without a failing cast inside the handler.

`Raise` builds a route from `args.RoutedEvent.RoutingStrategy`:

| Strategy | Route |
| --- | --- |
| `Direct` | The target element only. |
| `Bubble` | The target element followed by its ancestors up to the root. |
| `Tunnel` | The root followed by descendants down to the target element. |

Before invoking handlers for each element, `Raise` sets `args.Source` to the current `UIElement` and passes the same element to the handler as `sender`. `args.OriginalSource` is not changed by the router. When `args.Handled` is `true`, including after an individual handler sets it, the router skips handlers registered without `handledEventsToo`. Routing does not stop: it still visits every remaining route element, updates `Source`, and runs handlers registered with `handledEventsToo: true`.

The `UiInputTree` decides which handlers the router sees. Disabled elements return no handlers, but the route continues past them. The handler list of an element is copied when routing reaches that element. A handler added during dispatch to the element being visited does not run in that same dispatch; a handler added to an element that routing has not reached yet does run.

`RaisePair` raises the preview arguments first. It then copies a handled preview into the bubble arguments (`bubbleArgs.Handled |= previewArgs.Handled`) and always raises the bubble arguments. A handled preview therefore skips only the ordinary bubble handlers.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Raise(UiInputTree tree, UIElement target, RoutedEventArgs args)` | `void` | Raises a single routed event through the supplied input tree using the routing strategy stored on `args.RoutedEvent`. |
| `RaisePair(UiInputTree tree, UIElement target, RoutedEventArgs previewArgs, RoutedEventArgs bubbleArgs)` | `void` | Raises a preview event, then always raises the paired bubble event, marked handled when the preview was handled. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Raise(UiInputTree, UIElement, RoutedEventArgs)` | `ArgumentNullException` | `tree`, `target`, or `args` is `null`. |
| `Raise(UiInputTree, UIElement, RoutedEventArgs)` | `ArgumentException` | `args` is not an instance of `args.RoutedEvent.ArgsType` (`ParamName` is `args`). The check runs before any handler runs. For example, `MouseDownEvent` requires `MouseButtonEventArgs`, so a plain `new RoutedEventArgs(UIElement.MouseDownEvent, button)` throws. |
| `Raise(UiInputTree, UIElement, RoutedEventArgs)` | `ArgumentException` | `target` is not registered in the supplied `UiInputTree`. |
| `Raise(UiInputTree, UIElement, RoutedEventArgs)` | `InvalidOperationException` | `args.RoutedEvent.RoutingStrategy` is not a supported routing strategy value. |
| `RaisePair(UiInputTree, UIElement, RoutedEventArgs, RoutedEventArgs)` | `ArgumentNullException` | `previewArgs`, `bubbleArgs`, or the delegated `tree` or `target` argument is `null`. |
| `RaisePair(UiInputTree, UIElement, RoutedEventArgs, RoutedEventArgs)` | `ArgumentException` | `target` is not registered, or `previewArgs` or `bubbleArgs` is not an instance of its event's `ArgsType`, when the delegated raise is performed. A wrong `bubbleArgs` is found only after the preview raise has already run its handlers. |
| `RaisePair(UiInputTree, UIElement, RoutedEventArgs, RoutedEventArgs)` | `InvalidOperationException` | A delegated routed event has an unsupported routing strategy value. |

## Applies to

Cerneala retained UI input routing, including mouse, keyboard, focus, stylus, touch, drag/drop, and command-related routed events.

## See also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.RoutingStrategy`
- `Cerneala.UI.Input.UiInputTree`
- `Cerneala.UI.Input.InputEvents`
