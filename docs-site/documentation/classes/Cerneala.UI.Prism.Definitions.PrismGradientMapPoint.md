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
| `PrismGradientMapPoint(float offset, Vector3 linearSrgb)` | Creates an opaque point (`Alpha = 1`) with an offset and color channels in the range zero through one. |
| `PrismGradientMapPoint(float offset, Vector3 linearSrgb, float alpha)` | Creates a point with an offset, color channels, and alpha in the range zero through one. |

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
Adjacent points may use the same offset to define a hard stop. The
`GradientOverlay` lookup table multiplies the color of each point by its `Alpha`
before it interpolates, which prevents hidden RGB values in transparent points
from producing halos. The CPU reference `GradientMap` transfer function interpolates only
`LinearSrgb` and does not read `Alpha`.

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructors | `ArgumentOutOfRangeException` | `offset` is not finite or is outside `[0, 1]`; a `linearSrgb` channel is not finite or is outside `[0, 1]`; or `alpha` is not finite or is outside `[0, 1]`. |
