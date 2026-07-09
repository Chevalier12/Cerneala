# MotionPropertyOptions Class

## Definition

Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/MotionPropertyOptions.cs`

Stores the motion metadata used to animate a registered UI property.

```csharp
public sealed class MotionPropertyOptions
```

Inheritance:
`object` -> `MotionPropertyOptions`

## Examples

Create options for a render-only scalar property:

```csharp
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

MotionPropertyOptions options = new(
    typeof(FloatMixer),
    Motion.Tween(TimeSpan.FromMilliseconds(120)),
    MotionPropertyInvalidationCategory.Render,
    isSafeForImplicitAnimation: true);

Type mixerType = options.MixerType;
MotionSpec defaultSpec = options.DefaultSpec;
```

Register options for a UI property:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

AnimatablePropertyRegistry registry = new();

registry.Register(
    Control.BackgroundProperty,
    new MotionPropertyOptions(
        typeof(ColorMixer),
        Motion.Tween(TimeSpan.FromMilliseconds(160)),
        MotionPropertyInvalidationClassifier.Classify(Control.BackgroundProperty),
        isSafeForImplicitAnimation: true));
```

## Remarks

`MotionPropertyOptions` is the metadata value stored by `AnimatablePropertyRegistry` for each animatable `UiProperty`. It records the mixer type associated with the property value, the default motion spec to use when callers do not provide one, the invalidation category required by animation writes, and whether the property is safe for implicit animation.

Motion transactions consult the animatable property registry before animating a property mutation. When a property is registered, the transaction pipeline uses the registered `DefaultSpec` if no transaction-level spec is supplied, and it uses the invalidation category when animated values are written back through the motion property pipeline.

The constructor requires non-null `mixerType` and `defaultSpec` arguments. The class is immutable after construction.

## Constructors

| Name | Description |
| --- | --- |
| `MotionPropertyOptions(Type mixerType, MotionSpec defaultSpec, MotionPropertyInvalidationCategory invalidationCategory, bool isSafeForImplicitAnimation)` | Initializes motion metadata for an animatable UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `MixerType` | `Type` | Gets the value mixer type associated with the animated property value. |
| `DefaultSpec` | `MotionSpec` | Gets the default motion specification used when no overriding spec is supplied. |
| `InvalidationCategory` | `MotionPropertyInvalidationCategory` | Gets the invalidation category associated with animation writes for the property. |
| `IsSafeForImplicitAnimation` | `bool` | Gets a value indicating whether the property may be animated implicitly by registry-driven motion features. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionPropertyOptions(...)` | `ArgumentNullException` | `mixerType` or `defaultSpec` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry`
- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationCategory`
- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationClassifier`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
- `Cerneala.UI.Motion.Specs.MotionSpec`
