# MotionTransactionOptions Class

## Definition

Namespace: `Cerneala.UI.Motion.Transactions`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Transactions/MotionTransactionOptions.cs`

Configures the default motion specification and disabled state for a motion transaction.

```csharp
public sealed class MotionTransactionOptions
```

Inheritance:
`object` -> `MotionTransactionOptions`

## Examples

Start a transaction with an explicit options object:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Transactions;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

MotionTransactionOptions options = new(
    MotionFactory.Tween(TimeSpan.FromMilliseconds(120)));

using (root.Motion.BeginTransaction(options))
{
    control.Background = Color.White;
}
```

Create a disabled transaction options value:

```csharp
using Cerneala.UI.Motion.Transactions;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

MotionTransactionOptions disabled = new(
    MotionFactory.Tween(TimeSpan.FromMilliseconds(1)),
    isDisabled: true);
```

## Remarks

`MotionTransactionOptions` is consumed by `MotionTransactionContext.Begin(MotionTransactionOptions)` and by `MotionSystem.BeginTransaction(MotionTransactionOptions)`. The options instance is stored on the created `MotionTransaction` and is immutable after construction.

`DefaultSpec` is the transaction-level `MotionSpec` used when an animatable property mutation is converted into a motion property animation. The transaction pipeline uses the current top transaction on the stack, so nested transactions use the inner transaction's options for mutations performed inside the inner scope.

When `IsDisabled` is `true`, the transaction context suppresses transaction-created animations. Property mutations still apply normally through their original value source; the transaction simply does not create a motion property binding for them.

The constructor requires a non-null `defaultSpec`. Use `MotionTransactionContext.Disable()` or `MotionSystem.Disable()` when the goal is to suppress implicit transaction animation through the built-in disabled transaction helper.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTransactionOptions(MotionSpec defaultSpec, bool isDisabled = false)` | Initializes transaction options with the supplied default motion specification and optional disabled flag. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DefaultSpec` | `MotionSpec` | Gets the default motion specification used for animatable property mutations captured by the transaction. |
| `IsDisabled` | `bool` | Gets whether the transaction suppresses transaction-created animations. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionTransactionOptions(MotionSpec, bool)` | `ArgumentNullException` | `defaultSpec` is `null`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Transactions.MotionTransaction`
- `Cerneala.UI.Motion.Transactions.MotionTransactionContext`
- `Cerneala.UI.Motion.Transactions.MotionTransactionScope`
- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Specs.MotionSpec`
