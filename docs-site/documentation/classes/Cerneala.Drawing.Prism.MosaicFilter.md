# MosaicFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Mosaic` filter.

```csharp
public sealed class MosaicFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `MosaicFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CellSize` | `Vector4` | `10, 10` | Optional catalog parameter; unit: `dip`. |
| `PreserveEdges` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Mosaic` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
