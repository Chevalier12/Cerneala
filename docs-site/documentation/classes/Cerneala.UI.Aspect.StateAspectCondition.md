# StateAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal aspect condition node that matches when an `AspectMatchContext` contains a required `AspectState`.

```csharp
internal sealed class StateAspectCondition : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `StateAspectCondition`

## Examples

Create and evaluate a state condition through the public `AspectCondition` factory:

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
    // The wrapped StateAspectCondition found AspectState.Hover in context.States.
}
```

## Remarks

`StateAspectCondition` is created by `AspectCondition.State(AspectState)`. The type is internal, so callers normally work with it through the public `AspectCondition` wrapper.

The constructor requires a non-null `AspectState`. Evaluation checks `context.States.Contains(State)` and returns an `AspectConditionResult` with one `AspectConditionDependency` whose kind is `State` and whose `State` value is the required state.

The result diagnostic text is `state {State.Name} matched` when the state is present, or `state {State.Name} missing` when it is absent. The condition contributes `new AspectSpecificity(State: 1)` to rule specificity.

## Constructors

| Name | Description |
| --- | --- |
| `StateAspectCondition(AspectState state)` | Creates a state condition for `state`. Throws `ArgumentNullException` when `state` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `AspectState` | Gets the required state that must be present in the match context. |
| `Specificity` | `AspectSpecificity` | Gets a specificity value with the `State` component set to `1`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Checks whether `context.States` contains `State`, then returns the match result, state dependency, and diagnostic text. |

## Applies to

Cerneala UI aspect condition internals used by `AspectCondition.State(AspectState)` and aspect rule matching.

## See also

- `AspectCondition`
- `AspectConditionNode`
- `AspectState`
- `AspectStateSet`
- `AspectMatchContext`
- `AspectConditionResult`
- `AspectConditionDependency`
