# FieldBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `FieldBlur` filter.

```csharp
public sealed class FieldBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `FieldBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlurField` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Blur` | `float` | `8` | Optional catalog parameter; unit: `dip`. |
| `FocalDistance` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Invert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Quality` | `string` | `Best` | Optional catalog parameter; unit: `none`. |
| `Highlight` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `FieldBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
