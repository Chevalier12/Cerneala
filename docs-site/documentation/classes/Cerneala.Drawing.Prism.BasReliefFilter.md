# BasReliefFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `BasRelief` filter.

```csharp
public sealed class BasReliefFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `BasReliefFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Detail` | `float` | `13` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `3` | Optional catalog parameter; unit: `unitless`. |
| `LightDirection` | `string` | `BottomLeft` | Optional catalog parameter; unit: `none`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `BasRelief` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
