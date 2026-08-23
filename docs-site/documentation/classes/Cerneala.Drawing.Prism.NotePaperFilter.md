# NotePaperFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `NotePaper` filter.

```csharp
public sealed class NotePaperFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `NotePaperFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `ImageBalance` | `float` | `25` | Optional catalog parameter; unit: `unitless`. |
| `Graininess` | `float` | `10` | Optional catalog parameter; unit: `unitless`. |
| `Relief` | `float` | `11` | Optional catalog parameter; unit: `unitless`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `NotePaper` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
