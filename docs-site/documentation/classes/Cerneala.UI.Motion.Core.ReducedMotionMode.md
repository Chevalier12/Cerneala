# ReducedMotionMode Enum

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/ReducedMotionMode.cs`

Specifies how much motion a UI root plays.

```csharp
public enum ReducedMotionMode
```

## Examples

With `Reduce`, a tween finishes as soon as it starts:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

UIRoot root = new(100, 100, reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));
MotionValue<float> value = root.Motion.Graph.CreateValue(0f);
MotionHandle handle = value.AnimateTo(10f, Motion.Tween<float>(TimeSpan.FromMilliseconds(200)));

Console.WriteLine(value.Current);      // 10
Console.WriteLine(handle.IsCompleted); // True
```

## Remarks

The mode is held by `ReducedMotionPolicy.Mode`. A `UIRoot` gets its policy from the `reducedMotion` constructor parameter; when that is `null`, the policy starts with `NoPreference`. `UIRoot.SetPlatformServices` copies `IReducedMotionSource.Mode` into the policy when the services provide a source.

What each mode changes:

| Motion | `NoPreference` | `Reduce` | `DisableNonEssential` |
| --- | --- | --- | --- |
| `TweenSpec<T>` | Plays for its duration. | Duration becomes zero; the value jumps to the target (after any delay). | Plays for its duration. |
| `RepeatSpec<T>` and `PingPongSpec<T>` with no count (infinite) | Repeats forever. | Does not play; the value is set to the target. | Does not play; the value is set to the target. |
| Layout corrections | Run. | Run. | Not started. |

`RepeatSpec<T>` and `PingPongSpec<T>` with a count are not skipped; tweens inside them still follow the `TweenSpec<T>` row. Springs, decay, and keyframes are not changed by the mode. Each skipped animation increases `MotionDiagnostics.ReducedMotionSkipCount`.

`ReducedMotionMode` is different from `MotionPriority.ReducedMotion`, which is a conflict-resolution priority.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `NoPreference` | `0` | Play all motion. The default. |
| `Reduce` | `1` | Tweens finish immediately and infinite repeats do not play. |
| `DisableNonEssential` | `2` | Infinite repeats do not play and layout corrections are not started. Tweens still play. |

## Applies to

Cerneala motion and accessibility preferences.

## See also

- `Cerneala.UI.Motion.Core.ReducedMotionPolicy`
- `Cerneala.UI.Motion.Core.IReducedMotionSource`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Detective.MotionDiagnostics`
