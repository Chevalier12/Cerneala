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

`ColorMixer` mixes the red, green, blue, and alpha channels independently. Progress exactly `0` returns the source color, and progress exactly `1` returns the target color.

Each channel is linearly interpolated or, for progress outside `[0, 1]`, extrapolated. The result is rounded with `MidpointRounding.AwayFromZero` and clamped to the valid byte range `[0, 255]`. Alpha is treated the same way as the color channels. For example, mixing `Color(0, 200, 20, 100)` toward `Color(200, 0, 40, 150)` at progress `1.5` returns `Color(255, 0, 50, 175)`.

The built-in `ValueMixerRegistry` registers `ColorMixer` for `Color`. Brush-valued control properties such as `Control.BackgroundProperty` and `Control.BorderBrushProperty` use `BrushMixer`, which delegates solid-brush color interpolation to `ColorMixer`.

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
| `Mix(Color, Color, float)` | Interpolates or extrapolates each channel, rounds and clamps to the byte range, and preserves exact endpoints at progress `0` and `1`. |
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
