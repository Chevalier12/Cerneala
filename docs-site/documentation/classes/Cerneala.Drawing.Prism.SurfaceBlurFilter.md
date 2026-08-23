# SurfaceBlurFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `SurfaceBlur` filter.

```csharp
public sealed class SurfaceBlurFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `SurfaceBlurFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Radius` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Threshold` | `float` | `0.1` | Optional catalog parameter; unit: `unitless`. |
| `Quality` | `string` | `Good` | Optional catalog parameter; unit: `none`. |
| `EdgeMode` | `string` | `Clamp` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `SurfaceBlur` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
