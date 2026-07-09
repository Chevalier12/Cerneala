# SpringSpec<T>.VectorSpringSampler Class

## Definition
Namespace: `Cerneala.UI.Motion.Specs`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Specs/SpringSpec.cs`

Samples a `SpringSpec<T>` by integrating vector-capable values toward a target with spring force, damping, and mass.

```csharp
private sealed class VectorSpringSampler : MotionSampler<T>
```

Inheritance:
`object` -> `MotionSampler` -> `MotionSampler<T>` -> `SpringSpec<T>.VectorSpringSampler`

Containing type:
`SpringSpec<T>`

Access:
`private`; this nested sampler is an implementation detail created by `SpringSpec<T>.CreateSampler`.

## Examples

Callers do not create `VectorSpringSampler` directly. Create it through `SpringSpec<T>.CreateSampler`, usually with a spring created by the `Motion` factory.

```csharp
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Specs;

MotionSpecContext context = new(
    ReducedMotionPolicy.Default,
    new ValueMixerRegistry(),
    Diagnostics: null,
    Now: TimeSpan.Zero);

SpringSpec<float> spec = Motion.Spring<float>()
    .WithRestThresholds(restSpeed: 0.01f, restDelta: 0.01f);

MotionSampler<float> sampler = spec.CreateSampler(0f, 100f, new FloatMixer(), context);

sampler.Advance(TimeSpan.FromMilliseconds(16));

float value = sampler.Current;
MotionVelocity<float>? velocity = sampler.Velocity;
```

## Remarks

`VectorSpringSampler` stores the source `SpringSpec<T>`, endpoint values, current velocity, `ValueMixer<T>`, and `MotionSpecContext` supplied by `SpringSpec<T>.CreateSampler`. The owning spec creates it only after confirming that the mixer supports vector operations.

The sampler initializes `Current` to the starting value, sets the target to the requested destination, and initializes velocity to the zero vector produced by scaling the initial displacement by `0`.

`Advance(TimeSpan)` rejects negative deltas. For positive deltas, it integrates the spring in fixed `1 / 120` second substeps with a maximum of 1000 substeps per call. Deltas above that window are clamped and recorded as a diagnostic warning when `MotionSpecContext.Diagnostics` is available; without diagnostics, the oversized delta throws `ArgumentOutOfRangeException`.

The integrator uses semi-implicit Euler integration. It computes displacement from `current` to `target`, applies spring force from `Stiffness`, damping force from `Damping`, acceleration from `Mass`, then updates velocity before updating current value.

Completion is rest-threshold based. After advancing, the sampler snaps `Current` to the target, zeros velocity, and sets `IsComplete` when distance to target is less than or equal to `RestDelta` and velocity magnitude is less than or equal to `RestSpeed`.

`Retarget(T, RetargetMode)` changes only the target and marks the sampler incomplete. The `mode` argument is not inspected by this sampler. Velocity is preserved by default; when the owning spec uses `SpringVelocityMode.Reset`, retargeting zeros velocity.

## Constructors

| Name | Description |
| --- | --- |
| `VectorSpringSampler(SpringSpec<T> spec, T from, T to, ValueMixer<T> mixer, MotionSpecContext context)` | Initializes the sampler from the owning spring spec, starting value, target value, vector-capable mixer, and spec context. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `T` | Gets the current integrated spring value. |
| `IsComplete` | `bool` | Gets whether the value and velocity are within the owning spec's rest thresholds. |
| `Velocity` | `MotionVelocity<T>?` | Gets the current sampled velocity wrapped in `MotionVelocity<T>`. |
| `CurrentUntyped` | `object?` | Gets `Current` through the inherited `MotionSampler<T>` implementation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `void` | Advances the spring by fixed substeps, optionally clamps huge deltas through diagnostics, and completes when rest thresholds are met. |
| `Retarget(T to, RetargetMode mode)` | `void` | Changes the target, optionally resets velocity according to `SpringVelocityMode`, and clears completion. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `void` | Retargets through the inherited typed-value adapter from `MotionSampler<T>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Advance(TimeSpan delta)` | `ArgumentOutOfRangeException` | `delta` is negative. |
| `Advance(TimeSpan delta)` | `ArgumentOutOfRangeException` | `delta` exceeds the supported integration window and the context has no `MotionDiagnostics` instance to record the clamp. |
| `RetargetUntyped(object? to, RetargetMode mode)` | `ArgumentException` | `to` is not assignable to `T`, except for `null` when `T` allows it. |

## Applies to

Cerneala UI motion specifications that use vector-capable mixers for spring animation sampling.

## See also

- `SpringSpec<T>`
- `SpringVelocityMode`
- `MotionSampler<T>`
- `MotionVelocity<T>`
- `ValueMixer<T>`
- `MotionSpecContext`
