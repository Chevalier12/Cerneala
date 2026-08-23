# GraphicPenFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `GraphicPen` filter.

```csharp
public sealed class GraphicPenFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `GraphicPenFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeLength` | `float` | `15` | Optional catalog parameter; unit: `dip`. |
| `LightDarkBalance` | `float` | `50` | Optional catalog parameter; unit: `unitless`. |
| `StrokeDirection` | `string` | `RightDiagonal` | Optional catalog parameter; unit: `none`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `GraphicPen` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
