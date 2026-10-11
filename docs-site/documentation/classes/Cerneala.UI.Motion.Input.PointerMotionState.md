# PointerMotionState Enum

## Definition
Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/PointerMotionState.cs`

Reports the pointer state of a gesture or drag motion controller.

```csharp
public enum PointerMotionState
```

## Examples

Follow the state of a press gesture:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Input;
using Cerneala.UI.Motion.Specs;

UIRoot root = new(100, 100);
UIElement element = new();
root.VisualChildren.Add(element);
root.ProcessFrame();

GestureMotionController gesture = element.Motion().Gestures();
MotionSpec<float> spec = Motion.Tween<float>(TimeSpan.FromMilliseconds(100));

Console.WriteLine(gesture.State); // Idle
gesture.PointerPressed(spec);
Console.WriteLine(gesture.State); // Pressed
gesture.PointerReleased(spec);
Console.WriteLine(gesture.State); // Idle
```

## Remarks

`GestureMotionController.State` and `DragMotionController.State` return this value. Both start at `Idle`.

`GestureMotionController` uses `Idle` and `Pressed`:

- `PointerPressed` sets `Pressed` and scales the element to `0.97`.
- `PointerReleased` sets `Idle` and scales the element back to `1`.
- `Dispose` sets `Idle`.

`DragMotionController` uses `Idle`, `Dragging`, and `Settling`:

- `Begin` sets `Dragging`.
- `End` sets `Settling` and starts the settle animation.
- `PointerCaptureLost` sets `Settling` and animates back to the start position, but only when the state is `Dragging`; otherwise it does nothing.
- `Dispose` sets `Idle`. This is the only call that returns a drag controller to `Idle`. The state stays `Settling` after the settle animations finish, until the next `Begin` or `Dispose`.

Markup-generated gesture and drag handlers read the state to skip duplicate calls. For example, a generated drag move handler calls `Move` only when the state is `Dragging`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Idle` | `0` | No pointer interaction. The initial state. |
| `Pressed` | `1` | A gesture controller received `PointerPressed`. |
| `Dragging` | `2` | A drag controller received `Begin`. |
| `Settling` | `3` | A drag controller ended the drag and started settling. |

## Applies to

Cerneala pointer-driven motion.

## See also

- `Cerneala.UI.Motion.Input.GestureMotionController`
- `Cerneala.UI.Motion.Input.DragMotionController`
- `Cerneala.UI.Motion.MotionElementFacade`
