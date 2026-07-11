# MotionTransactionContext.UntypedMotionSpecAdapter<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Transactions`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Transactions/MotionTransactionContext.cs`

Adapts an untyped `MotionSpec` to the typed `MotionSpec<T>` contract used by transaction-created property animations.

```csharp
private sealed class UntypedMotionSpecAdapter<T> : MotionSpec<T>
```

Containing type:
`MotionTransactionContext`

Inheritance:
`object` -> `MotionSpec` -> `MotionSpec<T>` -> `MotionTransactionContext.UntypedMotionSpecAdapter<T>`

Access:
`private`; instances are created by `MotionTransactionContext` when a transaction default spec is not already a `MotionSpec<T>`.

## Type Parameters

| Name | Description |
| --- | --- |
| `T` | The effective value type of the mutated UI property being animated. |

## Examples

Use an untyped transaction spec with an animatable property. Internally, the transaction context adapts the untyped spec to the property's value type before starting the property binding animation.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);

using (root.Motion.BeginTransaction(Motion.Tween(TimeSpan.FromMilliseconds(150))))
{
    control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.White), UiPropertyValueSource.AspectBase);
}
```

## Remarks

`UntypedMotionSpecAdapter<T>` is used by `MotionTransactionContext.ToTypedSpec<T>`. If the transaction's default specification is already a `MotionSpec<T>`, the context uses it directly. Otherwise, it wraps the untyped `MotionSpec` with this adapter and supplies the resolved `ValueMixer<T>` for the mutated property's value type.

`CreateSampler` delegates to the wrapped `MotionSpec.CreateSamplerUntyped` call, passing the typed `from` and `to` values plus the adapter's stored mixer. The method ignores its own mixer parameter because the transaction context already resolved the mixer that matches `T`.

The returned untyped sampler must also be a `MotionSampler<T>`. If the inner spec creates a sampler for a different value type, the adapter throws `InvalidOperationException` instead of allowing the property animation to continue with an incompatible sampler.

This class has no public construction path. It exists only to let property transactions use shared untyped factories such as `Motion.Tween(TimeSpan, IEasing?)` and `Motion.Spring(...)` across different animatable UI property types.

## Constructors

| Name | Description |
| --- | --- |
| `UntypedMotionSpecAdapter(MotionSpec, ValueMixer<T>)` | Initializes the adapter with the untyped specification to wrap and the mixer resolved for `T`. |

## Public Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `MotionSampler<T>` | Creates a typed sampler by delegating to the wrapped untyped specification and verifying that the returned sampler is a `MotionSampler<T>`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `CreateSampler(T, T, ValueMixer<T>, MotionSpecContext)` | `InvalidOperationException` | The wrapped untyped specification returns a sampler that is not assignable to `MotionSampler<T>`. |

## Applies to

Cerneala UI motion transactions that animate registered animatable UI property mutations.

## See also

- `Cerneala.UI.Motion.Transactions.MotionTransactionContext`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.MotionSpec<T>`
- `Cerneala.UI.Motion.Specs.MotionSampler<T>`
- `Cerneala.UI.Motion.Interpolation.ValueMixer<T>`
