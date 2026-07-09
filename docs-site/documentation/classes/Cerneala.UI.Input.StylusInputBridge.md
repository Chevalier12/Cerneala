# StylusInputBridge Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/StylusInputBridge.cs`

Dispatches stylus input frames into the retained UI routed event system.

```csharp
public sealed class StylusInputBridge
```

Inheritance:
`Object` -> `StylusInputBridge`

## Examples

Dispatch stylus input points collected by a host into a retained UI root.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

StylusInputBridge bridge = new();

void ProcessStylus(UIRoot root, IReadOnlyList<StylusInputPoint> points)
{
    bridge.Dispatch(root, new StylusInputFrame(points));
}
```

## Remarks

`StylusInputBridge` uses the root input cache to obtain a current `ElementInputRouteMap`, hit-tests each stylus point, and raises the preview and bubbling stylus event pair for each hit target.

Points that do not hit an element are ignored.

The bridge maps `StylusInputAction` values to the matching routed events in `InputEvents`. Unsupported actions throw from the internal dispatch path.

The constructor accepts an optional `HitTestService`; when no service is supplied, a default one is created.

## Constructors

| Name | Description |
| --- | --- |
| `StylusInputBridge(HitTestService?)` | Initializes a stylus input bridge with an optional hit test service. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispatch(UIRoot, StylusInputFrame)` | `void` | Dispatches all stylus points in a frame into the retained UI root. Throws if `root` or `frame` is `null`. |

## Applies to

- `Cerneala.UI.Input.StylusInputBridge`

## See also

- `Cerneala.UI.Input.StylusInputFrame`
- `Cerneala.UI.Input.StylusInputPoint`
- `Cerneala.UI.Input.StylusEventArgs`
- `Cerneala.UI.Input.InputEvents`
