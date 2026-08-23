# PathBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `PathBlur` filter.

```csharp
public sealed class PathBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `PathBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Path` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Speed` | `float` | `20` | Optional catalog parameter; unit: `unitless`. |
| `Taper` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `CenteredBlur` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `EndSpeed` | `float` | `20` | Optional catalog parameter; unit: `unitless`. |
| `Shape` | `string` | `Basic` | Optional catalog parameter; unit: `none`. |
| `FlashSync` | `string` | `Rear` | Optional catalog parameter; unit: `none`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `PathBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
