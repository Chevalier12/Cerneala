# TileMap2DModel Class

## Definition

Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/TileMap2DModel.cs`

Defines one immutable/versioned stratum of free image placements or grid tile data.

```csharp
public sealed class TileMap2DModel
```

## Examples

```csharp
var model = new TileMap2DModel(
    "Ground",
    new DrawSize(16, 16),
    [new TileSet2D("Terrain", new ResourceId<ImageResource>("TerrainAtlas"),
        [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
    [new TileChunk2D(new TileCoordinate2D(0, 0), 2, 1,
        [new TileCell2D(1), default])],
    new TileMapBounds2D(0, 0, 2, 1));
var map = new TileMap2D { Source = TileMapSource2D.FromModel(model), Layer = model.Order };
```

## Remarks

The free-placement constructor copies `Tile` data without manufacturing a grid. `TileSets` and `Chunks` are empty, `TileSize` is zero/default, and `Bounds` is null. Each placement retains its image reference, independent optional dimensions, and zero or one immutable collider descriptor. Its default stable ID is `"Tiles"`. Supply distinct IDs when placing multiple models in a level.

The grid constructor stores `Chunks` directly, not nested layer models; `Tiles` is empty. Coordinates, atlas resource IDs, source rectangles, and metadata are backend-neutral. Positive tile IDs must resolve to exactly one definition across the model's tilesets; ID `0` is empty. Chunks cannot overlap. Null bounds permit sparse negative/remote coordinates without enumerating the gaps; finite bounds must contain every chunk.

Collections and property dictionaries are copied into read-only views. Opaque property values are not interpreted or deep-cloned. Neither constructor transfers image ownership. `Order` is metadata for composition: assign it to the map node's `Layer` under a scene with a corresponding order mode. The map applies model visibility, offset, opacity, and tint in addition to its own presentation state.

### Spatial source adapter

[TileMapSource2D.FromModel](Cerneala.UI.Controls.TileMapSource2D.md) derives a metadata catalog and exposes acquisitions over this complete in-memory data. It does not turn this model into an unloaded or partial map. The adapter retains its backing chunks/placements even when spatial acquisitions are released, and `TryGetCell` keeps its complete-snapshot semantics. For path-backed free placements with omitted dimensions, supply intrinsic image-size metadata to the adapter; it does not decode every image to discover spatial bounds. Assign the adapter to `TileMap2D.Source`. This is the same explicit preparation path as custom sources, including when collision queries are used without UI.

### Versions and retained caches

`Version` is a positive publication stamp, not an automatically incremented counter. For grid changes, publish replacement objects with changed versions and publish the replacement through a source with matching backing payloads, or assign a new adapter to `TileMap2D.Source`. A changed chunk must receive a changed `TileChunk2D.Version`. This includes every chunk affected by a definition, atlas-reference, collider-prototype or opaque-metadata change, even when its cell IDs are unchanged. A changed tileset or map version alone does not replace payload revisions. Reusing a cache-visible revision for different payload data is unsupported.

The drawing cache compares chunk payload versions, image resource versions, tile size, composed tint, and resolved image identity. Definitions belong to the loaded chunk, not a global catalog palette. Replacing the source retires its old acquisitions and caches. When retaining a source identity, changed free-placement payloads require changed chunk revisions just like grid payloads. Model objects never mutate themselves or instantiate live sprites.

### Validation

A grid model accepts at most 4,096 tilesets, 65,536 chunks, and 1,048,576 aggregate definitions and cells each. A free model accepts at most 1,048,576 placements. Expanded tile colliders are capped at 65,536 before coalescing. Every tile or definition carries at most one descriptor.

Construction checks IDs, versions, bounds, overlaps, cell references, and placed geometry. Atlas dimensions are external information: use [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md) or [Scene2DDocument](Cerneala.UI.Controls.Scene2DDocument.md) to validate resource references and source rectangles. Runtime recording validates resolved dimensions before building chunk commands; unresolved runtime resources remain deferred, whereas documents require complete declarations.

## Constructors

| Name | Description |
| --- | --- |
| `TileMap2DModel(IEnumerable<Tile> tiles, long version = 1, string id = "Tiles")` | Copies free pixel placements. |
| `TileMap2DModel(string id, DrawSize tileSize, IEnumerable<TileSet2D> tileSets, IEnumerable<TileChunk2D> chunks, TileMapBounds2D? bounds = null, int order = 0, bool isVisible = true, DrawPoint offset = default, float opacity = 1, Color? tint = null, long version = 1, IReadOnlyDictionary<string, object?>? properties = null)` | Copies and validates one grid stratum. Null tint means white. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Nonempty stable map identity; unique within a level. |
| `TileSize` | Grid destination cell size, or zero/default for free placements. |
| `Tiles` | Immutable free placements; empty for grids. |
| `Chunks` | Immutable grid chunks; empty for free placements. |
| `Bounds` | Finite grid-coordinate extent, or null for sparse/free data. |
| `TileSets` | Immutable grid palette. |
| `Order` | Source composition order metadata; default 0. |
| `IsVisible` | Model visibility; default true. |
| `Offset` | Model translation; default zero. |
| `Opacity` | Finite model opacity in [0,1]; default 1. |
| `Tint` | Model tint; default white. |
| `Version` | Positive publication version. |
| `Properties` | Copied opaque source metadata. |

## Methods

| Name | Description |
| --- | --- |
| `TryGetCell(TileCoordinate2D, out TileCell2D)` | Finds a coordinate in the supplied chunks, including an explicitly empty cell. Returns false outside them and for free placements. |
| `TryResolveTile(int, out TileSet2D?, out TileDefinition2D?)` | Resolves a positive tile ID; returns false for ID 0 or an undefined ID. |

## See also

- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
- [Tile](Cerneala.UI.Controls.Tile.md)
- [TileSet2D](Cerneala.UI.Controls.TileSet2D.md)
- [TileChunk2D](Cerneala.UI.Controls.TileChunk2D.md)
- [Scene2DLevel](Cerneala.UI.Controls.Scene2DLevel.md)
