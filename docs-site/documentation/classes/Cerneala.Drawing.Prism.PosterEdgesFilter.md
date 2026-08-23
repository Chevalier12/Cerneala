# PosterEdgesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PosterEdges` filter.

```csharp
public sealed class PosterEdgesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PosterEdgesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `EdgeThickness` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `EdgeIntensity` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Posterization` | `float` | `2` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `PosterEdges` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
