# TransformMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/TransformMixer.cs`](../../UI/Motion/Interpolation/TransformMixer.cs)

Interpolates `Transform` values for retained UI motion.

```csharp
public sealed class TransformMixer : ValueMixer<Transform>
```

Inheritance:
`object` -> `ValueMixer<Transform>` -> `TransformMixer`

Implements:
`IValueMixer`

## Examples

Interpolate between identity and a translated transform:

```csharp
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

TransformMixer mixer = new();
Transform target = new(Matrix3x2.CreateTranslation(10, 20));

Transform halfway = mixer.Mix(Transform.Identity, target, 0.5f);
```

Use matrix interpolation explicitly:

```csharp
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

TransformMixer mixer = new(TransformInterpolationMode.Matrix);
Transform from = new(new Matrix3x2(1, 2, 3, 4, 5, 6));
Transform to = new(new Matrix3x2(11, 12, 13, 14, 15, 16));

Transform mixed = mixer.Mix(from, to, 0.5f);
```

## Remarks

`TransformMixer` is the built-in `ValueMixer<Transform>` used by the motion interpolation layer. `ValueMixerRegistry.RegisterBuiltIns` registers it for `Transform`, and `AnimatablePropertyRegistry` uses it for `UIElement.RenderTransformProperty`.

By default, the mixer uses `TransformInterpolationMode.Components`. Component interpolation decomposes each transform into translation, scale, rotation, and skew components, interpolates those components, and composes a new transform. Rotation interpolation follows the shortest angular path.

`TransformInterpolationMode.Matrix` linearly interpolates the six affine matrix fields directly. Use this mode when component decomposition is not desired.

Component decomposition rejects transforms whose `ScaleX` or `ScaleY` is too close to zero. `Decompose` returns a canonical component form with `SkewY` set to `0`; `Compose` still honors both `SkewX` and `SkewY` when creating a transform from `TransformComponents`.

Progress values less than or equal to `0` return `from`, and values greater than or equal to `1` return `to`. `Mix`, `Decompose`, and `EqualsWithinTolerance` throw `ArgumentNullException` when passed a `null` transform. `EqualsWithinTolerance` compares each matrix component with a finite, non-negative absolute tolerance.

`TransformMixer` does not support vector operations. The vector operation members inherited from `ValueMixer<Transform>` throw `InvalidOperationException`.

## Constructors

| Name | Description |
| --- | --- |
| `TransformMixer(TransformInterpolationMode mode = TransformInterpolationMode.Components)` | Initializes a new `TransformMixer` that uses component interpolation by default or direct matrix interpolation when `mode` is `Matrix`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SupportsVectorOperations` | `bool` | Gets `false`, indicating that arithmetic and magnitude operations are not supported for `Transform`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Compose(TransformComponents components)` | `Transform` | Creates a transform by composing scale, skew, rotation, and translation components. |
| `Decompose(Transform transform)` | `TransformComponents` | Decomposes a transform into canonical components with `SkewY` set to `0`. |
| `EqualsWithinTolerance(Transform left, Transform right, float tolerance)` | `bool` | Returns whether all six matrix fields differ by no more than `tolerance`. |
| `Mix(Transform from, Transform to, float progress)` | `Transform` | Returns an interpolated transform, using the configured interpolation mode and exact endpoint clamping at `0` and `1`. |

## Applies To

Cerneala retained UI motion APIs that animate `Transform` values, especially `UIElement.RenderTransformProperty`.

## See Also

- [`Transform`](../../UI/Media/Transform.cs)
- [`TransformComponents`](../../UI/Motion/Interpolation/TransformMixer.cs)
- [`TransformInterpolationMode`](../../UI/Motion/Interpolation/TransformMixer.cs)
- [`ValueMixer<T>`](../../UI/Motion/Interpolation/ValueMixer.cs)
- [`ValueMixerRegistry`](../../UI/Motion/Interpolation/ValueMixerRegistry.cs)
