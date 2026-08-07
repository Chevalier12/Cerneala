# PrismLensFlareGhost Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Stores the piecewise ray-transfer model for one lens reflection path.

```csharp
public sealed class PrismLensFlareGhost
```

## Examples

```csharp
using Cerneala.UI.Prism.Definitions;

PrismSparsePolynomial constant = new(
    [new PrismSparsePolynomialTerm(1, 0, 0, 0, 0, 0, 0)]);
PrismLensFlarePolynomialRegion nearAxisRegion = new(
    0, 15, constant, constant, constant, constant, constant, constant);
PrismLensFlarePolynomialRegion edgeRegion = new(
    15, 30, constant, constant, constant, constant, constant, constant);
PrismLensFlareGhost ghost = new([nearAxisRegion, edgeRegion]);
```

## Remarks

Regions are sorted by minimum incidence angle. Their angle intervals must not
overlap. At render time Prism selects the containing region, or the nearest
region when the light angle falls outside the fitted range.

## Constructors

| Name | Description |
| --- | --- |
| `PrismLensFlareGhost(IEnumerable<PrismLensFlarePolynomialRegion> regions)` | Creates a ghost from one or more non-overlapping angular regions. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Regions` | `ImmutableArray<PrismLensFlarePolynomialRegion>` | Gets the ordered fitting regions. |

## See also

- [PrismLensProfileResource](Cerneala.UI.Prism.Definitions.PrismLensProfileResource.md)
- [PrismLensFlarePolynomialRegion](Cerneala.UI.Prism.Definitions.PrismLensFlarePolynomialRegion.md)
