# PrismLensFlarePolynomialInput Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Contains the normalized inputs consumed by a fitted lens polynomial.

```csharp
public readonly record struct PrismLensFlarePolynomialInput(
    Vector2 PupilPosition,
    float Radius,
    float InverseRadius,
    float NormalizedIncidenceAngle,
    float NormalizedWavelength);
```

## Examples

```csharp
using System.Numerics;

PrismLensFlarePolynomialInput input = new(
    new Vector2(0.2f, -0.1f),
    0.224f,
    0.776f,
    0.25f,
    0.5f);
```

## Remarks

The fitter maps angles from 0 to 60 degrees into `[0, 1]` and wavelengths
around 550 nm into `[-1, 1]`. `InverseRadius` is `1 - Radius`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PupilPosition` | `Vector2` | Gets the normalized pupil coordinate. |
| `Radius` | `float` | Gets the clamped pupil radius. |
| `InverseRadius` | `float` | Gets one minus the pupil radius. |
| `NormalizedIncidenceAngle` | `float` | Gets the normalized field angle. |
| `NormalizedWavelength` | `float` | Gets the normalized wavelength. |
