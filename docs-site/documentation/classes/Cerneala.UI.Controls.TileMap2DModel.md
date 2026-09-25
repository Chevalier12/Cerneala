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
var map = TileMap2D.FromModel(model);
```

## Remarks

The free-placement constructor copies `Tile` data without manufacturing a grid. `TileSets` and `Chunks` are empty, `TileSize` is zero/default, and `Bounds` is null. Each placement retains its image reference, independent optional dimensions, and zero or one immutable collider descriptor. Its default stable ID is `"Tiles"`. Supply distinct IDs when placing multiple models in a level.

The grid constructor stores `Chunks` directly, not nested layer models; `Tiles` is empty. Coordinates, atlas resource IDs, source rectangles, and metadata are backend-neutral. Positive tile IDs must resolve to exactly one definition across the model's tilesets; ID `0` is empty. Chunks cannot overlap. Null bounds permit sparse negative/remote coordinates without enumerating the gaps; finite bounds must contain every chunk.

Collections and property dictionaries are copied into read-only views. Opaque property values are not interpreted or deep-cloned. Neither constructor transfers image ownership. `Order` is metadata for composition: `TileMap2D.FromModel` copies it to the new map node's `Layer` for use under a scene with a corresponding order mode. The map applies model visibility, offset, opacity, and tint in addition to its own presentation state.

### In-memory map factory

[TileMap2D.FromModel](Cerneala.UI.Controls.TileMap2D.md) creates a detached node backed by this complete in-memory model. It does not turn the model into an unloaded or partial map; the internal adapter retains its backing chunks/placements even when spatial interest ends, and `TryGetCell` keeps its complete-snapshot semantics. For path-backed free placements with omitted dimensions, supply intrinsic image-size metadata to `TileMap2D.FromModel`; it does not decode every image to discover spatial bounds. Collision queries in a nonvisual scene still use the explicit simulation-context and region-preparation path.

### Versions and retained caches

`Version` is a positive model revision, not an automatically incremented counter. For grid changes, construct replacement immutable objects and a new map node through `TileMap2D.FromModel`; there is no public same-source chunk-publication path. `TileChunk2D.Version` is retained as positive authored revision metadata and participates in the new map's internal cache. A fresh map node has a fresh cache, so changing that version is not required merely to evict the old node's data.

The drawing cache compares chunk payload versions, image resource versions, tile size, composed tint, and resolved image identity. Replacing the map node retires the old node's acquisitions and caches. Model objects never mutate themselves or instantiate live sprites.

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
| `Order` | Composition order metadata copied to `TileMap2D.Layer` by `FromModel`; default 0. |
| `IsVisible` | Model visibility; default true. |
| `Offset` | Model translation; default zero. |
| `Opacity` | Finite model opacity in [0,1]; default 1. |
| `Tint` | Model tint; default white. |
| `Version` | Positive model revision. |
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
