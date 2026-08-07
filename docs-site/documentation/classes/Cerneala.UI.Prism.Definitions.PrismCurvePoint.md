# PrismCurvePoint Structure

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismCurvePoint.cs`

Defines one normalized input/output control point for a Prism curve.

```csharp
public readonly record struct PrismCurvePoint
```

## Examples

```csharp
PrismCurvePoint midtone = new(0.5f, 0.65f);
```

## Remarks

Both coordinates must be finite and between zero and one. A
`PrismCurvesResource` additionally validates the ordering and endpoints of the
complete point sequence.

## Constructors

| Name | Description |
| --- | --- |
| `PrismCurvePoint(float input, float output)` | Initializes a normalized curve control point. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Input` | `float` | Gets the normalized input coordinate. |
| `Output` | `float` | Gets the normalized output coordinate. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismCurvePoint(...)` | `ArgumentOutOfRangeException` | `input` or `output` is non-finite or outside zero through one. |

## Applies to

Prism `Curves` filter resources.

## See also

- [PrismCurvesResource](Cerneala.UI.Prism.Definitions.PrismCurvesResource.md)
