# MotionValue<T>.Subscription Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionValue{T}.cs`

Removes a single `MotionValue<T>` change listener when the subscription returned by `MotionValue<T>.Subscribe` is disposed.

```csharp
private sealed class Subscription : IDisposable
```

Containing type:
`MotionValue<T>`

Inheritance:
`object` -> `MotionValue<T>.Subscription`

Implements:
`IDisposable`

## Examples

`Subscription` is a private nested implementation detail. Application code receives it through `MotionValue<T>.Subscribe` as an `IDisposable`.

```csharp
using Cerneala.UI.Motion.Core;

MotionValue<double> opacity = graph.CreateValue(0d);
List<MotionValueChanged<double>> changes = [];

IDisposable subscription = opacity.Subscribe(changes.Add);

opacity.JumpTo(1d);
subscription.Dispose();
opacity.JumpTo(0.5d);

// The second JumpTo no longer reaches this listener.
```

## Remarks

`MotionValue<T>.Subscribe` stores the supplied `Action<MotionValueChanged<T>>` listener in the owning value's listener list, then returns a `Subscription` that captures the same list and listener.

Calling `Dispose` removes that listener from the list. Disposal is idempotent: the first call marks the subscription as disposed and removes the listener; later calls return without mutating the list again.

`MotionValue<T>` delivers change notifications from a snapshot of the listener list, so callbacks can cancel, complete, or start motion while notifications are being processed. Disposing a subscription prevents later notifications from reaching the captured listener.

Because the class is private, callers cannot construct it directly or depend on its concrete type. Treat the value returned by `Subscribe` as an `IDisposable` and dispose it when the listener should stop receiving value changes.

## Constructors

| Name | Description |
| --- | --- |
| `Subscription(List<Action<MotionValueChanged<T>>>, Action<MotionValueChanged<T>>)` | Captures the listener list and listener that should be removed during disposal. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Removes the captured listener from the captured listener list once. |

## Applies to

Cerneala motion core graph values.

## See also

- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionValue`
- `Cerneala.UI.Motion.Core.MotionValueChanged<T>`
