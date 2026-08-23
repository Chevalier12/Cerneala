# ScanlinesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Scanlines` filter.

```csharp
public sealed class ScanlinesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ScanlinesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Frequency` | `float` | `320` | Optional catalog parameter; unit: `unitless`. |
| `Thickness` | `float` | `0.5` | Optional catalog parameter; unit: `dip`. |
| `Phase` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Color` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `LineOpacity` | `float` | `0.18` | Optional catalog parameter; unit: `unitless`. |
| `Softness` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Scanlines` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
