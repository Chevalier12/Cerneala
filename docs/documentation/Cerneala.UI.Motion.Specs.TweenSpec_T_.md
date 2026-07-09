# TweenSpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/TweenSpec.cs`

Represents a typed fixed-duration tween specification that interpolates from a starting value to a target value with an easing function.

```csharp
public sealed class TweenSpec<T> : MotionSpec<T>
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `TweenSpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type. The supplied `ValueMixer<T>` is used to interpolate between the start and target values. |

## Examples

Create a linear tween and sample it halfway through its duration:

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

MotionSampler<float> sampler = spec.CreateSampler(
    from: 0f,
    to: 10f,
    mixer: new FloatMixer(),
    context: context);

sampler.Advance(TimeSpan.FromMilliseconds(50));
float midpoint = sampler.Current;
```

Add an initial delay and keep the starting value during that delay:

```csharp
using Cerneala.UI.Motion.Specs;

TweenSpec<float> spec = Motion.Tween<float>(
    TimeSpan.FromMilliseconds(100),
    Easings.Linear)
    .WithDelay(TimeSpan.FromMilliseconds(50))
    .WithFillMode(FillMode.Both);
```

## Remarks

`TweenSpec<T>` stores the duration, delay, easing, and fill mode used to create a `MotionSampler<T>`. The sampler starts at the supplied `from` value, advances elapsed time with `Advance`, transforms normalized progress through `Easing`, and passes the eased progress to the supplied `ValueMixer<T>`.

The constructor requires a positive `Duration` and a non-negative `Delay`. When `easing` is `null`, the spec uses `Easings.Standard`. `FillMode` defaults to `FillMode.Both`.

Before the delay has elapsed, `FillMode.Backwards` and `FillMode.Both` set the current value to the starting value. Other fill modes leave the sampler's current value unchanged during the delay. After the active duration completes, the sampler sets `Current` to the target value and marks itself complete.

When the motion context uses `ReducedMotionMode.Reduce`, `CreateSampler` uses an effective duration of zero and records a reduced-motion skip through diagnostics when diagnostics are available. With no delay, that sampler completes immediately at the target value. With a delay, it waits until the delay has elapsed, then jumps to the target value.

Retargeting with `RetargetMode.Restart` uses the current value as the new starting value and resets elapsed time to zero. Retargeting with `RetargetMode.PreserveProgress` keeps the existing elapsed time and resamples immediately against the new target.

Each call to `WithDelay` or `WithFillMode` returns a new `TweenSpec<T>` instance. The original spec is not mutated.

## Constructors

| Name | Description |
| --- | --- |
| `TweenSpec(TimeSpan, IEasing?, TimeSpan, FillMode)` | Initializes a tween specification with duration, optional easing, optional delay, and fill mode. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Duration` | `TimeSpan` | Gets the positive active interpolation duration. |
| `Delay` | `TimeSpan` | Gets the non-negative delay before active interpolation begins. |
| `Easing` | `IEasing` | Gets the easing function used to transform normalized progress. Defaults to `Easings.Standard`. |
| `FillMode` | `FillMode` | Gets how the sampler behaves before the delay elapses. Defaults to `FillMode.Both`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `WithDelay(TimeSpan)` | `TweenSpec<T>` | Returns a copy with the same duration, easing, and fill mode, but a different delay. |
| `WithFillMode(FillMode)` | `TweenSpec<T>` | Returns a copy with the same duration, easing, and delay, but a different fill mode. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a tween sampler from a starting value, target value, mixer, and motion context. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Inherited from `MotionSpec<T>`. Casts untyped values and mixer instances before delegating to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `TweenSpec(TimeSpan, IEasing?, TimeSpan, FillMode)` | `ArgumentOutOfRangeException` | `duration` is zero or negative, or `delay` is negative. |
| `WithDelay(TimeSpan)` | `ArgumentOutOfRangeException` | `delay` is negative. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `ArgumentNullException` | `mixer` or `context` is `null`. |

## Applies to

Cerneala UI motion specifications and value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.Easings`
- `Cerneala.UI.Motion.Specs.FillMode`
- `Cerneala.UI.Motion.Specs.RetargetMode`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
