# DecaySpec<T>.DecaySampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/DecaySpec.cs`

Samples decaying motion for a `DecaySpec<T>` until velocity settles, a bound is reached, or a bounce sampler completes.

```csharp
private sealed class DecaySampler : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `DecaySpec<T>.DecaySampler`

Containing type:
`DecaySpec<T>`

Access:
`private`; this nested sampler is an implementation detail created by `DecaySpec<T>.CreateSampler`.

## Examples

Callers do not create `DecaySampler` directly. Create it through `DecaySpec<T>.CreateSampler`, usually by using the `Motion.Decay` factory.

```csharp
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

DecaySpec<float> spec = Motion.Decay(new MotionVelocity<float>(1000), deceleration: 0.9f)
    .WithBounds(min: 0, max: 25);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 0, new FloatMixer(), context);

while (!sampler.IsComplete)
{
    sampler.Advance(TimeSpan.FromMilliseconds(16));
}

float settledValue = sampler.Current;
```

## Remarks

`DecaySampler` starts at the supplied `from` value and uses `DecaySpec<T>.InitialVelocity` as its initial velocity. Each `Advance` call integrates the current velocity over the elapsed seconds, then scales velocity by `Deceleration` raised to the number of 16.6666667 ms frame intervals in the delta.

The sampler requires a vector-capable `ValueMixer<T>` because it adds values, scales velocity, subtracts overshoot, and measures velocity magnitude. `DecaySpec<T>.CreateSampler` performs that validation before constructing the sampler.

When bounds are configured, the sampler clamps the next value to `Min` or `Max`. Without a bounce spec, hitting a bound zeroes velocity and completes the sampler. With a bounce spec, the sampler reflects the overshoot back inside the bounds, creates the bounce sampler from the clamped value to the reflected value, and then delegates subsequent `Advance`, `Current`, `Velocity`, and completion state to that bounce sampler.

The sampler completes when velocity magnitude falls to `0.01f` or lower, when an unbounced bound is reached, when its bounce sampler completes, or when `Retarget` is called.

## Constructors

| Name | Description |
| --- | --- |
| `DecaySampler(DecaySpec<T> spec, T from, ValueMixer<T> mixer, MotionSpecContext context)` | Initializes the sampler from a decay spec, starting value, vector-capable mixer, and motion context. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current sampled value. During bounce motion, this mirrors the active bounce sampler's current value. |
| `IsComplete` | `bool` | Gets whether the decay or delegated bounce sampler has completed. |
| `Velocity` | `MotionVelocity<T>?` | Gets the current velocity, preferring the active bounce sampler's velocity when available. |
| `CurrentUntyped` | `object?` | Gets `Current` through the inherited `MotionSampler<T>` implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances the decay by the elapsed time. Throws when `delta` is negative, no-ops after completion, and delegates to the bounce sampler when one is active. |
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

- `DecaySpec<T>`
- `MotionSampler<T>`
- `MotionVelocity<T>`
- `ValueMixer<T>`
