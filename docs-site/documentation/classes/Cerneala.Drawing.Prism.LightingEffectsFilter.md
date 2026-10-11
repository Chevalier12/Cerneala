# LightingEffectsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `LightingEffects` filter.

```csharp
public sealed class LightingEffectsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LightingEffectsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Lights` | `PrismResourceId` | `—` | Required catalog parameter; unit: `none`. |
| `Ambient` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Metallic` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Gloss` | `float` | `0.5` | Optional catalog parameter; unit: `unitless`. |
| `Exposure` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `Texture` | `PrismResourceId` | `—` | Optional catalog parameter; unit: `none`. |
| `TextureHeight` | `float` | `0` | Optional catalog parameter; unit: `dip`. |

## Remarks

Parameter assignments are validated against the `LightingEffects` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

`PrismResourceId` is `Cerneala.UI.Prism.Definitions.PrismResourceId`.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new LightingEffectsFilter())`.

Each property in the table also has a public static `MotionProperty<LightingEffectsFilter, T>` field named after it, for example `LightsProperty` of type `MotionProperty<LightingEffectsFilter, PrismResourceId>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`. |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentOutOfRangeException` | Drawing the operation: the required `Lights` property was set to `default(PrismResourceId)`. The setter accepts that value; the first draw that uses the operation throws. A `Lights` that was never set does not throw. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
