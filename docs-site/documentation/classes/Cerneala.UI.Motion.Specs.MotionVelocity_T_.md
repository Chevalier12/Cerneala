# MotionVelocity<T> Struct

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionVelocity.cs`

Wraps a typed velocity value reported by motion samplers and consumed by velocity-aware motion specifications.

```csharp
public readonly record struct MotionVelocity<T>(T Value);
```

Inheritance:
`ValueType` -> `MotionVelocity<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The value type used to represent velocity. It matches the value type sampled by the owning motion. |

## Examples

Create a decay specification with an initial float velocity:

```csharp
using Cerneala.UI.Motion.Specs;

DecaySpec<float> spec = Motion.Decay(
    new MotionVelocity<float>(1000f),
    deceleration: 0.9f);
```

Read velocity from a sampler that provides it:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = Motion.Spring<float>()
    .CreateSampler(0f, 100f, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(16));

MotionVelocity<float>? velocity = sampler.Velocity;
float speed = velocity?.Value ?? 0f;
```

## Remarks

`MotionVelocity<T>` is a small value wrapper around a velocity value of type `T`. Motion samplers expose it through `MotionSampler<T>.Velocity` when they can report current velocity. The base sampler returns `null`, while implementations such as spring and decay samplers return a `MotionVelocity<T>` for their current velocity state.

`DecaySpec<T>` uses `MotionVelocity<T>` as its required initial velocity. During sampling, the decay sampler copies `InitialVelocity.Value` into its internal velocity, advances the current value by that velocity over elapsed seconds, and decays it over time.

The type is a `readonly record struct`, so equality, deconstruction, and string formatting follow the compiler-generated record struct behavior. It does not validate the velocity value; any validation belongs to the motion specification or mixer that consumes it.

## Constructors

| Name | Description |
| --- | --- |
| `MotionVelocity(T)` | Initializes the velocity wrapper with the supplied typed value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `T` | Gets the wrapped velocity value. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out T)` | `void` | Deconstructs the record struct into its `Value`. |
| `Equals(MotionVelocity<T>)` | `bool` | Determines whether another velocity wrapper has the same record value. |
| `Equals(object?)` | `bool` | Determines whether an object is an equivalent `MotionVelocity<T>`. |
| `GetHashCode()` | `int` | Returns the record struct hash code for the wrapped value. |
| `ToString()` | `string` | Returns the compiler-generated record struct string representation. |

## Applies to

Cerneala UI motion specifications and samplers that preserve, report, or consume typed velocity.

## See also

- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.DecaySpec<T>`
- `Cerneala.UI.Motion.Specs.SpringSpec<T>`
- `Cerneala.UI.Motion.Specs.Motion`
