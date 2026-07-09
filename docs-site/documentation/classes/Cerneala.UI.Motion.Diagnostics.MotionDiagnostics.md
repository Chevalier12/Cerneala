# MotionDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Motion.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Diagnostics/MotionDiagnostics.cs`

Records optional motion trace events, per-frame diagnostic counters, warnings, and snapshot data for a `MotionSystem`.

```csharp
public sealed class MotionDiagnostics
```

Inheritance:
`object` -> `MotionDiagnostics`

## Examples

Enable tracing on a root-owned motion system, run an animation, and inspect the recorded event kinds:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Specs;

ManualMotionClock clock = new();
UIRoot root = new(100, 100, motionClock: clock);
root.Motion.Diagnostics.IsEnabled = true;

MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
value.AnimateTo(1f, Motion.Tween<float>(TimeSpan.FromMilliseconds(10)));

root.Motion.Tick();
clock.Advance(TimeSpan.FromMilliseconds(10));
root.Motion.Tick();

IReadOnlyList<MotionTraceEvent> events = root.Motion.Diagnostics.Trace.Events;
```

Capture a graph snapshot from the owning motion system:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Diagnostics;

UIRoot root = new();
MotionGraphSnapshot snapshot = root.Motion.Diagnostics.CreateSnapshot(root.Motion);

int activeNodes = snapshot.ActiveNodeCount;
bool needsAnotherFrame = snapshot.NeedsAnotherFrame;
```

## Remarks

`MotionDiagnostics` is created by `MotionSystem` and exposed through `MotionSystem.Diagnostics`. It keeps a reusable `MotionTrace`, the current diagnostic phase list, warnings, layout snapshot capture counts, and the number of motions skipped by reduced-motion handling.

Trace event recording is opt-in. `Record` returns without changing the trace when `IsEnabled` is `false`; when enabled, it appends a `MotionTraceEvent` with the supplied `MotionTraceEventKind` and optional debug name. `MotionTrace.Clear` clears the accumulated trace events.

Warnings are independent from `IsEnabled`. `RecordWarning` always validates and stores a non-empty message in `Warnings`. The per-frame phase and warning collections are cleared by the internal frame pipeline when it begins a frame.

`CreateSnapshot` reads aggregate state from a supplied `MotionSystem`: active graph nodes, active property bindings, active layout motion bindings, active presence exits, and whether another frame is needed. The current implementation reports `0` for `ValuesSampledThisFrame` and `PropertiesWrittenThisFrame`.

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
- `Cerneala.UI.Motion.Diagnostics.MotionTrace`
- `Cerneala.UI.Motion.Diagnostics.MotionGraphSnapshot`
