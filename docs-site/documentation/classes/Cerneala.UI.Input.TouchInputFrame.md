# TouchInputFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: [`UI/Input/TouchInputBridge.cs`](../../UI/Input/TouchInputBridge.cs)

Represents a batch of touch input points dispatched together by `TouchInputBridge`.

```csharp
public sealed record TouchInputFrame(IReadOnlyList<TouchInputPoint> Points)
```

Inheritance:
`object` -> `TouchInputFrame`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(100, 100);
TouchInputBridge bridge = new();

TouchInputFrame frame = new(
    new TouchInputPoint(7, 10, 12, TouchInputAction.Down),
    new TouchInputPoint(7, 15, 18, TouchInputAction.Move));

bridge.Dispatch(root, frame);
```

## Remarks

`TouchInputFrame` is a data container for one touch input dispatch pass. `TouchInputBridge.Dispatch` reads the `Points` collection in order and dispatches each `TouchInputPoint` to the current hit target or to the element captured for that touch id.

The frame does not copy or validate the supplied `IReadOnlyList<TouchInputPoint>`. Callers should pass a non-null collection and treat it as immutable while it is being dispatched. The `params` constructor stores the provided touch points as the frame's `Points` collection.

As a record, `TouchInputFrame` uses value-based record equality over its `Points` reference.

## Constructors

| Name | Description |
| --- | --- |
| `TouchInputFrame(IReadOnlyList<TouchInputPoint> Points)` | Initializes a frame with an existing read-only list of touch points. |
| `TouchInputFrame(params TouchInputPoint[] points)` | Initializes a frame from zero or more touch points passed as individual arguments. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<TouchInputPoint>` | Gets the touch points included in this input frame. |

## Applies to

`Cerneala` retained UI input runtime.

## See also

- [`TouchInputBridge`](../../UI/Input/TouchInputBridge.cs)
- [`TouchInputPoint`](../../UI/Input/TouchInputBridge.cs)
- [`TouchInputAction`](../../UI/Input/TouchInputBridge.cs)
- [`TouchEventArgs`](../../UI/Input/TouchInputBridge.cs)
