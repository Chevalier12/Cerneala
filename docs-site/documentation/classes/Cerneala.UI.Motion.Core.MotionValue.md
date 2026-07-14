# MotionValue Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionValue.cs`

Provides the non-generic base type and composition helper for motion values.

```csharp
public abstract class MotionValue
```

Inheritance:
`object` -> `MotionValue`

Derived:
`DerivedMotionValue<T>`, `MotionValue<T>`

## Examples

Create a derived value from two graph-owned motion values:

```csharp
using Cerneala.UI.Motion.Core;

MotionGraph graph = new();
MotionValue<double> x = graph.CreateValue(2d);
MotionValue<double> y = graph.CreateValue(3d);

using DerivedMotionValue<double> sum =
    MotionValue.Combine(x, y, static (left, right) => left + right);

using IDisposable subscription = sum.Subscribe(change =>
{
    double newSum = change.NewValue;
});

x.JumpTo(4d);
y.JumpTo(6d);

double currentSum = sum.Current;
```

## Remarks

`MotionValue` is the shared base for typed motion values and derived motion values. Use `MotionValue<T>` for mutable, graph-owned animated values, and `DerivedMotionValue<T>` for values computed from other motion values.

`Combine<T1, T2, TOut>` creates a `DerivedMotionValue<TOut>` whose current value is produced by calling the selector with the current values of both dependencies. The derived value subscribes to both source values and recomputes whenever either dependency reports a change.

Dispose the returned `DerivedMotionValue<TOut>` when it is no longer needed. Disposal unsubscribes from both source values and clears listeners held by the derived value.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Combine<T1, T2, TOut>(MotionValue<T1>, MotionValue<T2>, Func<T1, T2, TOut>)` | `DerivedMotionValue<TOut>` | Creates a derived value that recomputes from two source motion values whenever either source changes. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionValue<T>`
- `Cerneala.UI.Motion.Core.DerivedMotionValue<T>`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionValueChanged<T>`
