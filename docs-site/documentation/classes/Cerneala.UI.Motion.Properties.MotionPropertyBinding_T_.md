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
using Cerneala.UI.Media;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionValue<Brush?> value =
    root.Motion.Graph.CreateValue(control.Background);

using MotionPropertyBinding<Brush?> binding =
    new(root.Motion, control, Control.BackgroundProperty, value);

MotionHandle handle = binding.AnimateTo(
    new SolidColorBrush(Color.White),
    Motion.Tween<Brush?>(TimeSpan.FromMilliseconds(100)));

root.ProcessFrame();
```

Hold the final animated value instead of restoring the base property value:

```csharp
binding.AnimateTo(
    new SolidColorBrush(Color.White),
    Motion.Tween<Brush?>(TimeSpan.FromMilliseconds(100)),
    new MotionPropertyStartOptions { HoldOnComplete = true });
```

## Remarks

`MotionPropertyBinding<T>` owns the connection between one typed motion value and one UI property. It subscribes to the `MotionValue<T>`, records pending samples, and registers a private motion graph node while an animation is active. On each graph tick, pending samples are staged through `MotionPropertyStore` as `UiPropertyValueSource.Animation` writes.

The constructor requires the supplied `MotionValue<T>` to come from the same `MotionSystem` graph as the binding. This prevents samples from a foreign motion graph from being written into a target owned by another motion system.

While the binding is alive, `AnimateTo` and restart/preserve-progress retargets validate the requested target synchronously, before replacing active motion. Direct calls to the exposed `Value.AnimateTo` or `Value.JumpTo` also perform this validation. The property's existing coercion, metadata validation, and owner-specific mutation checks apply as for an explicit property write; read-only properties are rejected. Disposal removes the binding's target validation from the motion value.

Intermediate samples are coerced and checked through the existing property metadata validator when the property store flushes. A validator returning `false`, or an owner validation hook rejecting the value with `ArgumentException`, skips the write: the property retains its last valid animated value, motion continues, and later valid samples are written normally. The motion value itself retains the raw sample and may temporarily differ from the property's value. No mixer or per-property clamp is added. Exceptions thrown by coercers, metadata validators, or property-change handlers still propagate, as do non-value errors from owner checks. With Motion diagnostics enabled, skipped samples produce a warning in `UIRoot.Detective.Motion.Warnings`.

`AnimateTo` starts the underlying `MotionValue<T>` animation, stages the current value, and returns the `MotionHandle` from the motion graph. By default, natural completion clears the animation source so the target property falls back to its next available source, such as an aspect base value. When `MotionPropertyStartOptions.HoldOnComplete` is `true`, completion stages the current animated value instead.

`Clear` cancels the active handle with `MotionCancelBehavior.KeepCurrent`, then either clears the animation source or stages the current value depending on `MotionClearBehavior`. `Dispose` calls `Clear`, releases the value subscription, and removes the binding from its `MotionPropertyStore`. Calling `Clear` after disposal is a no-op; calling `AnimateTo` after disposal throws `ObjectDisposedException`.

When the target is a `UIElement`, `AnimateTo` immediately cancels the new handle if the target or one of its visual ancestors is not render-visible. An active binding also clears itself and cancels its handle when the target becomes detached, `Hidden`, `Collapsed`, or hidden by `IsVisible` on itself or a visual ancestor. Render-only and layout-affecting invalidation are chosen from the bound property by `MotionPropertyInvalidationClassifier`.

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
| `AnimateTo(T to, MotionSpec<T> spec, MotionPropertyStartOptions? options = null)` | `MotionHandle` | Starts animating `Value` toward `to`, stages samples into `Property`, and returns the motion handle; a non-visible `UIElement` target returns an immediately canceled handle. |
| `Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase)` | `void` | Cancels the active animation and either clears the animation source or holds the current sampled value. |
| `Dispose()` | `void` | Clears the binding once, unsubscribes from `Value` updates, and removes it from its property store. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionPropertyBinding(...)` | `ArgumentNullException` | `motion`, `target`, `property`, or `value` is `null`. |
| `MotionPropertyBinding(...)` | `InvalidOperationException` | `value` was created by a different `MotionSystem` graph than `motion`. |
| `AnimateTo(...)` | `ArgumentNullException` | `spec` is `null`. |
| `AnimateTo(...)` | `ObjectDisposedException` | The binding has already been disposed. |
| `AnimateTo(...)`, direct `Value.AnimateTo(...)` or `Value.JumpTo(...)` | `ArgumentException` | The target fails the property's metadata validation after coercion. Owner-specific checks may throw their existing exceptions. |
| `AnimateTo(...)` | `InvalidOperationException` | The bound property is read-only. |

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
