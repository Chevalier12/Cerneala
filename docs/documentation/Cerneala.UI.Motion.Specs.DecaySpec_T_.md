# DecaySpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/DecaySpec.cs`

Represents an inertial motion specification that advances from an initial velocity and decays that velocity over time.

```csharp
public sealed class DecaySpec<T> : MotionSpec<T>
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `DecaySpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type. The supplied `ValueMixer<T>` must support vector operations when a sampler is created. |

## Examples

Create a bounded float decay and sample it until completion:

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

MotionSampler<float> sampler = spec.CreateSampler(
    from: 0,
    to: 0,
    mixer: new FloatMixer(),
    context: context);

while (!sampler.IsComplete)
{
    sampler.Advance(TimeSpan.FromMilliseconds(16));
}
```

Add a bounce spec that runs after the decay reaches a bound:

```csharp
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

DecaySpec<float> spec = Motion.Decay(new MotionVelocity<float>(1000), deceleration: 0.9f)
    .WithBounds(min: 0, max: 25)
    .WithBounce(Motion.Tween<float>(TimeSpan.FromMilliseconds(100), Easings.Linear));

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(0, 0, new FloatMixer(), context);
sampler.Advance(TimeSpan.FromMilliseconds(100));
```

## Remarks

`DecaySpec<T>` models momentum-style motion. Each sampler starts at the `from` value passed to `CreateSampler`, uses `InitialVelocity.Value` as its current velocity, advances by `velocity * delta.TotalSeconds`, then scales the velocity by `Deceleration` for each 16.6666667 ms frame-equivalent interval. Sampling completes when velocity magnitude reaches `0.01f` or when an unbounced bound is hit.

The constructor requires `Deceleration` to be finite, greater than `0`, and less than `1`. Values closer to `1` decay more slowly.

Decay sampling requires a `ValueMixer<T>` whose `SupportsVectorOperations` value is `true`, because the sampler uses vector addition, subtraction, scaling, and magnitude. Bounds require values that implement `IComparable<T>` or `IComparable`; non-comparable bounded values are rejected when the sampler is created.

`WithBounds` and `WithBounce` return new `DecaySpec<T>` instances. The original spec is not mutated. A bounce spec can only be used when both minimum and maximum bounds are present. When the decay overshoots a bound and `Bounce` is set, the sampler clamps to the bound, reflects the overshoot back into the bounded range, and starts the bounce sampler from the bound toward that reflected value.

The `to` argument passed to `CreateSampler` is not used by the decay calculation. Retargeting a decay sampler sets the current value to the retarget value and completes the sampler.

## Constructors

| Name | Description |
| --- | --- |
| `DecaySpec(MotionVelocity<T>, float)` | Initializes a decay specification with an initial velocity and an optional deceleration value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `InitialVelocity` | `MotionVelocity<T>` | Gets the velocity used when a sampler starts. |
| `Deceleration` | `float` | Gets the per-frame-equivalent decay factor. The value is finite, greater than `0`, and less than `1`. |
| `Min` | `T?` | Gets the minimum bound value when `HasMin` is `true`; otherwise the default nullable value. |
| `Max` | `T?` | Gets the maximum bound value when `HasMax` is `true`; otherwise the default nullable value. |
| `HasMin` | `bool` | Gets whether the specification has a minimum bound. |
| `HasMax` | `bool` | Gets whether the specification has a maximum bound. |
| `Bounce` | `MotionSpec<T>?` | Gets the optional motion specification used after the decay hits a bound. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `WithBounds(T, T)` | `DecaySpec<T>` | Returns a copy with minimum and maximum bounds. Comparable bounds are validated so the minimum is not greater than the maximum. |
| `WithBounce(MotionSpec<T>)` | `DecaySpec<T>` | Returns a copy with a bounce specification. The bounce argument cannot be `null`. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a decay sampler after validating the mixer, optional bounds, and optional bounce requirements. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Inherited from `MotionSpec<T>`. Casts untyped values and mixer instances before delegating to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DecaySpec(MotionVelocity<T>, float)` | `ArgumentOutOfRangeException` | `deceleration` is not finite, is less than or equal to `0`, or is greater than or equal to `1`. |
| `WithBounds(T, T)` | `ArgumentOutOfRangeException` | `min` and `max` are comparable and `min` is greater than `max`. |
| `WithBounce(MotionSpec<T>)` | `ArgumentNullException` | `bounce` is `null`. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `ArgumentNullException` | `mixer` or `context` is `null`. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `InvalidOperationException` | The mixer does not support vector operations, bounds are present for non-comparable values, or a bounce spec is present without both bounds. |

## Applies to

Cerneala UI motion specifications and value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.MotionVelocity<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
