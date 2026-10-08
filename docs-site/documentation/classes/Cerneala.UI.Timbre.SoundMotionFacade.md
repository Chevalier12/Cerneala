# SoundMotionFacade Class

## Definition

Namespace: `Cerneala.UI.Timbre`

Assembly/Project: `Cerneala`

Source: `UI/Timbre/SoundMotionFacade.cs`

Audio Motion entry point for one captured [SoundPlayback](Cerneala.Timbre.SoundPlayback.md), returned by `playback.Motion()`.

```csharp
public sealed class SoundMotionFacade
```

Inheritance:
`object` -> `SoundMotionFacade`

## Examples

```csharp
using Cerneala.Timbre;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Specs;

void AnimatePlayback(SoundPlayback playback, SoundParameter<float> toneCutoff, MotionSpec<float> transition)
{
    playback.Motion()
        .Animate(SoundPlayback.VolumeParameter)
        .To(0.8f)
        .With(transition);

    playback.Motion()
        .Animate(toneCutoff)
        .To(6000f)
        .With(transition);
}
```

## Remarks

Create the facade with [MotionExtensions](Cerneala.UI.Motion.MotionExtensions.md): `Motion(this SoundPlayback)` samples the animation with the `UIRoot` that owns the playback's element scope (`UIElement.Sounds` or a generated markup sound session); `Motion(this SoundPlayback, UIRoot)` names the root explicitly for playbacks of `Application.Sounds` or a standalone runtime. No Aspect or markup is required.

The facade captures the playback instance, not a [SoundHandle](Cerneala.Timbre.SoundHandle.md) slot: an animation never moves to a playback that later occupies the same slot. `Animate` accepts `SoundPlayback.VolumeParameter` or a float parameter declared by `playback.Clip`; the descriptor is checked when the animation starts. See [SoundMotionAnimationBuilder](Cerneala.UI.Timbre.SoundMotionAnimationBuilder.md) for timing, cancellation and validation.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Playback` | `SoundPlayback` | The captured playback. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Animate(SoundParameter<float> parameter)` | `SoundMotionAnimationBuilder` | Starts describing an animation of the playback's Volume or of a declared parameter. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Animate` | `ArgumentNullException` | `parameter` is `null`. |

## Applies to

Timbre audio Motion for C# and generated markup.

## See also

- [SoundMotionAnimationBuilder](Cerneala.UI.Timbre.SoundMotionAnimationBuilder.md)
- [SoundPlayback](Cerneala.Timbre.SoundPlayback.md)
- [MotionExtensions](Cerneala.UI.Motion.MotionExtensions.md)
