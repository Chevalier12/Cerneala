# FrameDiagnosticsSnapshot Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/FrameDiagnostics.cs`

Represents an immutable snapshot of per-frame retained UI scheduler, render cache, hit-test, Motion, and Relay counters.

```csharp
public sealed record FrameDiagnosticsSnapshot(
    int InheritedElements,
    int CommandStateElements,
    int AspectElements,
    int QueuedMeasureElements,
    int QueuedArrangeElements,
    int MeasureCalls,
    int ArrangeCalls,
    int RenderedElements,
    int HitTestElements,
    int ReusedCaches,
    int NoWorkFrames,
    int MotionFrames,
    int MotionNodesSampled,
    int MotionValuesChanged,
    int MotionPropertyWrites,
    int MotionCompleted,
    int MotionRenderInvalidations,
    int MotionLayoutInvalidations,
    int MotionSkippedByReducedMotion,
    bool HasWork)
```

Inheritance:
`object` -> `FrameDiagnosticsSnapshot`

## Examples

Capture frame counters from a `FrameStats` instance and read the queued layout work:

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Invalidation;

FrameStats stats = new();
stats.Count(FramePhase.Measure);
stats.CountMeasureCall();
stats.Count(FramePhase.RenderCache);

FrameDiagnosticsSnapshot snapshot = FrameDiagnostics.Capture(stats);

int queuedMeasure = snapshot.QueuedMeasureElements;
int measureCalls = snapshot.MeasureCalls;
bool hasFrameWork = snapshot.HasWork;
string line = snapshot.ToString();
```

## Remarks

`FrameDiagnosticsSnapshot` is the value returned by `FrameDiagnostics.Capture(FrameStats)`. The capture copies counters from `FrameStats` into a stable record shape that can be stored, inspected, or formatted without keeping a mutable `FrameStats` instance around.

When captured through `FrameDiagnostics.Capture`, `QueuedMeasureElements` is copied from `FrameStats.MeasuredElements`, and `QueuedArrangeElements` is copied from `FrameStats.ArrangedElements`. `HasWork` is copied from `FrameStats.HasWork`.

The primary constructor preserves the original frame and Motion counter contract and initializes every Relay counter to zero. The overload that accepts Relay counters is used by `FrameDiagnostics.Capture(FrameStats)`. Neither constructor validates counter values; code that constructs the record directly is responsible for passing meaningful values.

`ToString()` uses invariant culture and returns a compact diagnostics line with stable counter names. The formatted string includes queued layout, layout calls, render cache, hit-test, cache reuse, no-work frame, Motion, Relay, and `HasWork` values.

## Constructors

| Name | Description |
| --- | --- |
| `FrameDiagnosticsSnapshot(..., bool HasWork)` | Initializes a snapshot with explicit frame, render, hit-test, and Motion counter values; Relay counters default to zero. |
| `FrameDiagnosticsSnapshot(..., int RelaySnapshotCallbacks, ..., bool HasWork)` | Initializes a snapshot with explicit frame, render, hit-test, Motion, and Relay counter values. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `InheritedElements` | `int` | Number of elements processed during the inherited properties frame phase. |
| `CommandStateElements` | `int` | Number of elements processed during the command state frame phase. |
| `AspectElements` | `int` | Number of elements processed during the aspect frame phase. |
| `QueuedMeasureElements` | `int` | Number of elements counted for the measure queue when captured from `FrameStats.MeasuredElements`. |
| `QueuedArrangeElements` | `int` | Number of elements counted for the arrange queue when captured from `FrameStats.ArrangedElements`. |
| `MeasureCalls` | `int` | Number of measure calls counted for the frame. |
| `ArrangeCalls` | `int` | Number of arrange calls counted for the frame. |
| `RenderedElements` | `int` | Number of elements processed for the render cache frame phase. |
| `HitTestElements` | `int` | Number of elements processed for the hit-test frame phase. |
| `ReusedCaches` | `int` | Number of reused caches counted for the frame. |
| `NoWorkFrames` | `int` | Number of no-work frames counted. |
| `MotionFrames` | `int` | Number of motion frames reported by motion processing. |
| `MotionNodesSampled` | `int` | Number of motion nodes sampled. |
| `MotionValuesChanged` | `int` | Number of motion values that changed. |
| `MotionPropertyWrites` | `int` | Number of motion property writes. |
| `MotionCompleted` | `int` | Number of completed motion operations. |
| `MotionRenderInvalidations` | `int` | Number of render invalidations caused by motion. |
| `MotionLayoutInvalidations` | `int` | Number of layout invalidations caused by motion. |
| `MotionSkippedByReducedMotion` | `int` | Number of motion operations skipped because of reduced-motion behavior. |
| `RelaySnapshotCallbacks` | `int` | Number of callbacks included in the Relay drain snapshot. |
| `RelayDequeuedCallbacks` | `int` | Number of Relay callbacks dequeued during the frame. |
| `RelayExecutedCallbacks` | `int` | Number of Relay callbacks executed during the frame. |
| `RelayCanceledCallbacks` | `int` | Number of canceled Relay callbacks observed during the frame. |
| `RelayFaultedCallbacks` | `int` | Number of faulted Relay callbacks observed during the frame. |
| `RelayDeferredCallbacks` | `int` | Number of Relay callbacks deferred to a later drain. |
| `RelayBacklog` | `int` | Number of Relay callbacks remaining after the drain. |
| `HasWork` | `bool` | Indicates whether captured frame counters reported work. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Formats selected counters into a stable invariant-culture diagnostics line. |
| `Deconstruct(...)` | `void` | Deconstructs the positional record into its original frame, render, hit-test, Motion, and `HasWork` component values. |

## Applies to

Cerneala retained UI diagnostics.

## See also

- `Cerneala.UI.Detective.FrameDiagnostics`
- `Cerneala.UI.Invalidation.FrameStats`
- `Cerneala.UI.Detective.DetectiveSnapshot`
