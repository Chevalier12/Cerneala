# PrismGradientMapPoint Structure

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismGradientMapResource.cs`

Defines one normalized point in a GradientMap transfer function.

```csharp
public readonly record struct PrismGradientMapPoint
```

## Constructors

| Name | Description |
| --- | --- |
| `PrismGradientMapPoint(float offset, Vector3 linearSrgb)` | Creates a point with a normalized offset and finite linear-sRGB color. |
| `PrismGradientMapPoint(float offset, Vector3 linearSrgb, float alpha)` | Creates a point with a normalized offset, finite linear-sRGB color, and alpha in the range zero through one. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Offset` | `float` | Gets the normalized LUT coordinate. |
| `LinearSrgb` | `Vector3` | Gets the straight linear-sRGB output color. |
| `Alpha` | `float` | Gets the straight alpha value. |

## Examples
```csharp
PrismGradientMapPoint transparentRed = new(
    1f,
    Vector3.UnitX,
    0f);
```

## Remarks
Adjacent points may use the same offset to define a hard stop. Gradient
interpolation associates the color channels with `Alpha` before blending,
which prevents hidden RGB values in transparent points from producing halos.
