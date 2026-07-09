# InputFrame.PointerFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputFrame.cs`

Represents pointer state changes between the previous and current pointer snapshots in an `InputFrame`.

```csharp
public sealed class InputFrame.PointerFrame
```

Inheritance:
`Object` -> `InputFrame.PointerFrame`

## Examples

Use the pointer frame to detect transitions during input dispatch.

```csharp
using Cerneala.UI.Input;

InputFrame frame = CaptureFrame();

if (frame.Pointer.IsPressed(InputMouseButton.Left))
{
    BeginSelection(frame.Pointer.X, frame.Pointer.Y);
}

if (frame.Pointer.WheelDelta != 0)
{
    ZoomBy(frame.Pointer.WheelDelta);
}
```

## Remarks

`InputFrame.PointerFrame` compares a previous `PointerSnapshot` with a current `PointerSnapshot`. Its coordinate and wheel properties expose current pointer values, while transition methods compare current and previous button state.

The constructor is internal. Instances are created by `InputFrame`.

`WheelDelta` is computed as current wheel value minus previous wheel value.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `WheelDelta` | `int` | Gets the wheel movement between the previous and current pointer snapshots. |
| `WheelValue` | `int` | Gets the current accumulated wheel value. |
| `X` | `float` | Gets the current pointer X coordinate. |
| `Y` | `float` | Gets the current pointer Y coordinate. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsDown(InputMouseButton)` | `bool` | Returns whether the button is down in the current snapshot. |
| `IsPressed(InputMouseButton)` | `bool` | Returns whether the button is down in the current snapshot and was up in the previous snapshot. |
| `IsReleased(InputMouseButton)` | `bool` | Returns whether the button is up in the current snapshot and was down in the previous snapshot. |

## Applies to

- `Cerneala.UI.Input.InputFrame.PointerFrame`

## See also

- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Input.PointerSnapshot`
- `Cerneala.UI.Input.InputMouseButton`
