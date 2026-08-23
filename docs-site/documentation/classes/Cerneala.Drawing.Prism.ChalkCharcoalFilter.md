# ChalkCharcoalFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ChalkCharcoal` filter.

```csharp
public sealed class ChalkCharcoalFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ChalkCharcoalFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CharcoalArea` | `float` | `6` | Optional catalog parameter; unit: `unitless`. |
| `ChalkArea` | `float` | `6` | Optional catalog parameter; unit: `unitless`. |
| `StrokePressure` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ChalkCharcoal` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
