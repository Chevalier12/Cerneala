# UiPropertyMutationObserver Class

## Definition
Namespace: `Cerneala.UI.Core`

Assembly/Project: `Cerneala`

Source: `UI/Core/UiPropertyMutationObserver.cs`

Receives low-level UI property mutation notifications from `UiObject` implementations that expose a mutation observer.

```csharp
public abstract class UiPropertyMutationObserver
```

Inheritance:
`object` -> `UiPropertyMutationObserver`

Derived:
`MotionTransactionContext`, and the internal root observer that `UIElement` uses.

## Remarks

`UiPropertyMutationObserver` is the internal observer base used by Cerneala's UI property system to report every set or clear operation as a `UiPropertyMutation`.

`UiObject` exposes a protected virtual `MutationObserver` property that returns `null` by default. When a derived object supplies an observer, `UiObject` calls that observer after each property mutation. This happens both when the mutation changes the effective value and when the requested source value changes without changing the resolved effective value.

`UIElement` returns its root's observer (`Root?.PropertyMutations`), or `null` when detached. That root observer forwards each mutation to three recipients: the root's motion layout, its motion transaction context, and its aspect processor. `MotionTransactionContext` uses these mutation records to animate eligible property changes during active motion transactions.

Although the class is public, its notification method is `internal abstract`. Consumers outside the `Cerneala` assembly can reference the type, but cannot implement a useful derived observer because the required override is assembly-internal.

## Constructors

| Name | Description |
| --- | --- |
| `UiPropertyMutationObserver()` | Initializes the observer base type. Because the class is abstract, instances are created through derived types. |

## Methods

`UiPropertyMutationObserver` declares no public methods.

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `UI/Core/UiPropertyMutationObserver.cs`
- `UI/Core/UiPropertyMutation.cs`
- `UI/Core/UiObject.cs`
- `UI/Elements/UIElement.cs`
- `UI/Motion/Transactions/MotionTransactionContext.cs`
