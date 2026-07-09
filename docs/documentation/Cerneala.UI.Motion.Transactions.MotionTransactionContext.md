# MotionTransactionContext Class

## Definition
Namespace: `Cerneala.UI.Motion.Transactions`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Transactions/MotionTransactionContext.cs`

Observes root-owned UI property mutations inside active motion transaction scopes and converts eligible animatable property changes into motion-driven animations.

```csharp
public sealed class MotionTransactionContext : UiPropertyMutationObserver, IDisposable
```

Inheritance:
`object` -> `UiPropertyMutationObserver` -> `MotionTransactionContext`

Implements:
`IDisposable`

## Examples

Begin a transaction through the root motion system and animate registered animatable property changes:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

control.SetValue(Control.BackgroundProperty, DrawColor.Black, UiPropertyValueSource.AspectBase);

using (root.Motion.Transactions.Begin(Motion.Tween(TimeSpan.FromMilliseconds(150))))
{
    control.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.AspectBase);
}
```

Suppress transaction-driven animation for a group of mutations:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

UIRoot root = new();
Control control = new();
root.VisualChildren.Add(control);

using (root.Motion.Transactions.Disable())
{
    control.SetValue(Control.BackgroundProperty, DrawColor.White, UiPropertyValueSource.AspectBase);
}
```

## Remarks

`MotionTransactionContext` is created by `MotionSystem` and exposed through `MotionSystem.Transactions`. The convenience methods `MotionSystem.BeginTransaction(...)` and `MotionSystem.Disable()` delegate to this context.

Each call to `Begin` pushes a `MotionTransaction` onto an internal stack and returns a `MotionTransactionScope`. Dispose scopes in last-in, first-out order; disposing an outer scope before an inner scope causes `InvalidOperationException`. Disposing the same scope more than once is harmless because `MotionTransactionScope` guards against duplicate pops.

Only the top transaction affects a mutation. Nested transactions therefore use the innermost transaction options for property changes made while both scopes are active.

The context ignores mutations when it is disposed, when no transaction is active, when the top transaction is disabled, when the write source is `UiPropertyValueSource.Animation`, when the effective value did not change, when the target is not an attached `UIElement` belonging to the same root, or when the property is not registered in `MotionSystem.AnimatableProperties`.

For an eligible mutation, the context resolves a value mixer for the property type, creates or reuses a `MotionPropertyBinding<T>`, jumps the binding value to the old effective value, and animates it to the new effective value. Untyped specs are adapted to the property type through an internal adapter before sampling.

`Begin`, `Disable`, and internal scope popping verify motion thread affinity through `MotionThreadGuard`. Use the context from the UI thread captured by the owning `MotionSystem`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTransactionContext(MotionSystem motion)` | Initializes a transaction context for the supplied root-owned motion system. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Depth` | `int` | Gets the number of currently active transaction scopes. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Begin(MotionSpec defaultSpec)` | `MotionTransactionScope` | Begins a transaction using the supplied default motion spec. |
| `Begin(MotionTransactionOptions options)` | `MotionTransactionScope` | Begins a transaction using explicit transaction options. |
| `Disable()` | `MotionTransactionScope` | Begins a disabled transaction scope that suppresses transaction-created animations for observed mutations. |
| `Dispose()` | `void` | Marks the context disposed and clears all active transaction state. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `MotionTransactionContext(MotionSystem)` | `ArgumentNullException` | `motion` is `null`. |
| `Begin(MotionSpec)` | `ArgumentNullException` | `defaultSpec` is `null`. |
| `Begin(MotionTransactionOptions)` | `ArgumentNullException` | `options` is `null`. |
| `Begin(MotionSpec)`, `Begin(MotionTransactionOptions)`, `Disable()` | `InvalidOperationException` | The current thread is not the thread captured by the owning `MotionSystem.ThreadGuard`. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Transactions.MotionTransaction`
- `Cerneala.UI.Motion.Transactions.MotionTransactionOptions`
- `Cerneala.UI.Motion.Transactions.MotionTransactionScope`
- `Cerneala.UI.Motion.Properties.MotionPropertyBinding<T>`
