# PrismColorMatrixResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismColorMatrixResource.cs`

Defines the affine 4x5 RGBA transform for the Prism `ColorMatrix` filter. The current SDL_GPU renderer does not read it; see Remarks.

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

The four components of `Offset` form the fifth column of the affine 4x5 matrix. In this
model, Prism premultiplies the transformed RGB components by the transformed alpha
component after the operation. `Clamp` is a property of the `ColorMatrix` filter, not of
this resource: with `Clamp=true`, all straight RGBA components are clamped to `[0, 1]`;
with `Clamp=false`, RGB keeps the half-float extended range while alpha stays in `[0, 1]`.

Current behavior: the SDL_GPU renderer packs the `ColorMatrix` filter options without
reading the referenced resource, so it always applies the identity matrix with a zero
offset. A filter whose `Matrix` property names this resource, a filter that names a
missing resource, and a filter that omits `Matrix` therefore draw the same pixels: the
unfiltered input, apart from the `Clamp` setting. The `swapRedAndBlue` example above does not swap any channel when it is
drawn through that renderer.

## Constructors

| Name | Description |
| --- | --- |
| `PrismColorMatrixResource(Matrix4x4 matrix, Vector4 offset)` | Creates an affine RGBA transform and rejects non-finite coefficients. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Matrix` | `Matrix4x4` | Gets the 4x4 linear transform. Each row produces red, green, blue, or alpha, in that order. |
| `Offset` | `Vector4` | Gets the fifth, affine column added after the matrix transform. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentOutOfRangeException` | A `matrix` coefficient or an `offset` component is not finite. |

## Applies to

Prism `ColorMatrix` filters whose `Matrix` property refers to this resource.
