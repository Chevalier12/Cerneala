# CustomConvolutionFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `CustomConvolution` filter.

```csharp
public sealed class CustomConvolutionFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `CustomConvolutionFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Kernel` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Scale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Offset` | `float` | `0` | Optional catalog parameter; unit: `dip`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |
| `AffectAlpha` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `CustomConvolution` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
