# FramePhase Enum

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/FramePhase.cs`

Identifies a phase of the work that `UIRoot.ProcessFrame` runs for one frame.

```csharp
public enum FramePhase
```

## Examples

An invalidation trace shows the phases of a frame with work, and of a frame without work:

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

InvalidationTrace trace = new();
UIRoot root = new(trace, 200, 100);
root.VisualChildren.Add(new UIElement { Width = 50, Height = 20 });

root.ProcessFrame();
Console.WriteLine(string.Join(" ", trace.Entries
    .Where(e => e.Kind == InvalidationTraceEventKind.PhaseSummary)
    .Select(e => e.Phase)));
// InheritedProperties CommandState Aspect InheritedProperties Measure Arrange RenderCache HitTest

int before = trace.Entries.Count;
FrameStats idle = root.ProcessFrame();
Console.WriteLine(string.Join(" ", trace.Entries
    .Skip(before)
    .Where(e => e.Kind == InvalidationTraceEventKind.PhaseSummary)
    .Select(e => e.Phase)) + " " + idle.NoWorkFrames);
// Idle 1
```

## Remarks

A frame with work runs the phases in this order: `InheritedProperties`, `CommandState`, `Aspect`, `InheritedProperties` again (for values that the aspect phase changed), `Measure`, `Arrange`, `RenderCache`, `HitTest`. Motion runs between these phases but is not a `FramePhase`.

A frame without queued work and without active motion runs none of them. It counts one `FrameStats.NoWorkFrames` and records an `Idle` summary in the trace.

`FramePhase` appears in three public places:

| API | Use |
| --- | --- |
| `FrameStats.Count(FramePhase)` | Adds one to the counter of the phase: `InheritedElements`, `CommandStateElements`, `AspectElements`, `MeasuredElements`, `ArrangedElements`, `RenderedElements`, or `HitTestElements`. `Input` and `Idle` change no counter. |
| `InvalidationTrace.RecordPhase` and `RecordPhaseSummary` | Record one processed element, or the number of elements a phase processed. `InvalidationTraceEntry.Phase` returns the phase. |
| `FramePhaseProcessors` | Has one optional callback per element phase (`InheritedProperties`, `CommandState`, `Aspect`, `Measure`, `Arrange`, `RenderCache`, `HitTest`), called for each element the phase processes. |

`Input` is not used by the frame scheduler. Input is dispatched by `UiHost.Update`, outside these phases.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Input` | `0` | Input processing. Not recorded by the frame scheduler. |
| `InheritedProperties` | `1` | Propagates inherited property values. |
| `CommandState` | `2` | Refreshes command availability, for example `CanExecute`. |
| `Aspect` | `3` | Applies aspects (styling). |
| `Measure` | `4` | Measures elements. |
| `Arrange` | `5` | Arranges elements. |
| `RenderCache` | `6` | Rebuilds render caches. |
| `HitTest` | `7` | Updates hit-testing data. |
| `Idle` | `8` | A frame with no work. |

## Applies to

`FrameStats`, `InvalidationTrace`, `InvalidationTraceEntry`, and `FramePhaseProcessors`.

## See also

- `Cerneala.UI.Invalidation.FrameStats`
- `Cerneala.UI.Invalidation.FramePhaseProcessors`
- `Cerneala.UI.Detective.InvalidationTrace`
- `Cerneala.UI.Elements.UIRoot`
