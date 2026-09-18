# TileMapCatalog2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TileMapCatalog2D.cs`

Describes a complete immutable static-map header and spatial chunk catalog without retaining cells, placements, full tile definitions, collider prototypes or opaque property dictionaries.

```csharp
public sealed class TileMapCatalog2D
```

## Examples

This catalog declares an empty-cell grid chunk without allocating its cells or loading an image:

```csharp
var entry = new SceneSpatialEntry2D(
    "west", new DrawRect(-160, 0, 160, 160), collisionBounds: null);
var chunk = new TileMapChunkInfo2D(
    entry, new TileMapBounds2D(-10, 0, 10, 10),
    Array.Empty<int>(), Array.Empty<ImageReference>());
var catalog = new TileMapCatalog2D(
    "Ground", [chunk], tileSize: new DrawSize(16, 16));
```

## Remarks

Omitting `tileSize` selects free-placement data. Supplying a finite positive size selects grid data. A catalog cannot mix the two representations. Grid metadata declares cell-coordinate rectangles, positive used tile IDs and conservative visual/collision bounds. Free-placement metadata declares a count, ordered chunk position, image references and bounds. Chunk IDs are unique within the catalog; the catalog's array order remains the order for free-placement painting. Overlapping grid chunks are rejected; overlapping free-placement chunks are allowed.

`Entries` and `Chunks` are stable read-only snapshots copied from constructor input. Both grid and free-placement chunks declare image dependencies; image-size metadata is copied into a read-only view. Full tile definitions and collider prototypes are obtained from [TileMapChunkData2D](Cerneala.UI.Controls.TileMapChunkData2D.md) after acquisition, not through this catalog. A declaration with a direct `ImageReference` still borrows that existing image, so such metadata is not a mechanism for unloading application-owned image objects.

Spatial bounds and cell destinations are map-local, before `Offset` and ancestor transforms. Construction checks finite geometry, positive grid size, unique IDs, finite map containment, grid overlap and aggregate limits. It does not load or validate unseen definitions: [TileMapSource2D](Cerneala.UI.Controls.TileMapSource2D.md) validates the acquired palette, including image dependencies and source rectangles against declared atlas sizes. Undeclared grid atlas sizes remain deferred until image resolution, not guessed from source rectangles.

`ImageSizes` maps ordinal resource keys to finite positive dimensions. `TryGetImageSize` also handles direct images using dimensions captured when the catalog is constructed. It performs no image load. Natural-size placements require metadata for every omitted dimension when an adapter or loader validates their data. Explicit destination dimensions remain independent of intrinsic size. If intrinsic dimensions change, publish matching metadata rather than changing geometry behind an old spatial envelope.

The catalog permits at most 4,096 image-size declarations, 65,536 chunks, 1,048,576 total cells/placements, and 65,536 expanded tile colliders before coalescing. Used definitions are validated with their loaded payloads, not retained as a global palette. These are validation limits, not measured RAM or GPU budgets. Sparse coordinates do not allocate the gaps. `Order`, visibility, offset, opacity and tint are metadata; this type does not mutate a scene node or perform rendering.

Each chunk can additionally declare [DataResidencyBytes](Cerneala.UI.Controls.TileMapChunkInfo2D.md), a conservative acquisition-owned data cost. Null is unknown; it is not converted to zero or estimated from the tile count. Catalog construction neither loads data nor sums these costs into a residency limit: required chunks can exceed an optional warm budget, and catalog-wide total cost is not simultaneous resident memory. Admission and reservation belong to the consumer of the source, not to this metadata container.

## Constructors

| Name | Description |
| --- | --- |
| `TileMapCatalog2D(string id, IEnumerable<TileMapChunkInfo2D> chunks, DrawSize? tileSize = null, TileMapBounds2D? bounds = null, int order = 0, bool isVisible = true, DrawPoint offset = default, float opacity = 1, Color? tint = null, long version = 1, IReadOnlyDictionary<string, DrawSize>? imageSizes = null)` | Copies and validates the complete catalog. Null tint means white; null grid size means free placements. |

## Properties

| Name | Description |
| --- | --- |
| `Id` | Nonempty map identity. |
| `IsFreePlacement` | Whether this catalog describes free placements rather than cells. |
| `TileSize` | Grid destination size, or default/zero for free placements. |
| `Bounds` | Optional finite cell-coordinate map bounds. |
| `Chunks` | Complete ordered chunk metadata, not loaded payloads. |
| `Entries` | Corresponding immutable spatial entries. |
| `ImageSizes` | Declared intrinsic sizes keyed by image resource key. |
| `Order` | Composition ordering metadata; default zero. |
| `IsVisible` | Map participation metadata; default true. |
| `Offset` | Map-local translation; default zero. |
| `Opacity` | Finite opacity in `[0,1]`; default one. |
| `Tint` | Map tint; default white. |
| `Version` | Positive map publication stamp, not an automatic chunk revision. |

## Methods

| Name | Description |
| --- | --- |
| `TryGetImageSize(ImageReference image, out DrawSize size)` | Reads captured intrinsic-size metadata without decoding an image. |

## See also

- [TileMapChunkInfo2D](Cerneala.UI.Controls.TileMapChunkInfo2D.md)
- [TileMapSource2D](Cerneala.UI.Controls.TileMapSource2D.md)
- [Scene2DAsset](Cerneala.UI.Controls.Scene2DAsset.md)
- [TileMap2DModel](Cerneala.UI.Controls.TileMap2DModel.md)
