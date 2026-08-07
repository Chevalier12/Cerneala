# PrismLensProfileFitter Class

## Definition
Namespace: `Cerneala.Drawing.Prism.Filters`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Filters/PrismLensProfileFitter.cs`

Fits piecewise sparse polynomial lens profiles from analytically traced rays.

```csharp
public static class PrismLensProfileFitter
```

## Examples

```csharp
using System.Linq;
using System.Numerics;
using Cerneala.UI.Prism.Definitions;

PrismLensFlareRaySample sample = new(
    GhostIndex: 0,
    PupilPosition: new Vector2(0.1f, 0.1f),
    IncidenceAngleDegrees: 0,
    WavelengthNanometers: 550,
    AperturePosition: Vector2.Zero,
    SensorPosition: Vector2.Zero,
    Transmission: 1,
    RelativeRadius: 0.5f);
var raySamples = Enumerable.Repeat(sample, 6);

PrismLensProfileResource profile = PrismLensProfileFitter.Fit(
    raySamples,
    new PrismLensProfileFitOptions
    {
        RegionCount = 1,
        MaximumTermCount = 12,
        MinimumSamplesPerRegion = 6
    });
```

## Remarks

The fitter partitions each reflection path by incidence angle and applies
deterministic orthogonal matching pursuit with a shared monomial basis for all
six outputs. Coefficients are solved with ridge-regularized least squares.

Run fitting when a lens model changes, then retain the resulting immutable
profile as a resource. Do not fit profiles once per rendered frame.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Fit(IEnumerable<PrismLensFlareRaySample>, PrismLensProfileFitOptions?)` | `PrismLensProfileResource` | Fits all ghost paths and angular regions. |

## See also

- [PrismLensProfileFitOptions](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitOptions.md)
- [PrismLensFlareRaySample](Cerneala.Drawing.Prism.Filters.PrismLensFlareRaySample.md)
- [PrismLensProfileResource](Cerneala.UI.Prism.Definitions.PrismLensProfileResource.md)
