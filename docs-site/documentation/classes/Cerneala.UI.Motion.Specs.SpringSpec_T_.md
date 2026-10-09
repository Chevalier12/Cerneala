# SpringSpec<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/SpringSpec.cs`

Represents a typed spring motion specification that moves vector-capable values or decomposed transforms toward a target using stiffness, damping, and mass parameters.

```csharp
public sealed class SpringSpec<T> : MotionSpec<T>
```

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `SpringSpec<T>`

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The animated value type. The supplied `ValueMixer<T>` must support vector operations, except that the built-in `TransformMixer` uses a component-space transform spring sampler. |

## Examples

Create a float spring and advance it until the rest thresholds complete the sampler:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

SpringSpec<float> spec = Motion.Spring<float>(stiffness: 520, damping: 38, mass: 1)
    .WithRestThresholds(restSpeed: 0.01f, restDelta: 0.01f);

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

MotionSampler<float> sampler = spec.CreateSampler(
    from: 0f,
    to: 100f,
    mixer: new FloatMixer(),
    context: context);

while (!sampler.IsComplete)
{
    sampler.Advance(TimeSpan.FromMilliseconds(16));
}

float finalValue = sampler.Current;
```

Reset velocity when retargeting a spring:

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

SpringSpec<float> spec = Motion.Spring<float>()
    .WithVelocityMode(SpringVelocityMode.Reset);

MotionSampler<float> sampler = spec.CreateSampler(
    0f,
    100f,
    new FloatMixer(),
    new MotionSpecContext(
        ReducedMotionPolicy.Default,
        new ValueMixerRegistry(),
        Diagnostics: null,
        Now: TimeSpan.Zero));

sampler.Advance(TimeSpan.FromMilliseconds(16));
sampler.Retarget(50f, RetargetMode.Restart);
```

## Remarks

`SpringSpec<T>` creates a sampler that integrates spring motion with fixed 1/120 second substeps. The sampler tracks current value, target value, and velocity, then applies a semi-implicit Euler step using spring force `-Stiffness * displacement`, damping force `-Damping * velocity`, and acceleration divided by `Mass`.

Sampling completes when the distance to the target is less than or equal to `RestDelta` and the velocity magnitude is less than or equal to `RestSpeed`. It also completes when an advance that integrates time leaves both the value and the velocity bitwise unchanged: near a large target, single-precision values can stop a few representable steps short while the velocity settles at `-Stiffness * displacement / Damping`, too small to move the value and possibly above `RestSpeed`. That state is a fixed point the integration can never leave, so the spring ends there on its target instead of running forever. On completion, the sampler snaps `Current` to the target and zeroes its velocity.

Spring sampling requires a `ValueMixer<T>` whose `SupportsVectorOperations` value is `true`, because the sampler uses vector subtraction, addition, scaling, and magnitude. Non-vector mixers are rejected when `CreateSampler` is called.

The built-in `TransformMixer` is an exception to that requirement: `SpringSpec<Transform>` decomposes its endpoints into translation X/Y, scale X/Y, rotation radians, and skew using `TransformMixer.Decompose`. Each component is an independent one-dimensional spring with the supplied stiffness, damping, mass, and rest thresholds. Rotation takes the same shortest angular path as a transform tween. The sampler retains component positions and velocities and composes only the sampled output, so intermediate zero scales do not trigger another decomposition. Existing endpoint decomposition restrictions still apply.

For this transform sampler, completion requires every component's absolute target distance and speed to meet their respective thresholds; the generic fixed-point completion shortcut does not apply. Completion returns the exact target transform. Component velocities are retained internally, but `Velocity` returns `null`: a transform matrix cannot losslessly represent those velocities.

Retargeting an active spring through `MotionValue<T>.AnimateTo` carries its current position and, with the incoming `VelocityMode.Preserve`, its velocity into the incoming spring parameters, including stiffness, damping, mass, and rest thresholds. `VelocityMode.Reset` resets velocity to zero. Both retarget modes continue without elapsed-time replay: a spring has no duration or progress, so `PreserveProgress` preserves motion state. The built-in transform spring carries its component positions and velocities internally, and a zero-scale target resolves against its current rotation and skew; see `TransformMixer` for endpoints that no component frame can represent. If cancellation callbacks change the current value, the normal new-sampler path starts from that value instead of retaining stale spring state. Duration-based specs retain their existing progress behavior.

Each call to `WithRestThresholds` or `WithVelocityMode` returns a new `SpringSpec<T>` instance. The original spec is not mutated.

Retargeting always changes the sampler target and marks the sampler incomplete. When `VelocityMode` is `SpringVelocityMode.Preserve`, the current velocity is kept. When it is `SpringVelocityMode.Reset`, velocity is reset to zero. The `RetargetMode` argument is accepted by the sampler but does not otherwise change spring retargeting behavior.

Very large advances are limited to `1000` fixed substeps, or about `8.333` seconds of simulated time. If the advance delta exceeds that window and the `MotionSpecContext` has diagnostics, the sampler records a warning and clamps the delta. Without diagnostics, the sampler throws `ArgumentOutOfRangeException` instead of silently clamping.

## Constructors

| Name | Description |
| --- | --- |
| `SpringSpec(float, float, float, float, float, SpringVelocityMode)` | Initializes a spring specification with optional stiffness, damping, mass, rest speed, rest delta, and velocity retargeting mode. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Stiffness` | `float` | Gets the positive spring stiffness used to pull the current value toward the target. The default is `520`. |
| `Damping` | `float` | Gets the non-negative damping value applied against current velocity. The default is `38`. |
| `Mass` | `float` | Gets the positive mass divisor used when calculating acceleration. The default is `1`. |
| `RestSpeed` | `float` | Gets the non-negative velocity magnitude threshold used to decide completion. The default is `0.01f`. |
| `RestDelta` | `float` | Gets the non-negative distance-to-target threshold used to decide completion. The default is `0.01f`. |
| `VelocityMode` | `SpringVelocityMode` | Gets whether retargeting preserves or resets the sampler velocity. The default is `SpringVelocityMode.Preserve`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `WithRestThresholds(float, float)` | `SpringSpec<T>` | Returns a copy with different rest speed and rest delta thresholds. |
| `WithVelocityMode(SpringVelocityMode)` | `SpringSpec<T>` | Returns a copy with a different velocity retargeting mode. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a spring sampler using a vector-capable mixer or the built-in transform component path. |
| `CreateSamplerUntyped(object?, object?, IValueMixer, MotionSpecContext)` | `MotionSampler` | Inherited from `MotionSpec<T>`. Casts untyped values and mixer instances before delegating to `CreateSampler`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `SpringSpec(float, float, float, float, float, SpringVelocityMode)` | `ArgumentOutOfRangeException` | `stiffness`, `mass`, `restSpeed`, or `restDelta` is not finite; `stiffness` or `mass` is less than or equal to `0`; or `damping`, `restSpeed`, or `restDelta` is negative. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `ArgumentNullException` | `mixer` or `context` is `null`, or a built-in transform spring endpoint is `null`. |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `InvalidOperationException` | The supplied mixer does not support vector operations and is not the built-in `TransformMixer`, or a transform endpoint cannot be decomposed. |

## Applies to

Cerneala UI motion specifications and value animation sampling.

## See also

- `Cerneala.UI.Motion.Specs.Motion`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Specs.MotionVelocity<T>`
- `Cerneala.UI.Motion.Specs.SpringVelocityMode`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
