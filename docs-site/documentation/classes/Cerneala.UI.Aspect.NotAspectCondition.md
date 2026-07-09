# NotAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal compound aspect-condition node that inverts the match result of one child condition.

```csharp
internal sealed class NotAspectCondition : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `NotAspectCondition`

## Examples

Create and evaluate a negated condition through the public `AspectCondition.Not` factory:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.Not(
    AspectCondition.State(AspectState.Disabled));

Button button = new();

bool matchesWhenDisabledIsMissing = condition
    .Evaluate(new AspectMatchContext(button))
    .Matches;

bool matchesWhenDisabledIsPresent = condition
    .Evaluate(new AspectMatchContext(
        button,
        states: AspectStateSet.Empty.Add(AspectState.Disabled)))
    .Matches;
```

## Remarks

`NotAspectCondition` is created by `AspectCondition.Not(AspectCondition)`. The type is internal, so callers normally use the public `AspectCondition` wrapper instead of constructing the node directly.

The constructor requires a non-null child `AspectConditionNode`. Evaluation first evaluates that child, then returns an `AspectConditionResult` whose `Matches` value is the inverse of the child result. The returned result keeps the child's dependencies unchanged, uses `not` as its diagnostic text, and stores the child result in `Children`.

Specificity is compound-specific: the condition contributes one compound point and adds the child condition's specificity.

## Constructors

| Name | Description |
| --- | --- |
| `NotAspectCondition(AspectConditionNode child)` | Initializes a negating condition around `child`. Throws `ArgumentNullException` when `child` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets `new AspectSpecificity(Compound: 1) + child.Specificity`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates the child condition, inverts its match result, preserves its dependencies, and records the child result. |

## Applies to

Cerneala UI aspect condition internals used by `AspectCondition.Not(AspectCondition)` and aspect rule matching.

## See also

- [AspectCondition](Cerneala.UI.Aspect.AspectCondition.md)
- [AspectConditionNode](Cerneala.UI.Aspect.AspectConditionNode.md)
- `AspectConditionResult`
- `AspectMatchContext`
- `AspectSpecificity`
