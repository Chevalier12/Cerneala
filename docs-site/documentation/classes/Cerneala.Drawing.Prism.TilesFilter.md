# TilesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Tiles` filter.

```csharp
public sealed class TilesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `TilesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Tiles` | `float` | `10` | Optional catalog parameter; unit: `count`. |
| `MaximumOffset` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |
| `Fill` | `string` | `Background` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#00000000` | Optional catalog parameter; unit: `none`. |
| `Seed` | `int` | `0` | Optional catalog parameter; unit: `count`. |

## Remarks

Parameter assignments are validated against the `Tiles` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
