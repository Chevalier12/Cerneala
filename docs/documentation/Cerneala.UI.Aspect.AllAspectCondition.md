# AllAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal compound aspect condition node that matches only when every child condition matches.

```csharp
internal sealed class AllAspectCondition : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `AllAspectCondition`

## Examples

Create and evaluate an all-condition through the public `AspectCondition` factory:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.All(
    AspectCondition.State(AspectState.Hover),
    AspectCondition.State(AspectState.Focus));

Button button = new();
AspectMatchContext context = new(
    button,
    states: AspectStateSet.Empty
        .Add(AspectState.Hover)
        .Add(AspectState.Focus));

AspectConditionResult result = condition.Evaluate(context);

if (result.Matches)
{
    // Every child condition matched. The result also carries each child result.
}
```

## Remarks

`AllAspectCondition` is created by `AspectCondition.All(params AspectCondition[])`. The type is internal, so callers normally compose conditions through the public `AspectCondition` wrapper.

The public factory requires at least one non-null child condition before constructing this node. The node constructor itself rejects a `null` child list.

Evaluation runs every child condition, then returns an `AspectConditionResult` whose `Matches` value is `true` only when all child results matched. Child dependencies are flattened into the parent result, the diagnostic text is `all`, and the child results are preserved in `AspectConditionResult.Children`.

The condition specificity starts with `new AspectSpecificity(Compound: 1)` and adds the specificity of each child condition.

## Constructors

| Name | Description |
| --- | --- |
| `AllAspectCondition(IReadOnlyList<AspectConditionNode> children)` | Creates an all-condition from child nodes. Throws `ArgumentNullException` when `children` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the compound specificity point plus the summed specificity of all child conditions. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates every child condition and returns a matching result only when all child results match. |

## Applies to

Cerneala UI aspect condition internals used by `AspectCondition.All(params AspectCondition[])` and aspect rule matching.

## See also

- `AspectCondition`
- `AspectConditionNode`
- `AspectConditionResult`
- `AspectConditionDependency`
- `AspectMatchContext`
- `AspectSpecificity`
