# ColorFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Color` filter.

```csharp
public sealed class ColorFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColorFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Brightness` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Contrast` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Exposure` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Saturation` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Hue` | `float` | `0` | Optional catalog parameter; unit: `degrees`. |
| `Temperature` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Tint` | `Color` | `#00000000` | Optional catalog parameter; unit: `none`. |
| `Matrix` | `string` | `Identity` | Optional catalog parameter; unit: `none`. |
| `Clamp` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `Color` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
