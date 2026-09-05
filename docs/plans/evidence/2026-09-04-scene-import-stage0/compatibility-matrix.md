# Scene2D import v1: frozen Stage 0 contract

Selected on 2026-09-05 under the user's explicit, repeated delegation of decisions for this plan. This selects the implementation contract; it does not claim implementation or verification is complete.

The field-by-field inventory is [`compatibility-matrix.json`](../../../../tests/Fixtures/Scene2DImport/compatibility-matrix.json). Its generator checks LDtk field names against the pinned official schema. Unknown fields are unsupported, not silently ignored. Definitions and editor-only data are classified explicitly even when they are not needed to render baked exported content.

## Versions and execution

| Format | Accepted format version | Reference / verification |
| --- | --- | --- |
| Tiled JSON map and external JSON tileset | Exact string `1.11` | [Tiled 1.12.2 format writer](https://raw.githubusercontent.com/mapeditor/tiled/v1.12.2/src/libtiled/fileformat.cpp), signed Tiled 1.12.2 Windows release, native JSON export round trips. |
| LDtk project | Exact `jsonVersion` string `1.5.3` | [Official schema](https://ldtk.io/files/JSON_SCHEMA.json), SHA-256 `2AA84B0DB6E5EF1B530B1F557C5802DA1AA8BC62D10184D358C346734FB84893`. |
| LDtk separate level | Parent project's format version; optional header must identify LDtk and app version `1.5.3` | Validate the level payload against the schema's `Level` definition; validate its transport header separately. |

Tiled's editor version is provenance, not the format version. No implicit range, legacy numeric version, compatibility export mode or future schema is accepted. Adding a version requires its own audited fixture.

Import runs once at map load. The optional importer assembly references the core and `System.Text.Json`, not a backend. `.crn` binds the resulting core model through existing binding/resource APIs. There is no new build converter, source-generator JSON parser, GPU upload path or automatic reload feature.

The official [separate-level sample at tag v1.5.3](https://raw.githubusercontent.com/deepnight/ldtk/v1.5.3/app/extraFiles/samples/SeparateLevelFiles/World_Level_0.ldtkl) has a `__header__` which the schema's closed `Level` object does not describe. The fixture verifier validates that header explicitly and then validates the unchanged payload. It does not weaken the official schema or claim a headerless sample was editor-produced. Header `schema`/`doc`/`url` values are metadata, never instructions to fetch remote content.

## Representation and conditional fields

### Tiled

- Orthogonal maps, finite or sparse infinite chunks, with `right-down` tile render order. Other render orders receive `SCN2D004`; the importer does not pretend the core's static cell order is another ordering convention.
- One uniform destination grid per map. Atlas tiles must match the map tile dimensions. Tileset `tileoffset` is absent or zero, `tilerendersize` absent or `tile`, `fillmode` absent or `stretch`, and optional tileset grid is orthogonal. Other values require a different static-cell geometry contract and are diagnosed rather than rescaled or shifted silently.
- Embedded and external JSON atlas tilesets; numeric tile arrays and little-endian unsigned 32-bit GIDs in uncompressed, zlib or gzip base64. Decoded length must match the declared cells exactly; trailing/missing data is an error.
- Preserve horizontal, vertical and diagonal flags. Apply diagonal before horizontal/vertical. Clear the historical hex rotation bit without interpreting it as a tile ID on an orthogonal map, as specified by [Tiled GIDs](https://doc.mapeditor.org/en/stable/reference/global-tile-ids/).
- Preserve group/layer order, offsets, visibility, opacity and tint through composition. Group ancestry is source metadata. Layer IDs remain their stable numeric IDs represented as strings; names do not replace identity.
- Layer `x/y` are zero; parallax factors are absent or one; blend `mode` is absent or `normal`. Other values are unsupported. Parallax origins with no parallax are preserved as metadata.
- Object-layer `draworder` supports `index` and `topdown`; resulting entity ordering is recorded as data. Rotation is clockwise in source coordinates. Both legacy object `class` and current `type` are recognized, but conflicting values are an error.
- Rectangles, exact ellipses, convex polygons, open polylines and points are represented without lossy conversion. A collider-role polyline becomes consecutive, zero-thickness, two-sided segments. Points are spawn/metadata, not zero-radius colliders. Tile collision-editor object groups default to collider role; ordinary object layers default to metadata.
- Isometric, oblique, staggered and hexagonal orientations, zstd, image layers, image-collection tilesets, tile objects (`gid`), templates, text/capsule objects, tile animation and transparent-color keys are explicitly outside v1. No partial map is returned for these features.

### LDtk

- Inline levels and `.ldtkl` files produce the same core level data. Relative atlas paths remain relative to the project, not to a separate level's directory.
- Root-level worlds and the schema's `worlds` container are supported. Simultaneous nonempty legacy and multi-world containers are rejected as ambiguous. World/level identity is retained. `Free` and `GridVania` use exported world positions; linear layouts use level-array order and accumulate level dimensions instead of treating `-1` as a real position.
- Each core level has its own uniform grid, tile map, placement, entities and promotion references. Different levels may use different grids. Within a level, mismatched layer grids, unsnapped tile positions, stacked tiles in one cell and tile alpha other than one receive explicit unsupported diagnostics. This preserves the existing one-cell/one-static-tile contract; no overdraw is discarded and no per-cell UI layer is manufactured to hide the mismatch.
- Tiles and baked AutoLayer tiles preserve `px/src/t/f` semantics, layer visibility and opacity. Reverse the top-first layer-instance array into the core's bottom-first layer order. Do not reverse individual tile arrays. Use total pixel offsets once, not total plus definition offsets.
- IntGrid values and their definitions are retained as data; nonzero does not imply collision or navigation. Auto rules are not executed because baked tile output is the runtime input.
- Entities preserve instance/definition identity, pivots, size, placement and primitive fields. Visual definition metadata is retained; scene composition owns the actual sprite and effects.
- Primitive field kinds: `Int`, `Float`, `String`, `Multilines`, `Bool`, `Color`, `FilePath`, including null where the format allows it. Arrays, enums, points, entity references, tile-valued fields and unknown kinds are unsupported rather than dropped. Background images and nonzero parallax are outside v1.

## Shared metadata conventions

All ordinary supported primitive properties remain accessible by their original name. Keys beginning with `$` are reserved for importer provenance; user collisions with that namespace are errors. `$SourceName` retains an original layer name without replacing its stable ID.

| Property / field | Meaning |
| --- | --- |
| `CernealaRole` | `Metadata` (default), `Spawn`, `Collider`, or `Promote`. Unknown role is unsupported. |
| `CollisionLayer`, `CollisionMask` | Unsigned 32-bit bitsets. Accept a nonnegative integer or a decimal/`0x` string; defaults are `1` and all bits. Zero is legal. Negative/out-of-range/malformed values are errors. |
| `IsTrigger` | Boolean, default false. |
| `ColliderShape`, `ColliderPoints` | LDtk collider role: Box (default), Ellipse, Polygon or Polyline; points use the core invariant-culture `x,y` coordinate syntax. Tiled uses its explicit object geometry. |
| `InitialState` | Preserved primitive state, not a parser-owned state machine. |
| `TileLayer`, `TileX`, `TileY` | Required stable layer/cell address for `Promote`; never inferred from a nearby tile. |
| `TileId` | Optional core/global ID override; required when promoting an existing empty cell. |

Promotion references are sparse data. Duplicates, nonexistent layers/cells and empty-cell promotion without an override are errors. Only scene/markup composition creates a `TileInstance2D`, attaches input/colliders and applies Aspect/Motion/Prism.

## Diagnostics and publication

| Code | Contract |
| --- | --- |
| `SCN2D001` | Required file cannot be read. |
| `SCN2D002` | Invalid JSON/field syntax or required structural field. |
| `SCN2D003` | Unsupported or inconsistent format version. |
| `SCN2D004` | Recognized unsupported or unknown construct. |
| `SCN2D005` | Invalid dimensions or finite bounds. |
| `SCN2D006` | Invalid/unresolved tile ID. |
| `SCN2D007` | Source rectangle outside its atlas or invalid atlas grid. |
| `SCN2D008` | Invalid/degenerate/nonconvex collider. |
| `SCN2D009` | Invalid collision layer/mask. |
| `SCN2D010` | Invalid asset reference or prohibited path. |
| `SCN2D011` | Overlapping chunks in one layer. |
| `SCN2D012` | Invalid/duplicate promotion address or override. |
| `SCN2D013` | Resource/work limit exceeded. |
| `SCN2D014` | Nonfinite or out-of-range numeric geometry. |
| `SCN2D015` | Duplicate/conflicting identity or reserved metadata. |
| `SCN2D016` | Invalid supported property value. |
| `SCN2D017` | Known editor-only data not used at runtime. |

Each diagnostic carries code, severity, message, source file and JSON path when available. Fatal, Error and Unsupported prevent document publication; Warning does not. Diagnostic order follows deterministic source traversal. Known editor warnings are aggregated per file; diagnostic truncation must preserve failure state even if later errors do not fit in the retained list.

Core construction and the document validator share structural rules. Existing constructors continue to reject invalid data with their established exception categories; the importer does not maintain a second set of geometry rules. No invalid document is installed into a graphics cache or collision world.

## Path and resource limits

- Default asset root is the map/project directory; callers may provide an explicit wider local root. Every map, external tileset, external level, atlas and nonempty file-valued property must stay inside that root.
- Normalize `.`/`..` and both path separators; `..` is not itself forbidden if the final target stays inside the root. Reject URI/network/device paths, alternate data streams and rooted external references. Reject reparse-point components below the root rather than following a link outside the permitted tree. Import never executes source commands or follows remote schema URLs.
- Default limits: 16 MiB per JSON file, 64 MiB total JSON input, 1,024 files, depth 64, 1,048,576 decoded cells, 65,536 chunks, 4,096 layers, 65,536 entities, 4,096 points per shape and 128 retained diagnostics. These bounds are checked before allocating attacker-controlled sizes. Options may lower budgets; raising them is an explicit trusted-caller choice, not content-controlled behavior.
- Atlas byte decoding/upload remains the existing resource system's responsibility; importer path/metadata validation does not claim GPU resource validity.

Stage 1 safety refinement under the same delegated decisions: the core also caps expanded tile collider descriptors at 65,536 per map **before** coalescing. A small repeated tile payload can otherwise expand into arbitrarily many scene-owned collision adapters. Component construction caps (chunk cells/tileset definitions 1,048,576; layer chunks 65,536; map layers/tilesets and document levels/assets 4,096; level entities/promotions 65,536 each; shape/descriptor collections 4,096; point text 393,216 UTF-16 characters) remain in effect when a trusted caller raises aggregate document budgets. Invalid geometry must be rejected before adapter creation and must fit the existing drawing coordinate/size range, not merely be finite. See the canonical [core validator contract](../../../../docs-site/documentation/classes/Cerneala.UI.Controls.Scene2DModelValidator.md).

## Stage boundaries

Additional Stage 1 aggregate safeguards: a map has at most 1,048,576 definitions, 1,048,576 cells and 65,536 chunks, including shared references. A level has at most 65,536 entity collider descriptors. Diagnostic message/file/JSON-path fields retain at most 4,096 UTF-16 characters each. Core validation may stop after a known failure fills the diagnostic budget and explicitly marks truncation.

Stage 2 parser safeguard: source-expanded collider descriptors are also capped at 65,536 overall and 4,096 per owner before materialization, including unused atlas definitions. The hostile regression (32 objects with 4,096 polyline points each) allocated 482,011,224 bytes before the late core rejection in the initial parser. Import now checks the owner/aggregate budget before creating those descriptors rather than depending on the eventual immutable model constructor to stop expansion.

Stage 0 adds fixtures, independent golden expectations and executable RED tests. Stage 1 implements core validation and the necessary core data/geometry contracts, but not an external JSON parser. Truncated JSON, compression and external-path hostile tests become executable against the optional parser in Stages 2/3; core hostile-data tests run in Stage 1. This is an ordering dependency, not a waiver of the final hostile-input gate.

The debug overlay is presentation only. Flags default off; disabled overlay emits no commands and allocates nothing after warmup. Geometry, collision/picking and gameplay ordering do not depend on flags or effects. Line thickness and font size are scene-space values and therefore scale with zoom. The navigation grid is supplied externally; no pathfinding is introduced.
