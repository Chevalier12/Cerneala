# NeonGlowFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `NeonGlow` filter.

```csharp
public sealed class NeonGlowFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `NeonGlowFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `GlowSize` | `float` | `5` | Optional catalog parameter; unit: `dip`. |
| `GlowBrightness` | `float` | `15` | Optional catalog parameter; unit: `unitless`. |
| `GlowColor` | `Color` | `#FF00FFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `NeonGlow` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new NeonGlowFilter())`.

Each property in the table also has a public static `MotionProperty<NeonGlowFilter, T>` field named after it, for example `GlowSizeProperty` of type `MotionProperty<NeonGlowFilter, float>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`. |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
