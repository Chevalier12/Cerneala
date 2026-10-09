# DropShadowStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `DropShadow` style.

```csharp
public sealed class DropShadowStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `DropShadowStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Multiply` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `UseGlobalLight` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `Angle` | `float` | `120` | Optional catalog parameter; unit: `degrees`. |
| `Distance` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Spread` | `float` | `0` | Nonnegative coverage spread in DIPs. No fixed 32-device-pixel ceiling. |
| `Size` | `float` | `5` | Nonnegative blur size in DIPs. No fixed 32-device-pixel ceiling. |
| `Contour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `LayerKnocksOut` | `bool` | `True` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `DropShadow` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

Spatial parameters follow the owning element's transform and pixel scale. The raster blur radius is `max(ceil(Size * spatialScale * 1.5), 1)` device pixels; the spread radius is `ceil(Spread * spatialScale)` when the scaled spread is at least half a device pixel. Bounds reserve these requested radii and the shadow offset.

On SDL GPU, each blur or spread radius above 32 device pixels is evaluated at reduced resolution rather than silently clamped. Raster planning chooses the integer factor `k = ceil(radius / 32)`, box-filters source coverage down by `k`, applies the bounded horizontal and vertical kernels at `radius / k`, then reconstructs coverage bilinearly in the original raster domain. This is a filtered approximation at the requested scale; it does not replace a large shadow with a 32-pixel shadow or omit the operation. Blur and spread choose their factors independently. Radii at or below 32 retain the existing full-resolution path.

The absence of a fixed radius ceiling does not remove the renderer's surface-allocation and resource limits.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
