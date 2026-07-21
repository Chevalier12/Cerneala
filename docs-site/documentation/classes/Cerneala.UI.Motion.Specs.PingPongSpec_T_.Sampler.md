# PingPongSpec<T>.Sampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/PingPongSpec.cs`

Samples a `PingPongSpec<T>` by alternating tween progress between the starting and target values for a fixed or unbounded number of cycles.

```csharp
private sealed class Sampler : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `PingPongSpec<T>.Sampler`

Containing type:
`PingPongSpec<T>`

Access:
`private`; this nested sampler is an implementation detail created by `PingPongSpec<T>.CreateSampler`.

## Examples

Callers do not create `Sampler` directly. Create it through `PingPongSpec<T>.CreateSampler`, usually with a tween created by the `Motion` factory.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

ValueMixerRegistry mixers = new();
mixers.RegisterBuiltIns();

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    mixers,
    Diagnostics: null,
    Now: TimeSpan.Zero);

PingPongSpec<float> spec = new(
    Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear),
    cycles: 2);

MotionSampler<float> sampler = spec.CreateSampler(0, 10, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(100));
float firstTurnaround = sampler.Current; // 10

sampler.Advance(TimeSpan.FromMilliseconds(50));
float returningValue = sampler.Current; // 5
```

## Remarks

`Sampler` stores the wrapped `TweenSpec<T>`, optional cycle count, starting value, target value, and `ValueMixer<T>` supplied by `PingPongSpec<T>.CreateSampler`. It initializes `Current` to the starting value and completes after `spec.Duration * cycles` when the count is finite. A `null` count remains active until canceled by its owner.

Each `Advance` call adds the supplied delta to elapsed time, identifies the current cycle from elapsed milliseconds, and computes progress within that cycle. Odd-numbered cycles invert progress with `1 - progress`, so the sampled value moves from `to` back toward `from` instead of restarting at `from`.

The sampled progress is transformed through the wrapped tween's `Easing` and then mixed from `from` to `to`. When elapsed time reaches or passes the total duration, `Current` is set to `from` for an even cycle count and `to` for an odd cycle count, then `IsComplete` becomes `true`.

`Retarget` is currently a no-op. Calling it does not change the target value, elapsed time, current value, or completion state.

## Constructors

| Name | Description |
| --- | --- |
| `Sampler(TweenSpec<T> spec, int? cycles, T from, T to, ValueMixer<T> mixer)` | Initializes the sampler from a tween spec, optional positive cycle count, endpoint values, and mixer. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current sampled value. |
| `IsComplete` | `bool` | Gets whether elapsed time has reached or passed the total ping-pong duration. |
| `Velocity` | `MotionVelocity<T>?` | Inherited from `MotionSampler<T>`. Ping-pong sampling does not provide velocity, so the value is `null`. |
| `CurrentUntyped` | `object?` | Gets `Current` through the inherited `MotionSampler<T>` implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances elapsed time, samples the active forward or reverse cycle, and completes at the configured cycle boundary. |
| `Retarget(T to, RetargetMode mode)` | `void` | No-ops. The `to` and `mode` arguments are not used by the current implementation. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Retargets through the inherited typed-value adapter from `MotionSampler<T>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RetargetUntyped(object? to, RetargetMode mode)` | `ArgumentException` | `to` is not assignable to `T`, except for `null` when `T` allows it. |

## Applies to

Cerneala motion spec internals.

## See also

- `PingPongSpec<T>`
- `TweenSpec<T>`
- `MotionSampler<T>`
- `Easings`
- `ValueMixer<T>`
