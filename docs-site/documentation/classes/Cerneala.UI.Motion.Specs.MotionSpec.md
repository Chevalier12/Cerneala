# MotionSpec Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/MotionSpec.cs`

Defines the untyped base contract for creating motion samplers when the animated value type is supplied by an `IValueMixer`.

```csharp
public abstract class MotionSpec
```

Inheritance:
`object` -> `MotionSpec`

Derived:
`MotionSpec<T>` and the untyped specifications returned by `Motion.Tween` and `Motion.Spring`.

## Examples

Create an untyped tween specification and sample a `float` animation by passing a `FloatMixer`:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSpec spec = Motion.Tween(TimeSpan.FromMilliseconds(120), Easings.Linear);
MotionSampler sampler = spec.CreateSamplerUntyped(0f, 1f, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(60));
object? current = sampler.CurrentUntyped;
```

## Remarks

`MotionSpec` is the non-generic entry point for motion specifications whose concrete value type is not known at the call site. It is used by factory-created untyped specifications such as `Motion.Tween(TimeSpan, IEasing?)` and `Motion.Spring(float, float, float)`, and it is also the base class for `MotionSpec<T>`.

`CreateSamplerUntyped` receives the starting value, target value, value mixer, and sampling context, then returns a `MotionSampler`. Implementations use the `IValueMixer.ValueType` or a typed `ValueMixer<T>` to resolve the concrete value type before constructing the matching typed sampler.

The base class does not perform validation itself. Derived implementations validate the supplied values, mixer, and context. For example, `MotionSpec<T>.CreateSamplerUntyped` throws `ArgumentException` when `from`, `to`, or `mixer` do not match `T`, while concrete specifications such as tween and spring samplers may also validate duration, vector support, and frame deltas.

Use `MotionSpec<T>` directly when the value type is known at compile time. Use `MotionSpec` when a motion token, property binding, transaction, or registry needs to carry a specification before the animated property type is available.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSamplerUntyped(object? from, object? to, IValueMixer mixer, MotionSpecContext context)` | `MotionSampler` | Creates an untyped sampler for a motion run using the supplied start value, target value, mixer, and context. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `ArgumentException` | A derived implementation receives values or a mixer that are not compatible with the resolved value type. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `ArgumentNullException` | A derived implementation requires a non-null mixer or context and receives `null`. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `InvalidOperationException` | A derived implementation requires mixer capabilities, such as vector operations for springs, that the supplied mixer does not support. |

## Applies to

Cerneala UI motion specifications, motion properties, motion tokens, and transaction APIs that create samplers without statically knowing the animated value type.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler`
- `Cerneala.UI.Motion.Specs.MotionSpecContext`
- `Cerneala.UI.Motion.Interpolation.IValueMixer`
