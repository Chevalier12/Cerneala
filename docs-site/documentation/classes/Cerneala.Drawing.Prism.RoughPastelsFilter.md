# RoughPastelsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `RoughPastels` filter.

```csharp
public sealed class RoughPastelsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `RoughPastelsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `StrokeLength` | `float` | `6` | Optional catalog parameter; unit: `dip`. |
| `StrokeDetail` | `float` | `4` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `string` | `Canvas` | Optional catalog parameter; unit: `none`. |
| `Scaling` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Relief` | `float` | `0.2` | Optional catalog parameter; unit: `unitless`. |
| `LightDirection` | `string` | `Top` | Optional catalog parameter; unit: `none`. |
| `Invert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `RoughPastels` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
