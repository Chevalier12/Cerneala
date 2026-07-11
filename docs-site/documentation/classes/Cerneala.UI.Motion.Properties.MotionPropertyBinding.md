# MotionPropertyBinding Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyBinding.cs`

Represents the disposable, untyped base contract for a motion value that writes animation samples into a UI property.

```csharp
public abstract class MotionPropertyBinding : IDisposable
```

Inheritance:
`object` -> `MotionPropertyBinding`

Derived:
`MotionPropertyBinding<T>`

Implements:
`IDisposable`

## Examples

Store a typed property binding through the untyped base contract when only the target and property identity are needed:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Properties;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionPropertyBinding<Color> typedBinding =
    root.Motion.Properties.GetOrCreateBinding(
        root.Motion,
        control,
        Control.BackgroundProperty);

MotionPropertyBinding binding = typedBinding;

Console.WriteLine(binding.Target == control);
Console.WriteLine(binding.PropertyUntyped == Control.BackgroundProperty);

binding.Clear(MotionClearBehavior.RestoreBase);
```

## Remarks

`MotionPropertyBinding` is the shared base for property animation bindings stored by `MotionPropertyStore`. It exposes the `UiObject` target, the untyped `UiProperty`, and the lifetime operations that callers can use without knowing the property's value type.

The concrete `MotionPropertyBinding<T>` owns the typed `MotionValue<T>`, subscribes to value changes, stages animation writes through `MotionPropertyStore`, and registers a motion graph node while an animation is active. The base class keeps those details behind an untyped API for storage and cleanup.

`Clear` removes the current animation contribution from the target property by default. Passing `MotionClearBehavior.HoldCurrent` asks the concrete binding to keep the current sampled value as the animation source instead. `Dispose` releases the binding; concrete implementations are expected to clear their staged animation state and unsubscribe from motion value updates.

Property writes use the `Animation` value source in the UI property system. Local values can still mask an animated value, and clearing the local value can reveal the active animation source again.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Target` | `UiObject` | Gets the object whose property receives animation samples. |
| `PropertyUntyped` | `UiProperty` | Gets the animated property without a generic value type. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Clear(MotionClearBehavior behavior = MotionClearBehavior.RestoreBase)` | `void` | Clears or holds the animation contribution for `PropertyUntyped` on `Target`, depending on the requested clear behavior. |
| `Dispose()` | `void` | Releases the binding and its concrete subscriptions or staged animation state. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
- `Cerneala.UI.Motion.Properties.MotionPropertyStore`
- `Cerneala.UI.Motion.Properties.MotionClearBehavior`
- `Cerneala.UI.Core.UiObject`
- `Cerneala.UI.Core.UiProperty`
