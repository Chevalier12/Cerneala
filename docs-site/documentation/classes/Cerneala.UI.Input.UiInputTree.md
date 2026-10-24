# UiInputTree Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/UiInputTree.cs`

Stores an input element hierarchy and routed event handlers for retained input dispatch.

```csharp
public sealed class UiInputTree
```

Inheritance:
`object` -> `UiInputTree`

## Examples

The following example builds a root-to-button hierarchy and raises a bubbling mouse event. `RoutedEventRouter` asks the tree for the route and invokes handlers from the target toward the root.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

UiInputTree tree = new();
Border root = new();
Border panel = new();
Border button = new();
List<string> calls = [];

tree.Add(root, parent: null);
tree.Add(panel, parent: root);
tree.Add(button, parent: panel);

tree.AddHandler(root, InputEvents.MouseDownEvent, (_, _) => calls.Add("root"));
tree.AddHandler(panel, InputEvents.MouseDownEvent, (_, _) => calls.Add("panel"));
tree.AddHandler(button, InputEvents.MouseDownEvent, (_, _) => calls.Add("button"));

MouseButtonEventArgs args = new(
    InputEvents.MouseDownEvent,
    originalSource: button,
    InputMouseButton.Left,
    x: 0,
    y: 0,
    clickCount: 1);

RoutedEventRouter.Raise(tree, button, args);

// calls: "button", "panel", "root"
```

## Remarks

`UiInputTree` is a lightweight routing data structure. Elements are registered by `UIElement` (compared by reference), optionally with a parent element, and the parent must already be registered before a child is added. The root element uses `null` as its parent.

The tree itself returns routes from a target element to the root. Routing direction is applied by `RoutedEventRouter`: bubble events use the route as returned, tunnel events reverse it, and direct events use only the target.

Handlers are registered per element and `RoutedEvent`. A `RoutedEventHandler` receives the element whose handler runs as `sender`. When dispatch asks for handlers on a disabled element, the tree returns no handlers for that element. Disabled elements are still part of the route, so their enabled ancestors and descendants can still receive the event when the router reaches them.

The router asks the tree for an element's handlers when routing reaches that element, and gets a copy of the list at that moment. A handler added during dispatch to the element being visited does not run in that same dispatch. A handler added to an element that routing has not reached yet does run in that same dispatch. For example, during a bubble event, a child handler that adds a handler to its parent makes the new parent handler run right after the child.

## Constructors

| Name | Description |
| --- | --- |
| `UiInputTree()` | Initializes an empty input tree. |

## Methods

| Name | Description |
| --- | --- |
| `Add(UIElement element, UIElement? parent, bool isEnabled = true)` | Registers an input element with an optional parent and enabled state. Throws `ArgumentNullException` when `element` is `null`, `ArgumentException` when the parent is not registered, and `InvalidOperationException` when the element is already registered. |
| `AddHandler(UIElement element, RoutedEvent routedEvent, RoutedEventHandler handler, bool handledEventsToo = false)` | Registers a handler for an element and routed event. With `handledEventsToo` set to `true`, the handler also runs after an earlier handler set `Handled` to `true`. Throws `ArgumentNullException` for a null element, event, or handler, and `ArgumentException` when the element is not registered. |
| `GetRouteToRoot(UIElement target)` | Returns the registered route from `target` up through its ancestors. Throws `ArgumentNullException` when `target` is `null` and `ArgumentException` when the target is not registered. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.ElementInputRouteMap`
