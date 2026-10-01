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
        typeof(BrushMixer),
        Motion.Tween(TimeSpan.FromMilliseconds(160)),
        MotionPropertyInvalidationClassifier.Classify(Control.BackgroundProperty),
        isSafeForImplicitAnimation: true));
```

## Remarks

`MotionPropertyOptions` is the metadata value stored by `AnimatablePropertyRegistry` for each animatable `UiProperty`. It records the mixer type associated with the property value, the default motion spec to use when callers do not provide one, the invalidation category required by animation writes, and whether the property is safe for implicit animation.

Motion transactions consult the registry as an allow-list, but use `MotionTransactionOptions.DefaultSpec`, not the registered default. Generated markup property-motion sessions read the registered `DefaultSpec` when their caller omits a spec. Property bindings resolve the mixer through `ValueMixerRegistry` and classify invalidation directly from the `UiProperty` metadata rather than consuming `MixerType` or `InvalidationCategory` here. `IsSafeForImplicitAnimation` is descriptive metadata; the current transaction path does not enforce it as an eligibility check.

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
| `IsSafeForImplicitAnimation` | `bool` | Gets the registered safety metadata for implicit animation; this flag is not enforced by the current transaction path. |

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
