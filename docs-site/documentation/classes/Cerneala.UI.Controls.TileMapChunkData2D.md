# TileMapChunkData2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileMapSource2D.cs`

Contains one acquired static chunk's immutable cells and used definitions, or ordered free placements.

```csharp
public sealed class TileMapChunkData2D
```

## Remarks

Use the grid constructor for an existing immutable `TileChunk2D` and its used `TileSet2D` definitions, or the placement constructor to copy a bounded sequence of immutable `Tile` references. The representations are exclusive: a grid payload has no placements; a placement payload has a null `Grid` and an empty `TileSets`. Placement order is preserved. Copying does not deep-clone the tiles, images, descriptors or opaque application metadata.

A grid payload must supply exactly the definitions used by its nonzero cells. Every used ID must resolve; unused definitions, duplicate IDs, null sets and empty sets are rejected. A completely empty-cell grid uses an empty palette. The payload copies the supplied set sequence, with a limit of 4,096 sets. Full definitions, collider prototypes and their opaque properties belong here, not in the resident catalog. Resolve definitions through `TryResolveTile` while holding the data acquisition. A changed definition, atlas reference or metadata value requires a new revision for each affected chunk payload.

This is payload data, not a scene node or an ownership token. A loader transfers one acquisition through `SceneSpatialLease2D<TileMapChunkData2D>`; its release callback owns backing-store cleanup. The data type itself does not dispose borrowed images, unload a complete in-memory model, or retain a graphical-cache acquisition. Holding a separate application reference may retain the data after its source acquisition ends; applications must not use backing resources after the lease contract says they are invalid.

The constructor's local checks do not establish consistency with a catalog. `TileMapSource2D.LoadAsync` checks representation, counts, grid coordinates/revision, dependencies and geometry against the captured `TileMapCatalog2D`. In particular, an empty placement array cannot satisfy a declared positive-count chunk and is not a successful substitute for missing data.

## Constructors

| Name | Description |
| --- | --- |
| `TileMapChunkData2D(TileChunk2D grid, IEnumerable<TileSet2D> tileSets)` | Borrows the immutable grid and copies its used palette references; rejects incomplete or extraneous definitions. |
| `TileMapChunkData2D(IEnumerable<Tile> placements)` | Copies up to 1,048,576 non-null ordered placement references into a read-only collection. |

## Properties

| Name | Description |
| --- | --- |
| `Grid` | The immutable grid chunk, or null for placements. |
| `Placements` | Read-only ordered placements, empty for grids. |
| `TileSets` | Read-only used grid palette, empty for free placements. Definitions and collider prototypes are acquired data. |

## Methods

| Name | Description |
| --- | --- |
| `TryResolveTile(int tileId, out TileSet2D? tileSet, out TileDefinition2D? definition)` | Resolves a loaded positive tile ID; returns false with null outputs for an absent ID, including zero. Does not load data. |

## See also

- [TileMapChunkInfo2D](Cerneala.UI.Controls.TileMapChunkInfo2D.md)
- [TileMapSource2D](Cerneala.UI.Controls.TileMapSource2D.md)
- [SceneSpatialLease2D&lt;T&gt;](Cerneala.UI.Controls.SceneSpatialLease2D_T_.md)
- [TileChunk2D](Cerneala.UI.Controls.TileChunk2D.md)
- [Tile](Cerneala.UI.Controls.Tile.md)
