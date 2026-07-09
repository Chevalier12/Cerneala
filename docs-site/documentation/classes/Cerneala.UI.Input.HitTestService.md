# HitTestService Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/HitTestService.cs`

Provides hit testing for the retained UI tree and returns the foremost input-routable element at a layout coordinate.

```csharp
public sealed class HitTestService
```

Inheritance:
`object` -> `HitTestService`

## Examples

Hit test a `UIRoot` by using its input cache and current route map:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

UIRoot root = new(800, 600);

UIElement surface = new();
surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 100)));
root.VisualChildren.Add(surface);

HitTestResult? result = new HitTestService().HitTest(root, 25, 25);

if (result is not null)
{
    UIElement element = result.Element;
    UiElementId id = result.ElementId;
}
```

Use a filter to reject an element and its descendants while still allowing siblings behind it to be tested:

```csharp
HitTestFilter filter = new(element =>
    ReferenceEquals(element, overlay)
        ? HitTestFilterBehavior.ExcludeSubtree
        : HitTestFilterBehavior.Include);

HitTestResult? result = new HitTestService().HitTest(root, 25, 25, filter);
```

## Remarks

`HitTestService` walks `VisualChildren` from the last child to the first child, so later visual children win when elements overlap.

The `HitTest(UIRoot, float, float, HitTestFilter?)` overload delegates through `UIRoot.InputCache`, which ensures the cached `ElementInputRouteMap` is current before performing the hit test.

The `HitTest(UIElement, ElementInputRouteMap, float, float, HitTestFilter?)` overload uses the supplied route map directly. A matching element must be present in the route map; otherwise the method returns `null` even if the point is inside that element.

Elements are skipped when they are presence-exiting, do not participate in hit testing through visibility state, or are disabled. Disabled elements also cause their subtree to be skipped.

Clipping is honored only when a `ClipNode` is set on an element. A child outside its parent's arranged bounds can still be hit when the parent is not clipped. The root viewport is special: points outside a `UIRoot` viewport are rejected.

Hit testing uses arranged layout bounds. Render transforms do not change the tested bounds.

The returned `HitTestResult` stores the hit `UIElement`, its `UiElementId`, and the original `X` and `Y` coordinates passed to the service.

## Constructors

| Name | Description |
| --- | --- |
| `HitTestService()` | Initializes a new stateless hit-test service instance. |

## Methods

| Name | Description |
| --- | --- |
| `HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)` | Hit tests a `UIRoot` using `root.InputCache` and returns the foremost matching element, or `null` when no routable element is hit. |
| `HitTest(UIElement root, ElementInputRouteMap routeMap, float x, float y, HitTestFilter? filter = null)` | Hit tests an element subtree with an explicit route map and returns the foremost matching mapped element, or `null` when no mapped element is hit. |

## Method Details

### HitTest(UIRoot, float, float, HitTestFilter?)

```csharp
public HitTestResult? HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)
```

Parameters:

| Name | Type | Description |
| --- | --- | --- |
| `root` | `UIRoot` | Root whose input cache supplies the current route map. |
| `x` | `float` | X coordinate in layout space. |
| `y` | `float` | Y coordinate in layout space. |
| `filter` | `HitTestFilter?` | Optional filter. When `null`, all eligible elements are included. |

Returns: `HitTestResult?`

The foremost routable element at the coordinate, or `null` when the point is outside the root viewport or no eligible mapped element is found.

Exceptions:

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `root` is `null`. |

### HitTest(UIElement, ElementInputRouteMap, float, float, HitTestFilter?)

```csharp
public HitTestResult? HitTest(
    UIElement root,
    ElementInputRouteMap routeMap,
    float x,
    float y,
    HitTestFilter? filter = null)
```

Parameters:

| Name | Type | Description |
| --- | --- | --- |
| `root` | `UIElement` | Element subtree to test. |
| `routeMap` | `ElementInputRouteMap` | Map used to translate hit elements into `UiElementId` values. |
| `x` | `float` | X coordinate in layout space. |
| `y` | `float` | Y coordinate in layout space. |
| `filter` | `HitTestFilter?` | Optional filter. When `null`, all eligible elements are included. |

Returns: `HitTestResult?`

The foremost mapped element at the coordinate, or `null` when no eligible mapped element is found.

Exceptions:

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `root` is `null`. |
| `ArgumentNullException` | `routeMap` is `null`. |

## Filtering

| `HitTestFilterBehavior` | Effect |
| --- | --- |
| `Include` | Allows the element and its descendants to participate normally. |
| `Exclude` | Allows descendants to be tested, but excludes the current element as the final hit result. |
| `ExcludeSubtree` | Excludes the current element and its descendants. |

## Applies To

`Cerneala` retained UI input routing.

## See Also

- `Cerneala.UI.Input.HitTestResult`
- `Cerneala.UI.Input.HitTestFilter`
- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Elements.UIRoot`
