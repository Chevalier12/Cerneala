# MotionVisualStateController Class

## Definition
Namespace: `Cerneala.UI.Motion.States`

Assembly/Project: `Cerneala`

Source: `UI/Motion/States/MotionVisualStateController.cs`

Captures the input and enabled visual-state flags of a `UIElement` into a `MotionVisualStateSnapshot`.

```csharp
public sealed class MotionVisualStateController
```

Inheritance:
`object` -> `MotionVisualStateController`

## Examples

Capture the current visual state for an element:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.States;

UIElement element = new()
{
    IsPointerOver = true,
    IsEnabled = true
};

MotionVisualStateController controller = new();
MotionVisualStateSnapshot snapshot = controller.Capture(element);

bool isHovered = snapshot.IsPointerOver;
bool isEnabled = snapshot.IsEnabled;
```

## Remarks

`MotionVisualStateController` is a small adapter between `UIElement` state and the motion-state snapshot type. `Capture` reads the element's current `IsPointerOver`, `IsKeyboardFocused`, `IsKeyboardFocusWithin`, and `IsEnabled` values and returns them as a new `MotionVisualStateSnapshot`.

The controller does not subscribe to element changes, mutate the element, start animations, or cache previous snapshots. Call `Capture` each time current visual-state flags are needed.

`Capture` requires a non-null `UIElement` and throws `ArgumentNullException` for `null`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionVisualStateController()` | Initializes a new visual-state controller. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Capture(UIElement element)` | `MotionVisualStateSnapshot` | Captures pointer-over, keyboard-focus, keyboard-focus-within, and enabled state from the supplied element. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Capture(UIElement element)` | `ArgumentNullException` | `element` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Motion.States.MotionVisualStateSnapshot`
- `UI/Motion/States/MotionVisualStateController.cs`
