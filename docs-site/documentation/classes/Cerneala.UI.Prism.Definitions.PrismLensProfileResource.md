# PrismLensProfileResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Stores pre-fitted sparse polynomial ray-transfer models for the Prism
`LensFlare` filter. The current SDL_GPU renderer does not read it; see Remarks.

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
path, while each angular region stores six sparse transfer polynomials.

Current behavior: the SDL_GPU renderer resolves the resource reference of the
`LensFlare` filter only as an image, so it never reads a `PrismLensProfileResource`.
The pupil-grid evaluation, blocked-ray rejection, and tile binning exist only in a
CPU reference renderer that no rendering path calls.

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
| Constructor | `ArgumentNullException` | `ghosts` is `null`. |
| Constructor | `ArgumentException` | `ghosts` is empty. |
| Constructor | `ArgumentOutOfRangeException` | `pupilGridSize` is outside `[2, 32]`. |

## See also

- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
- [PrismLensFlareGhost](Cerneala.UI.Prism.Definitions.PrismLensFlareGhost.md)
