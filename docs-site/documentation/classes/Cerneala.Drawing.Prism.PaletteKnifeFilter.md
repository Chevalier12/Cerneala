# PaletteKnifeFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PaletteKnife` filter.

```csharp
public sealed class PaletteKnifeFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PaletteKnifeFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeSize` | `float` | `3` | Optional catalog parameter; unit: `dip`. |
| `StrokeDetail` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Softness` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `PaletteKnife` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
