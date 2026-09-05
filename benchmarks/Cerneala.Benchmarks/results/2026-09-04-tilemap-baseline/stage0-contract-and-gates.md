# TileMap2D stage 0 contract, inventory and gates

Captured: 2026-09-04

This file freezes the stage-0 decisions used by
`docs/plans/2026-09-04-rendersurface2d-tilemap-and-scale.md`. It is not public API
documentation. Public documentation is produced only when the API exists.

## `DrawSpriteBatch` semantic inventory

RoslynIndexer found 28 exact references to `Cerneala.Drawing.DrawSpriteBatch`.
The relevant ownership chain is:

1. `Drawing/DrawBatches.cs` owns the immutable copied sprite list, one image,
   common sampling/address modes, retained mesh, version and bounds. Construction
   eagerly creates four vertices and six indices per sprite.
2. `Drawing/DrawCommand.Images.cs` puts the retained mesh and original batch on a
   `DrawSpriteBatch` command. There is no separate backend tile command.
3. `Drawing/DrawingContext.Images.cs` resolves image-backed/`PrismImage` resources
   and emits the command. A `PrismImage` becomes explicit begin/content/end Prism
   commands.
4. `UI/Controls/RenderSurface2DFrame.Images.cs` is the frame-facing caller and
   delegates to `DrawingContext`.
5. `Drawing/DrawCommandMetadata.cs` owns bounds, retained identity and recursive
   image dependency tracking for the batch and its mesh.
6. `UI/Rendering/DrawCommandListBuilder.cs` translates and applies render-scope
   opacity by producing a transformed batch/mesh. A tilemap must therefore avoid
   rebuilding unchanged batch commands through unrelated transforms.
7. `Drawing/MonoGame/MonoGameDrawingBackend.cs` routes the command through
   `DrawAdvancedMesh`; this performs one indexed primitive draw and uses the
   command sampling/address mode.
8. `Cerneala.Backends.SdlGpu/Gpu/SdlGpuDrawingBackend.cs` routes the same retained
   mesh to `AddCommandMesh`/`AddImageGeometry`; Cerberus can coalesce compatible
   geometry by texture, topology, sampler and current state.

Known direct non-owner callers/tests are the RenderSurface2D playground showcase,
`DrawingImageMeshBatchTests` and `DrawingIntegrationLifecycleTests`. Those tests
freeze copied inputs, one-command recording, version/bounds identity, image
tracking, retained damage and one primitive draw. Any future low-level command
change would also have to update both backend command switches, command metadata,
state transforms, lifecycle tests and the showcase. Stage 0 found no evidence
justifying such a change, so the approved tilemap path reuses `DrawSpriteBatch`.

## Frozen core model

All types live in `Cerneala.UI.Controls` and remain platform-neutral:

- `TileMap2DModel`: `TileSize`, nullable finite `Bounds`, `TileSets`, `Layers`,
  positive `Version`, opaque `Properties`.
- `TileSet2D`: stable `Id`, `AtlasResourceId`, unique positive tile definitions,
  positive `Version`, opaque `Properties`.
- `TileDefinition2D`: positive global `Id`, explicit atlas `SourceRect`, opaque
  `Properties`.
- `TileLayer2DModel`: stable `Id`, visibility, `Offset`, opacity, tint, semantic
  `Order`, immutable chunks, positive `Version`, opaque `Properties`.
- `TileChunk2D`: tile-coordinate `Origin`, positive `Width`/`Height`, row-major
  `Tiles` of exactly `Width * Height`, positive `Version`, opaque `Properties`.
- `TileCell2D`: `TileId` and `TileFlip2D` flags.
- `TileCoordinate2D`, `TileCellKey2D` and `TileMapBounds2D` are value identities.

`TileId == 0` means empty and never resolves an atlas. Positive IDs must resolve
to exactly one `TileDefinition2D`; duplicate or unresolved positive IDs are
invalid. `TileFlip2D` has horizontal and vertical flags, matching the existing
`DrawImageFlip` contract reused by `DrawSpriteBatch`. Inputs are copied into
immutable views. A cache-visible mutation requires a replacement with a new
positive version; mutating an input collection behind an unchanged version is not
supported.

Finite maps publish `TileMapBounds2D`. Sparse/infinite maps publish `Bounds ==
null` and are indexed exclusively by existing chunk origins. Chunks in the same
layer may not overlap. Negative chunk origins are valid. Importer properties use
copied `IReadOnlyDictionary<string, object?>` values and are not interpreted by
the renderer.

## Frozen scene-node and promotion contract

- `TileMap2D : SceneNode2D` owns `Model`, one addressable `TileLayer2D` per model
  layer, sparse promoted instances, caches and diagnostics.
- `TileLayer2D : SceneNode2D` is identified by `LayerId`; it is a presentation
  node, not the layer data model. It owns only its sparse `PromotedTiles` nodes.
- `TileInstance2D : SceneNode2D` is identified by the containing map plus
  `TileCellKey2D(layerId, coordinate)`. It exposes `X`, `Y` and optional tile,
  source-rect, tint and flip overrides.
- `TileMap2D.Promote`, `Demote` and `TryGetPromoted` are the code-first lifecycle.
  A second code-first promotion is idempotent and returns the existing instance;
  duplicate static markup is an authoring error with a located generator
  diagnostic.
- Promoting a non-empty existing cell reuses its atlas/source rect. An empty cell
  requires an explicit positive tile override. A missing layer or a coordinate
  outside both finite bounds and existing sparse chunks is rejected. Demoting a
  missing promotion returns `false`.
- While promoted, the static cell is suppressed. The node records in the exact
  semantic cell slot, splitting only the affected batch segment. Demotion restores
  the cell to that slot. There is never both a static and promoted draw.
- Node count is `O(layer count + promoted cell count)`, never `O(tile count)`.

The markup shape is regular Cerneala child syntax, not XML-style effects:

```xml
<TileMap2D Model="$DataContext.WorldMap:OneWay">
  <TileMap2D.Aspect>
    <Aspect>
      @on Loaded
      {
        @animate with Tween(100ms)
        {
          @to { Opacity = 0.9; }
        }
      }
    </Aspect>
  </TileMap2D.Aspect>
  @prism
  {
    @layer MapContent { Opacity = 1; @filter Blur { Radius = 1; } }
  }
  <TileLayer2D LayerId="Buildings">
    <TileInstance2D X="18" Y="11" />
  </TileLayer2D>
</TileMap2D>
```

Aspect, `@animate with` and `@prism` attach normally to map, layer and promoted
tile nodes. `@templates` remains the collection path for independent entities; it
is not added to every static tile.

## Existing nested Prism characterization

Nested Prism scopes are already structural begin/end scopes rather than a flat
effect property. `PrismFrameAnalyzer` records parent/depth relationships. The
MonoGame WindowsDX conformance test
`ExecutedGraphDumpIsDeterministicAndCorrelatesNestedTransformScopes` verifies a
nested graph with `depth=1 parent=0`. SDL_GPU's
`NestedPrismPresentationRemainsVisibleAwayFromTheHostOrigin` exercises nested
presentation through the real hidden native window, and its fake-GPU retained
tests exercise the same child-surface path repeatedly.

Therefore map -> layer -> promoted tile is legal nesting. Illegal Prism grammar,
including a filter directly under `@prism`, remains a generator diagnostic. A
scope must not be silently dropped merely because the tile is extracted from a
batch.

## Deterministic fixture and RED evidence

`TileMapVillageFixture` is generated entirely from integer formulas: 128 x 96
tiles, three layers, 16 x 16 chunks, two synthetic atlas IDs, empty cells, flip
flags, 18 visible chunks, 126 invisible chunks and three sparse chunks including
negative and remote origins. It contains no external image asset. Its SHA-256
fingerprint is
`AB31673783B86A0D9EA7789AB0F133B0FB2AEC270DAFD9D4A6EECF7B317416E5`.

RED runs are archived as `stage0-core-red.trx` and
`stage0-sourcegen-red.trx`:

- core: 8 tests, 1 fixture test passed and 7 contract tests failed for the
  explicitly absent tilemap API/diagnostics;
- SourceGen: 2 tests failed: one reports `CERNEALAUI002` for unsupported
  `TileMap2D`; one reports the missing located duplicate-coordinate diagnostic;
- the real A/M/P syntax parses far enough that there is no unrelated Motion or
  Prism catalog error.

These failures distinguish the absent model/markup path, absent culling counters,
absent local invalidation counters and absent promoted/nested-scope path. They are
not fixture, compilation or environment failures.

## Measured baseline and numeric gates

The reproducible runner is:

```powershell
dotnet run --no-build --no-restore -c Release `
  --project .\benchmarks\Cerneala.Benchmarks\Cerneala.Benchmarks.csproj -- `
  --tilemap-baseline `
  .\benchmarks\Cerneala.Benchmarks\results\2026-09-04-tilemap-baseline\baseline.json
```

Configuration: 64 warmup frames, 512 measured operations, fixture seed 23063,
128 x 96 x 3 tiles, 16 x 16 chunks, 48 x 32 tile viewport. Hardware/runtime and
the exact commit are stored in `baseline.json`.

| Scenario | P50 CPU | P95 CPU | Allocated/op | Commands | Rebuilds | Retained bytes |
|---|---:|---:|---:|---:|---:|---:|
| warm static | 1808.6 us | 3495.2 us | 2,910,116 B | 36 | 36 | 652,808 |
| camera pan | 1988.0 us | 2915.2 us | 3,822,182 B | 48 | 48 | 871,416 |
| chunk mutation | 1485.3 us | 2262.2 us | 2,865,317 B | 36 | 36 | 652,808 |

The baseline eagerly rebuilds visible `DrawSpriteBatch` meshes and scans all 144
chunks. The optimized implementation must satisfy all of these gates on the same
fixture/configuration after warmup:

- warm static: P95 <= 875 us, allocated <= 30,000 B/op, zero rebuilds, at least
  36 reused segments, and no more than 36 commands;
- camera pan: P95 <= 1,460 us, allocated <= 192,000 B/op, zero geometry rebuilds,
  and no more than 48 commands at the widest observed chunk boundary;
- one chunk mutation: P95 <= 1,135 us, allocated <= 717,000 B/op, exactly one
  dependent chunk/atlas/order segment rebuilt, all other visible segments reused,
  and exactly one tile invalidation;
- culling: candidate chunks must come from the chunk index and visible chunks;
  candidate tiles may come only from visible chunks, never all 144 chunks;
- memory: <= 1 MiB retained tilemap cache for the warm static viewport and <= 5
  MiB for the complete finite fixture. The full-fixture budget is the measured
  104 bytes per drawn tile rounded to 128 bytes plus 4 KiB per resident chunk;
- structure: no persistent node per static tile and no backend-native tilemap
  command.

The ratios are intentionally strict because the current baseline is the known
bad eager-rebuild path: 25% of baseline P95 for static, 50% for changing scenarios,
1% of baseline allocation for static, 5% for pan and 25% for mutation. Stage 4
must record actual optimized measurements; passing structural counters without the
numeric measurements is not sufficient.
