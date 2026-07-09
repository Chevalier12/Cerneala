# HoverTracker Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: [`UI/Input/HoverTracker.cs`](../../UI/Input/HoverTracker.cs)

Tracks the current pointer hover target and synchronizes hover state for the target's visual ancestor path.

```csharp
public sealed class HoverTracker
```

Inheritance:
`object` -> `HoverTracker`

## Examples

Update hover state for a hit-tested element, then clear hover state when the pointer leaves all elements.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);

ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
routeMap.TryGetId(child, out UiElementId childId);

HoverTracker tracker = new();

tracker.Update(new HitTestResult(child, childId, 12.4f, 8.6f), routeMap);
bool isPointerOver = child.IsPointerOver; // true

tracker.Update(null, routeMap, 20, 10);
bool isStillPointerOver = child.IsPointerOver; // false
```

## Remarks

`HoverTracker` is used by `ElementInputBridge` during pointer dispatch to keep `UIElement.IsPointerOver` current for the element under the pointer and its visual ancestors. It compares the previous hovered path with the next path built from `UIElement.VisualParent`.

When the hover target changes, elements that leave the hover path receive `InputEvents.MouseLeaveEvent` and have `IsPointerOver` set to `false`. Elements that enter the hover path receive `InputEvents.MouseEnterEvent` and have `IsPointerOver` set to `true`. Both events are direct routed events raised through the provided `ElementInputRouteMap`.

If the new target is the same `UIElement` instance as `HoveredElement`, `Update` returns `false` and does not reapply state or raise events. A `null` target clears the current hover path. Use the overload with explicit `x` and `y` coordinates when clearing hover after a pointer move, otherwise the two-parameter overload supplies `0, 0` for a `null` target.

Mouse event coordinates are rounded from `float` to `int` before `MouseEventArgs` is raised. If an element is not present in the route map, its `IsPointerOver` state is still updated, but no enter or leave event is raised for that element.

## Constructors

| Name | Description |
| --- | --- |
| `HoverTracker()` | Initializes a new hover tracker with no hovered element. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HoveredElement` | `UIElement?` | Gets the current direct hover target, or `null` when no element is hovered. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Update(HitTestResult? target, ElementInputRouteMap routeMap)` | `bool` | Updates hover state from a hit-test result. Uses the hit-test coordinates when `target` is not `null`; otherwise uses `0, 0`. |
| `Update(HitTestResult? target, ElementInputRouteMap routeMap, float x, float y)` | `bool` | Updates hover state from a hit-test result and explicit pointer coordinates. Returns `true` when the hovered element changes; otherwise `false`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Update(...)` | `ArgumentNullException` | `routeMap` is `null`. |

## Applies To

Cerneala retained UI input routing.

## See Also

- [`ElementInputBridge`](../../UI/Input/ElementInputBridge.cs)
- [`HitTestResult`](../../UI/Input/HitTestResult.cs)
- [`ElementInputRouteMap`](../../UI/Input/ElementInputRouteMap.cs)
- [`InputEvents`](../../UI/Input/InputEvents.cs)
