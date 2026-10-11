# ColoredPencilFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `ColoredPencil` filter.

```csharp
public sealed class ColoredPencilFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `ColoredPencilFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `PencilWidth` | `float` | `3` | Optional catalog parameter; unit: `dip`. |
| `StrokePressure` | `float` | `8` | Optional catalog parameter; unit: `unitless`. |
| `PaperBrightness` | `float` | `0.25` | Optional catalog parameter; unit: `unitless`. |
| `PaperColor` | `Color` | `#FFFFFFFF` | Optional catalog parameter; unit: `none`. |

## Remarks

Parameter assignments are validated against the `ColoredPencil` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new ColoredPencilFilter())`.

Each property in the table also has a public static `MotionProperty<ColoredPencilFilter, T>` field named after it, for example `PencilWidthProperty` of type `MotionProperty<ColoredPencilFilter, float>`. Motion uses these fields to animate the value.

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
