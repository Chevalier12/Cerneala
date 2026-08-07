# PrismLensProfileResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Stores pre-fitted sparse polynomial ray-transfer models used by the Prism
`LensFlare` filter.

```csharp
public sealed class PrismLensProfileResource
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
PrismLensProfileResource profile = new(
    [new PrismLensFlareGhost([region])],
    pupilGridSize: 9);
```

## Remarks

The profile is immutable and reusable. Each ghost describes one reflection
path, while each angular region stores six sparse transfer polynomials. Prism
evaluates a coarse pupil grid, rejects blocked rays, bins the resulting
triangles into screen tiles, and caches the flare texture for rendering.

Fitting is an offline operation performed by
`PrismLensProfileFitter`; rendering does not analytically trace the lens.

## Constructors

| Name | Description |
| --- | --- |
| `PrismLensProfileResource(IEnumerable<PrismLensFlareGhost> ghosts, int pupilGridSize = 8)` | Creates a profile with a pupil grid between 2 and 32 samples per dimension. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Ghosts` | `ImmutableArray<PrismLensFlareGhost>` | Gets the fitted reflection paths. |
| `PupilGridSize` | `int` | Gets the ray-grid dimension used to rasterize each ghost. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Constructor | `ArgumentException` | `ghosts` is empty. |
| Constructor | `ArgumentOutOfRangeException` | `pupilGridSize` is outside `[2, 32]`. |

## See also

- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
- [PrismLensFlareGhost](Cerneala.UI.Prism.Definitions.PrismLensFlareGhost.md)
