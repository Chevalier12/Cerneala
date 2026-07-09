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
using Cerneala.UI.Input;

UiInputTree tree = new();
UiElementId root = new("root");
UiElementId panel = new("panel");
UiElementId button = new("button");
List<string> calls = [];

tree.Add(root, parentId: null);
tree.Add(panel, parentId: root);
tree.Add(button, parentId: panel);

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

`UiInputTree` is a lightweight routing data structure. Elements are registered by `UiElementId`, optionally with a parent id, and the parent must already exist before a child is added. The root element uses `null` as its parent id.

The tree itself returns routes from a target element to the root. Routing direction is applied by `RoutedEventRouter`: bubble events use the route as returned, tunnel events reverse it, and direct events use only the target.

Handlers are registered per `(UiElementId, RoutedEvent)` pair. When dispatch asks for handlers on a disabled element, the tree returns no handlers for that element. Disabled elements are still part of the route, so their enabled ancestors and descendants can still receive the event when the router reaches them.

Handlers returned for dispatch are copied to an array. A handler added while an event is already being dispatched does not run during that same dispatch; it can run on a later dispatch.

## Constructors

| Name | Description |
| --- | --- |
| `UiInputTree()` | Initializes an empty input tree. |

## Methods

| Name | Description |
| --- | --- |
| `Add(UiElementId id, UiElementId? parentId, bool isEnabled = true)` | Registers an input element with an optional parent and enabled state. Throws `ArgumentException` when the parent id is not registered, and `InvalidOperationException` when the element id is already registered. |
| `AddHandler(UiElementId id, RoutedEvent routedEvent, RoutedEventHandler handler)` | Registers a handler for an element and routed event. Throws `ArgumentNullException` for a null event or handler, and `ArgumentException` when the element id is not registered. |
| `GetRouteToRoot(UiElementId targetId)` | Returns the registered route from `targetId` up through its ancestors. Throws `ArgumentException` when the target id is not registered. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Input.RoutedEventRouter`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Input.UiElementId`
- `Cerneala.UI.Input.ElementInputRouteMap`
