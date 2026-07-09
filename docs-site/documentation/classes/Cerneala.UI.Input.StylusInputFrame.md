# StylusInputFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/StylusInputBridge.cs`

Represents one batch of stylus input points to dispatch through `StylusInputBridge`.

```csharp
public sealed record StylusInputFrame(IReadOnlyList<StylusInputPoint> Points)
```

Inheritance:
`Object` -> `StylusInputFrame`

## Examples

Create a frame from stylus samples and dispatch it into an existing retained UI root.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

void DispatchStylusSamples(UIRoot root)
{
    StylusInputFrame frame = new(
        new StylusInputPoint(9, 11, 13, StylusInputAction.Down, 0.75f),
        new StylusInputPoint(9, 12, 14, StylusInputAction.Move, 0.7f));

    new StylusInputBridge().Dispatch(root, frame);
}
```

## Remarks

`StylusInputFrame` is an immutable record wrapper around an `IReadOnlyList<StylusInputPoint>`. `StylusInputBridge.Dispatch` enumerates `Points`, hit-tests each point against the target `UIRoot`, and raises the matching preview and bubbling stylus routed events for points that hit an element.

The primary constructor stores the supplied `Points` list. The `params` constructor is a convenience overload for creating a frame from one or more `StylusInputPoint` values.

The type does not copy or validate the supplied list. Pass a non-null collection whose contents should remain stable for the duration of dispatch.

## Constructors

| Name | Description |
| --- | --- |
| `StylusInputFrame(IReadOnlyList<StylusInputPoint>)` | Initializes a frame with the supplied stylus point list. |
| `StylusInputFrame(params StylusInputPoint[])` | Initializes a frame from a variable number of stylus points. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Points` | `IReadOnlyList<StylusInputPoint>` | Gets the stylus points included in the frame. |

## Applies to

- `Cerneala.UI.Input.StylusInputFrame`

## See also

- `Cerneala.UI.Input.StylusInputBridge`
- `Cerneala.UI.Input.StylusInputPoint`
- `Cerneala.UI.Input.StylusInputAction`
- `Cerneala.UI.Input.StylusEventArgs`
