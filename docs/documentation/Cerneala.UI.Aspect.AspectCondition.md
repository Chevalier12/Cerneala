# AspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectCondition.cs`

Represents a reusable predicate used by aspect targets to decide whether an aspect declaration applies to an element, state, variant, property, or data context.

```csharp
public sealed class AspectCondition
```

Inheritance:
`object` -> `AspectCondition`

## Examples

Match a button only while it is in the hover state:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.State(AspectState.Hover);
AspectMatchContext context = new(
    new Button(),
    states: AspectStateSet.Empty.Add(AspectState.Hover));

bool applies = condition.Evaluate(context).Matches;
```

Combine state, variant, property, and data-context checks:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

AspectVariantKey<Button, string> kind = AspectVariantKey.For<Button, string>("kind");

AspectCondition condition = AspectCondition.All(
    AspectCondition.State(AspectState.Hover),
    AspectCondition.Variant(kind, "primary"),
    AspectCondition.Property(ButtonBase.IsPressedProperty).Is(false),
    AspectCondition.Data<UserCard>(
        "important user",
        user => user.IsImportant,
        AspectDataDependency.Property<UserCard, bool>(nameof(UserCard.IsImportant))));
```

## Remarks

`AspectCondition` is the public factory and evaluation wrapper around internal aspect-condition nodes. Aspect targets use conditions to add specificity and to evaluate whether a rule matches the current `AspectMatchContext`.

Conditions report an `AspectConditionResult`, which includes the match result, diagnostic text, child results for compound conditions, and dependencies used by aspect invalidation. Built-in condition kinds report dependencies for states, variants, UI properties, data context entries, and custom predicates.

`State` matches when the context's `AspectStateSet` contains the requested state. `Variant` matches when the context's `AspectVariantSet` has the requested key and the stored value equals the expected value. `Property` creates an `AspectPropertyConditionBuilder<TValue>` so callers can match a `UiProperty<TValue>` by equality or by a custom predicate. `Data` matches only when `AspectMatchContext.Data` has the requested data type and the supplied predicate succeeds.

`All`, `Any`, and `Not` compose other conditions. `All` requires every child to match, `Any` requires at least one child to match, and `Not` inverts its single child result. Compound conditions evaluate their children and carry the child dependencies forward.

Factory methods validate their required inputs through the underlying condition nodes. Empty diagnostic names are rejected for data, property predicate, and predicate conditions. Data conditions must declare at least one `AspectDataDependency`. `All` and `Any` require at least one non-null child condition.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the specificity contributed by the condition. Built-in single conditions contribute to their matching specificity category; compound conditions add their children and a compound specificity point. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `All(params AspectCondition[] conditions)` | `AspectCondition` | Creates a compound condition that matches only when every child condition matches. |
| `Any(params AspectCondition[] conditions)` | `AspectCondition` | Creates a compound condition that matches when at least one child condition matches. |
| `Data<TData>(string diagnosticName, Func<TData, bool> predicate, params AspectDataDependency[] dependencies)` | `AspectCondition` | Creates a typed data-context condition that evaluates `predicate` against `AspectMatchContext.Data` when it is `TData`. |
| `Data<TData, TValue>(string diagnosticName, Func<TData, TValue> selector, Func<TValue, bool> predicate, params AspectDataDependency[] dependencies)` | `AspectCondition` | Creates a typed data-context condition that selects a value from `TData` and evaluates `predicate` against the selected value. |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates the condition against a match context and returns match, diagnostic, dependency, and child-result information. |
| `Not(AspectCondition condition)` | `AspectCondition` | Creates a compound condition that inverts a child condition. |
| `Predicate(string diagnosticName, Func<AspectMatchContext, bool> predicate)` | `AspectCondition` | Creates a custom condition that evaluates the full match context and records a predicate dependency by diagnostic name. |
| `Property<TValue>(UiProperty<TValue> property)` | `AspectPropertyConditionBuilder<TValue>` | Starts a property condition for the supplied UI property. |
| `State(AspectState state)` | `AspectCondition` | Creates a condition that matches when the context contains the supplied aspect state. |
| `Variant<TControl, TValue>(AspectVariantKey<TControl, TValue> key, TValue value)` | `AspectCondition` | Creates a condition that matches when the context contains the supplied variant key with an equal value. |

## Applies to

Cerneala UI aspect resolution and component-template styling.

## See also

- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Aspect.AspectConditionResult`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectPropertyConditionBuilder<TValue>`
