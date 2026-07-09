# PredicateAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Evaluates an aspect condition by passing the full `AspectMatchContext` to a caller-supplied predicate.

```csharp
internal sealed class PredicateAspectCondition : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `PredicateAspectCondition`

## Examples

Predicate conditions are normally created through `AspectCondition.Predicate`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.Predicate(
    "item has index",
    context => context.ItemIndex.HasValue);

Button button = new();
AspectDataContext dataContext = new(data: "row", index: 2);
AspectMatchContext context = new(button, dataContext: dataContext);

AspectConditionResult result = condition.Evaluate(context);

if (result.Matches)
{
    // The internal PredicateAspectCondition matched and reported a Predicate dependency.
}
```

## Remarks

`PredicateAspectCondition` is the internal condition node created by `AspectCondition.Predicate(string, Func<AspectMatchContext, bool>)`. It is useful when a condition needs to inspect the complete match context instead of only a single state, variant, UI property, or typed data value.

During evaluation, the node calls the stored `Func<AspectMatchContext, bool>` with the supplied context. It returns an `AspectConditionResult` whose `Matches` value is the predicate result, whose `Dependencies` contains one `AspectConditionDependency` with `AspectConditionDependencyKind.Predicate`, and whose `DiagnosticName` is the condition diagnostic name.

The diagnostic text is derived from the diagnostic name: `"<name> matched"` when the predicate returns `true`, and `"<name> did not match"` when it returns `false`.

The node contributes one predicate point to aspect specificity by returning `new AspectSpecificity(Predicate: 1)`.

## Constructors

| Name | Description |
| --- | --- |
| `PredicateAspectCondition(string, Func<AspectMatchContext, bool>)` | Initializes a predicate condition with a diagnostic name and a predicate that receives the full match context. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the condition specificity contribution, with `Predicate` set to `1`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext)` | `AspectConditionResult` | Applies the stored predicate to the supplied context and returns match state, a predicate dependency, and diagnostic text. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PredicateAspectCondition(string, Func<AspectMatchContext, bool>)` | `ArgumentException` | `diagnosticName` is `null`, empty, or whitespace. |
| `PredicateAspectCondition(string, Func<AspectMatchContext, bool>)` | `ArgumentNullException` | `predicate` is `null`. |

## Applies to

Cerneala retained UI aspect condition internals used by `AspectCondition.Predicate(string, Func<AspectMatchContext, bool>)`.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectConditionNode`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectConditionResult`
- `Cerneala.UI.Aspect.AspectConditionDependency`
