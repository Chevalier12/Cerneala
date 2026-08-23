# SumiEFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SumiE` filter.

```csharp
public sealed class SumiEFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SumiEFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeWidth` | `float` | `10` | Optional catalog parameter; unit: `dip`. |
| `StrokePressure` | `float` | `2` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `2` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `SumiE` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
