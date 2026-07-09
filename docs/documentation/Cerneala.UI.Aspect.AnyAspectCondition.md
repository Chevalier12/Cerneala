# AnyAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal compound condition node used by `AspectCondition.Any` to match when at least one child condition matches.

```csharp
internal sealed class AnyAspectCondition : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `AnyAspectCondition`

## Examples

Create an `AnyAspectCondition` through the public `AspectCondition.Any` factory:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.Any(
    AspectCondition.State(AspectState.Hover),
    AspectCondition.State(AspectState.Focus));

AspectMatchContext context = new(
    new Button(),
    states: AspectStateSet.Empty.Add(AspectState.Focus));

AspectConditionResult result = condition.Evaluate(context);

bool applies = result.Matches;
```

## Remarks

`AnyAspectCondition` is an internal node. User code normally creates it through `AspectCondition.Any(params AspectCondition[] conditions)`, which validates that at least one non-null child condition is supplied before wrapping the node in an `AspectCondition`.

Evaluation runs every child condition against the supplied `AspectMatchContext`. The returned `AspectConditionResult` matches when at least one child result matches. Its `Dependencies` collection contains the dependencies reported by all child results, its `DiagnosticText` is `any`, and its `Children` collection contains the individual child results.

The node does not short-circuit after the first matching child. This preserves dependency and diagnostic information for every child condition in the compound expression.

Specificity starts with one compound specificity point and then adds the specificity of each child condition.

## Constructors

| Name | Description |
| --- | --- |
| `AnyAspectCondition(IReadOnlyList<AspectConditionNode> children)` | Initializes the node with the child condition nodes to evaluate. Throws `ArgumentNullException` when `children` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets one compound specificity point plus the summed specificity of all child conditions. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates all child conditions and returns a compound result that matches when any child result matches. |

## Applies to

Cerneala UI aspect condition internals used by `AspectCondition.Any` and the aspect resolution pipeline.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectConditionNode`
- `Cerneala.UI.Aspect.AspectConditionResult`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectSpecificity`
