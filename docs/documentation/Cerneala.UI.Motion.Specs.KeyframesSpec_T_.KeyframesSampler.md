# KeyframesSpec<T>.KeyframesSampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/KeyframesSpec.cs`

Samples a `KeyframesSpec<T>` by mapping elapsed time to normalized keyframe progress.

```csharp
private sealed class KeyframesSampler : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `KeyframesSpec<T>.KeyframesSampler`

Containing type:
`KeyframesSpec<T>`

Access:
`private`; this nested sampler is an implementation detail created by `KeyframesSpec<T>.CreateSampler`.

## Examples

Callers do not create `KeyframesSampler` directly. Create it through `KeyframesSpec<T>.CreateSampler`, usually by using the `Motion.Keyframes` factory.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

KeyframesSpec<float> spec = Motion.Keyframes(
    new MotionKeyframe<float>(0, 5),
    new MotionKeyframe<float>(0.5f, 15),
    new MotionKeyframe<float>(1, 30))
    .WithDuration(TimeSpan.FromMilliseconds(300));

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(5, 30, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(150));
float midway = sampler.Current;
```

## Remarks

`KeyframesSampler` starts with the first value in `KeyframesSpec<T>.Frames`. `Advance` adds the supplied delta to its elapsed time, divides elapsed seconds by `KeyframesSpec<T>.Duration`, clamps that progress to `[0, 1]`, and samples the matching keyframe segment.

For normal segments, the sampler transforms segment progress with the starting keyframe's `Easing` value, or `Easings.Linear` when that easing is `null`, then interpolates from the start value to the end value with the `ValueMixer<T>` supplied to `CreateSampler`. If the starting keyframe has `Hold` set to `true`, or if adjacent keyframes share the same offset, the sampler returns the starting value for that segment instead of mixing.

The sampler completes when clamped progress reaches `1`. Calls to `Advance` after completion do nothing. `Retarget` sets the current value to the retarget value and immediately marks the sampler complete; the `RetargetMode` argument is not used.

`KeyframesSpec<T>.CreateSampler` passes the validated spec and non-null mixer to this sampler. The sampler does not use the `from`, `to`, or `MotionSpecContext` values passed to `CreateSampler`.

## Constructors

| Name | Description |
| --- | --- |
| `KeyframesSampler(KeyframesSpec<T> spec, ValueMixer<T> mixer)` | Initializes the sampler from a keyframes spec and value mixer, setting `Current` to the first keyframe value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current sampled value. |
| `IsComplete` | `bool` | Gets whether elapsed progress has reached the final keyframe or the sampler was retargeted. |
| `Velocity` | `MotionVelocity<T>?` | Inherited from `MotionSampler<T>`. Keyframe sampling does not provide velocity, so the value is `null`. |
| `CurrentUntyped` | `object?` | Gets `Current` through the inherited `MotionSampler<T>` implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances elapsed time, samples the matching keyframe segment, and completes when progress reaches `1`. Throws when `delta` is negative. |
| `Retarget(T to, RetargetMode mode)` | `void` | Sets `Current` to `to` and marks the sampler complete. The `mode` value is not used. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Retargets through the inherited typed-value adapter from `MotionSampler<T>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `ArgumentOutOfRangeException` | `delta` is less than `TimeSpan.Zero`. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `ArgumentException` | `to` is not assignable to `T`, except for `null` when `T` allows it. |

## Applies to

Cerneala motion spec internals.

## See also

- `KeyframesSpec<T>`
- `MotionKeyframe<T>`
- `MotionSampler<T>`
- `Easings`
- `ValueMixer<T>`
