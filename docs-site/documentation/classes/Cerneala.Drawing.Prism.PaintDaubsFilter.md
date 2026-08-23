# PaintDaubsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PaintDaubs` filter.

```csharp
public sealed class PaintDaubsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PaintDaubsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BrushSize` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `Sharpness` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |
| `BrushType` | `string` | `Simple` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `PaintDaubs` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
