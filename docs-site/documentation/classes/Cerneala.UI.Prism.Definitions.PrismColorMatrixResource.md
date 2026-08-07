# PrismColorMatrixResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismColorMatrixResource.cs`

Defines the affine 4x5 RGBA transform consumed by the Prism `ColorMatrix` filter.

```csharp
public sealed class PrismColorMatrixResource
```

## Examples

The following matrix swaps the red and blue channels while preserving green and alpha:

```csharp
PrismColorMatrixResource swapRedAndBlue = new(
    new Matrix4x4(
        0, 0, 1, 0,
        0, 1, 0, 0,
        1, 0, 0, 0,
        0, 0, 0, 1),
    Vector4.Zero);
```

## Remarks

The filter evaluates unassociated linear-sRGB values using the W3C row convention:

```text
[R', G', B', A']T = Matrix * [R, G, B, A]T + Offset
```

The four components of `Offset` form the fifth column of the affine 4x5 matrix. Prism
premultiplies the transformed RGB components by the transformed alpha component after
the operation. With `Clamp=true`, all straight RGBA components are clamped to `[0, 1]`.
With `Clamp=false`, RGB retains the half-float extended range while alpha remains bounded
to `[0, 1]`.

The resource identity and version participate in retained draw dependencies. Omitting the
optional `Matrix` resource applies the identity transform; naming a missing resource falls
back to the unfiltered input.

## Constructors

| Name | Description |
| --- | --- |
| `PrismColorMatrixResource(Matrix4x4 matrix, Vector4 offset)` | Creates an affine RGBA transform and rejects non-finite coefficients. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Matrix` | `Matrix4x4` | Gets the 4x4 linear transform. Each row produces red, green, blue, or alpha, in that order. |
| `Offset` | `Vector4` | Gets the fifth, affine column added after the matrix transform. |

## Applies to

Prism `ColorMatrix` filters whose `Matrix` property refers to this resource.
