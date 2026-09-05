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

Most fields describe the latest map recording. `Promotions` and `Demotions` are cumulative explicit-operation counters as copied at that recording. `TileInvalidations` counts model replacements since the preceding recording. Retained bytes/objects describe the map's managed retained estimates, not GPU memory. Cache release can clear those retained estimates without another recording. A never-recorded map starts with zero counters.

Capturing requires a map currently attached to the Detective's root. Snapshots contain values only and do not retain the map, scene, or graphics resources. Collection remains the existing internal tilemap bookkeeping; this API adds no per-frame subscription or trace. The focused test measures zero managed allocations for 10,000 snapshot calls after 256 warmup calls; this is not a zero-allocation claim for map rendering or JSON serialization.

## Properties

| Name | Description |
| --- | --- |
| `TotalChunks` | Number of chunks in the model's layers. |
| `CandidateChunks`, `VisibleChunks` | Chunk candidates and chunks selected by the last recording's culling. |
| `CandidateTiles`, `DrawnTiles` | Candidate static cells and recorded nonempty tiles, including visible promotions in the latter. |
| `BatchesBuilt`, `BatchesRebuilt`, `BatchesReused` | New, replaced, and reused retained static batches for that recording. |
| `DrawCommands` | Recorded tile batch/image commands, not native GPU draw calls. |
| `RetainedBytes`, `RetainedObjects` | Managed retained-cache estimates, not total process or GPU memory. |
| `TileInvalidations` | Model replacement invalidations accumulated before that recording. |
| `PromotedInstancesVisible`, `PromotedInstancesCulled` | Promoted instances recorded or rejected by viewport culling. |
| `Promotions`, `Demotions` | Explicit promotion/demotion operation totals copied at recording time. Declarative promoted children do not count as calls to `Promote`. |
| `BatchSplits` | Static order-segment splits around promoted instances. |

## See also

- [Detective](Cerneala.UI.Detective.Detective.md)
- [TileMap2D](Cerneala.UI.Controls.TileMap2D.md)
