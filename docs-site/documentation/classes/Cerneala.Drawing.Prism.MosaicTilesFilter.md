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

Parameter assignments are validated against the `MosaicTiles` catalog entry. Add the operation to a `PrismPipeline` or pass it directly to `Prism.Apply`.

## See Also

- `PrismFilter`
- `PrismPipeline`
- `Prism.Apply`
