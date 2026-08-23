# AccentedEdgesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `AccentedEdges` filter.

```csharp
public sealed class AccentedEdgesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `AccentedEdgesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `EdgeWidth` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `EdgeBrightness` | `float` | `38` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `AccentedEdges` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
