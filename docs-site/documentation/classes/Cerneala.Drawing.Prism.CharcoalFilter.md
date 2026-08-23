# CharcoalFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Charcoal` filter.

```csharp
public sealed class CharcoalFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `CharcoalFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `CharcoalThickness` | `float` | `1` | Optional catalog parameter; unit: `dip`. |
| `Detail` | `float` | `5` | Optional catalog parameter; unit: `unitless`. |
| `LightDarkBalance` | `float` | `50` | Optional catalog parameter; unit: `unitless`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Charcoal` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
