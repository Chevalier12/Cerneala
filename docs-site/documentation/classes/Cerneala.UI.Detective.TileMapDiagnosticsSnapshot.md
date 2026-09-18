# TileMapDiagnosticsSnapshot Struct

## Definition

Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/TileMapDiagnosticsSnapshot.cs`

An immutable value snapshot of tilemap recording and retained-cache counters.

```csharp
public readonly record struct TileMapDiagnosticsSnapshot
```

## Examples

Read after a frame that records the attached map:

```csharp
TileMapDiagnosticsSnapshot snapshot = root.Detective.CaptureTileMap(map);
Console.WriteLine($"Chunks {snapshot.VisibleChunks}/{snapshot.TotalChunks}; " +
    $"rebuilt {snapshot.BatchesRebuilt}, reused {snapshot.BatchesReused}");
```

## Remarks

`Detective.CaptureTileMap` copies already collected values. It does not request a frame, rebuild geometry, load resources, run collision queries, or reset counters. Repeated reads without another recording return the same values. A retained frame that does not record the map leaves the previous recording's counters intact; they are not measurements of work executed by the capture call or necessarily the current UI frame.

The fields describe the latest map recording. `TileInvalidations` counts model replacements since the preceding recording. Retained bytes/objects describe the map's managed retained estimates, not GPU memory. Cache release can clear those retained estimates without another recording. A never-recorded map starts with zero counters.

For grid maps, `RetainedBytes` includes visible and optional warm entries; subtract `WarmRetainedBytes` to obtain the visible-entry estimate. `WarmChargedBytes` uses a different accounting basis: measured managed construction allocations (including temporary builders) plus logical RGBA image bytes exclusive to warm entries. `WarmImageBytes` is the latter portion. Shared images also required by visible entries are not charged again. Neither value is a process/RSS/native/GPU measurement or includes caller-held models and the spatial metadata index.

`BatchesBuilt` includes optional preparation; `BatchesRebuilt` and `BatchesReused` describe entries consumed by visible recording. `WarmBatchesPrepared` and `WarmTilesPrepared` expose additional work inside that same recording, including a tentative build that is rejected by final budget admission. Warm preparation emits no draw commands. Cache release clears warm residency counts/bytes, while work counters remain historical until the next recording. See [TileMap2D](Cerneala.UI.Controls.TileMap2D.md) for the spatial, cell-work and charge limits.

Read a completed surface recording for final warm and retained values. Optional preparation and cache retirement finish after required scene commands, so a capture made while another node is still recording can see intermediate counters. The 256-cell optional allowance is shared by all maps on that surface recording, not granted independently to every snapshot owner.

Capturing requires a map currently attached to the Detective's root. Snapshots contain values only and do not retain the map, scene, or graphics resources. Collection remains the existing internal tilemap bookkeeping; this API adds no per-frame subscription or trace. The focused test measures zero managed allocations for 10,000 snapshot calls after 256 warmup calls; this is not a zero-allocation claim for map rendering or JSON serialization.

## Properties

| Name | Description |
| --- | --- |
| `TotalChunks` | Number of grid chunks or retained free-placement chunks in this map. |
| `CandidateChunks`, `VisibleChunks` | Chunk candidates and chunks selected by the last recording's culling. |
| `CandidateTiles`, `DrawnTiles` | Candidate static cells/placements and recorded nonempty tiles. |
| `BatchesBuilt` | All new retained static batches constructed during recording, including optional preparation. |
| `BatchesRebuilt`, `BatchesReused` | Constructed and reused batches used by the visible recording. |
| `DrawCommands` | Recorded tile batch commands, not native GPU draw calls. |
| `RetainedBytes`, `RetainedObjects` | Managed retained-cache estimates, not total process or GPU memory. |
| `TileInvalidations` | Model replacement invalidations accumulated before that recording. |
| `WarmChunks` | Admitted optional grid entries after the latest recording; zero after cache release. |
| `WarmBatchesPrepared`, `WarmTilesPrepared` | Additional batch/cell preparation work inside that recording, not extra drawing. |
| `WarmRetainedBytes` | Warm-entry portion of the historical retained-cache estimate. |
| `WarmChargedBytes` | Optional budget charge: measured managed construction allocations plus warm-only logical RGBA image bytes. |
| `WarmImageBytes` | Unique warm-only image portion of `WarmChargedBytes`. |

## See also

- [Detective](Cerneala.UI.Detective.Detective.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
