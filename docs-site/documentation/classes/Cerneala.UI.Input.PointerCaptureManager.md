# PointerCaptureManager Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/PointerCaptureManager.cs`

Tracks pointer capture for retained UI input and redirects pointer targets to the captured element.

```csharp
public sealed class PointerCaptureManager
```

Inheritance:
`Object` -> `PointerCaptureManager`

## Examples

Capture the pointer for an element during a drag operation and release it when the drag completes.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

void BeginDrag(UIElement element, ElementInputRouteMap routeMap, PointerCaptureManager capture)
{
    capture.Capture(element, routeMap);
}

void EndDrag(ElementInputRouteMap routeMap, PointerCaptureManager capture)
{
    capture.Release(routeMap);
}
```

## Remarks

`PointerCaptureManager` stores the current captured `UIElement`. When capture is active, `OverrideTarget` replaces the hit-test target with a `HitTestResult` for the captured element and the supplied pointer coordinates.

`Capture` raises `LostMouseCaptureEvent` for the previous captured element and `GotMouseCaptureEvent` for the new captured element when the captured element changes. Calling `Capture` with the current captured element does nothing.

`Release` clears capture and raises `LostMouseCaptureEvent` for the old captured element. Releasing when no element is captured does nothing.

If the captured element is no longer present in the supplied route map, `OverrideTarget` clears capture, raises `LostMouseCaptureEvent` for the old captured element, and returns the original hit target. The element leaves the route map when it is detached, hidden, collapsed or disabled, itself or through an ancestor. `ElementInputBridge.Dispatch` calls `OverrideTarget` at the start of every dispatched input frame, also one in which the pointer does not move, so a dragging `Thumb` that becomes hidden ends its drag with `DragCompleted` and `Canceled = true` in the next dispatched frame. When a `LostMouseCaptureEvent` handler captures another element that the route map contains, `OverrideTarget` returns that element instead of the hit target, as for any captured element.

The old captured element always gets `LostMouseCaptureEvent`. When it is in the supplied route map, the event bubbles along its input route. When it is not, the event is raised with `UIElement.RaiseEvent`. For an attached element, that bubbles the event along the element's ancestors, also when the element or an ancestor is disabled, hidden or collapsed, as WPF does. For example, a dragging `Thumb` that becomes disabled gets `LostMouseCaptureEvent`, ends its drag, and its `Thumb.DragCompletedEvent` reaches the `Slider` around it, which closes its AutoToolTip. Only a detached element runs just its own handlers.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CapturedElement` | `UIElement?` | Gets the element that currently owns pointer capture, or `null` when capture is inactive. |
| `HasCapture` | `bool` | Gets whether pointer capture is currently active. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Capture(UIElement, ElementInputRouteMap)` | `void` | Captures pointer input for an element and raises capture change events. Throws if `element` or `routeMap` is `null`. |
| `OverrideTarget(HitTestResult?, ElementInputRouteMap, float, float)` | `HitTestResult?` | Returns the captured element as the pointer target when capture is active and the route map contains it; otherwise releases a capture the route map does not contain and returns the supplied hit target. Throws if `routeMap` is `null`. |
| `Release(ElementInputRouteMap)` | `void` | Releases pointer capture and raises a lost-capture event when an element was captured. Throws if `routeMap` is `null`. |

## Applies to

- `Cerneala.UI.Input.PointerCaptureManager`

## See also

- `Cerneala.UI.Input.InputEvents`
- `Cerneala.UI.Input.HitTestResult`
- `Cerneala.UI.Input.ElementInputRouteMap`
