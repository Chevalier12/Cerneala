# ColorMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/ColorMixer.cs`

Interpolates `Color` values for Cerneala motion animations.

```csharp
public sealed class ColorMixer : ValueMixer<Color>
```

Inheritance:
`Object` -> `ValueMixer<Color>` -> `ColorMixer`

Implements:
`IValueMixer` through `ValueMixer<Color>`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ColorMixer mixer = new();

Color start = new(0, 10, 20, 0);
Color end = new(100, 110, 120, 200);

Color halfway = mixer.Mix(start, end, 0.5f);
// halfway is Color(50, 60, 70, 100).
```

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<Color> mixer = registry.Resolve<Color>();
Color value = mixer.Mix(Color.Black, Color.White, 0.25f);
```

## Remarks

`ColorMixer` mixes the red, green, blue, and alpha channels independently. Progress values less than or equal to `0` return the source color, and values greater than or equal to `1` return the target color.

For intermediate progress values, each channel is linearly interpolated, rounded with `MidpointRounding.AwayFromZero`, and clamped to the valid byte range. Alpha is treated the same way as the color channels.

The built-in `ValueMixerRegistry` registers `ColorMixer` for `Color`. The motion property registry uses it for color properties such as `Control.BackgroundProperty` and `Control.BorderColorProperty`.

`ColorMixer` keeps the default `ValueMixer<Color>` vector behavior, so `SupportsVectorOperations` is `false` and vector methods such as `Add`, `Subtract`, `Scale`, and `Magnitude` throw `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `ColorMixer()` | Initializes a new color mixer. |

## Properties

| Name | Description |
| --- | --- |
| `ValueType` | Gets `typeof(Color)`. Inherited from `ValueMixer<Color>`. |
| `SupportsVectorOperations` | Gets `false`; color mixing does not expose vector operations. Inherited from `ValueMixer<Color>`. |

## Methods

| Name | Description |
| --- | --- |
| `Mix(Color, Color, float)` | Returns an interpolated `Color`, preserving exact endpoints for progress outside or at the `0` to `1` range. |
| `EqualsWithinTolerance(Color, Color, float)` | Returns `true` when every RGBA channel differs by no more than the supplied finite, non-negative tolerance. |
| `MixUntyped(object?, object?, float)` | Casts the inputs to `Color` and delegates to `Mix`. Inherited from `ValueMixer<Color>`. |
| `EqualsWithinToleranceUntyped(object?, object?, float)` | Casts the inputs to `Color` and delegates to `EqualsWithinTolerance`. Inherited from `ValueMixer<Color>`. |
| `Add(Color, Color)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<Color>`. |
| `Subtract(Color, Color)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<Color>`. |
| `Scale(Color, float)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<Color>`. |
| `Magnitude(Color)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<Color>`. |

## Applies to

Cerneala UI motion interpolation for `Color` values.

## See also

- `Cerneala.Drawing.Color`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
- `Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry`
