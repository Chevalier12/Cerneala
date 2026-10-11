# MotionCompletion Enum

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionCompletion.cs`

Names two completion states, `Running` and `Complete`.

```csharp
public enum MotionCompletion
```

## Remarks

No Cerneala API takes or returns `MotionCompletion`, and the motion runtime does not read it. It is not accepted in markup.

To find out whether a motion is still running, use these members instead:

- `MotionHandle.IsActive`, `MotionHandle.IsCompleted`, and `MotionHandle.IsCanceled` for one started motion.
- `MotionCompletedEventArgs.State`, a `MotionCompletionState`, in the `MotionHandle.Completed` event.
- `MotionValue<T>.IsAnimating` for a motion value.
- `MotionSampler<T>.IsComplete` for a sampler.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Running` | `0` | The motion is still running. |
| `Complete` | `1` | The motion is complete. |

## Applies to

Cerneala motion specs.

## See also

- `Cerneala.UI.Motion.Core.MotionCompletionState`
- `Cerneala.UI.Motion.Core.MotionHandle`
