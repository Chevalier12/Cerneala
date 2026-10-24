# ElementInputRouteMap Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ElementInputRouteMap.cs`

Stores the retained set of input-routable `UIElement` instances, their route order, and the backing `UiInputTree`.

```csharp
public sealed class ElementInputRouteMap
```

Inheritance:
`Object` -> `ElementInputRouteMap`

## Examples

Create a route map manually, check that an element is registered, and query the input route to the root.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

ElementInputRouteMap map = new();

UIElement root = new();
UIElement child = new();

map.Add(root, parent: null);
map.Add(child, parent: root);

bool childIsRegistered = map.Contains(child); // true

IReadOnlyList<UIElement> route = map.InputTree.GetRouteToRoot(child);
// route is child, then root.
```

## Remarks

`ElementInputRouteMap` is the lookup surface used by retained input code after an input route has been built. It records which `UIElement` objects are registered, preserves the order in which elements were added, and owns the `UiInputTree` used by routed input events.

Element lookup uses reference equality for `UIElement` keys. Two distinct element instances that compare equal through `Equals` are still tracked as distinct elements.

Scene-node lookup also checks the live preparation state of its owning [RenderSurface2D](Cerneala.UI.Controls.RenderSurface2D.md). `Contains` returns `false` while that scene's required visible coverage is unavailable, even when the node is still registered in this snapshot. This prevents existing capture/focus targets from bypassing scene readiness after asynchronous source publication. It does not remove entries from `Count`, `ElementsInRouteOrder`, or the backing `InputTree`, and it does not disable the node or its simulation. Use the normal retained input dispatch services rather than manually raising input through a stale backing tree.

`Add` registers the element in all internal structures and immediately adds the same element to `InputTree`. The supplied `parent`, when present, must already exist in the `InputTree`; `UiInputTree.Add` throws if a child is registered before its parent. The enabled state stored in the `InputTree` is copied from `element.IsEnabled` at the time of `Add`.

For a map built by `ElementInputRouteBuilder`, the `InputTree` route of content projected by an `Overlay` continues from the projected presenter to the overlay and its ancestors. Retained input services that ask where the pointer or the focus is visually (pointer-over and focus-within state, cursor resolution, and the focus, pressed, click, and drag owner of a pointer press) use the element's visual ancestors instead, which stop at the root-owned overlay layer. A map filled through the public `Add` uses the same parent for both.

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
| `InputTree` | `UiInputTree` | Gets the input tree associated with the registered elements. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(UIElement, UIElement?)` | `void` | Registers an element with its parent, appends the element to route order, and registers it with the input tree. Throws `ArgumentNullException` if `element` is `null` and `ArgumentException` if the element is already registered. |
| `Contains(UIElement)` | `bool` | Returns whether the element is registered in this map and its input route is currently available. Throws if `element` is `null`. |

## Applies to

- `Cerneala.UI.Input.ElementInputRouteMap`

## See also

- `Cerneala.UI.Input.ElementInputCache`
- `Cerneala.UI.Input.ElementInputRouteBuilder`
- `Cerneala.UI.Input.UiInputTree`
