# KeyframesSpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/KeyframesSpec.cs`

Represents a motion specification that samples explicit keyframe values over a fixed duration.

```csharp
public sealed class KeyframesSpec<T> : MotionSpec<T>
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `KeyframesSpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type. The supplied `ValueMixer<T>` is used to interpolate between non-held keyframe values. |

## Examples

Create a float keyframe sequence and sample it to completion:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

KeyframesSpec<float> spec = Motion.Keyframes(
    new MotionKeyframe<float>(0, 0),
    new MotionKeyframe<float>(0.5f, 12, Easings.EaseOut),
    new MotionKeyframe<float>(1, 20))
    .WithDuration(TimeSpan.FromMilliseconds(300));

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 20, new FloatMixer(), context);
sampler.Advance(TimeSpan.FromMilliseconds(150));

float current = sampler.Current;
```

Use a held keyframe to keep the starting value until the next segment:

```csharp
using Cerneala.UI.Motion.Specs;

KeyframesSpec<float> spec = new(
[
    new MotionKeyframe<float>(0, 0, Hold: true),
    new MotionKeyframe<float>(0.4f, 10),
    new MotionKeyframe<float>(1, 20)
]);
```

## Remarks

`KeyframesSpec<T>` stores an ordered snapshot of `MotionKeyframe<T>` entries. The constructor copies the supplied frame list, so later changes to the original list do not change the spec.

The first keyframe must use offset `0` and the last keyframe must use offset `1`. At least two keyframes are required. Offsets must be finite enough to avoid `NaN`, must be in `[0, 1]`, and must be sorted in ascending order. Duplicate adjacent offsets are allowed; when a sampled segment has the same start and end offset, the sampler returns the start value for that segment.

Sampling progress is calculated as elapsed time divided by `Duration`, clamped to `[0, 1]`. For each segment, the sampler applies the starting keyframe's `Easing` value when present, or `Easings.Linear` when it is `null`, then interpolates with the supplied `ValueMixer<T>`. If the starting keyframe has `Hold` set to `true`, the sampler returns the starting value instead of mixing across that segment.

The `from`, `to`, and `context` arguments passed to `CreateSampler` are not used by the keyframe calculation. The sampler starts at the first frame's value. Retargeting a keyframes sampler sets the current value to the retarget value and completes the sampler.

`WithDuration` returns a new `KeyframesSpec<T>` with the same frames and the requested duration. The original spec is not mutated.

## Constructors

| Name | Description |
| --- | --- |
| `KeyframesSpec(IReadOnlyList<MotionKeyframe<T>>, TimeSpan?)` | Initializes a keyframe specification from an ordered frame list and optional duration. The default duration is one second. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Frames` | `IReadOnlyList<MotionKeyframe<T>>` | Gets the copied, validated keyframe list used for sampling. |
| `Duration` | `TimeSpan` | Gets the positive duration used to map elapsed time to normalized progress. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `WithDuration(TimeSpan)` | `KeyframesSpec<T>` | Returns a new keyframe specification with the same frames and a different duration. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a sampler that starts at the first keyframe value and samples through the keyframe sequence. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Inherited from `MotionSpec<T>`. Casts untyped values and mixer instances before delegating to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `KeyframesSpec(IReadOnlyList<MotionKeyframe<T>>, TimeSpan?)` | `ArgumentNullException` | `frames` is `null`. |
| `KeyframesSpec(IReadOnlyList<MotionKeyframe<T>>, TimeSpan?)` | `ArgumentException` | Fewer than two frames are supplied, the first offset is not `0`, the last offset is not `1`, an offset is `NaN` or outside `[0, 1]`, or offsets are not sorted. |
| `KeyframesSpec(IReadOnlyList<MotionKeyframe<T>>, TimeSpan?)` | `ArgumentOutOfRangeException` | `duration` is zero or negative. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `ArgumentNullException` | `mixer` is `null`. |

## Applies to

Cerneala UI motion specifications and value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionKeyframe<T>`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.Easings`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
