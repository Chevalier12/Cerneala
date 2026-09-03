# MotionDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/MotionDiagnostics.cs`

Records optional motion trace events, per-frame diagnostic counters, warnings, and snapshot data for a `MotionSystem`.

```csharp
public sealed class MotionDiagnostics
```

Inheritance:
`object` -> `MotionDiagnostics`

## Examples

Enable tracing on a root-owned motion system, sample an animation, and inspect the recorded event kinds:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Detective;
using Cerneala.UI.Motion.Specs;

SystemMotionClock clock = new();
UIRoot root = new(100, 100, motionClock: clock);
root.Detective.Motion.IsEnabled = true;

MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
value.AnimateTo(1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(10)));

root.Motion.Tick();

IReadOnlyList<MotionTraceEvent> events = root.Detective.Motion.Trace.Events;
```

Capture a graph snapshot from the owning motion system:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Detective;

UIRoot root = new();
MotionGraphSnapshot snapshot = root.Detective.Motion.CreateSnapshot(root.Motion);

int activeNodes = snapshot.ActiveNodeCount;
bool needsAnotherFrame = snapshot.NeedsAnotherFrame;
```

## Remarks

`MotionDiagnostics` is created by `MotionSystem` and exposed through `MotionSystem.Diagnostics`. It keeps a reusable `MotionTrace`, the current diagnostic phase list, warnings, layout snapshot capture counts, and the number of motions skipped by reduced-motion handling.

Trace event recording is opt-in. `Record` returns without changing the trace when `IsEnabled` is `false`; when enabled, it appends a `MotionTraceEvent` with the supplied `MotionTraceEventKind` and optional debug name. `MotionTrace.Clear` clears the accumulated trace events.

Warnings are independent from `IsEnabled`. `RecordWarning` always validates and stores a non-empty message in `Warnings`. The per-frame phase and warning collections are cleared by the internal frame pipeline when it begins a frame.

`CreateSnapshot` reads aggregate state from a supplied `MotionSystem`: active graph nodes, property bindings that are actually animating or have pending samples, active layout motions, active presence exits, the most recent frame's sampled-node and property-write counts, and whether another frame is needed. Retained but idle property bindings are not reported as active.

## Constructors

| Name | Description |
| --- | --- |
| `MotionDiagnostics()` | Initializes an empty diagnostics recorder with tracing disabled. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsEnabled` | `bool` | Gets or sets whether `Record` appends trace events to `Trace`. The default is `false`. |
| `Trace` | `MotionTrace` | Gets the trace object that stores recorded `MotionTraceEvent` values. |
| `Phases` | `IReadOnlyList<MotionFramePhase>` | Gets the motion frame phases recorded for the current diagnostic frame. |
| `Warnings` | `IReadOnlyList<string>` | Gets diagnostic warning messages recorded for the current diagnostic frame. |
| `BeforeLayoutSnapshotCaptures` | `int` | Gets the number of before-layout snapshot capture requests recorded for the current frame. |
| `AfterLayoutSnapshotCaptures` | `int` | Gets the number of after-layout snapshot capture requests recorded for the current frame. |
| `ReducedMotionSkipCount` | `int` | Gets the total number of reduced-motion skips recorded by the motion pipeline. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `RecordWarning(string message)` | `void` | Validates and appends a diagnostic warning message. |
| `Record(MotionTraceEventKind kind, string? debugName = null)` | `void` | Appends a trace event when `IsEnabled` is `true`; otherwise does nothing. |
| `CreateSnapshot(MotionSystem motion)` | `MotionGraphSnapshot` | Creates a snapshot from the supplied motion system's graph, property, layout, presence, and active-frame state. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RecordWarning(string message)` | `ArgumentException` | `message` is `null`, empty, or whitespace. |
| `CreateSnapshot(MotionSystem motion)` | `ArgumentNullException` | `motion` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Detective.MotionTrace`
- `Cerneala.UI.Detective.MotionGraphSnapshot`
