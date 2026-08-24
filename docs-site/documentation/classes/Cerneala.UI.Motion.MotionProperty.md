# MotionProperty Class

## Definition

Namespace: `Cerneala.UI.Motion`

Assembly/Project: `Cerneala`

Source: `UI/Motion/MotionProperty.cs`

Creates typed property descriptors that let Motion read and update properties on arbitrary reference objects.

```csharp
public static class MotionProperty
```

## Examples

```csharp
using Cerneala.UI.Motion;

MotionProperty<Marker, float> ScaleProperty = MotionProperty.Create<Marker, float>(
    "Scale",
    marker => marker.Scale,
    (marker, value) => marker.Scale = value);

Marker marker = new() { Scale = 1f };
marker.Motion().Animate(ScaleProperty).To(1.5f).Start(spec);
```

## Remarks

Use `Create` for values that have a registered or explicitly supplied `ValueMixer<T>`. Use `CreateDiscrete` for values that should retain their starting value until the animation reaches its destination. A descriptor contains accessors only; it neither owns a target nor starts an animation.

Prism filter and style classes expose generated descriptors such as `OuterGlowStyle.SizeProperty`, so those properties do not require hand-written descriptors.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create<TTarget, TValue>(string, Func<TTarget, TValue>, Action<TTarget, TValue>, ValueMixer<TValue>? = null)` | `MotionProperty<TTarget, TValue>` | Creates an interpolated property descriptor, optionally with a property-specific mixer. |
| `CreateDiscrete<TTarget, TValue>(string, Func<TTarget, TValue>, Action<TTarget, TValue>)` | `MotionProperty<TTarget, TValue>` | Creates a property descriptor that switches discretely to its destination. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Create`, `CreateDiscrete` | `ArgumentException` | `name` is empty or whitespace. |
| `Create`, `CreateDiscrete` | `ArgumentNullException` | An accessor is `null`. |

## See also

- `Cerneala.UI.Motion.MotionProperty<TTarget, TValue>`
- `Cerneala.UI.Motion.ObjectMotionFacade`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
