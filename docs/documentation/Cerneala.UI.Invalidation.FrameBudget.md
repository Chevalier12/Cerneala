# FrameBudget Struct

## Definition
Namespace: `Cerneala.UI.Invalidation`  
Assembly/Project: `Cerneala`  
Source: `UI/Invalidation/FrameBudget.cs`

Represents the frame scheduler budget value passed to `UiFrameScheduler.ProcessFrame` and `UIRoot.ProcessFrame`.

```csharp
public readonly record struct FrameBudget(int? MaxWorkItems)
```

Inheritance:  
`Object` -> `ValueType` -> `FrameBudget`

Implements:  
`IEquatable<FrameBudget>` through the generated record struct members.

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();

FrameStats stats = root.ProcessFrame(
    FramePhaseProcessors.Empty,
    new FrameBudget(1));
```

In the current scheduler implementation, the `FrameBudget(1)` value is accepted by the API, but the frame still processes all queued work in the current deterministic snapshots.

## Remarks

`FrameBudget` is a small immutable value used by frame processing APIs to carry an optional maximum work item count. `FrameBudget.ProcessAll` and `default(FrameBudget)` both represent an unbounded budget because `MaxWorkItems` is `null`.

The current MVP scheduler does not enforce `MaxWorkItems`. `UiFrameScheduler.ProcessFrame` normalizes the default value to `FrameBudget.ProcessAll`, then processes all queued work for each phase snapshot. The `DefersWork` property always returns `false`, so callers should not use it as evidence that remaining queue work was deferred by budget enforcement.

`FrameBudget` is a `readonly record struct`, so value equality, deconstruction, and display formatting are generated from the primary constructor state.

## Constructors

| Name | Description |
| --- | --- |
| `FrameBudget(int? MaxWorkItems)` | Initializes a budget with an optional maximum work item count. `null` means process all work. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `MaxWorkItems` | `int?` | Gets the optional maximum work item count supplied to the budget. The current scheduler does not enforce this value. |
| `ProcessAll` | `FrameBudget` | Gets an unbounded budget represented by `new FrameBudget(null)`. |
| `DefersWork` | `bool` | Gets `false`; the current budget implementation does not report deferred work. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out int? MaxWorkItems)` | Deconstructs the generated record struct state into `MaxWorkItems`. |
| `Equals(FrameBudget other)` | Determines whether another `FrameBudget` has the same record value. |
| `Equals(object? obj)` | Determines whether an object is an equal `FrameBudget` value. |
| `GetHashCode()` | Returns the hash code generated from the record value. |
| `ToString()` | Returns the generated record string representation. |

## Operators

| Name | Description |
| --- | --- |
| `operator ==(FrameBudget left, FrameBudget right)` | Compares two `FrameBudget` values for record equality. |
| `operator !=(FrameBudget left, FrameBudget right)` | Compares two `FrameBudget` values for record inequality. |

## Applies to

Cerneala retained UI invalidation and frame scheduling.

## See also

- `UI/Invalidation/UiFrameScheduler.cs`
- `UI/Elements/UIRoot.cs`
- `UI/Invalidation/FrameStats.cs`
