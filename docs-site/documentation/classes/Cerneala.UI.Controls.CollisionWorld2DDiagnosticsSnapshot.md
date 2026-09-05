# CollisionWorld2DDiagnosticsSnapshot Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CollisionWorld2DDiagnosticsSnapshot.cs`

Captures immutable cumulative collision-world diagnostics at one point in time.

```csharp
public sealed class CollisionWorld2DDiagnosticsSnapshot
```

## Examples

```csharp
CollisionWorld2DDiagnosticsSnapshot before = scene.CollisionWorld.GetDiagnosticsSnapshot();
playerCollider.TranslateX += 1;
CollisionWorld2DDiagnosticsSnapshot after = scene.CollisionWorld.GetDiagnosticsSnapshot();

bool updatedIncrementally = after.RebuildCount == before.RebuildCount;
```

## Remarks

Candidate and exact-test counts are cumulative. Query timing uses `Stopwatch` on the current process and is diagnostic rather than a cross-machine performance guarantee. `EstimatedRetainedBytes` describes retained broadphase storage; it is not a managed-heap measurement of the complete scene.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `EntryCount` | `int` | Active indexed colliders. |
| `CellCount` | `int` | Occupied sparse-grid cells. |
| `BroadphaseCandidateCount` | `long` | Candidates emitted by the broadphase. |
| `ExactTestCount` | `long` | Exact shape tests performed after filtering. |
| `RebuildCount` | `long` | Full index builds. |
| `IncrementalUpdateCount` | `long` | Applied mutation notifications. |
| `UpdatedEntryCount` | `long` | Entries added, removed, or refreshed incrementally. |
| `QueryCount` | `long` | Timed world queries completed. |
| `LastQueryDuration` | `TimeSpan` | Duration of the latest query. |
| `TotalQueryDuration` | `TimeSpan` | Cumulative query duration. |
| `EstimatedRetainedBytes` | `long` | Estimated broadphase retained storage. |

## Applies to

Project: `Cerneala`

## See also

- `CollisionWorld2D`
