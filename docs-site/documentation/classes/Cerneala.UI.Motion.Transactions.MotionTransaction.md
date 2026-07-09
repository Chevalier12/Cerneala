# MotionTransaction Class

## Definition

Namespace: `Cerneala.UI.Motion.Transactions`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Transactions/MotionTransaction.cs`

Represents the immutable transaction state currently pushed into a `MotionTransactionContext`.

```csharp
public sealed class MotionTransaction
```

Inheritance:
`object` -> `MotionTransaction`

## Examples

Inspect the transaction represented by a transaction scope:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Transactions;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

UIRoot root = new();

using MotionTransactionScope scope =
    root.Motion.BeginTransaction(MotionFactory.Tween(TimeSpan.FromMilliseconds(100)));

MotionTransaction transaction = scope.Transaction;
MotionTransactionOptions options = transaction.Options;
bool suppressesAnimation = transaction.IsDisabled;
```

## Remarks

`MotionTransaction` is created internally by `MotionTransactionContext.Begin(MotionTransactionOptions)` when a transaction scope starts. Application code normally observes a transaction through `MotionTransactionScope.Transaction` rather than constructing one directly.

The transaction stores the immutable `MotionTransactionOptions` supplied to the begin call. `MotionTransactionContext` reads the top transaction on its stack when handling a UI property mutation. If `IsDisabled` is `true`, the context suppresses transaction-created animations for matching mutations.

Nested transaction scopes use the innermost transaction. Mutations made while multiple scopes are active are therefore controlled by the options on the top transaction, not by an outer scope.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Options` | `MotionTransactionOptions` | Gets the immutable options associated with the transaction. |
| `IsDisabled` | `bool` | Gets whether `Options` marks the transaction as disabled. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Transactions.MotionTransactionContext`
- `Cerneala.UI.Motion.Transactions.MotionTransactionOptions`
- `Cerneala.UI.Motion.Transactions.MotionTransactionScope`
- `Cerneala.UI.Motion.Core.MotionSystem`
