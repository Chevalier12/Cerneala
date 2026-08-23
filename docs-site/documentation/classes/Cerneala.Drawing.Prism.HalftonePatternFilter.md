# HalftonePatternFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `HalftonePattern` filter.

```csharp
public sealed class HalftonePatternFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `HalftonePatternFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Size` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `Contrast` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |
| `PatternType` | `string` | `Dot` | Optional catalog parameter; unit: `none`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `HalftonePattern` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
