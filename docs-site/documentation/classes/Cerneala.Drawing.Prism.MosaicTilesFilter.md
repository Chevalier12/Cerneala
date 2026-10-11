# MosaicTilesFilter Class

## Definition

Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`

Provides the typed code API for the Prism `MosaicTiles` filter.

```csharp
public sealed class MosaicTilesFilter : PrismFilter
```

## Constructors

| Signature | Description |
| --- | --- |
| `MosaicTilesFilter()` | Creates the operation with Prism catalog defaults. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `TileSize` | `float` | `12` | Optional catalog parameter; unit: `dip`. |
| `GroutWidth` | `float` | `2` | Optional catalog parameter; unit: `dip`. |
| `LightenGrout` | `float` | `9` | Optional catalog parameter; unit: `unitless`. |

## Remarks

Parameter assignments are validated against the `MosaicTiles` catalog entry. The accepted symbols and numeric bounds of each parameter are, in that entry of `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json`, the parameter's `symbols` field and the `minimum` and `maximum` fields of its `domain` object.

Add the operation to a `PrismPipeline`, or pass it to `Prism.Apply` together with the source image: `Prism.Apply(source, new MosaicTilesFilter())`.

Each property in the table also has a public static `MotionProperty<MosaicTilesFilter, T>` field named after it, for example `TileSizeProperty` of type `MotionProperty<MosaicTilesFilter, float>`. Motion uses these fields to animate the value.

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
