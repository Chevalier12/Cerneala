# Stage 2 map, spatial, and collision owner verification

This is the map owner's scoped checkpoint, not the Stage 2 gate or a full-repository result. All commands below used the current Release `net8.0-windows` Core test assembly and `-p:DefaultItemExcludesInProjectFolder=artifacts/**`. The first accidental Debug no-build invocation matched **zero** tests (`core-marker-drain.log`); it is retained as raw evidence and is **not** counted as verification.

## Contract migration and observed failures

- The old `SceneCollisionStreamingTests` used a publicly spatial `SceneItems2D` fixture. It now exercises actual private `TileMapSource2D` chunks and their real local `grid:x:0:1:1` `EntryId` values. Explicit region sharing, isolation of a failed *different map*, contact fringe, cancellation and late release, stale version retirement, transforms, and worker publication remain tested. The former pre-load `SceneSpatialEntry2D.IsSimulated` bootstrap is intentionally gone; its replacement uses already-realized `Collider2D.IsSimulated` actors. Frozen contract: proposal §4 (lines 114–120), matrix rows 18–20.
- `TileMapSpatialResidencyTests.SimulatedActorKeepsTerrainCollisionWithoutKeepingOffCameraImages` now marks the actor's real collider near the far tile; no synthetic SceneItems spatial source is created. `ModelPublicationInsideAnotherMaterializerDoesNotExposeRetiredTerrain` uses an eager enumerable publisher. During its preflight, the collision query must reject the uncommitted requested snapshot with `InvalidOperationException`; the test also checks the old far collider was retired during map publication and that the committed post-factory query has no stale hit. Frozen contract: proposal §2 lines 42–46 and §4 line 114, matrix rows 15–19.
- The first migrated collision/spatial run passed 55/57. A simulated-actor test timed out with **0** map collider children, **0** chunk loads, and **2** collision interests (`simulated-instrument-red.log`). The fixture had only pumped `Root.ProcessFrame` after changing markers; it had not performed the surface's time-sensitive spatial update. Adding an explicit fixture `Tick()` after marker add/remove made the two focused failures pass 2/2 (`collision-two-repair-first.log`). This is a test update-path correction, not a production collider change.
- `TileMapStreamingTests.ReleaseFailuresRemainObservableAndDoNotRetainSiblingPayloads` originally expected context retirement to throw map-owned lease-release errors synchronously. That conflicts with the selected terminal map drain. The `throughContext:true` case now completes owner-thread context retirement and map detach, then awaits `TileMap2D.DisposeAsync()` for the aggregate release error. The direct explicit-region `throughContext:false` synchronous error path and both sibling-release assertions remain. Frozen contract: proposal §3 line 110, matrix row 26. The first broad run was 231/232 with only this obsolete timing assertion failing; the narrow repaired theory passed 2/2.

## Verification

| Evidence | Result |
| --- | --- |
| `core-tests-release-observable-build.log` | Release Core.Tests build: 0 warnings, 0 errors |
| `collision-two-repair-first.log` / TRX | 2/2 focused migrated failures GREEN |
| `release-observable-repair.log` / TRX | 2/2 release-error theory GREEN |
| `core-map-owned-focused-green.log` / TRX | 63/63 new and migrated owner tests GREEN |
| `core-map-spatial-broad-green.log` / TRX | 232/232 affected map, spatial, and collision classes GREEN |
| `core-marker-drain-final.log` / TRX | 6/6 new marker and terminal-drain contracts GREEN |

The broad filter includes the map source/catalog, spatial residency/source/selection, map construction/cache/presentation/streaming, Stage 3 tile collision characterization, migrated scene collision streaming, and new collider/drain classes. The exact filter is in the invoking task's command record. These results do **not** cover the full Core suite, SourceGen, package/importer/Tetris suites, native SDL, visual conformance, performance, ApiCompat, or canonical documentation gates; the root integration owner coordinates those separately.

The Stage 4 benchmark still uses the internal Core-friend `TileMapSource2D.SetCatalog` seam. No node-replacement proxy was substituted for its one-chunk publication measurement. The four shared conformance/capture fixtures were migrated to public `TileMap2D.FromModel`; the native package grid fixture uses `CreateTileMap("map")`, detaches/drains both maps, then awaits package terminal disposal. Those non-Core fixtures have not been run in this owner checkpoint.
