# DustScratchesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DustScratches` filter.

```csharp
public sealed class DustScratchesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DustScratchesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `Threshold` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `DustScratches` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
