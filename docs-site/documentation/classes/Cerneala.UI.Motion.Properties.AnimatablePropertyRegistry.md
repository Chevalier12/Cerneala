# AnimatablePropertyRegistry Class

## Definition
Namespace: `Cerneala.UI.Motion.Properties`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Properties/AnimatablePropertyRegistry.cs`

Stores the UI properties that the motion system may animate, together with the mixer, default motion spec, invalidation category, and implicit-animation eligibility for each property.

```csharp
public sealed class AnimatablePropertyRegistry
```

Inheritance:
`object` -> `AnimatablePropertyRegistry`

## Examples

Resolve the options for a built-in animatable property:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Properties;

AnimatablePropertyRegistry registry = new();

if (registry.TryGet(UIElement.OpacityProperty, out MotionPropertyOptions options))
{
    Type mixerType = options.MixerType;
    bool canAnimateImplicitly = options.IsSafeForImplicitAnimation;
}
```

Register an additional UI property with explicit motion options:

```csharp
using Cerneala.UI.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;

AnimatablePropertyRegistry registry = new();
UiProperty<float> progressProperty = UiProperty<float>.Register(
    "Progress",
    typeof(object),
    new UiPropertyMetadata<float>(
        0f,
        UiPropertyOptions.AffectsRender));

registry.Register(
    progressProperty,
    new MotionPropertyOptions(
        typeof(FloatMixer),
        Motion.Tween(TimeSpan.FromMilliseconds(150)),
        MotionPropertyInvalidationClassifier.Classify(progressProperty),
        isSafeForImplicitAnimation: true));
```

## Remarks

`AnimatablePropertyRegistry` is created by `MotionSystem` and exposed through `MotionSystem.AnimatableProperties`. It is the allow-list used by motion transactions and property-motion plumbing to decide which `UiProperty` instances have animation metadata.

The constructor registers the built-in motion-aware properties for controls and elements. Color properties use `ColorMixer`; thickness properties use `ThicknessMixer`; opacity, transform parts, and scalar transform properties use `FloatMixer` or `TransformMixer`. The default specs are short tweens ranging from 120 to 180 milliseconds.

Built-in properties marked safe for implicit animation include background color, border color, opacity, render transform, translation, scale, rotation, and skew properties. Layout-affecting thickness properties such as border thickness, padding, and margin are registered but are not marked safe for implicit animation.

`Register` replaces any existing entry for the same `UiProperty`. `TryGet` returns `false` for unregistered properties, while `Get` throws an `InvalidOperationException` that includes the property's diagnostic name.

`RegisteredProperties` exposes the backing dictionary as an `IReadOnlyDictionary`. Treat it as a registry view for lookup and diagnostics; register new entries through `Register`.

## Constructors

| Name | Description |
| --- | --- |
| `AnimatablePropertyRegistry()` | Creates a registry and registers the built-in animatable control and element properties. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RegisteredProperties` | `IReadOnlyDictionary<UiProperty, MotionPropertyOptions>` | Gets the registered animatable properties and their motion options. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Register(UiProperty property, MotionPropertyOptions options)` | `void` | Registers or replaces the motion options for `property`. |
| `TryGet(UiProperty property, out MotionPropertyOptions options)` | `bool` | Attempts to get motion options for `property`. |
| `Get(UiProperty property)` | `MotionPropertyOptions` | Gets the motion options for `property`, or throws when the property is not registered as animatable. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Register(UiProperty, MotionPropertyOptions)` | `ArgumentNullException` | `property` or `options` is `null`. |
| `TryGet(UiProperty, out MotionPropertyOptions)` | `ArgumentNullException` | `property` is `null`. |
| `Get(UiProperty)` | `ArgumentNullException` | `property` is `null`. |
| `Get(UiProperty)` | `InvalidOperationException` | `property` is not registered as animatable. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Properties.MotionPropertyOptions`
- `Cerneala.UI.Motion.Properties.MotionPropertyInvalidationClassifier`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
