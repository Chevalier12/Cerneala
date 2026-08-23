# GradientOverlayStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `GradientOverlay` style.

```csharp
public sealed class GradientOverlayStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `GradientOverlayStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Normal` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Gradient` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Method` | `string` | `Perceptual` | Optional catalog parameter; unit: `none`. |
| `Style` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `Angle` | `float` | `90` | Optional catalog parameter; unit: `degrees`. |
| `Scale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `AlignWithLayer` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `Reverse` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Dither` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Offset` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `GradientOverlay` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
