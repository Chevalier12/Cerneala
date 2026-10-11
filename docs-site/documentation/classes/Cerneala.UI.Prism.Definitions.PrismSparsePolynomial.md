# PrismSparsePolynomial Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismLensProfileResource.cs`

Represents a sparse polynomial over six normalized lens-ray inputs.

```csharp
public sealed class PrismSparsePolynomial
```

## Examples

```csharp
using System.Numerics;
using Cerneala.UI.Prism.Definitions;

PrismSparsePolynomial sensorX = new(
[
    new PrismSparsePolynomialTerm(0.3f, 1, 0, 0, 0, 0, 0),
    new PrismSparsePolynomialTerm(0.05f, 0, 0, 0, 0, 0, 1)
]);
PrismLensFlarePolynomialInput input = new(
    new Vector2(0.2f, -0.1f),
    0.224f,
    0.776f,
    0.25f,
    0.5f);
float value = sensorX.Evaluate(input);
```

## Remarks

Each exponent is limited to degree two. Terms are evaluated over pupil X,
pupil Y, pupil radius, inverse radius, incidence angle, and wavelength.

## Members

| Name | Type | Description |
| --- | --- | --- |
| `PrismSparsePolynomial(IEnumerable<PrismSparsePolynomialTerm> terms)` | constructor | Copies `terms`. Throws `ArgumentNullException` when `terms` is `null` and `ArgumentException` when it is empty. |
| `Terms` | `ImmutableArray<PrismSparsePolynomialTerm>` | Gets the terms in the order supplied. Terms with a zero coefficient are kept. |
| `Evaluate(PrismLensFlarePolynomialInput)` | `float` | Evaluates the polynomial for normalized inputs. |

## See also

- [PrismSparsePolynomialTerm](Cerneala.UI.Prism.Definitions.PrismSparsePolynomialTerm.md)
- [PrismLensFlarePolynomialInput](Cerneala.UI.Prism.Definitions.PrismLensFlarePolynomialInput.md)
