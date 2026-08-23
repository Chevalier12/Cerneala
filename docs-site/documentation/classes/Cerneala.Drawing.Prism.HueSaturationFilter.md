# HueSaturationFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `HueSaturation` filter.

```csharp
public sealed class HueSaturationFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `HueSaturationFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Channel` | `string` | `Master` | Optional catalog parameter; unit: `none`. |
| `Hue` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Saturation` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Lightness` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Colorize` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `HueSaturation` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
