# PrismCurvesResource Class

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismCurvePoint.cs`

Defines immutable composite and per-channel control points for the Prism
`Curves` filter.

```csharp
public sealed class PrismCurvesResource
```

## Examples

```csharp
PrismCurvesResource curves = new(
    composite:
    [
        new PrismCurvePoint(0, 0),
        new PrismCurvePoint(0.5f, 0.62f),
        new PrismCurvePoint(1, 1)
    ],
    red:
    [
        new PrismCurvePoint(0, 0),
        new PrismCurvePoint(1, 0.9f)
    ]);
```

## Remarks

Each channel requires at least two points, begins at input zero, ends at input
one, and has strictly increasing inputs. An omitted channel uses the identity
curve.

Prism compiles the four point sets into one 1024-sample RGB lookup texture.
Each color channel is evaluated first and the composite curve is applied to
that result. Evaluation occurs in linear sRGB while preserving source alpha.

## Constructors

| Name | Description |
| --- | --- |
| `PrismCurvesResource(IEnumerable<PrismCurvePoint>? composite = null, IEnumerable<PrismCurvePoint>? red = null, IEnumerable<PrismCurvePoint>? green = null, IEnumerable<PrismCurvePoint>? blue = null)` | Initializes immutable composite, red, green, and blue point sets. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Composite` | `ImmutableArray<PrismCurvePoint>` | Gets the composite curve applied after each channel curve. |
| `Red` | `ImmutableArray<PrismCurvePoint>` | Gets the red channel curve. |
| `Green` | `ImmutableArray<PrismCurvePoint>` | Gets the green channel curve. |
| `Blue` | `ImmutableArray<PrismCurvePoint>` | Gets the blue channel curve. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismCurvesResource(...)` | `ArgumentException` | A curve has fewer than two points, lacks the required input endpoints, or has inputs that are not strictly increasing. |

## Applies to

Versioned resources referenced by the required `Curves` property of the Prism
`Curves` filter.

## See also

- [PrismCurvePoint](Cerneala.UI.Prism.Definitions.PrismCurvePoint.md)
- [PrismResourceId](Cerneala.UI.Prism.Definitions.PrismResourceId.md)
