# InnerGlowStyle Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `InnerGlow` style.

```csharp
public sealed class InnerGlowStyle : PrismStyle
```

## Constructors

| Signature | Description |
| --- | --- |
| `InnerGlowStyle()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `BlendMode` | `string` | `Screen` | Optional catalog parameter; unit: `none`. |
| `Color` | `Color` | `#FFFFFFBE` | Optional catalog parameter; unit: `none`. |
| `Gradient` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `Opacity` | `float` | `0.75` | Optional catalog parameter; unit: `unitless`. |
| `Noise` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Technique` | `string` | `Softer` | Optional catalog parameter; unit: `none`. |
| `Origin` | `string` | `Edge` | Optional catalog parameter; unit: `none`. |
| `Choke` | `float` | `0` | Optional catalog parameter; unit: `dip`. |
| `Size` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `Contour` | `string` | `Linear` | Optional catalog parameter; unit: `none`. |
| `AntiAlias` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `Range` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Jitter` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `InnerGlow` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new InnerGlowStyle())`.

Each property in the table also has a public static `MotionProperty<InnerGlowStyle, T>` field named after it, for example `BlendModeProperty` of type `MotionProperty<InnerGlowStyle, string>`. Motion uses these fields to animate the value.

The class inherits `StyleId` and `Visible` from `PrismStyle`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static field `VisibleProperty` for the inherited `Visible`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismStyle`
- `PrismPipeline`
- `Prism.Apply`
