# SmudgeStickFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SmudgeStick` filter.

```csharp
public sealed class SmudgeStickFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SmudgeStickFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeLength` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `HighlightArea` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Intensity` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `SmudgeStick` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
