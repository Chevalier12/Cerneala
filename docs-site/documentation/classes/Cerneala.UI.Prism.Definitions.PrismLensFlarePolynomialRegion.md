# PrismLensFlarePolynomialRegion Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Stores six sparse ray-transfer polynomials for an incidence-angle interval.

```csharp
public sealed class PrismLensFlarePolynomialRegion
```

## Examples

```csharp
using Cerneala.UI.Prism.Definitions;

PrismSparsePolynomial constant = new(
    [new PrismSparsePolynomialTerm(1, 0, 0, 0, 0, 0, 0)]);
PrismLensFlarePolynomialRegion region = new(
    0, 15,
    constant, constant,
    constant, constant,
    constant, constant);
```

## Remarks

`ApertureX` and `ApertureY` reproduce aperture blocking. `SensorX` and
`SensorY` place the ghost on the sensor. `Transmission` carries absorption and
coating loss, and `RelativeRadius` rejects rays blocked by the lens housing.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `MinimumIncidenceAngleDegrees` | `float` | Gets the inclusive lower angle bound. |
| `MaximumIncidenceAngleDegrees` | `float` | Gets the exclusive upper angle bound. |
| `ApertureX`, `ApertureY` | `PrismSparsePolynomial` | Get the aperture-plane coordinates. |
| `SensorX`, `SensorY` | `PrismSparsePolynomial` | Get the sensor-plane coordinates. |
| `Transmission` | `PrismSparsePolynomial` | Gets accumulated ray transmission. |
| `RelativeRadius` | `PrismSparsePolynomial` | Gets the normalized housing radius. |

## See also

- [PrismSparsePolynomial](Cerneala.UI.Prism.Definitions.PrismSparsePolynomial.md)
- [PrismLensFlareGhost](Cerneala.UI.Prism.Definitions.PrismLensFlareGhost.md)
