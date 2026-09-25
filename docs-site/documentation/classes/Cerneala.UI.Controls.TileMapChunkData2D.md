# TileMapChunkData2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileMapSource2D.cs`

Contains either one grid chunk with its used tile definitions or an ordered sequence of free tile placements.

```csharp
public sealed class TileMapChunkData2D
```

## Examples

```csharp
var grid = new TileChunk2D(
    new TileCoordinate2D(0, 0),
    2,
    1,
    [new TileCell2D(1), default]);
var terrain = new TileSet2D(
    "Terrain",
    new ResourceId<ImageResource>("TerrainAtlas"),
    [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))]);
var data = new TileMapChunkData2D(grid, [terrain]);

bool found = data.TryResolveTile(1, out TileSet2D? set, out TileDefinition2D? definition);
```

## Remarks

The constructors select exclusive representations. A grid payload has a non-null `Grid`, a read-only copy of the supplied `TileSets` sequence, and no `Placements`. A placement payload has a null `Grid`, no `TileSets`, and a read-only copy of the placement sequence in its original order. These are bounded shallow copies: the referenced grid, sets, tiles, images, descriptors, and opaque application metadata are not deep-cloned.

For a grid payload, each nonzero cell ID must resolve to exactly one supplied definition. Unused definitions, duplicate IDs, null or empty tilesets, and missing definitions are rejected. An all-empty grid uses an empty palette. The sequence is limited to 4,096 tilesets. The placement constructor accepts at most 1,048,576 non-null placements.

This is a public data value, not a scene node, image owner, disposable acquisition, or map-loading API. Constructing one does not install it in a `TileMap2D` or give an application a public streaming source. For a complete in-memory map, use [TileMap2D.FromModel](Cerneala.UI.Controls.TileMap2D.md) with a [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md). Framework-owned package streaming is exposed through [Scene2DPackageLevel.CreateTileMap](Cerneala.Scene2D.Packages.Scene2DPackageLevel.md), not through this type.

## Constructors

| Name | Description |
| --- | --- |
| `TileMapChunkData2D(TileChunk2D grid, IEnumerable<TileSet2D> tileSets)` | Retains the grid reference and copies its used palette references. |
| `TileMapChunkData2D(IEnumerable<Tile> placements)` | Copies ordered placement references into a read-only collection. |

## Properties

| Name | Description |
| --- | --- |
| `Grid` | Grid chunk, or null for placements. |
| `Placements` | Ordered placements; empty for a grid payload. |
| `TileSets` | Used grid palette; empty for a placement payload. |

## Methods

| Name | Description |
| --- | --- |
| `TryResolveTile(int tileId, out TileSet2D? tileSet, out TileDefinition2D? definition)` | Finds a supplied positive grid tile ID. Returns false with null outputs for an absent ID, including zero; does not load data. |

## See also

- [TileChunk2D](Cerneala.UI.Controls.TileChunk2D.md)
- [TileSet2D](Cerneala.UI.Controls.TileSet2D.md)
- [Tile](Cerneala.UI.Controls.Tile.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
