# PingPongSpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/PingPongSpec.cs`

Represents a finite motion specification that alternates between the start and target values for a fixed number of tween-based cycles.

```csharp
public sealed class PingPongSpec<T> : MotionSpec<T>
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `PingPongSpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type. The supplied `ValueMixer<T>` is used to interpolate between the `from` and `to` values. |

## Examples

Create a two-cycle ping-pong sampler. The first cycle moves from `0` to `10`; the second cycle reverses toward `0`.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

PingPongSpec<float> spec = new(
    Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
    cycles: 2);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(100));
float endOfFirstCycle = sampler.Current; // 10

sampler.Advance(TimeSpan.FromMilliseconds(50));
float returningValue = sampler.Current; // 5
```

## Remarks

`PingPongSpec<T>` wraps a `TweenSpec<T>` and repeats its duration for the requested number of cycles. Even-numbered cycles reverse progress so the sampler alternates between the `from` and `to` values instead of restarting at the beginning of each cycle.

Each sampler starts at `from`. During sampling, progress is calculated from elapsed milliseconds within the current cycle, transformed through the wrapped tween's `Easing`, and mixed with the supplied `ValueMixer<T>`. The wrapped tween's `Duration` and `Easing` are used directly.

The total runtime is `inner.Duration * cycles`. When the sampler reaches or passes that total duration, it completes and sets `Current` to `from` for an even cycle count, or `to` for an odd cycle count.

`Retarget` is implemented as a no-op by the current sampler. Calling it does not change the target, current value, elapsed time, or completion state.

## Constructors

| Name | Description |
| --- | --- |
| `PingPongSpec(TweenSpec<T>, int)` | Initializes a ping-pong specification from an inner tween and a positive cycle count. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a sampler that alternates between `from` and `to` using the configured tween duration, easing, and cycle count. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Inherited from `MotionSpec<T>`. Casts untyped values and mixer instances before delegating to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PingPongSpec(TweenSpec<T>, int)` | `ArgumentNullException` | `inner` is `null`. |
| `PingPongSpec(TweenSpec<T>, int)` | `ArgumentOutOfRangeException` | `cycles` is zero or negative. |

## Applies to

Cerneala UI motion specifications and finite alternating value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.Easings`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
