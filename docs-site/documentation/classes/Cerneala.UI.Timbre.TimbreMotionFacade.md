# TimbreMotionFacade Class

## Definition

Namespace: `Cerneala.UI.Timbre`

Assembly/Project: `Cerneala`

Source: `UI/Timbre/TimbreMotionFacade.cs`

Audio Motion entry point for one captured [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md), returned by `playback.Motion()`.

```csharp
public sealed class TimbreMotionFacade
```

Inheritance:
`object` -> `TimbreMotionFacade`

## Examples

```csharp
using Cerneala.Timbre;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

void AnimatePlayback(TimbrePlayback playback, TimbreParameter<float> toneCutoff, MotionSpec<float> transition)
{
    playback.Motion()
        .Animate(TimbrePlayback.VolumeParameter)
        .To(0.8f)
        .With(transition);

    playback.Motion()
        .Animate(toneCutoff)
        .To(6000f)
        .With(transition);
}
```

## Remarks

Create the facade with [MotionExtensions](Cerneala.UI.Motion.MotionExtensions.md): `Motion(this TimbrePlayback)` samples the animation with the `UIRoot` that owns the playback's element scope (`UIElement.Timbre` or a generated markup sound session); `Motion(this TimbrePlayback, UIRoot)` names the root explicitly for playbacks of `Application.Timbre` or a standalone runtime. No Aspect or markup is required.

The facade captures the playback instance, not a [TimbreHandle](Cerneala.Timbre.TimbreHandle.md) slot: an animation never moves to a playback that later occupies the same slot. `Animate` accepts `TimbrePlayback.VolumeParameter` or a float parameter declared by `playback.Clip`; the descriptor is checked when the animation starts. See [TimbreMotionAnimationBuilder](Cerneala.UI.Timbre.TimbreMotionAnimationBuilder.md) for timing, cancellation and validation.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Playback` | `TimbrePlayback` | The captured playback. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Animate(TimbreParameter<float> parameter)` | `TimbreMotionAnimationBuilder` | Starts describing an animation of the playback's Volume or of a declared parameter. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Animate` | `ArgumentNullException` | `parameter` is `null`. |

## Applies to

Timbre audio Motion for C# and generated markup.

## See also

- [TimbreMotionAnimationBuilder](Cerneala.UI.Timbre.TimbreMotionAnimationBuilder.md)
- [TimbrePlayback](Cerneala.Timbre.TimbrePlayback.md)
- [MotionExtensions](Cerneala.UI.Motion.MotionExtensions.md)
