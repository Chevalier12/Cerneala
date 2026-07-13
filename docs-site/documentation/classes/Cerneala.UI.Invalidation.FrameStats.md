# FrameStats Class

## Definition
Namespace: `Cerneala.UI.Invalidation`  
Assembly/Project: `Cerneala`  
Source: `UI/Invalidation/FrameStats.cs`

Collects per-frame counters for retained UI invalidation, layout, rendering, hit testing, cache reuse, and motion work.

```csharp
public sealed class FrameStats
```

Inheritance:  
`Object` -> `FrameStats`

## Examples

```csharp
using Cerneala.UI.Invalidation;
using Cerneala.UI.Motion.Core;

FrameStats stats = new();

stats.Count(FramePhase.Measure);
stats.CountMeasureCall();
stats.CountMeasureCall();
stats.Count(FramePhase.RenderCache);
stats.CountMotion(MotionFrameResult.Empty(default));

bool hasWork = stats.HasWork;
```

The example records one queued measure element, two actual measure calls, and one render-cache element. `HasWork` returns `true` because queued measure or render work was recorded.

## Remarks

`FrameStats` is a mutable diagnostics container used by frame processing APIs such as `UIRoot.ProcessFrame`, `UiFrameScheduler.ProcessFrame`, `UiHost.Update`, `UiFrame.Stats`, and `FrameDiagnostics.Capture`. New instances start with all numeric counters at `0`.

The `Count(FramePhase)` method maps scheduler phases to the matching element counters. It increments counters for `InheritedProperties`, `CommandState`, `Aspect`, `Measure`, `Arrange`, `RenderCache`, and `HitTest`. The current implementation does not change any counter for `Input` or `Idle`.

Queued layout counters are separate from actual layout-pass counters. `MeasuredElements` and `ArrangedElements` count elements processed from the scheduler queues, while `MeasureCalls` and `ArrangeCalls` count recursive layout work performed after the element cache checks. Cache hits are not counted as layout passes.

`CountNoWorkFrame()` increments `NoWorkFrames` and also records one reused cache through `CountReusedCache()`. A no-work frame by itself does not make `HasWork` return `true`. `HasWork` also ignores `ReusedCaches`, `MeasureCalls`, and `ArrangeCalls`; it reports true for retained phase counters and motion counters that represent frame work.

Motion counters are accumulated from `MotionFrameResult` by `CountMotion(MotionFrameResult result)`.

## Constructors

| Name | Description |
| --- | --- |
| `FrameStats()` | Initializes a new stats container with all counters set to `0`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `InheritedElements` | `int` | Gets the number of elements processed for inherited property propagation. |
| `CommandStateElements` | `int` | Gets the number of elements processed for command-state refresh. |
| `AspectElements` | `int` | Gets the number of elements processed for aspect resolution. |
| `MeasuredElements` | `int` | Gets the number of elements processed from the measure queue. |
| `ArrangedElements` | `int` | Gets the number of elements processed from the arrange queue. |
| `MeasureCalls` | `int` | Gets the number of non-cached measure passes recorded during the frame. |
| `ArrangeCalls` | `int` | Gets the number of non-cached arrange passes recorded during the frame. |
| `RenderedElements` | `int` | Gets the number of elements processed for render-cache work. |
| `HitTestElements` | `int` | Gets the number of elements processed for hit-test cache work. |
| `ReusedCaches` | `int` | Gets the number of cache reuse events recorded for the frame. |
| `NoWorkFrames` | `int` | Gets the number of no-work frame events recorded on this stats instance. |
| `MotionFrames` | `int` | Gets the accumulated number of motion frame samples reported by motion processing. |
| `MotionNodesSampled` | `int` | Gets the accumulated number of sampled motion nodes. |
| `MotionValuesChanged` | `int` | Gets the accumulated number of motion values that changed. |
| `MotionPropertyWrites` | `int` | Gets the accumulated number of motion property writes. |
| `MotionCompleted` | `int` | Gets the accumulated number of completed motion entries. |
| `MotionRenderInvalidations` | `int` | Gets the accumulated number of render invalidations produced by motion. |
| `MotionLayoutInvalidations` | `int` | Gets the accumulated number of layout invalidations produced by motion. |
| `MotionSkippedByReducedMotion` | `int` | Gets the accumulated number of motion entries skipped because of reduced-motion behavior. |
| `HasWork` | `bool` | Gets a value indicating whether retained phase counters or motion counters recorded frame work. |

## Methods

| Name | Description |
| --- | --- |
| `Count(FramePhase phase)` | Increments the counter associated with a supported scheduler phase. |
| `CountReusedCache()` | Increments `ReusedCaches`. |
| `CountMeasureCall()` | Increments `MeasureCalls`. |
| `CountArrangeCall()` | Increments `ArrangeCalls`. |
| `CountNoWorkFrame()` | Increments `NoWorkFrames` and `ReusedCaches`. |
| `CountMotion(MotionFrameResult result)` | Adds all motion counters from `result` to this stats instance. |

## Applies to

Cerneala retained UI invalidation, frame scheduling, diagnostics, hosting, and motion frame reporting.

## See also

- `UI/Invalidation/UiFrameScheduler.cs`
- `UI/Elements/UIRoot.cs`
- `UI/Hosting/UiHost.cs`
- `UI/Diagnostics/FrameDiagnostics.cs`
- `UI/Motion/Core/MotionFrameResult.cs`
