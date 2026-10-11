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

Parameter assignments are validated against the `BevelEmboss` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`. `Vector4` is `System.Numerics.Vector4`. A `Vector4` default written with two numbers, such as `0, 0`, sets X and Y; Z and W are 0.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new BevelEmbossStyle())`.

Each property in the table also has a public static `MotionProperty<BevelEmbossStyle, T>` field named after it, for example `StyleProperty` of type `MotionProperty<BevelEmbossStyle, string>`. Motion uses these fields to animate the value.

The class inherits `StyleId` and `Visible` from `PrismStyle`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static field `VisibleProperty` for the inherited `Visible`.

Highlight and shadow contributions are analyzed independently. A visible contribution
with positive opacity and a non-`Normal` blend mode requires the backdrop; a zero-opacity
contribution does not. Changing either contribution's opacity or mode updates the frame
requirement through the instance's value version.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `Vector4` property gets a NaN or infinite component; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
