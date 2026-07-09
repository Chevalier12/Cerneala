# MotionSampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionSampler.cs`

Defines the untyped base API for advancing, reading, completing, and retargeting a motion sampler.

```csharp
public abstract class MotionSampler
```

Inheritance:
`object` -> `MotionSampler`

Derived:
`MotionSampler<T>`

## Examples

Create a typed tween specification, use it through the untyped sampler base, and advance it by a frame delta:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSpec spec = Motion.Tween(TimeSpan.FromMilliseconds(100), Easings.Linear);
MotionSampler sampler = spec.CreateSamplerUntyped(0f, 10f, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(50));
object? current = sampler.CurrentUntyped;
```

## Remarks

`MotionSampler` is the non-generic sampling contract used when the value type is not known at the call site. `MotionSpec.CreateSamplerUntyped` returns this base type, while typed specifications usually implement the derived `MotionSampler<T>` contract.

The sampler owns the current sampled value and completion state for a single motion run. Call `Advance` with a non-negative frame delta to update the sample. Implementations may finish immediately, finish after enough elapsed time, or keep running until canceled by the owner.

`CurrentUntyped` exposes the current value as `object?`. Use `MotionSampler<T>.Current` when the value type is known and compile-time typing matters.

`RetargetUntyped` changes the sampler target through the untyped API. The generic implementation accepts values assignable to `T`, accepts `null` only when `default(T)` is `null`, and throws `ArgumentException` for mismatched value types.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CurrentUntyped` | `object?` | Gets the current sampled value through the untyped API. |
| `IsComplete` | `bool` | Gets whether the sampler has finished producing motion samples. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances the sampler by the supplied elapsed time. Implementations define the concrete sampling behavior and validation. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Retargets the sampler through the untyped API using the requested retargeting mode. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `RetargetUntyped(object?, RetargetMode)` | `ArgumentException` | The derived generic implementation receives a value that is not compatible with its `T` value type. |

## Applies to

Cerneala UI motion specifications and motion values that need to sample animations without statically knowing the animated value type.

## See also

- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.RetargetMode`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Interpolation.IValueMixer`
