# Motion.UntypedSpringSpec Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/Motion.cs`

Provides the private untyped spring specification returned by `Motion.Spring(float, float, float)`.

```csharp
private sealed class UntypedSpringSpec : MotionSpec
```

Inheritance:
`object` -> `MotionSpec` -> `Motion.UntypedSpringSpec`

Containing type:
`Motion`

Access:
`private`; callers receive instances through the non-generic `Motion.Spring` factory as `MotionSpec`.

## Examples

Create an untyped spring specification and create a sampler by supplying a mixer whose `ValueType` resolves the concrete animated type:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpec spec = Motion.Spring(stiffness: 520, damping: 38, mass: 1);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler sampler = spec.CreateSamplerUntyped(
    from: 0f,
    to: 100f,
    mixer: new FloatMixer(),
    context: context);

sampler.Advance(TimeSpan.FromMilliseconds(16));
object? current = sampler.CurrentUntyped;
```

## Remarks

`UntypedSpringSpec` stores spring parameters for APIs that cannot name the animated value type up front. `CreateSamplerUntyped` reads `mixer.ValueType`, builds the matching closed generic `SpringSpec<T>` sampler through the private `Motion.CreateSpringSampler<T>` helper, and returns it as `MotionSampler`.

The constructor validates its physical parameters immediately. `stiffness` and `mass` must be finite and greater than `0`; `damping` must be finite and greater than or equal to `0`. The untyped overload uses the same defaults as `Motion.Spring<T>` for those three parameters: `520`, `38`, and `1`.

Sampler creation still follows `SpringSpec<T>` rules after the type is resolved. The supplied `mixer` must be a `ValueMixer<T>` for `mixer.ValueType`, the `from` and `to` values must be assignable to that same type, and the mixer must support vector operations. The inner exception thrown by the typed spring creation path is rethrown without being wrapped in a reflection `TargetInvocationException`.

## Constructors

| Name | Description |
| --- | --- |
| `UntypedSpringSpec(float stiffness, float damping, float mass)` | Initializes the untyped spring specification and validates spring stiffness, damping, and mass. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSamplerUntyped(object? from, object? to, IValueMixer mixer, MotionSpecContext context)` | `MotionSampler` | Creates a typed spring sampler by closing the spring creation helper over `mixer.ValueType`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `UntypedSpringSpec(float, float, float)` | `ArgumentOutOfRangeException` | `stiffness` or `mass` is not finite or is less than or equal to `0`; `damping` is not finite or is less than `0`. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `ArgumentException` | `mixer` is not a `ValueMixer<T>` for `mixer.ValueType`, or `from`/`to` cannot be cast to that type. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `InvalidOperationException` | The resolved typed spring cannot create a sampler, such as when the mixer does not support vector operations. |

## Applies to

Cerneala UI motion internals and untyped motion specification factories.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.SpringSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler`
- `Cerneala.UI.Motion.Interpolation.IValueMixer`
