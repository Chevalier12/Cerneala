# ColoredPencilFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColoredPencil` filter.

```csharp
public sealed class ColoredPencilFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColoredPencilFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `PencilWidth` | `float` | `3` | Optional catalog parameter; unit: `dip`. |
| `StrokePressure` | `float` | `8` | Optional catalog parameter; unit: `unitless`. |
| `PaperBrightness` | `float` | `0.25` | Optional catalog parameter; unit: `unitless`. |
| `PaperColor` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ColoredPencil` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
