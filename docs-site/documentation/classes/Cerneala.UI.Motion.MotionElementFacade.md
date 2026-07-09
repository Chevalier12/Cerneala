# MotionElementFacade Class

## Definition
Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionElementFacade.cs`

Provides the fluent motion entry point for a `UIElement`.

```csharp
public sealed class MotionElementFacade
```

Inheritance:
`object` -> `MotionElementFacade`

## Examples

Animate one of the built-in shortcut properties on an attached element:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
UIElement element = new();
root.VisualChildren.Add(element);

MotionHandle handle = element.Motion()
    .Opacity
    .To(0.5f, Motion.Tween<float>(TimeSpan.FromMilliseconds(100)));
```

Animate any registered UI property by passing its `UiProperty<T>` identifier:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionHandle handle = control.Motion()
    .Animate(Control.BackgroundProperty)
    .To(DrawColor.White)
    .With(Motion.Tween<DrawColor>(TimeSpan.FromMilliseconds(150)));
```

## Remarks

`MotionElementFacade` is created through `MotionExtensions.Motion(UIElement)`. Its constructor is internal, so callers normally use `element.Motion()` rather than constructing the facade directly.

The facade stores the target `UIElement` and resolves the owning `MotionSystem` when an animation starts. The element must already be attached to a `UIRoot`, or be the root itself, before property animation can run. Detached elements throw an `InvalidOperationException` when the facade tries to resolve motion for property animation.

`Opacity`, `TranslateX`, `TranslateY`, and `Scale` are convenience shortcuts over the corresponding `UIElement` motion properties. For other animatable properties, use `Animate<T>(UiProperty<T>)`.

`Gestures()` and `Drag()` create input-oriented controllers for the same element. `Drag()` requires the element to be attached because its controller creates motion values immediately. `ScrollTimeline()` is only valid for `ScrollViewer` instances and throws for other element types.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Opacity` | `MotionPropertyShortcut<float>` | Gets a shortcut for animating `UIElement.OpacityProperty`. |
| `TranslateX` | `MotionPropertyShortcut<float>` | Gets a shortcut for animating `UIElement.TranslateXProperty`. |
| `TranslateY` | `MotionPropertyShortcut<float>` | Gets a shortcut for animating `UIElement.TranslateYProperty`. |
| `Scale` | `MotionPropertyShortcut<float>` | Gets a shortcut for animating `UIElement.ScaleProperty`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Animate<T>(UiProperty<T> property)` | `MotionAnimationBuilder<T>` | Creates a builder for animating the supplied UI property on the target element. Throws `ArgumentNullException` when `property` is `null`. |
| `States()` | `MotionStateBuilder` | Creates a state builder tied to the same motion facade. |
| `Gestures()` | `GestureMotionController` | Creates a pointer gesture controller for press and release scale motion on the target element. |
| `Drag()` | `DragMotionController` | Creates a drag controller for translating the target element with motion-backed drag values. |
| `ScrollTimeline()` | `ScrollTimeline` | Creates a scroll timeline for the target element when it is a `ScrollViewer`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Animate<T>(UiProperty<T>)` | `ArgumentNullException` | `property` is `null`. |
| Property animation through `Animate<T>(...)` or a shortcut | `InvalidOperationException` | The target element is detached and is not a `UIRoot`. |
| `Drag()` | `InvalidOperationException` | The target element is detached when the drag controller is created. |
| `ScrollTimeline()` | `InvalidOperationException` | The target element is not a `ScrollViewer`, or the `ScrollViewer` is detached when the timeline is created. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.MotionExtensions`
- `Cerneala.UI.Motion.MotionAnimationBuilder<T>`
- `Cerneala.UI.Motion.MotionPropertyShortcut<T>`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Motion.Core.MotionSystem`
