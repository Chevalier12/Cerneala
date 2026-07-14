# DerivedMotionValue<T> Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/DerivedMotionValue{T}.cs`

Represents a read-only motion value computed from other motion values.

```csharp
public sealed class DerivedMotionValue<T> : MotionValue, IDisposable
```

Inheritance:
`object` -> `MotionValue` -> `DerivedMotionValue<T>`

Implements:
`IDisposable`

## Examples

```csharp
using Cerneala.UI.Motion.Core;

MotionGraph graph = new();
MotionValue<double> x = graph.CreateValue(2d);
MotionValue<double> y = graph.CreateValue(3d);

using DerivedMotionValue<double> sum =
    MotionValue.Combine(x, y, static (left, right) => left + right);

using IDisposable subscription = sum.Subscribe(change =>
{
    double currentSum = change.NewValue;
});

x.JumpTo(4d);
y.JumpTo(6d);

double finalSum = sum.Current;
```

## Remarks

`DerivedMotionValue<T>` is created by `MotionValue.Combine<T1, T2, TOut>`. The value is computed once when the derived value is constructed, then recomputed whenever one of the source `MotionValue<T>` instances notifies a change.

The class compares the recomputed value with `EqualityComparer<T>.Default`. Subscribers are notified only when the computed value changes. Notifications use `MotionValueChanged<T>` with the old value, new value, target equal to the new value, and `IsAnimating` set to `false`.

Call `Dispose` when the derived value is no longer needed. Disposal unsubscribes from its source values, clears listeners, keeps the last computed `Current` value, and causes later calls to `Subscribe` to throw `ObjectDisposedException`.

## Properties

| Name | Description |
| --- | --- |
| `Current` | Gets the last computed value. |

## Methods

| Name | Description |
| --- | --- |
| `Subscribe(Action<MotionValueChanged<T>>)` | Adds a listener for computed value changes and returns a subscription that removes the listener when disposed. |
| `Dispose()` | Unsubscribes from dependencies and clears all listeners. |

## Applies to

Cerneala motion value composition.

## See also

- `Cerneala.UI.Motion.Core.MotionValue`
- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionValueChanged<T>`
