# TimbreMotionAnimationBuilder Class

## Definition

Namespace: `Cerneala.UI.Timbre`

Assembly/Project: `Cerneala`

Source: `UI/Timbre/TimbreMotionFacade.cs`

Describes and starts one animation of a captured playback's Volume or declared float parameter.

```csharp
public sealed class TimbreMotionAnimationBuilder
```

Inheritance:
`object` -> `TimbreMotionAnimationBuilder`

## Examples

```csharp
using Cerneala.Timbre;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

MotionHandle fade = playback.Motion()
    .Animate(TimbrePlayback.VolumeParameter)
    .From(0f)
    .To(1f)
    .With(new TweenSpec<float>(TimeSpan.FromMilliseconds(300), Easings.EaseOut));

fade.Cancel(); // keeps the last sampled volume
```

## Remarks

The builder mirrors `MotionAnimationBuilder<T>`: `From` is optional (the animation starts from the parameter's current value), `To` sets the destination, and `With` starts the animation and returns a `MotionHandle`. Any `MotionSpec<float>` works (tween, spring, decay, keyframes, repeat); `MotionPropertyStartOptions` supply the retarget mode, priority and `HoldOnComplete`. A new animation of the same parameter follows the existing Motion conflict policy; different parameters animate independently.

**Clock.** Samples are taken on the root's Motion frames, with the root's clock and maximum delta. Animation time advances only while the playback produces PCM: it starts at the first mixed block (not while `Pending`), stops while the playback is paused (including a pending pause and a delay tail) and while a seek is pending, and continues afterwards without restarting. Looping does not restart an animation. Hiding or collapsing an element does not stop the samples; a hidden window whose root is not pumped is not sampled, and the transport keeps playing. Reduced Motion does not disable audio animations.

**Publication.** Each root frame publishes the changed values of one playback together; they affect only PCM produced afterwards, at block granularity. Audio Motion causes no layout or render invalidation.

**Ending.** The final value stays applied; with `HoldOnComplete = false` the value from before the animation is restored on natural completion. Assigning `Volume` or calling `Set` on the playback cancels only that parameter's animation and keeps the assigned value. A sample that is not finite or lies outside the parameter's range cancels only that animation, keeps the last valid value and is counted in the root's sound diagnostics; the sound continues. When the playback is canceled, replaced, fails or completes, no further value is published to it and its animations end as canceled.

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `From(float value)` | `TimbreMotionAnimationBuilder` | Sets the start value. |
| `To(float value)` | `TimbreMotionAnimationBuilder` | Sets the destination. |
| `With(MotionSpec<float> spec)` | `MotionHandle` | Starts the animation and holds the final value. |
| `With(MotionSpec<float> spec, MotionPropertyStartOptions options)` | `MotionHandle` | Starts the animation with explicit retarget, priority and hold options. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `With` | `ArgumentNullException` | `spec` or `options` is `null`. |
| `With` | `ArgumentException` | The parameter is neither `TimbrePlayback.VolumeParameter` nor declared by the playback's clip. |
| `With` | `ArgumentOutOfRangeException` | The start or destination value is not finite or is outside the parameter's range. |
| `With` | `InvalidOperationException` | The playback is terminal, its scope has no owning root and no root was given, the playback is already animated by another root, or the call is not on the root's thread. |

## Applies to

Timbre audio Motion for C# and generated markup.

## See also

- [TimbreMotionFacade](Cerneala.UI.Timbre.TimbreMotionFacade.md)
- [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md)
- [GeneratedMarkup](Cerneala.UI.Markup.GeneratedMarkup.md)
