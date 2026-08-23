# PlasterFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Plaster` filter.

```csharp
public sealed class PlasterFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PlasterFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `ImageBalance` | `float` | `20` | Optional catalog parameter; unit: `unitless`. |
| `Smoothness` | `float` | `2` | Optional catalog parameter; unit: `unitless`. |
| `LightDirection` | `string` | `TopLeft` | Optional catalog parameter; unit: `none`. |
| `Foreground` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Background` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Plaster` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
