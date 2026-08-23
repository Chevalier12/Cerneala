# SharpenEdgesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SharpenEdges` filter.

```csharp
public sealed class SharpenEdgesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SharpenEdgesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Amount` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Threshold` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `SharpenEdges` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
