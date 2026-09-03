# FrameDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/FrameDiagnostics.cs`

Captures and formats frame-level invalidation, layout, render-cache, hit-test, Motion, and Relay counters from a `FrameStats` instance.

```csharp
public static class FrameDiagnostics
```

Inheritance:
`Object` -> `FrameDiagnostics`

## Examples

Capture a snapshot from frame stats and format the same counters as a stable diagnostic line.

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Invalidation;

FrameStats stats = new();
stats.Count(FramePhase.Measure);
stats.CountMeasureCall();

FrameDiagnosticsSnapshot snapshot = FrameDiagnostics.Capture(stats);
int queuedMeasureElements = snapshot.QueuedMeasureElements;

string line = FrameDiagnostics.Format(stats);
```

## Remarks

`FrameDiagnostics` is a small adapter over `Cerneala.UI.Invalidation.FrameStats`. `Capture` copies the current counter values into an immutable `FrameDiagnosticsSnapshot`; `Format` captures the same snapshot and returns its invariant-culture string representation.

The formatted output uses stable counter names such as `queuedMeasure`, `queuedArrange`, `measureCalls`, `renderCache`, `motionRender`, `relayExecuted`, `relayBacklog`, and `hasWork`. The `HasWork` value is copied from `FrameStats.HasWork`; reused caches, no-work frames, and raw measure/arrange call counters do not by themselves make that flag true.

`Detective.Capture` uses this class to embed frame diagnostics inside a wider root snapshot.

## Methods

| Name | Description |
| --- | --- |
| `Capture(FrameStats stats)` | Creates a `FrameDiagnosticsSnapshot` by copying the current counters from `stats`. Throws `ArgumentNullException` when `stats` is `null`. |
| `Format(FrameStats stats)` | Returns `Capture(stats).ToString()`. Throws `ArgumentNullException` when `stats` is `null`. |

## Snapshot Values

`Capture` returns a `FrameDiagnosticsSnapshot` record with these public values.

| Name | Type | Description |
| --- | --- | --- |
| `InheritedElements` | `int` | Number of elements processed for inherited-property propagation. |
| `CommandStateElements` | `int` | Number of elements processed for command-state refresh. |
| `AspectElements` | `int` | Number of elements processed by the aspect phase. |
| `QueuedMeasureElements` | `int` | Number of elements counted in the measure phase. |
| `QueuedArrangeElements` | `int` | Number of elements counted in the arrange phase. |
| `MeasureCalls` | `int` | Number of measure calls counted during the frame. |
| `ArrangeCalls` | `int` | Number of arrange calls counted during the frame. |
| `RenderedElements` | `int` | Number of elements processed for render-cache work. |
| `HitTestElements` | `int` | Number of elements processed for hit-test work. |
| `ReusedCaches` | `int` | Number of reused caches counted for the frame. |
| `NoWorkFrames` | `int` | Number of no-work frames counted. |
| `MotionFrames` | `int` | Number of motion frames reported by motion processing. |
| `MotionNodesSampled` | `int` | Number of motion nodes sampled. |
| `MotionValuesChanged` | `int` | Number of motion values that changed. |
| `MotionPropertyWrites` | `int` | Number of motion property writes. |
| `MotionCompleted` | `int` | Number of completed motion entries. |
| `MotionRenderInvalidations` | `int` | Number of render invalidations caused by motion. |
| `MotionLayoutInvalidations` | `int` | Number of layout invalidations caused by motion. |
| `MotionSkippedByReducedMotion` | `int` | Number of motion entries skipped because reduced motion was active. |
| `RelaySnapshotCallbacks` | `int` | Number of callbacks included in the Relay drain snapshot. |
| `RelayDequeuedCallbacks` | `int` | Number of Relay callbacks dequeued during the frame. |
| `RelayExecutedCallbacks` | `int` | Number of Relay callbacks executed during the frame. |
| `RelayCanceledCallbacks` | `int` | Number of canceled Relay callbacks observed during the frame. |
| `RelayFaultedCallbacks` | `int` | Number of faulted Relay callbacks observed during the frame. |
| `RelayDeferredCallbacks` | `int` | Number of Relay callbacks deferred to a later drain. |
| `RelayBacklog` | `int` | Number of Relay callbacks remaining after the drain. |
| `HasWork` | `bool` | Indicates whether `FrameStats` reported frame work. |

## Applies to

Cerneala UI diagnostics in the `Cerneala` project.

## See also

- `Cerneala.UI.Detective.Detective`
- `Cerneala.UI.Detective.FrameDiagnosticsSnapshot`
- `Cerneala.UI.Invalidation.FrameStats`
