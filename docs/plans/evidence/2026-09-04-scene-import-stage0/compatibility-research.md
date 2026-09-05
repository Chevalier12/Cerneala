# Scene import: Stage 0 compatibility research

Date: 2026-09-05. Status: historical research; implementation decisions are recorded in the parent plan and the Stage 0 compatibility matrix. This report alone is not a completed gate.

Parent: [import/debug/validation plan](../../2026-09-04-rendersurface2d-import-debug-validation.md).

## Decisions already established

- Runtime import at map load, not per frame; no build conversion pipeline in v1.
- Preserve the proposed subset rather than silently dropping data that the core cannot currently represent.
- The recommendation to extend diagonal tile flips and local affine collider descriptors was presented before the user's instruction to follow the recommended decisions. These extensions remain subject to RED tests, API documentation and both-backend verification.
- Subsequent explicit delegation resolved the polyline decision: preserve the path as data; collider-role paths become consecutive zero-thickness, two-sided segments, with no implicit closure or extrusion. Degenerate segments are data errors. See the parent plan for the authoritative decision.

## Verified external version evidence

| Source | Observation | Consequence |
| --- | --- | --- |
| [Tiled v1.12.2 JSON writer](https://raw.githubusercontent.com/mapeditor/tiled/v1.12.2/src/libtiled/maptovariantconverter.cpp) | The modern JSON writer obtains `version` from `FileFormat::versionString()`; `tiledversion` is separate. | Do not use the editor version as the schema version. |
| [Tiled v1.12.2 format implementation](https://raw.githubusercontent.com/mapeditor/tiled/v1.12.2/src/libtiled/fileformat.cpp) | Current output format is `1.11`; compatibility export can write `1.8`, `1.9` or `1.10`. | Candidate first fixture target: JSON `version: "1.11"`, editor `1.12.2`. Older compatibility modes need their own fixture before being accepted. |
| [Official LDtk schema](https://ldtk.io/files/JSON_SCHEMA.json) | Downloaded schema identifies itself as LDtk `1.5.3`, JSON Schema draft-07. | Candidate first fixture target: `jsonVersion: "1.5.3"`; no inferred support for other versions. |

SHA-256 of the downloaded LDtk schema bytes:

`2AA84B0DB6E5EF1B530B1F557C5802DA1AA8BC62D10184D358C346734FB84893`

The download URL is mutable. Future fixture verification must compare this hash or explicitly audit a new schema. This document does not claim the version acceptance gate is complete.

## Compatibility work map

This table records required coverage and unresolved mappings. It is not the complete field-by-field matrix required by Stage 0.

| Area | Fields / constructs to characterize | Core consequence / remaining work |
| --- | --- | --- |
| Tiled identity | `version`, `tiledversion`, `type` | Separate format validation from provenance. |
| Tiled layout | `orientation`, `renderorder`, `width`, `height`, `infinite` | All accepted ordering modes need semantic tests; current core records static cells row-major. Do not silently accept a different order. |
| Tiled storage | `data`, `encoding`, `compression`, `chunks` | Numeric JSON and raw/zlib/gzip base64 are in the proposed subset. Bound decoded size before allocation. |
| Tiled tilesets | `firstgid`, `source`, `image`, atlas dimensions, margin/spacing | Resolve each path relative to its declaring file, under the eventual approved asset root policy. |
| Tiled presentation | groups, offsets, visibility, opacity/tint | Preserve composition and layer identity without one node per static cell. |
| Tiled objects | rectangle, ellipse, convex polygon, polyline, point, rotation | Exact affine tile descriptors are needed. Polyline gameplay semantics remain unresolved. |
| Tiled properties | name, type, value and custom property type | Primitive metadata must survive; unknown semantics must be diagnosed, not guessed. |
| Tiled exclusions | other orientations, zstd, templates, image layers | Explicit unsupported diagnostics as required by the plan. |
| Tiled newer fields | layer `mode`, object `capsule` and object `opacity` | These occur in current official documentation. Classify them explicitly; an old field allowlist is not sufficient. |
| LDtk structure | `jsonVersion`, `externalLevels`, `levels`, `worlds` | Do not assume every project stores its levels in the root array. Accepted world layouts still need an explicit matrix. |
| LDtk level placement | `worldX`, `worldY`, `worldDepth`, `layerInstances` | Level placement and display ordering must remain independent of source property ordering. |
| LDtk layer placement | `__pxTotalOffsetX/Y`, `pxOffsetX/Y`, definition offsets | Avoid adding definition offsets twice. |
| LDtk tile data | `gridTiles`, `autoLayerTiles`, `px`, `src`, `t`, `f`, `a` | Preserve ordering. Per-tile alpha and stacked tiles need explicit classification; the core cell currently holds only ID and flip. |
| LDtk IntGrid | `intGridCsv`, definitions, baked auto-layer tiles | Preserve integer data; do not invent a collision meaning for every nonzero value. |
| LDtk entities | identity, definition UID, pivot, position, field instances | Preserve data; scene composition owns instantiated nodes and Aspect/Motion/Prism. |
| LDtk separate level | `externalRelPath`, level identity, null inline layer content | Missing or inconsistent external content must not publish a partial scene. |

Sources for format semantics: [Tiled JSON](https://doc.mapeditor.org/en/stable/reference/json-map-format/), [Tiled GIDs](https://doc.mapeditor.org/en/stable/reference/global-tile-ids/), [LDtk layers](https://ldtk.io/docs/game-dev/json-overview/layer-instances/), [LDtk separate levels](https://ldtk.io/docs/game-dev/json-overview/optional-separate-levels/), and the pinned schema observation above.

## Source-confirmed core gaps

- `UI/Controls/TileMap2DModel.cs`: `TileFlip2D` contains only Horizontal and Vertical; `TileCell2D` rejects all other bits. Tiled diagonal conversion requires a real model/rendering/collision contract, not clearing the flag.
- `UI/Controls/TileColliderDescriptor2D.cs`: Box/Circle/Polygon descriptors contain offsets but no local affine transform.
- `UI/Controls/ColliderGeometry2D.cs`: collision geometry already carries an affine shape-to-scene transform. The collision prerequisite plan explicitly preserves affine-transformed circles as ellipses. No ellipse approximation is justified.
- `UI/Controls/TileMap2DModel.cs`: constructors already reject many invalid inputs. The planned shared validator must preserve their established behavior rather than create an independently maintained second rule set.

## Verification state

- PowerShell `7.6.5` and `Test-Json` are available for schema checks.
- `Get-Command tiled` did not locate an editor CLI on PATH. This does not establish that no installation exists elsewhere.
- The official LDtk schema was fetched and its hash/version inspected. No fixture has yet been validated against it.
- No fixtures, golden models, regression tests or production implementation were added by this research pass.
- No Stage 0 checkbox or gate is satisfied by this report alone. The complete field-level matrix and executable fixture/RED evidence are still required.
