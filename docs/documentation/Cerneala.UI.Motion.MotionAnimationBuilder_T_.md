# MotionAnimationBuilder<T> Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionAnimationBuilder.cs`

Builds a fluent animation or scroll-linked binding for a typed `UiProperty<T>` on a `UIElement`.

```csharp
public sealed class MotionAnimationBuilder<T>
```

Inheritance:
`object` -> `MotionAnimationBuilder<T>`

## Examples

Animate a property from its current visual value to a new value:

```csharp
using System;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIElement target = new();
UIRoot root = new();
root.VisualChildren.Add(target);

float currentOpacity = target.Opacity;
target.ClearValue(UIElement.OpacityProperty);

MotionHandle handle = target.Motion()
    .Animate(UIElement.OpacityProperty)
    .From(currentOpacity)
    .To(0.35f)
    .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(180)));
```

Bind scroll progress to a transform property:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Input;

UIRoot root = new();
ScrollViewer scrollViewer = new();
UIElement progressBar = new();
root.VisualChildren.Add(scrollViewer);
root.VisualChildren.Add(progressBar);

ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();

progressBar.Motion()
    .Animate(UIElement.ScaleXProperty)
    .Bind(timeline.Progress.Map(0.04f, 1f));
```

## Remarks

`MotionAnimationBuilder<T>` is returned by `MotionElementFacade.Animate<T>(UiProperty<T>)`, usually through the `UIElement.Motion()` extension method. The builder stores the target element facade and the typed UI property being animated.

`From` sets an explicit starting value. When `With` is later called, the underlying `MotionPropertyBinding<T>` first jumps its `MotionValue<T>` to that value. If `From` is not called, the binding starts from the current motion value maintained for the element/property pair.

`To` stores the target value. `With` resolves the element's `MotionSystem`, gets or creates a reusable property binding for the element and property, and starts `MotionPropertyBinding<T>.AnimateTo` with `HoldOnComplete = true`. The returned `MotionHandle` can be canceled, completed, disposed, or observed for completion.

The element must be attached to a `UIRoot`, or be a `UIRoot` itself, before `With` can start the animation. The selected property type must also have a registered value mixer compatible with the supplied `MotionSpec<T>`.

`Bind` does not start a time-based animation. It attaches a `ScrollMotionBinding<T>` to the same element/property pair so the property is written from scroll timeline progress.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `From(T value)` | `MotionAnimationBuilder<T>` | Sets an explicit starting value for the next `With` call and returns the same builder. |
| `To(T value)` | `MotionAnimationBuilder<T>` | Sets the target value for the next `With` call and returns the same builder. |
| `With(MotionSpec<T> spec)` | `MotionHandle` | Starts the property animation with the supplied motion specification and returns the active handle. |
| `Bind(ScrollMotionBinding<T> binding)` | `void` | Binds a scroll-linked value source to the target element property. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `With(MotionSpec<T>)` | `ArgumentNullException` | `spec` is `null`. |
| `With(MotionSpec<T>)` | `InvalidOperationException` | The element is not attached to a `UIRoot`, or no compatible mixer exists for the property value type. |
| `Bind(ScrollMotionBinding<T>)` | `ArgumentNullException` | `binding` is `null`. |
| `Bind(ScrollMotionBinding<T>)` | `InvalidOperationException` | The scroll binding rejects the target property, such as a layout-affecting property without `AllowLayout()`, or the binding type is not supported by the current scroll binding implementation. |

## Applies to

Cerneala retained UI motion facade animations and scroll-linked property motion.

## See also

- `Cerneala.UI.Motion.MotionElementFacade`
- `Cerneala.UI.Motion.MotionPropertyShortcut<T>`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
- `Cerneala.UI.Motion.Input.ScrollMotionBinding<T>`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
