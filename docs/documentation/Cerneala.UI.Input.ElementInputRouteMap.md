# ElementInputRouteMap Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ElementInputRouteMap.cs`

Stores the retained input routing map between `UIElement` instances, `UiElementId` values, route order, and the backing `UiInputTree`.

```csharp
public sealed class ElementInputRouteMap
```

Inheritance:
`Object` -> `ElementInputRouteMap`

## Examples

Create a route map manually, then resolve in both directions and query the input route to the root.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

ElementInputRouteMap map = new();

UIElement root = new();
UIElement child = new();
UiElementId rootId = new("root");
UiElementId childId = new("child");

map.Add(root, rootId, parentId: null);
map.Add(child, childId, parentId: rootId);

bool foundChildId = map.TryGetId(child, out UiElementId resolvedChildId);
bool foundChild = map.TryGetElement(childId, out UIElement? resolvedChild);

IReadOnlyList<UiElementId> route = map.InputTree.GetRouteToRoot(childId);
// route is childId, then rootId.
```

## Remarks

`ElementInputRouteMap` is the lookup surface used by retained input code after an input route has been built. It keeps a bidirectional association between `UIElement` objects and `UiElementId` values, preserves the order in which elements were added, and owns the `UiInputTree` used by routed input events.

Element lookup uses reference equality for `UIElement` keys. Two distinct element instances that compare equal through `Equals` are still tracked as distinct elements.

`Add` registers the element in all internal structures and immediately adds the same id to `InputTree`. The supplied `parentId`, when present, must already exist in the `InputTree`; `UiInputTree.Add` throws if a child is registered before its parent. The enabled state stored in the `InputTree` is copied from `element.IsEnabled` at the time of `Add`.

`ElementsInRouteOrder` reflects insertion order. When the map is produced by `ElementInputRouteBuilder`, that order comes from the builder's traversal of attached, input-participating visual elements.

## Constructors

| Name | Description |
| --- | --- |
| `ElementInputRouteMap()` | Initializes an empty route map with an empty `UiInputTree`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of elements registered in the map. |
| `ElementsInRouteOrder` | `IReadOnlyList<UIElement>` | Gets the elements in the order they were added to the map. |
| `InputTree` | `UiInputTree` | Gets the input tree associated with the registered element ids. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(UIElement, UiElementId, UiElementId?)` | `void` | Adds an element/id pair, appends the element to route order, and registers the id with the input tree. Throws if `element` is `null`; duplicate elements or ids are rejected by the underlying dictionaries/input tree. |
| `TryGetElement(UiElementId, out UIElement?)` | `bool` | Attempts to resolve an element from a registered id. |
| `TryGetId(UIElement, out UiElementId)` | `bool` | Attempts to resolve the id for a registered element. Throws if `element` is `null`. |

## Applies to

- `Cerneala.UI.Input.ElementInputRouteMap`

## See also

- `Cerneala.UI.Input.ElementInputCache`
- `Cerneala.UI.Input.ElementInputRouteBuilder`
- `Cerneala.UI.Input.UiInputTree`
