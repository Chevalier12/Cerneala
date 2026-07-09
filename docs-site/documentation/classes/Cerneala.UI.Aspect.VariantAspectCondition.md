# VariantAspectCondition Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Represents the internal aspect-condition node that matches an `AspectMatchContext` only when a variant key is present and its value equals the expected value.

```csharp
internal sealed class VariantAspectCondition : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `VariantAspectCondition`

## Examples

Create a public `AspectCondition` that wraps a `VariantAspectCondition` through the `AspectCondition.Variant` factory:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectVariantKey<Button, ButtonKind> key = AspectVariantKey.For<Button, ButtonKind>("kind");
AspectCondition condition = AspectCondition.Variant(key, ButtonKind.Primary);

AspectMatchContext matchingContext = new(
    new Button(),
    variants: AspectVariantSet.Empty.Set(key, ButtonKind.Primary));

bool matches = condition.Evaluate(matchingContext).Matches;

enum ButtonKind
{
    Neutral,
    Primary
}
```

## Remarks

`VariantAspectCondition` is the implementation node used by `AspectCondition.Variant<TControl, TValue>`. It is internal, so callers normally create it through the public `AspectCondition` factory rather than constructing it directly.

The constructor requires a non-null `AspectVariantKey`. The expected value may be `null`.

When evaluated, the condition reads `AspectMatchContext.Variants` with the stored key. It matches only when the variant set contains that key and `object.Equals(actual, expectedValue)` returns `true`. A missing key does not match, even when the expected value is `null`.

Successful and failed evaluations both report one `AspectConditionDependency` with `Kind` set to `AspectConditionDependencyKind.Variant` and `Variant` set to the stored key. The diagnostic text is `variant {key.Name} matched` when the value matches and `variant {key.Name} did not match` otherwise.

The condition contributes variant specificity by returning `new AspectSpecificity(Variant: 1)`.

## Constructors

| Name | Description |
| --- | --- |
| `VariantAspectCondition(AspectVariantKey key, object? expectedValue)` | Initializes a variant condition for `key` and the value it must equal. Throws `ArgumentNullException` when `key` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the specificity contribution for a variant condition: `new AspectSpecificity(Variant: 1)`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext context)` | `AspectConditionResult` | Evaluates the context's `Variants` set against the stored key and expected value, then returns match status, variant dependency, and diagnostic text. |

## Applies to

Cerneala UI aspect condition evaluation and component-template styling.

## See also

- [AspectCondition](Cerneala.UI.Aspect.AspectCondition.md)
- `AspectConditionResult`
- `AspectMatchContext`
- `AspectVariantKey`
- `AspectVariantSet`
