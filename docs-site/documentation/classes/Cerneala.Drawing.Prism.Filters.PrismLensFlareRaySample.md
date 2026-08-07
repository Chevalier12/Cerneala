# PrismLensFlareRaySample Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Filters`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Filters/PrismLensProfileFitter.cs`

Contains one analytically traced ray used for lens-profile fitting.

```csharp
public readonly record struct PrismLensFlareRaySample(
    int GhostIndex,
    Vector2 PupilPosition,
    float IncidenceAngleDegrees,
    float WavelengthNanometers,
    Vector2 AperturePosition,
    Vector2 SensorPosition,
    float Transmission,
    float RelativeRadius,
    bool IsValid = true);
```

## Examples

```csharp
using System.Numerics;

PrismLensFlareRaySample sample = new(
    ghostIndex: 0,
    pupilPosition: new Vector2(0.2f, -0.1f),
    incidenceAngleDegrees: 15,
    wavelengthNanometers: 550,
    aperturePosition: new Vector2(0.18f, -0.08f),
    sensorPosition: new Vector2(-0.3f, 0.04f),
    transmission: 0.42f,
    relativeRadius: 0.6f);
```

## Remarks

Invalid rays remain useful training samples: the fitter maps their
transmission to zero and their relative radius outside the valid housing
interval so the runtime can reproduce blocking boundaries.

## See also

- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
