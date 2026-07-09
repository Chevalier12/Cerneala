# DataAspectCondition<TData> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal aspect-condition node used by `AspectCondition.Data<TData>` to evaluate a predicate against the current typed aspect data context.

```csharp
internal sealed class DataAspectCondition<TData> : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `DataAspectCondition<TData>`

## Examples

Create a public data condition through `AspectCondition.Data<TData>`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.Data<UserCard>(
    "important user",
    user => user.IsImportant,
    AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant)));

AspectMatchContext context = new(
    new Button(),
    dataContext: new AspectDataContext(new UserCard(true)));

AspectConditionResult result = condition.Evaluate(context);

if (result.Matches)
{
    // The data object was a UserCard and the predicate returned true.
}

internal sealed record UserCard(bool IsImportant);
```

## Remarks

`DataAspectCondition<TData>` is an internal condition node. Callers normally create it through `AspectCondition.Data<TData>(string, Func<TData, bool>, params AspectDataDependency[])`.

The constructor requires a non-empty diagnostic name, a non-null predicate, and at least one data dependency. Dependencies are copied into an array so later changes to the caller-provided list do not change the node's stored dependency set.

`Evaluate(AspectMatchContext)` matches only when `context.Data` is assignable to `TData` and the predicate returns `true`. A missing data context, `null` data value, wrong data type, or predicate result of `false` produces a non-matching result.

Every evaluation reports one `AspectConditionDependency` with `Kind` set to `DataContext` for each declared `AspectDataDependency`. The diagnostic text is based on the diagnostic name and ends in either `matched` or `did not match`.

The node contributes `new AspectSpecificity(Data: 1)` to rule ordering.

## Constructors

| Name | Description |
| --- | --- |
| `DataAspectCondition(string diagnosticName, Func<TData, bool> predicate, IReadOnlyList<AspectDataDependency> dependencies)` | Initializes a typed data condition with diagnostic text, a predicate, and declared data-context dependencies. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the data specificity contribution for this condition. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates the current data context as `TData`, applies the predicate, and returns match state, data-context dependencies, and diagnostic text. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `DataAspectCondition(...)` | `ArgumentException` | `diagnosticName` is null, empty, or whitespace. |
| `DataAspectCondition(...)` | `ArgumentException` | `dependencies` is null or empty. |
| `DataAspectCondition(...)` | `ArgumentNullException` | `predicate` is null. |

## Applies to

Cerneala UI aspect matching internals used by `AspectCondition.Data<TData>` and aspect invalidation.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectDataContext`
- `Cerneala.UI.Aspect.AspectDataDependency`
- `Cerneala.UI.Aspect.AspectConditionDependency`
- `Cerneala.UI.Aspect.AspectMatchContext`
