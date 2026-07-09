# TransformComponents Struct

## Definition
Namespace: `Cerneala.UI.Motion.Interpolation`

Assembly/Project: `Cerneala`

Source: [`UI/Motion/Interpolation/TransformMixer.cs`](../../UI/Motion/Interpolation/TransformMixer.cs)

Represents the component form used by `TransformMixer` when composing, decomposing, and interpolating two-dimensional transforms.

```csharp
public readonly record struct TransformComponents(
    float TranslationX,
    float TranslationY,
    float ScaleX,
    float ScaleY,
    float RotationRadians,
    float SkewX,
    float SkewY);
```

Inheritance:
`ValueType` -> `TransformComponents`

## Examples

Create a transform from explicit components:

```csharp
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

Transform transform = TransformMixer.Compose(new TransformComponents(
    TranslationX: 7,
    TranslationY: 11,
    ScaleX: 2,
    ScaleY: 3,
    RotationRadians: 0.75f,
    SkewX: 0,
    SkewY: 0));
```

Decompose a transform into canonical components:

```csharp
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Interpolation;

Transform transform = new(Matrix3x2.CreateTranslation(10, 20));
TransformComponents components = TransformMixer.Decompose(transform);

float x = components.TranslationX;
float y = components.TranslationY;
```

## Remarks

`TransformComponents` stores translation, scale, rotation, and skew values for affine UI transforms. `TransformMixer.Compose` consumes all seven values and composes the transform in scale, skew, rotation, then translation order.

`TransformMixer.Decompose` returns an equivalent canonical component form. The returned value preserves translation, scale, rotation, and X skew, and always sets `SkewY` to `0`. `Compose` still honors both skew axes when a caller creates `TransformComponents` directly.

Rotation and skew values are expressed in radians. The component interpolation path in `TransformMixer` linearly interpolates translation, scale, and skew values, while rotation interpolation uses the shortest angular path.

Decomposing a transform can throw `InvalidOperationException` when either scale axis is too close to zero, because the matrix cannot be represented by the component form used by `TransformMixer`.

## Constructors

| Name | Description |
| --- | --- |
| `TransformComponents(float TranslationX, float TranslationY, float ScaleX, float ScaleY, float RotationRadians, float SkewX, float SkewY)` | Initializes a component value with translation, scale, rotation, and skew fields. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `TranslationX` | `float` | Gets the X translation component. |
| `TranslationY` | `float` | Gets the Y translation component. |
| `ScaleX` | `float` | Gets the X scale component. |
| `ScaleY` | `float` | Gets the Y scale component. |
| `RotationRadians` | `float` | Gets the rotation component in radians. |
| `SkewX` | `float` | Gets the X skew component in radians. |
| `SkewY` | `float` | Gets the Y skew component in radians. |

## Applies To

Cerneala retained UI motion APIs that interpolate `Transform` values with `TransformInterpolationMode.Components`.

## See Also

- [`TransformMixer`](../../UI/Motion/Interpolation/TransformMixer.cs)
- [`TransformInterpolationMode`](../../UI/Motion/Interpolation/TransformMixer.cs)
- [`Transform`](../../UI/Media/Transform.cs)
- [`Matrix3x2`](../../UI/Media/Matrix3x2.cs)
