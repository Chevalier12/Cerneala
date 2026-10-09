# TransformMixer Class

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/TransformMixer.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/TransformMixer.cs)

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

Collapse a rotated transform without losing its rotation:

```csharp
using Cerneala.UI.Motion.Interpolation;

TransformMixer mixer = new();
var visible = TransformMixer.Compose(new TransformComponents(
    7, 11, 1, 1, MathF.PI / 6, 0, 0));
var collapsed = TransformMixer.Compose(new TransformComponents(
    7, 11, 0, 0, 0, 0, 0));

var halfway = mixer.Mix(visible, collapsed, 0.5f);
// Scale is 0.5 on both axes; rotation remains 30 degrees.
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

The public `Decompose` method remains strict: it throws `InvalidOperationException` when `ScaleX` or the absolute value of `ScaleY` is less than or equal to `1e-6`. It returns a canonical component form with `SkewY` set to `0`; `Compose` still honors both `SkewX` and `SkewY` when creating a transform from `TransformComponents`.

`Mix` handles zero and near-zero scales in `Components` mode without requiring public decomposition to succeed for both endpoints:

- If exactly one endpoint fails those scale thresholds, it borrows rotation and skew from the non-degenerate endpoint. Its signed scale components are solved in that borrowed frame, and its translation is read directly. Component interpolation then proceeds normally. A rotated transform collapsing to zero scale therefore retains its rotation at interior samples.
- The borrowed frame must reproduce every field of the degenerate endpoint's linear matrix within an absolute tolerance of `1e-6`. If it cannot, the pair uses direct matrix interpolation instead; an incompatible collapse axis is one example.
- If both endpoints fail the scale thresholds, the pair also uses direct matrix interpolation. These unresolved degenerate pairs are the only implicit matrix-interpolation case in `Components` mode. Otherwise, direct matrix interpolation requires explicit `TransformInterpolationMode.Matrix`.

Resolution depends only on the endpoint pair, not on previous samples. Matrix mode always interpolates the six fields directly and does not use component resolution.

Progress values less than or equal to `0` return `from`, and values greater than or equal to `1` return `to`. `Mix`, `Decompose`, and `EqualsWithinTolerance` throw `ArgumentNullException` when passed a `null` transform. `EqualsWithinTolerance` compares each matrix component with a finite, non-negative absolute tolerance.

`TransformMixer` does not support vector operations. The vector operation members inherited from `ValueMixer<Transform>` throw `InvalidOperationException`.

`SpringSpec<Transform>` nevertheless supports this built-in mixer through a transform-specific component sampler. It resolves the endpoints into components the same way `Mix` does, springs translation X/Y, scale X/Y, rotation, and skew independently, and composes the output with `Compose`. Rotation follows the same shortest angular path as component interpolation, including when retargeted. Matrix interpolation mode does not change this spring component-space contract.

Spring position and per-component velocity remain in component space between frames. Intermediate scales can pass through zero without being re-decomposed. A zero or near-zero endpoint borrows rotation and skew from the other endpoint; on retarget, a degenerate target borrows them from the current spring state. If no shared frame reproduces both endpoints, the identity frame (no rotation or skew) is tried; if that also fails, the spring cannot run in matrix space and completes at its target immediately. Public `Decompose` stays strict. A spring completes only when every component meets both rest thresholds, then returns the exact target transform.

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

- [`Transform`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Media/Transform.cs)
- [`TransformComponents`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/TransformMixer.cs)
- [`TransformInterpolationMode`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/TransformMixer.cs)
- [`ValueMixer<T>`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/ValueMixer.cs)
- [`ValueMixerRegistry`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Motion/Interpolation/ValueMixerRegistry.cs)
