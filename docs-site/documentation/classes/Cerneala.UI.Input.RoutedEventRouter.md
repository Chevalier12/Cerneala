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
using Cerneala.UI.Input;

UiInputTree tree = new();
UiElementId root = new("root");
UiElementId panel = new("panel");
UiElementId button = new("button");
List<string> calls = new();

tree.Add(root, parentId: null);
tree.Add(panel, parentId: root);
tree.Add(button, parentId: panel);

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

Raise a preview/bubble pair where the bubble event is skipped when preview handling marks the event as handled.

```csharp
using Cerneala.UI.Input;

UiInputTree tree = new();
UiElementId root = new("root");
UiElementId button = new("button");

tree.Add(root, parentId: null);
tree.Add(button, parentId: root);
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
```

## Remarks

`RoutedEventRouter` is the retained input routing dispatcher. It does not store handlers itself; it reads the route and handler lists from the supplied `UiInputTree`.

`Raise` builds a route from `args.RoutedEvent.RoutingStrategy`:

| Strategy | Route |
| --- | --- |
| `Direct` | The target element only. |
| `Bubble` | The target element followed by its ancestors up to the root. |
| `Tunnel` | The root followed by descendants down to the target element. |

Before invoking handlers for each element, `Raise` sets `args.Source` to the current `UiElementId`. `args.OriginalSource` is not changed by the router. Routing stops immediately when `args.Handled` is `true`, including after an individual handler sets it.

`UiInputTree.GetHandlers` controls which handlers are visible to the router. In the current implementation, disabled elements return no handlers, but the route can continue past them. Handler lookup returns a snapshot array, so handlers added during a dispatch do not run until a later dispatch.

`RaisePair` raises the preview arguments first. It raises the bubble arguments only when `previewArgs.Handled` is still `false` after preview routing completes.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Raise(UiInputTree tree, UiElementId targetId, RoutedEventArgs args)` | `void` | Raises a single routed event through the supplied input tree using the routing strategy stored on `args.RoutedEvent`. |
| `RaisePair(UiInputTree tree, UiElementId targetId, RoutedEventArgs previewArgs, RoutedEventArgs bubbleArgs)` | `void` | Raises a preview event, then raises the paired bubble event only if the preview event was not handled. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Raise(UiInputTree, UiElementId, RoutedEventArgs)` | `ArgumentNullException` | `tree` or `args` is `null`. |
| `Raise(UiInputTree, UiElementId, RoutedEventArgs)` | `ArgumentException` | `targetId` is not registered in the supplied `UiInputTree`. |
| `Raise(UiInputTree, UiElementId, RoutedEventArgs)` | `InvalidOperationException` | `args.RoutedEvent.RoutingStrategy` is not a supported routing strategy value. |
| `RaisePair(UiInputTree, UiElementId, RoutedEventArgs, RoutedEventArgs)` | `ArgumentNullException` | `previewArgs`, `bubbleArgs`, or the delegated `tree` argument is `null`. |
| `RaisePair(UiInputTree, UiElementId, RoutedEventArgs, RoutedEventArgs)` | `ArgumentException` | `targetId` is not registered when either delegated raise is performed. |
| `RaisePair(UiInputTree, UiElementId, RoutedEventArgs, RoutedEventArgs)` | `InvalidOperationException` | A delegated routed event has an unsupported routing strategy value. |

## Applies to

Cerneala retained UI input routing, including mouse, keyboard, focus, stylus, touch, drag/drop, and command-related routed events.

## See also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.RoutingStrategy`
- `Cerneala.UI.Input.UiInputTree`
- `Cerneala.UI.Input.InputEvents`
