# DerivedMotionValue<T>.Subscription Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/DerivedMotionValue{T}.cs`

Removes a single `DerivedMotionValue<T>` change listener when the subscription returned by `DerivedMotionValue<T>.Subscribe` is disposed.

```csharp
private sealed class Subscription : IDisposable
```

Containing type:
`DerivedMotionValue<T>`

Inheritance:
`object` -> `DerivedMotionValue<T>.Subscription`

Implements:
`IDisposable`

## Examples

`Subscription` is a private nested implementation detail. Application code receives it through `DerivedMotionValue<T>.Subscribe` as an `IDisposable`.

```csharp
using Cerneala.UI.Motion.Core;

MotionValue<double> x = graph.CreateValue(2d);
MotionValue<double> y = graph.CreateValue(3d);

using DerivedMotionValue<double> sum =
    MotionValue.Combine(x, y, static (left, right) => left + right);

int notifications = 0;
IDisposable subscription = sum.Subscribe(_ => notifications++);

x.JumpTo(4d);
subscription.Dispose();
y.JumpTo(6d);

// The second dependency change no longer reaches this listener.
```

## Remarks

`DerivedMotionValue<T>.Subscribe` stores the supplied `Action<MotionValueChanged<T>>` listener in the owning derived value's listener list, then returns a `Subscription` that captures the same list and listener.

Calling `Dispose` removes that listener from the list. Disposal is idempotent: the first call marks the subscription as disposed and removes the listener; later calls return without mutating the list again.

Because the class is private, callers cannot construct it directly or depend on its concrete type. Treat the value returned by `Subscribe` as an `IDisposable` and dispose it when the listener should stop receiving recomputed value changes.

## Constructors

| Name | Description |
| --- | --- |
| `Subscription(List<Action<MotionValueChanged<T>>>, Action<MotionValueChanged<T>>)` | Captures the listener list and listener that should be removed during disposal. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Removes the captured listener from the captured listener list once. |

## Applies to

Cerneala motion value composition.

## See also

- `Cerneala.UI.Motion.Core.DerivedMotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionValue`
- `Cerneala.UI.Motion.Core.MotionValueChanged<T>`
