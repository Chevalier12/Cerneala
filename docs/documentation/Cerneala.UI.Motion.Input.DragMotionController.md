# DragMotionController Class

## Definition
Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/DragMotionController.cs`

Coordinates pointer drag movement with graph-bound motion values that drive a `UIElement`'s translation.

```csharp
public sealed class DragMotionController
```

Inheritance:
`object` -> `DragMotionController`

## Examples

Create a drag controller for an attached element, move it, and settle it with a tween:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Input;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new(100, 100);
UIElement element = new();
root.VisualChildren.Add(element);
root.ProcessFrame();

DragMotionController drag = element.Motion().Drag();

TimeSpan start = TimeSpan.Zero;
drag.Begin(0, 0, start);
drag.Move(24, 12, start + TimeSpan.FromMilliseconds(16));
drag.End(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));

float translatedX = element.TranslateX;
float translatedY = element.TranslateY;
```

## Remarks

`DragMotionController` is created through `MotionElementFacade.Drag()`. The target element must already be attached to a `UIRoot`; construction reads the root motion system and throws if the element has no root.

The controller creates two `MotionValue<float>` instances from the element's current `TranslateX` and `TranslateY` values. Each motion value subscribes back to the element and writes translation changes with `UiPropertyValueSource.Animation`, so drag movement updates render translation without requiring layout invalidation.

`Begin` enters `PointerMotionState.Dragging`, records the starting translation, stores the pointer-to-translation origin, and resets velocity tracking. `Move` adds a velocity sample and immediately jumps `DragX` and `DragY` to the pointer offset from that origin.

`End` enters `PointerMotionState.Settling` and animates both axes toward the current position plus ten percent of the captured velocity. `PointerCaptureLost` only acts while dragging; it animates both axes back to the translation recorded by `Begin`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `PointerMotionState` | Gets the current pointer motion state. The controller sets this to `Dragging` from `Begin` and `Settling` from `End` or handled pointer-capture loss. |
| `DragX` | `MotionValue<float>` | Gets the graph-bound horizontal translation value that writes to `UIElement.TranslateXProperty`. |
| `DragY` | `MotionValue<float>` | Gets the graph-bound vertical translation value that writes to `UIElement.TranslateYProperty`. |
| `VelocityX` | `float` | Gets the most recent horizontal pointer velocity in units per second. |
| `VelocityY` | `float` | Gets the most recent vertical pointer velocity in units per second. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Begin(float x, float y, TimeSpan time)` | `void` | Starts a drag at the supplied pointer position and timestamp, records the current translation as the drag start, and resets velocity tracking. |
| `Move(float x, float y, TimeSpan time)` | `void` | Adds a velocity sample and immediately moves `DragX` and `DragY` to follow the pointer offset captured by `Begin`. |
| `End(MotionSpec<float> settleSpec)` | `void` | Switches to settling and animates both axes toward the current translation plus velocity-based momentum using `settleSpec`. |
| `PointerCaptureLost(MotionSpec<float> settleSpec)` | `void` | If currently dragging, switches to settling and animates both axes back to the translation recorded by `Begin`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionElementFacade.Drag()` | `InvalidOperationException` | The element is not attached to a `UIRoot`, so no root motion system is available for the controller. |
| `PointerCaptureLost` | `ArgumentNullException` | `settleSpec` is `null`. |
| `End` | `ArgumentNullException` | `settleSpec` is `null`; this is thrown by the underlying `MotionValue<float>.AnimateTo` call. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Motion/Input/DragMotionController.cs`
- `UI/Motion/MotionElementFacade.cs`
- `UI/Motion/Input/PointerMotionState.cs`
- `UI/Motion/Input/VelocityTracker.cs`
- `UI/Motion/Core/MotionValue{T}.cs`
- `UI/Motion/Specs/MotionSpec{T}.cs`
