# MotionTransactionScope Class

## Definition

Namespace: `Cerneala.UI.Motion.Transactions`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Transactions/MotionTransactionScope.cs`

Represents the disposable lifetime of a motion transaction pushed into a `MotionTransactionContext`.

```csharp
public sealed class MotionTransactionScope : IDisposable
```

Inheritance:
`object` -> `MotionTransactionScope`

Implements:
`IDisposable`

## Examples

Use the scope returned by `MotionSystem.BeginTransaction` in a `using` block so the transaction is popped even when a property mutation throws:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

using (root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100))))
{
    control.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.White));
}
```

## Remarks

`MotionTransactionScope` is created by `MotionTransactionContext.Begin`, `MotionSystem.BeginTransaction`, or `MotionSystem.Disable`. It holds the `MotionTransaction` that was pushed onto the context stack and removes that transaction when `Dispose` is called.

Disposal is idempotent. Calling `Dispose` more than once on the same scope is harmless after the first successful pop.

Transaction scopes are stack ordered. Nested scopes must be disposed in reverse creation order; disposing an outer scope while an inner scope is still active causes the owning context to throw an `InvalidOperationException`.

The scope constructor is internal, so application code normally obtains instances from the root motion system rather than constructing them directly.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Transaction` | `MotionTransaction` | Gets the transaction represented by this scope. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispose()` | `void` | Pops `Transaction` from the owning transaction context the first time the scope is disposed. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Dispose()` | `InvalidOperationException` | The current thread is not the motion thread, or transaction scopes are disposed out of stack order. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Transactions.MotionTransaction`
- `Cerneala.UI.Motion.Transactions.MotionTransactionContext`
- `Cerneala.UI.Motion.Transactions.MotionTransactionOptions`
- `Cerneala.UI.Motion.Core.MotionSystem`
