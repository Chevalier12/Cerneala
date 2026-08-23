# RadialBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `RadialBlur` filter.

```csharp
public sealed class RadialBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `RadialBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Mode` | `string` | `Spin` | Optional catalog parameter; unit: `none`. |
| `Amount` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |
| `Center` | `Vector4` | `0.5, 0.5` | Optional catalog parameter; unit: `unitless`. |
| `Quality` | `string` | `Good` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `RadialBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
