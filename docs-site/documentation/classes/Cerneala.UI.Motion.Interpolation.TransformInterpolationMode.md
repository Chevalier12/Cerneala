# TransformInterpolationMode Enum

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Interpolation/TransformInterpolationMode.cs`

Specifies how `TransformMixer` interpolates between two transforms.

```csharp
public enum TransformInterpolationMode
```

## Examples

Mix halfway between no rotation and a 180-degree rotation in both modes:

```csharp
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

Transform from = Transform.Identity;
Transform to = new(Matrix3x2.CreateRotation(MathF.PI));

Matrix3x2 components = new TransformMixer().Mix(from, to, 0.5f).Matrix;
Matrix3x2 matrix = new TransformMixer(TransformInterpolationMode.Matrix).Mix(from, to, 0.5f).Matrix;

// components: M11 = 0, M12 = -1, M21 = 1, M22 = 0  (a 90-degree rotation)
// matrix:     M11 = 0, M12 = 0,  M21 = 0, M22 = 0  (the element collapses to a point)
```

## Remarks

Pass the mode to the `TransformMixer` constructor. The default is `Components`. The built-in mixer registered by `ValueMixerRegistry.RegisterBuiltIns` uses `Components`. To use `Matrix` for a motion system, register a replacement: `root.Motion.Mixers.Register(new TransformMixer(TransformInterpolationMode.Matrix))`.

`Components` splits each transform into translation, scale, rotation, and skew (a `TransformComponents` value), interpolates each part, and builds a new matrix. When a transform cannot be split into parts, the mixer falls back to matrix interpolation. `Matrix` always interpolates each matrix element directly, which can shrink a rotating element, as the example shows.

The mode affects tweens and other mixer-based interpolation. A `SpringSpec<Transform>` uses its own component-based spring sampler and ignores the mode.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Components` | `0` | Interpolates translation, scale, rotation, and skew separately. The default. |
| `Matrix` | `1` | Interpolates each matrix element directly. |

## Applies to

Cerneala transform motion.

## See also

- `Cerneala.UI.Motion.Interpolation.TransformMixer`
- `Cerneala.UI.Motion.Interpolation.ValueMixerRegistry`
- `Cerneala.UI.Media.Transform`
