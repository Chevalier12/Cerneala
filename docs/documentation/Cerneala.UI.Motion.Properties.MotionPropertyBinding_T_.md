# MotionPropertyBinding<T> Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyBinding{T}.cs`

Binds a typed `MotionValue<T>` to a `UiProperty<T>` and stages animation samples into a target `UiObject`.

```csharp
public sealed class MotionPropertyBinding<T> : MotionPropertyBinding
```

Inheritance:
`object` -> `MotionPropertyBinding` -> `MotionPropertyBinding<T>`

Implements:
`IDisposable` through `MotionPropertyBinding`

## Examples

Create a binding directly and animate a control background property:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionValue<DrawColor> value =
    root.Motion.Graph.CreateValue(control.Background);

using MotionPropertyBinding<DrawColor> binding =
    new(root.Motion, control, Control.BackgroundProperty, value);

MotionHandle handle = binding.AnimateTo(
    DrawColor.White,
    Motion.Tween<DrawColor>(TimeSpan.FromMilliseconds(100)));

root.ProcessFrame();
```

Hold the final animated value instead of restoring the base property value:

```csharp
binding.AnimateTo(
    DrawColor.White,
    Motion.Tween<DrawColor>(TimeSpan.FromMilliseconds(100)),
    new MotionPropertyStartOptions { HoldOnComplete = true });
```

## Remarks

`MotionPropertyBinding<T>` owns the connection between one typed motion value and one UI property. It subscribes to the `MotionValue<T>`, records pending samples, and registers a private motion graph node while an animation is active. On each graph tick, pending samples are staged through `MotionPropertyStore` as `UiPropertyValueSource.Animation` writes.

The constructor requires the supplied `MotionValue<T>` to come from the same `MotionSystem` graph as the binding. This prevents samples from a foreign motion graph from being written into a target owned by another motion system.

`AnimateTo` starts the underlying `MotionValue<T>` animation, stages the current value, and returns the `MotionHandle` from the motion graph. By default, natural completion clears the animation source so the target property falls back to its next available source, such as an aspect base value. When `MotionPropertyStartOptions.HoldOnComplete` is `true`, completion stages the current animated value instead.

`Clear` cancels the active handle with `MotionCancelBehavior.KeepCurrent`, then either clears the animation source or stages the current value depending on `MotionClearBehavior`. `Dispose` calls `Clear` and releases the value subscription. Calling `Clear` after disposal is a no-op; calling `AnimateTo` after disposal throws `ObjectDisposedException`.

When the target is a `UIElement` and becomes detached, the binding clears itself during its next tick and the active handle is canceled. Render-only and layout-affecting invalidation are chosen from the bound property by `MotionPropertyInvalidationClassifier`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionPropertyBinding(MotionSystem motion, UiObject target, UiProperty<T> property, MotionValue<T> value)` | Initializes a binding for `property` on `target`, verifies all arguments, classifies the property's invalidation category, and subscribes to `value` changes. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Target` | `UiObject` | Gets the object whose property receives staged animation samples. |
| `Property` | `UiProperty<T>` | Gets the typed UI property written by the binding. |
| `PropertyUntyped` | `UiProperty` | Gets the same property without its generic value type. |
| `Value` | `MotionValue<T>` | Gets the typed motion value that supplies animation samples. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AnimateTo(T to, MotionSpec<T> spec, MotionPropertyStartOptions? options = null)` | `MotionHandle` | Starts animating `Value` toward `to`, stages samples into `Property`, and returns the active motion handle. |
| `Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase)` | `void` | Cancels the active animation and either clears the animation source or holds the current sampled value. |
| `Dispose()` | `void` | Clears the binding once and unsubscribes from `Value` updates. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionPropertyBinding(...)` | `ArgumentNullException` | `motion`, `target`, `property`, or `value` is `null`. |
| `MotionPropertyBinding(...)` | `InvalidOperationException` | `value` was created by a different `MotionSystem` graph than `motion`. |
| `AnimateTo(...)` | `ArgumentNullException` | `spec` is `null`. |
| `AnimateTo(...)` | `ObjectDisposedException` | The binding has already been disposed. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyBinding`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Properties.MotionPropertyStartOptions`
- `Cerneala.UI.Motion.Properties.MotionClearBehavior`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty<T>`
