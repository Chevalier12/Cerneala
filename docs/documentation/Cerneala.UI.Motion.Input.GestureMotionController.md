# GestureMotionController Class

## Definition
Namespace: `Cerneala.UI.Motion.Input`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Input/GestureMotionController.cs`

Coordinates pointer press and release motion for a `UIElement`.

```csharp
public sealed class GestureMotionController
```

Inheritance:
`object` -> `GestureMotionController`

## Examples

Create a gesture controller from an element's motion facade and retarget scale when the pointer is pressed or released.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIElement element = new();
GestureMotionController gestures = element.Motion().Gestures();

gestures.PointerPressed(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
gestures.PointerReleased(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
```

## Remarks

`GestureMotionController` is created through `MotionElementFacade.Gestures()`. Its constructor is internal, so callers use `element.Motion().Gestures()` rather than constructing the controller directly.

`PointerPressed` sets `State` to `PointerMotionState.Pressed` and starts a scale animation toward `0.97f`. `PointerReleased` sets `State` to `PointerMotionState.Idle` and starts a scale animation back to `1`.

The controller delegates animation work to the wrapped element's motion facade. The target element must satisfy the same motion requirements as other `UIElement.Motion()` property animations.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `PointerMotionState` | Gets the current pointer motion state tracked by the controller. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `PointerPressed(MotionSpec<float>)` | `void` | Marks the pointer as pressed and animates the element scale to `0.97f` using the supplied motion spec. |
| `PointerReleased(MotionSpec<float>)` | `void` | Marks the pointer as idle and animates the element scale to `1` using the supplied motion spec. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.Input.PointerMotionState`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
