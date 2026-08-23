# WaterPaperFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `WaterPaper` filter.

```csharp
public sealed class WaterPaperFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `WaterPaperFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `FiberLength` | `float` | `15` | Optional catalog parameter; unit: `dip`. |
| `Brightness` | `float` | `60` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `80` | Optional catalog parameter; unit: `unitless`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `WaterPaper` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
