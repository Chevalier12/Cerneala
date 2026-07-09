# AspectConditionNode Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Provides the internal base node for aspect condition evaluation, dependency tracking, and specificity calculation.

```csharp
internal abstract class AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode`

Derived:
`StateAspectCondition`, `VariantAspectCondition`, `PropertyAspectCondition<TValue>`, `DataAspectCondition<TData>`, `DataAspectCondition<TData, TValue>`, `AllAspectCondition`, `AnyAspectCondition`, `NotAspectCondition`, `PredicateAspectCondition`

## Examples

Create and evaluate a condition through the public `AspectCondition` wrapper:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.State(AspectState.Hover);

Button button = new();
AspectMatchContext context = new(
    button,
    states: AspectStateSet.Empty.Add(AspectState.Hover));

AspectConditionResult result = condition.Evaluate(context);

if (result.Matches)
{
    // The wrapped StateAspectCondition matched and reported a State dependency.
}
```

## Remarks

`AspectConditionNode` is the internal representation stored by `AspectCondition`. The public factory methods on `AspectCondition` create concrete node types, while `AspectCondition.Evaluate(AspectMatchContext)` delegates evaluation to the wrapped node.

Each node returns an `AspectConditionResult` containing whether the condition matched, the dependencies that should invalidate the result when relevant inputs change, and diagnostic text. Compound nodes preserve child results so diagnostics can show how `All`, `Any`, and `Not` conditions were resolved.

Specificity is calculated by each concrete node. State, variant, property, data, and predicate nodes each increment the matching specificity category; compound nodes add one compound point and then add the specificity of their children.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the specificity contribution used when aspect rules are ordered or compared. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates the condition against an aspect match context and returns match state, dependencies, diagnostics, and optional child results. |

## Applies to

Cerneala UI aspect condition internals used by `AspectCondition` and the aspect resolution pipeline.

## See also

- `AspectCondition`
- `AspectConditionResult`
- `AspectConditionDependency`
- `AspectMatchContext`
- `AspectSpecificity`
