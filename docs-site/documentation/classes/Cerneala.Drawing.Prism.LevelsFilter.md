# LevelsFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `Levels` filter.

```csharp
public sealed class LevelsFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `LevelsFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Channel` | `string` | `Composite` | Optional catalog parameter; unit: `none`. |
| `Auto` | `bool` | `False` | Optional catalog parameter; unit: `none`. |
| `InputBlack` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `InputWhite` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `Gamma` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |
| `OutputBlack` | `float` | `0` | Optional catalog parameter; unit: `unitless`. |
| `OutputWhite` | `float` | `1` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `Levels` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new LevelsFilter())`.

Each property in the table also has a public static `MotionProperty<LevelsFilter, T>` field named after it, for example `ChannelProperty` of type `MotionProperty<LevelsFilter, string>`. Motion uses these fields to animate the value.

The class inherits `FilterId`, `Visible`, `Opacity` and `BlendMode` from `PrismFilter`, and `CatalogInfo` and `Version` from `PrismOperation`. It also has the static fields `VisibleProperty`, `OpacityProperty` and `BlendModeProperty` for the inherited `Visible`, `Opacity` and `BlendMode`.

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A property setter: a `float` property is set to NaN, an infinity, or a value outside the catalog `minimum`/`maximum`; a `string` property is set to a value that is not one of the catalog symbols (the comparison is case-sensitive). |
| `ArgumentOutOfRangeException` | The inherited `Opacity` is set to NaN, an infinity, or a value outside 0 to 1. |
| `ArgumentException` | The inherited `BlendMode` is set to `PrismBlendMode.PassThrough`. |
| `ArgumentNullException` | Drawing the operation: a `string` property was set to `null`. The setter does not reject `null`; the first draw that uses the operation throws. |

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
