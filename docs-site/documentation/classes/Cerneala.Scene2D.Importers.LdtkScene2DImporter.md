# LdtkScene2DImporter Class

## Definition

Namespace: `Cerneala.Scene2D.Importers`

Assembly/Project: `Cerneala.Scene2D.Importers` (optional)

Source: `Cerneala.Scene2D.Importers/LdtkScene2DImporter.cs`

Imports the declared LDtk JSON 1.5.3 subset into validated backend-neutral Scene2D data.

```csharp
public static class LdtkScene2DImporter
```

## Examples

```csharp
using Cerneala.Scene2D.Importers;
using Cerneala.UI.Controls;

var imported = LdtkScene2DImporter.Import("Content/village.ldtk");
if (imported.Success)
{
    Scene2DLevel level = imported.Document!.Levels[0];
    var tiles = new TileMap2D { Model = level.TileMap };
    // Composition registers the declared images, applies level placement,
    // and creates dynamic entities or sparse promotions separately.
}
```

## Remarks

Import is synchronous, intended for map loading rather than the frame loop. The optional assembly references core but no renderer/backend. It returns data: it does not decode images, register resources, upload textures, create nodes, attach input or Aspect/Motion/Prism, execute AutoLayer rules, or build navigation/collision from IntGrid values.

### Version, files and publication

The project requires exact string `jsonVersion: "1.5.3"`. Separate `.ldtkl` levels inherit that version. When a transport `__header__` is present, it must identify app `LDtk`, app version `1.5.3` and file type `LDtk Project JSON`. Header URLs are not fetched. This is a closed semantic importer, not a general-purpose JSON Schema validator of every editor-only value.

Inline and official separate-level exports are supported. Both the project reference and external payload are checked; identity, dimensions and world placement must agree. Missing files, repeated/circular payload paths, duplicate definition UIDs or instance IIDs, unresolved references and unsupported constructs prevent publication. IID uniqueness compares parsed GUID identity, including differently cased spellings.

All atlas and nonempty FilePath fields resolve relative to the project, including fields inside separate levels. Returned asset keys/paths are normalized relative to the configured local root. Missing assets fail even though image decoding is deferred. See [options](Cerneala.Scene2D.Importers.Scene2DImportOptions.md) for exact budgets, stable-tree containment and reparse-point policy. Import does not protect against hostile concurrent filesystem replacement.

### Worlds, grids and tiles

Legacy root levels and multi-world containers are supported, but cannot both be nonempty. Returned levels follow world-array then level-array order. `Free` and `GridVania` preserve exported world coordinates; `LinearHorizontal` and `LinearVertical` accumulate level pixel dimensions in source order. World identity/layout/grid metadata and level world depth are retained.

Each level has one uniform grid; different levels may differ. Level dimensions must align to that grid. An empty level with no layer instances uses a one-pixel grid and its declared finite pixel bounds. Layer order reverses LDtk's top-first instance array into bottom-first core order; tile arrays are not reversed. Core layer IDs are definition UIDs as invariant strings; instance IIDs and names remain metadata. Total layer pixel offsets are used once; raw definition/instance offsets are retained, not added again.

Each tileset declares one atlas with grid, dimensions, padding and spacing. Core global IDs start at one and are assigned in project tileset order, then local tile order. `$LocalTileId` and `$DefinitionUid` retain source identity. Tile source coordinates must match the declared local ID. Horizontal/vertical flips, layer visibility and opacity are preserved.

`Tiles` reads `gridTiles`; `AutoLayer` and `IntGrid` read baked `autoLayerTiles`. IntGrid's exact CSV and definitions remain `$IntGrid` and `$IntGridDefinitions` metadata. Values must be zero or defined positive values. Entity layers become empty-cell core layers with valid entity references. There is no implicit collider, rule execution or pathfinding.

Unsupported representations produce `SCN2D004`: mixed grids within a level, atlas/destination grid mismatch, non-grid-aligned level dimensions, unsnapped tile positions, stacked tiles, per-tile alpha other than one, nonzero parallax, embedded atlases, background images and unknown layouts/types/extensions. These are not rescaled, flattened or silently discarded.

### Entities, fields and promotion

Entities retain instance/definition identity, size, pivot and source position. Core geometry origin is `px - size * pivot`, without editor pixel rounding. `$SourcePx` retains the exported anchor; `Pivot` remains source metadata rather than an additional executed transform. Layer and world offsets remain separate. Visual definition metadata belongs to composition.

Supported primitive fields are `Int` (Int64), `Float` (finite Double), `String`, `Multilines`, `Bool`, `Color` (`#RRGGBB` mapped to `Color`) and `FilePath` (root-relative string). Null is accepted only when the definition permits it. Instance name/type/UID must match its owning definition. Arrays, enums, points, entity references and tile-valued fields are unsupported. Names beginning with `$` are reserved.

`CernealaRole` is `Metadata` by default, or explicit `Spawn`, `Collider`, `Promote`. Collision fields share the [Tiled conventions](Cerneala.Scene2D.Importers.TiledScene2DImporter.md): unsigned layer/mask, Boolean trigger and preserved initial state. LDtk collider geometry uses `ColliderShape` (`Box`, `Ellipse`, `Polygon`, `Polyline`) and invariant-culture `ColliderPoints`. Ellipses remain affine circles; open polylines become consecutive zero-thickness two-sided segments. Core constructors and the shared validator own geometry validity.

Promotion requires explicit `TileLayer`, `TileX`, `TileY`; an optional `TileId` uses core/global identity and is required over an empty cell. Duplicate or unresolved addresses fail. The parser returns sparse promotion data; composition alone calls `TileMap2D.Promote` and configures the resulting node.

### Field disposition and provenance

The [versioned field inventory](../../../tests/Fixtures/Scene2DImport/compatibility-matrix.json) lists every accepted field at all 16 LDtk scopes. Unknown fields fail, including nested definitions, tile rectangles and external level references. Known editor-only fields produce aggregated `SCN2D017` warnings, once per source file. Their values are not interpreted as runtime instructions.

| Scope | Runtime contract / retained metadata |
| --- | --- |
| Root | Version, header, definitions, identity, level/world containers; background colors retained. |
| Definitions | Tilesets/layers/entities parsed; enums, external enums and level field definitions retained; level fields also checked against the primitive subset. |
| World / Level | Layout, placement, dimensions and identity; world grid, depth, neighbors and colors retained. External wrapper properties remain `$ProjectReference`. |
| Layer definition / instance | Type, grid, identity, atlas and offsets checked; definition metadata, raw offsets, source name and IID retained. |
| Tile / Tileset | Atlas source/grid/ID/flip/alpha checked; custom tile data, enum tags and tags retained. |
| Entity definition / instance | Geometry, identity, fields checked; visual dimensions/pivots/color/tile rendering metadata retained under `$Definition`; grid, tags, color and optional tile rectangle retained on the instance. |
| Field definition / instance | Primitive type, nullability and owner checked; entity `$Definition.$FieldDefinitions` retains UID/name/kind/nullability. Optional field tiles retain independent owners in `$FieldMetadata[fieldName]`, not one overwritten entity-level key. |
| IntGrid value / group | Unique positive IDs and group references checked; colors, names, groups and tile rectangles retained. |
| Tile custom metadata / Tileset rectangle | Tile identity, dimensions and atlas containment checked. |

## Methods

| Name | Description |
| --- | --- |
| `Import(string filePath, Scene2DImportOptions? options = null)` | Reads a local project and required references, returning a located diagnostic result with a validated document only on success. |

Null/empty/whitespace paths throw an argument exception. Nonpositive caller budgets throw `ArgumentOutOfRangeException`. Content, file, numeric and core-validation failures become diagnostics; no partial document is returned. The importer does not claim recovery from process exhaustion.

## See also

- [Scene2DImportOptions](Cerneala.Scene2D.Importers.Scene2DImportOptions.md)
- [Scene2DImportResult](Cerneala.Scene2D.Importers.Scene2DImportResult.md)
- [Scene2DLevel](Cerneala.UI.Controls.Scene2DLevel.md)
- [Scene2DModelValidator](Cerneala.UI.Controls.Scene2DModelValidator.md)
