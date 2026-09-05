# TiledScene2DImporter Class

## Definition

Namespace: `Cerneala.Scene2D.Importers`

Assembly/Project: `Cerneala.Scene2D.Importers` (optional)

Source: `Cerneala.Scene2D.Importers/TiledScene2DImporter.cs`

Imports the declared Tiled JSON 1.11 subset into validated backend-neutral Scene2D data.

```csharp
public static class TiledScene2DImporter
```

## Examples

```csharp
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

var imported = TiledScene2DImporter.Import("Content/village.tmj");
if (imported.Success)
{
    TileMap2DModel model = imported.Document!.Levels[0].TileMap;
    var tileMap = new TileMap2D { Model = model };
    // Composition separately registers the declared image resources and adds
    // dynamic entities/promoted tiles to the scene.
}
```

## Remarks

Import runs synchronously at map load, not in the frame loop or source generator. The optional project uses `System.Text.Json` and a bounded strict DEFLATE decoder. It references core, not a platform/backend. It neither decodes images nor registers resources, creates nodes, installs caches, executes gameplay, or attaches Aspect/Motion/Prism. The application composes the returned data through existing scene and binding APIs.

### Version and geometry

The exact string format version `1.11` is required for maps and external JSON tilesets. An embedded tileset inherits its map's version; if it declares a version, that must also match. `tiledversion` is editor provenance, not a compatibility range. No future, legacy numeric, TMX/XML or compatibility-export version is assumed supported.

Maps must be orthogonal with `right-down` render order and a uniform destination grid. Atlas tile dimensions must match that grid. Finite maps preserve bounds; infinite maps preserve sparse chunks, including negative/remote coordinates, without filling gaps. Tile IDs retain global `firstgid + localId` identity. Horizontal, vertical and diagonal flips survive in core; diagonal is applied before horizontal/vertical. The historical hex-rotation flag is cleared, not treated as an ID.

Each tileset has one atlas image, with declared dimensions, margin, spacing and columns. Asset paths and resource keys are normalized root-relative paths. External tileset paths resolve relative to the map; their atlas/file properties resolve relative to the tileset. Missing assets are errors even though pixel decoding remains deferred. See [path policy and budgets](Cerneala.Scene2D.Importers.Scene2DImportOptions.md).

Numeric tile arrays and base64 little-endian UInt32 data are supported. Base64 may be raw, zlib or gzip. Decompression is bounded to exactly four bytes per declared cell; malformed framing/checksums, incomplete input and trailing compressed garbage fail. Gzip concatenated members and optional headers are validated. Preset compression dictionaries are unsupported.

### Layers, objects and promotion

Layer IDs are stable numeric IDs represented as strings; `$SourceName` retains names. Source traversal becomes bottom-first core order. Groups flatten into their children while offsets, opacity, tint and visibility compose. `$GroupAncestors` preserves group identity/properties. Object layers remain empty-cell core layers so entity layer references remain valid. `index` and stable `topdown` object ordering are represented by entity `Order`; `$SourceOrder` retains original order.

Rectangles become boxes, ellipses remain exact affine circles, convex polygons retain their points, open polylines retain their path, and points remain metadata/spawn data. Collider-role polylines become consecutive zero-thickness two-sided segments, not closed polygons. Tiled degrees become clockwise scene radians. Entity position/rotation remain separate from layer offset; tile collision objects fold their own placement into descriptor-local transforms. Core constructors and the shared validator own geometry validation.

Ordinary objects default to `Metadata`; tile collision-editor objects default to `Collider`. Primitive properties preserve their names and values: string/file as strings, int as Int64, float as finite Double, bool as Boolean and color as `Color`. `$` names are reserved. Supported conventions are `CernealaRole` (`Metadata`, `Spawn`, `Collider`, `Promote`), unsigned `CollisionLayer`/`CollisionMask`, Boolean `IsTrigger`, and primitive `InitialState`. Promotion requires explicit `TileLayer`, `TileX`, `TileY`, with optional positive `TileId` override. Empty-cell promotion needs that override. Missing/duplicate addresses fail core validation. Promotion stays sparse data; only composition calls `TileMap2D.Promote` and applies effects to the resulting node.

### Field dispositions

The following inventory is closed. Unlisted fields and listed unsupported constructs fail with `SCN2D004`; a field belonging to a different layer kind is not silently discarded. Known editor-only fields produce an aggregated `SCN2D017` warning. Metadata is retained under `$` provenance keys.

| Scope | Mapped or conditionally checked fields | Metadata | Editor-only |
| --- | --- | --- | --- |
| Map | `height infinite layers orientation parallaxoriginx parallaxoriginy properties renderorder tileheight tilesets tilewidth type version width` | `backgroundcolor class tiledversion`; parallax origins retained | `compressionlevel editorsettings nextlayerid nextobjectid` |
| Layer | `chunks compression data draworder encoding height id layers mode name objects offsetx offsety opacity parallaxx parallaxy properties tintcolor type visible width x y` (kind-specific) | `class`; name, kind, ancestry and object ordering retained | `locked startx starty` |
| Tileset | `columns fillmode firstgid grid image imageheight imagewidth margin name objectalignment properties source spacing tilecount tileheight tileoffset tilerendersize tiles tilewidth type version` | `backgroundcolor class tiledversion`; source/name/alignment retained | `editorsettings terrains transformations wangsets` |
| Tile | `id objectgroup properties` | `class type`; collision objects/group retained | `probability terrain` |
| Chunk | `data height width x y` | — | — |
| Object | `class ellipse height id name opacity point polygon polyline properties rotation type visible width x y` | name/class/order retained | — |
| Property | `name propertytype type value` | — | — |
| Point / TileOffset | `x y` | — | — |
| Grid | `orientation` | — | `height width` |

Conditional values: layer `x/y == 0`, parallax `== 1`, blend mode `normal`; zero tileset offset; tile render size `tile`; fill mode `stretch`; orthogonal tileset grid. Custom property types are unsupported. Conflicting nonempty object `class` and `type` are errors.

Explicit non-goals include nonorthogonal orientations, other render orders, zstd, image layers/repetition, image-collection tilesets, transparent-color keys, tile objects (`gid`), templates, text/capsule objects, animation, per-tile image/source overrides and unknown extensions. No “mostly imported” document is returned for these constructs.

## Methods

| Name | Description |
| --- | --- |
| `Import(string filePath, Scene2DImportOptions? options = null)` | Reads a local map and its required local references, returning a `Scene2DImportResult`. |

Null/empty/whitespace `filePath` throws an argument exception. Invalid caller budgets throw `ArgumentOutOfRangeException`. Content, file, supported numeric and shared model-validation failures are returned as located diagnostics. This method does not claim recovery from process exhaustion or concurrent hostile filesystem mutation.

## See also

- [Scene2DImportResult](Cerneala.Scene2D.Importers.Scene2DImportResult.md)
- [Scene2DImportOptions](Cerneala.Scene2D.Importers.Scene2DImportOptions.md)
- [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md)
- [TilePromotion2D](Cerneala.UI.Controls.TilePromotion2D.md)
