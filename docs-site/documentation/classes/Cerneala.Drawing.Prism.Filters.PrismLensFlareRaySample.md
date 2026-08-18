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

The record does not validate or normalize its fields when it is constructed.
Normalization happens inside `PrismLensProfileFitter.Fit` when the fitter
builds polynomial inputs.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `GhostIndex` | `int` | Identifies the reflection path to which the ray belongs. |
| `PupilPosition` | `System.Numerics.Vector2` | Position of the ray in the normalized pupil. |
| `IncidenceAngleDegrees` | `float` | Ray incidence angle in degrees. |
| `WavelengthNanometers` | `float` | Ray wavelength in nanometers. |
| `AperturePosition` | `System.Numerics.Vector2` | Fitted aperture-plane position for the ray. |
| `SensorPosition` | `System.Numerics.Vector2` | Fitted sensor-plane position for the ray. |
| `Transmission` | `float` | Measured transmission for the ray. |
| `RelativeRadius` | `float` | Measured relative housing radius for the ray. |
| `IsValid` | `bool` | Indicates whether the ray reached the valid optical path; defaults to `true`. |

## See also

- [PrismLensProfileFitter](Cerneala.Drawing.Prism.Filters.PrismLensProfileFitter.md)
