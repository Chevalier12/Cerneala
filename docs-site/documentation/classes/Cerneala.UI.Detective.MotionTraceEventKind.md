# MotionTraceEventKind Enum

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/MotionTraceEvent.cs`

Identifies what happened to a motion value in a `MotionTraceEvent`.

```csharp
public enum MotionTraceEventKind
```

## Examples

With reduced motion set to `Reduce`, a tween is skipped and completes at once. The three events are recorded synchronously by `AnimateTo`.

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;

UIRoot root = new(reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
root.Detective.Motion.IsEnabled = true;

root.Motion.Graph.CreateValue(0f).AnimateTo(
    1f,
    Cerneala.UI.Motion.Specs.Motion.Tween<float>(TimeSpan.FromMilliseconds(200)),
    new MotionStartOptions(DebugName: "fade"));

foreach (MotionTraceEvent traceEvent in root.Detective.Motion.Trace.Events)
{
    Console.WriteLine(traceEvent);
}

// MotionTraceEvent { Kind = MotionSkippedReducedMotion, DebugName = fade }
// MotionTraceEvent { Kind = MotionStarted, DebugName =  }
// MotionTraceEvent { Kind = MotionCompleted, DebugName =  }
```

Without reduced motion, the same tween records `MotionStarted`, then one `MotionSampled` per frame while it runs, then `MotionCompleted`.

## Remarks

Events are recorded only while `root.Detective.Motion.IsEnabled` is `true`; it is `false` by default. Only motion values created from the root's graph, `UIRoot.Motion.Graph`, are recorded. Object motion (`.Motion()` called on an object that is not a `UIElement` or a `TimbrePlayback`), `ManualMotionTimeline`, and Timbre playback motion use graphs without diagnostics and record nothing. A `MotionGraph` you create yourself records when you pass it a `MotionDiagnostics`.

`MotionTrace.Events` keeps every event until you call `MotionTrace.Clear()`. It has no capacity limit.

Only four kinds are currently produced:

| Kind | Recorded when | `DebugName` |
| --- | --- | --- |
| `MotionStarted` | `AnimateTo` starts a new motion. Retargeting a running motion records nothing. | `null` |
| `MotionSampled` | The graph advances a running motion value, once per value per frame. | `null` |
| `MotionCompleted` | A motion finishes by itself or through `MotionHandle.Complete()`. | `null` |
| `MotionSkippedReducedMotion` | Reduced motion replaces the animation: a `Tween` with a duration under `ReducedMotionMode.Reduce`, or an endless `Repeat` or `PingPong` under any mode except `NoPreference`. | `MotionStartOptions.DebugName` |

`MotionRetargeted`, `MotionCanceled`, `MotionPropertyWritten`, `MotionInvalidatedRender`, and `MotionInvalidatedLayout` are declared but not recorded anywhere in the framework. You can record any kind yourself with `MotionDiagnostics.Record`.

`MotionDiagnostics.ReducedMotionSkipCount` counts reduced-motion skips even while `IsEnabled` is `false`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `MotionStarted` | `0` | A motion started. |
| `MotionRetargeted` | `1` | A running motion was retargeted. Not recorded by the framework. |
| `MotionSampled` | `2` | A running motion was advanced by one frame. |
| `MotionCompleted` | `3` | A motion completed. |
| `MotionCanceled` | `4` | A motion was canceled. Not recorded by the framework. |
| `MotionPropertyWritten` | `5` | A motion wrote a property. Not recorded by the framework. |
| `MotionInvalidatedRender` | `6` | A motion write invalidated rendering. Not recorded by the framework. |
| `MotionInvalidatedLayout` | `7` | A motion write invalidated layout. Not recorded by the framework. |
| `MotionSkippedReducedMotion` | `8` | Reduced motion replaced an animation with its final value. |

## Applies to

`MotionTraceEvent`, `MotionTrace`, and `MotionDiagnostics`.

## See also

- `Cerneala.UI.Detective.MotionTraceEvent`
- `Cerneala.UI.Detective.MotionTrace`
- `Cerneala.UI.Detective.MotionDiagnostics`
- `Cerneala.UI.Detective.Detective`
