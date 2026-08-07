# PrismSparsePolynomialTerm Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Defines one coefficient and exponent tuple in a sparse lens polynomial.

```csharp
public readonly record struct PrismSparsePolynomialTerm
```

## Examples

```csharp
PrismSparsePolynomialTerm pupilX =
    new(0.3f, 1, 0, 0, 0, 0, 0);
```

## Remarks

The exponent order is pupil X, pupil Y, radius, inverse radius, normalized
incidence angle, and normalized wavelength. Every exponent must be in
`[0, 2]`, and the coefficient must be finite.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Coefficient` | `float` | Gets the monomial coefficient. |
| `PupilXExponent`, `PupilYExponent` | `byte` | Get the pupil-coordinate exponents. |
| `RadiusExponent`, `InverseRadiusExponent` | `byte` | Get the radial exponents. |
| `IncidenceAngleExponent` | `byte` | Gets the angle exponent. |
| `WavelengthExponent` | `byte` | Gets the wavelength exponent. |

## See also

- [PrismSparsePolynomial](Cerneala.UI.Prism.Definitions.PrismSparsePolynomial.md)
