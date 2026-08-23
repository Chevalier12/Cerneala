# BevelEmbossStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `BevelEmboss` style.

```csharp
public sealed class BevelEmbossStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `BevelEmbossStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Style` | `string` | `InnerBevel` | Optional catalog parameter; unit: `none`. |
| `Technique` | `string` | `Smooth` | Optional catalog parameter; unit: `none`. |
| `Depth` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Direction` | `string` | `Up` | Optional catalog parameter; unit: `none`. |
| `Size` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Soften` | `float` | `0` | Optional catalog parameter; unit: `dip`. |
| `UseGlobalLight` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `Angle` | `float` | `120` | Optional catalog parameter; unit: `degrees`. |
| `Altitude` | `float` | `30` | Optional catalog parameter; unit: `degrees`. |
| `GlossContour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `HighlightMode` | `string` | `Screen` | Optional catalog parameter; unit: `none`. |
| `HighlightColor` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |
| `HighlightOpacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `ShadowMode` | `string` | `Multiply` | Optional catalog parameter; unit: `none`. |
| `ShadowColor` | `Color` | `#FF000000` | Optional catalog parameter; unit: `none`. |
| `ShadowOpacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `ContourEnabled` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Contour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `ContourAntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `ContourRange` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `TextureEnabled` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Pattern` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `TextureScale` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `TextureDepth` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `TextureInvert` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `TextureLinkWithLayer` | `bool` | `True` | Optional catalog parameter; unit: `none`. |
| `TextureOffset` | `Vector4` | `0, 0` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `BevelEmboss` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
