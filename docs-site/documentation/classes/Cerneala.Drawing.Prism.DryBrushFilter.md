# DryBrushFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DryBrush` filter.

```csharp
public sealed class DryBrushFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `DryBrushFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BrushSize` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `BrushDetail` | `float` | `8` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `DryBrush` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
