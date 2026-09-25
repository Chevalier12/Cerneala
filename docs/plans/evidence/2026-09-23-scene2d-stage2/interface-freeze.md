# Stage 2 internal integration seam — selected, not yet verified

This is the coordination record for the **atomic Stage 2 cutover** in
`docs/plans/2026-09-23-scene2d-simple-collections-and-internal-spatial.md`.
It is not a checklist checkpoint or proof of a passing build. The selected
public behavior remains in the Stage 0 companion and contract matrix.

## Package → Core map handoff

- Core grants implementation assembly friendship to the existing sibling assembly
  `Cerneala.Scene2D.Packages` through `Properties/AssemblyInfo.cs`. Current
  projects are unsigned; a future strong-name change would need the friend's
  full public key. This does **not** make internal spatial types public.
- Core also grants `Cerneala.Tests.Scene2DPackages` test-only friendship. Its
  project has no `AssemblyName` override, so that is the actual default
  assembly name. This preserves direct private CPV2 catalog/header and
  nullable-residency-charge assertions after Core types are internal, rather
  than reopening those types as public APIs.
- The map owner supplies `internal static TileMap2D FromSource(TileMapSource2D
  source)`. It rejects null, returns a **new** map retaining that internal
  source, and snapshots `Layer = source.Catalog.Order`, matching the public
  `FromModel` factory's initial order. It does not clone payloads, copy every
  map header into redundant node state, or promise future `Layer` updates if
  an internal source catalog is replaced. The package-created catalog is
  stable after publication. Public `TileMap2D.FromModel` remains available.
- The package owner exposes selected `TileMapIds` in CPV2 level/index order
  (not sorted by layer order) and `CreateTileMap(mapId)`. Each call constructs
  a fresh internal `TileMapSource2D` from that map's private catalog and
  chunk-block loader, then calls the internal factory. Map residency, warm
  cache, render batches and collision adapters are per returned map instance.
  The source loader borrows the caller-owned package; the map does not dispose
  the package. The caller keeps it available until map drain completes.
- The current package adapter zips catalog chunk headers with block descriptors
  at matching indices and validates ID/version before reading. Preserve CPV2
  bytes, declared image IDs/sizes, visual/collision bounds, nullable residency
  charge, authored map order, and ordered placement reconstruction. Remote
  images remain resolved through the application image/resource seam;
  `GetFilePath(relativePath)` remains local-only. No public spatial source,
  catalog, header, entry, lease, or renamed wrapper is authorized.
- The strict Stage 2 API audit identified one unapproved extra break:
  `TileMapChunkData2D` had been internalized along with those owners. The
  selected resolution retains that existing **public domain payload value**
  and its baseline signature; it is not a source/catalog/header/lease facade.
  The map owner restored its public declaration only. The package boundary
  test demonstrated RED then GREEN, the canonical page and manifest entry were
  restored, and the current strict Core raw diff has no `TileMapChunkData2D`
  diagnostic. Evidence is under `integration/api-compat-review.md` and
  `package/verification.md`; this does not by itself complete Stage 2.

## Scene/collision internal contract

- `ISceneSpatialParticipant2D` remains an internal refresh/readiness owner,
  but `SimulationCatalog` is removed from the interface and implementers.
  No collection synthetic entry ID or pre-realization spatial metadata is
  introduced. `GetUnpreparedCollisionEntry` returns null for a simple
  collection; package/tile failures retain their real chunk ID.
- The current owner may expose an **internal** `Exception?
  CollisionReadinessError` accessor. It is null only when the requested
  snapshot is healthy and committed. A preflight-pending snapshot or terminal
  structural fault yields its actual readiness error; `PrepareRegionAsync`
  propagates that error and `IsSpatialRegionReady` stays false. This is not a
  new public error property or a renamed spatial provider.
- Collision interest comes from each marked `Collider2D.IsSimulated` with its
  own active scene geometry. The existing `CollisionMutationVersion` is the
  initial invalidation mechanism; no global all-collider pin, descendant
  inheritance, or speculative revision subsystem. Tile-only readiness and
  missing chunks continue to report real package/map chunk IDs.
- Map retirement tracks **known map-owned** source, residency, image/cache,
  and lease release failures: `StopSource` registers each retiring generation
  before cleanup, retains its errors for the terminal `DisposeAsync` drain,
  and continues draining other generations. This is not a blanket exception
  sink for arbitrary application lifecycle hooks. `RemoveCollisionNodes` can
  invoke `AttachSurface(null)` and `LogicalChildren.RemoveOwned`, including
  user-defined attach/detach hooks; structural hook failures remain
  synchronous/faulted under the selected tree contract, not silently folded
  into an unrelated map-resource drain.

## Ownership and verification boundary

The map/internal-spatial Sol owns `TileMap2D*.cs`, `TileMapSource2D.cs`,
`TileMapCatalog2D.cs`, internal spatial residency/source files,
`ISceneSpatialParticipant2D.cs`, `CollisionWorld2D.Streaming.cs`, and
`Collider2D.cs`. The package Sol owns package level/reader/compiler and its
package tests; the Playground Sol owns the package-caller showcase and its
test. The collection Sol owns `SceneItems2D.cs`, Tetris, and its named tests;
the mixed-scene regression Sol owns its named Core scene/Prism tests; the
native migration Sol owns six disjoint SDL scene/image/Prism tests.
Integration owns the Core assembly friend declaration, `UiMarkupGenerator.cs`
and affected SourceGen tests, two named benchmarks, cross-cone API/ApiCompat
evidence, the checklist, and build/test scheduling. Canonical API docs and
`manifest.json` have one separate writer. No two workers edit the same source
or generated output.

The Stage 2 post-implementation caller audit also found the Language semantic
and completion rules that still advertised `TileMap2D.Source`. Integration owns
their four exact Language source files and two direct Language test files. The
special cases now reject only that removed TileMap member; generic `Image.Source`
binding and direct static Tile declarations remain supported. The focused
RED→GREEN, full Language run, and SourceGen rerun are under
`integration/language/`. The internal Stage 4 tile benchmark is an intentional
Core-friend publication workload; it was not rewritten to public `FromModel`
because that would change the measured scenario. Its README now states this
limit; no performance result is asserted.

These signatures and owner boundaries were selected under the user's
delegated design authority. Their correctness is still subject to focused
RED→GREEN, affected suites, strict ApiCompat classification, public-surface
audit, canonical documentation, and Stage 2 independent audit.
