# TouchInputBridge Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/TouchInputBridge.cs`

Dispatches touch input frames into the retained UI routed event system and manages per-touch capture.

```csharp
public sealed class TouchInputBridge
```

Inheritance:
`Object` -> `TouchInputBridge`

## Examples

Dispatch touch points collected by a host into a retained UI root.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

TouchInputBridge bridge = new();

void ProcessTouch(UIRoot root, IReadOnlyList<TouchInputPoint> points)
{
    bridge.Dispatch(root, new TouchInputFrame(points));
}
```

## Remarks

`TouchInputBridge` hit-tests each touch point and raises the preview and bubbling touch event pair for the resolved target.

Capture is tracked per touch id. When a touch id is captured, later points with the same id are routed to the captured element when that element is still present in the current route map. If the captured element is no longer routable, capture for that touch id is cleared and the hit-test target is used.

`Capture` and `Release` raise touch capture change events when capture changes. Calling `Capture` for the same touch id and same element does nothing.

The constructor accepts an optional `HitTestService`; when no service is supplied, a default one is created.

## Constructors

| Name | Description |
| --- | --- |
| `TouchInputBridge(HitTestService?)` | Initializes a touch input bridge with an optional hit test service. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Capture(int, UIElement, ElementInputRouteMap)` | `void` | Captures a touch id for an element and raises capture change events. Throws if `element` or `routeMap` is `null`. |
| `Dispatch(UIRoot, TouchInputFrame)` | `void` | Dispatches all touch points in a frame into the retained UI root. Throws if `root` or `frame` is `null`. |
| `Release(int, ElementInputRouteMap)` | `void` | Releases capture for a touch id and raises a lost-capture event when capture existed. Throws if `routeMap` is `null`. |

## Applies to

- `Cerneala.UI.Input.TouchInputBridge`

## See also

- `Cerneala.UI.Input.TouchInputFrame`
- `Cerneala.UI.Input.TouchInputPoint`
- `Cerneala.UI.Input.TouchEventArgs`
- `Cerneala.UI.Input.InputEvents`
