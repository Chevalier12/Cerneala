# ColorMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/ColorMixer.cs`

Interpolates `DrawColor` values for Cerneala motion animations.

```csharp
public sealed class ColorMixer : ValueMixer<DrawColor>
```

Inheritance:
`Object` -> `ValueMixer<DrawColor>` -> `ColorMixer`

Implements:
`IValueMixer` through `ValueMixer<DrawColor>`

## Examples

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ColorMixer mixer = new();

DrawColor start = new(0, 10, 20, 0);
DrawColor end = new(100, 110, 120, 200);

DrawColor halfway = mixer.Mix(start, end, 0.5f);
// halfway is DrawColor(50, 60, 70, 100).
```

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Motion.Interpolation;

ValueMixerRegistry registry = new();
registry.RegisterBuiltIns();

ValueMixer<DrawColor> mixer = registry.Resolve<DrawColor>();
DrawColor value = mixer.Mix(DrawColor.Black, DrawColor.White, 0.25f);
```

## Remarks

`ColorMixer` mixes the red, green, blue, and alpha channels independently. Progress values less than or equal to `0` return the source color, and values greater than or equal to `1` return the target color.

For intermediate progress values, each channel is linearly interpolated, rounded with `MidpointRounding.AwayFromZero`, and clamped to the valid byte range. Alpha is treated the same way as the color channels.

The built-in `ValueMixerRegistry` registers `ColorMixer` for `DrawColor`. The motion property registry uses it for color properties such as `Control.BackgroundProperty` and `Control.BorderColorProperty`.

`ColorMixer` keeps the default `ValueMixer<DrawColor>` vector behavior, so `SupportsVectorOperations` is `false` and vector methods such as `Add`, `Subtract`, `Scale`, and `Magnitude` throw `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `ColorMixer()` | Initializes a new color mixer. |

## Properties

| Name | Description |
| --- | --- |
| `ValueType` | Gets `typeof(DrawColor)`. Inherited from `ValueMixer<DrawColor>`. |
| `SupportsVectorOperations` | Gets `false`; color mixing does not expose vector operations. Inherited from `ValueMixer<DrawColor>`. |

## Methods

| Name | Description |
| --- | --- |
| `Mix(DrawColor, DrawColor, float)` | Returns an interpolated `DrawColor`, preserving exact endpoints for progress outside or at the `0` to `1` range. |
| `EqualsWithinTolerance(DrawColor, DrawColor, float)` | Returns `true` when every RGBA channel differs by no more than the supplied finite, non-negative tolerance. |
| `MixUntyped(object?, object?, float)` | Casts the inputs to `DrawColor` and delegates to `Mix`. Inherited from `ValueMixer<DrawColor>`. |
| `EqualsWithinToleranceUntyped(object?, object?, float)` | Casts the inputs to `DrawColor` and delegates to `EqualsWithinTolerance`. Inherited from `ValueMixer<DrawColor>`. |
| `Add(DrawColor, DrawColor)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawColor>`. |
| `Subtract(DrawColor, DrawColor)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawColor>`. |
| `Scale(DrawColor, float)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawColor>`. |
| `Magnitude(DrawColor)` | Throws `InvalidOperationException` because vector operations are not supported. Inherited from `ValueMixer<DrawColor>`. |

## Applies to

Cerneala UI motion interpolation for `DrawColor` values.

## See also

- `Cerneala.Drawing.DrawColor`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
- `Cerneala.UI.Motion.Properties.AnimatablePropertyRegistry`
