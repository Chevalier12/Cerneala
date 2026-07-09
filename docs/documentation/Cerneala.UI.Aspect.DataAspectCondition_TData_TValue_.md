# DataAspectCondition<TData, TValue> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal aspect condition node that reads typed data-context input, selects a value from it, and matches when a predicate accepts that selected value.

```csharp
internal sealed class DataAspectCondition<TData, TValue> : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `DataAspectCondition<TData, TValue>`

## Examples

Create the condition through the public `AspectCondition.Data<TData, TValue>` factory:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.Data<UserCard, bool>(
    "important user",
    user => user.IsImportant,
    isImportant => isImportant,
    AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant)));

AspectConditionResult result = condition.Evaluate(
    new AspectMatchContext(new Button(), data: new UserCard(true)));

bool applies = result.Matches;
```

## Remarks

`DataAspectCondition<TData, TValue>` is created by `AspectCondition.Data<TData, TValue>(string, Func<TData, TValue>, Func<TValue, bool>, params AspectDataDependency[])`. The class is internal, so application code normally works with the returned `AspectCondition`.

Evaluation first checks whether `AspectMatchContext.Data` is assignable to `TData`. If the data object is not `TData`, the condition does not match and the selector and predicate are not invoked. When the data object is `TData`, the condition calls the selector, passes the selected `TValue` to the predicate, and uses the predicate result as the match result.

The constructor rejects an empty or whitespace diagnostic name, a missing or empty dependency list, a null selector, and a null predicate. The dependency list is copied to an array during construction.

Every declared `AspectDataDependency` is reported as an `AspectConditionDependency` with kind `DataContext`. This lets aspect invalidation know that the condition depends on data-context state. The result diagnostic text is `{diagnosticName} matched` or `{diagnosticName} did not match`. The condition contributes `new AspectSpecificity(Data: 1)` to rule specificity.

## Constructors

| Name | Description |
| --- | --- |
| `DataAspectCondition(string diagnosticName, Func<TData, TValue> selector, Func<TValue, bool> predicate, IReadOnlyList<AspectDataDependency> dependencies)` | Creates a data-context condition that selects a value from typed data and evaluates that selected value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets a specificity value with the `Data` component set to `1`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates the condition against the match context data, reports data-context dependencies, and returns diagnostic text for the result. |

## Applies to

Cerneala UI aspect condition internals used by `AspectCondition.Data<TData, TValue>` and aspect rule matching.

## See also

- `AspectCondition`
- `AspectConditionNode`
- `AspectMatchContext`
- `AspectConditionResult`
- `AspectConditionDependency`
- `AspectDataDependency`
- `AspectSpecificity`
