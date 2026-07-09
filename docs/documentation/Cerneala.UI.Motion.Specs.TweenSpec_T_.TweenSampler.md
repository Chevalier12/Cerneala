# TweenSpec<T>.TweenSampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/TweenSpec.cs`

Samples a `TweenSpec<T>` by advancing elapsed time, applying delay and easing, and mixing from the starting value to the target value.

```csharp
private sealed class TweenSampler : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `TweenSpec<T>.TweenSampler`

Containing type:
`TweenSpec<T>`

Access:
`private`; this nested sampler is an implementation detail created by `TweenSpec<T>.CreateSampler`.

## Examples

Callers do not create `TweenSampler` directly. Create it through `TweenSpec<T>.CreateSampler`, usually by using the `Motion.Tween` factory.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

TweenSpec<float> spec = Motion.Tween<float>(
    TimeSpan.FromMilliseconds(100),
    Easings.Linear);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(50));
float midpoint = sampler.Current;
```

## Remarks

`TweenSampler` starts with `Current` set to the `from` value passed to `TweenSpec<T>.CreateSampler`. Each `Advance` call adds the supplied delta to elapsed time and resamples the tween unless the sampler is already complete.

Before `TweenSpec<T>.Delay` has elapsed, the sampler returns the starting value when `FillMode` is `Backwards` or `Both`; otherwise it leaves the current value unchanged. After the delay, it computes active progress as elapsed active time divided by the effective duration, transforms that progress with `TweenSpec<T>.Easing`, and passes the eased progress to the supplied `ValueMixer<T>`.

When active progress reaches or exceeds `1`, the sampler sets `Current` to the target value and marks itself complete. If reduced motion makes the effective duration zero, the sampler jumps to the target and completes once any configured delay has elapsed; without a delay, it completes at construction time.

Retargeting with `RetargetMode.Restart` uses the current sampled value as the new start value and resets elapsed time to zero. Retargeting with other modes preserves the existing elapsed time, replaces the target value, clears completion, and samples immediately.

`TweenSampler` does not override `MotionSampler<T>.Velocity`, so velocity is reported as `null`.

## Constructors

| Name | Description |
| --- | --- |
| `TweenSampler(TweenSpec<T>, T, T, ValueMixer<T>, TimeSpan)` | Initializes the sampler from a tween spec, start value, target value, mixer, and effective duration. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current sampled value. |
| `IsComplete` | `bool` | Gets whether the sampler has reached the target value or completed an instant reduced-motion tween. |
| `Velocity` | `MotionVelocity<T>?` | Inherited from `MotionSampler<T>`. Tween sampling does not provide velocity, so the value is `null`. |
| `CurrentUntyped` | `object?` | Gets `Current` through the inherited `MotionSampler<T>` implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances elapsed time and samples the tween. Throws when `delta` is negative and does nothing after completion. |
| `Retarget(T to, RetargetMode mode)` | `void` | Replaces the target value, optionally restarts from the current value, clears completion, and samples immediately. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Retargets through the inherited typed-value adapter from `MotionSampler<T>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `ArgumentOutOfRangeException` | `delta` is less than `TimeSpan.Zero`. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `ArgumentException` | `to` is not assignable to `T`, except for `null` when `T` allows it. |

## Applies to

Cerneala motion spec internals.

## See also

- `TweenSpec<T>`
- `MotionSampler<T>`
- `ValueMixer<T>`
- `IEasing`
- `FillMode`
- `RetargetMode`
