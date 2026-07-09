# PropertyAspectCondition<TValue> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectConditionNode.cs`

Evaluates an aspect condition by reading a typed UI property from the matched element and applying a predicate to that value.

```csharp
internal sealed class PropertyAspectCondition<TValue> : AspectConditionNode
```

Inheritance:
`object` -> `AspectConditionNode` -> `PropertyAspectCondition<TValue>`

## Examples

Property conditions are normally created through the public `AspectCondition.Property<TValue>(UiProperty<TValue>)` builder:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

Button button = new() { IsPressed = true };

AspectCondition condition =
    AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true);

AspectConditionResult result = condition.Evaluate(new AspectMatchContext(button));

if (result.Matches)
{
    // The internal PropertyAspectCondition<bool> matched IsPressed == true.
}
```

Use `Matches` when the rule needs a custom predicate and diagnostic label:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition hasText =
    AspectCondition.Property(TextBlock.TextProperty)
        .Matches(text => !string.IsNullOrWhiteSpace(text), "text is visible");
```

## Remarks

`PropertyAspectCondition<TValue>` is the internal condition node behind `AspectPropertyConditionBuilder<TValue>.Is` and `AspectPropertyConditionBuilder<TValue>.Matches`. During evaluation it calls `context.Element.GetValue(property)`, passes the current value to the stored predicate, and returns an `AspectConditionResult`.

The result always reports one `AspectConditionDependency` with `AspectConditionDependencyKind.UiProperty` and the tracked `UiProperty<TValue>`. This lets the aspect invalidation system know that the condition depends on the element's current UI property value.

`Is` creates this node with a predicate that compares the current value against the expected value using `property.Metadata.EqualityComparer`. `Matches` passes the supplied predicate and diagnostic name through directly.

The node contributes one property point to aspect specificity by returning `new AspectSpecificity(Property: 1)`.

## Constructors

| Name | Description |
| --- | --- |
| `PropertyAspectCondition(UiProperty<TValue>, Func<TValue, bool>, string)` | Initializes a property condition for the specified UI property, predicate, and diagnostic name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Specificity` | `AspectSpecificity` | Gets the condition specificity contribution, with `Property` set to `1`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Evaluate(AspectMatchContext)` | `AspectConditionResult` | Reads the tracked property from `context.Element`, applies the predicate, and returns match state, a UI property dependency, and diagnostic text. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PropertyAspectCondition(UiProperty<TValue>, Func<TValue, bool>, string)` | `ArgumentNullException` | `property` or `predicate` is `null`. |
| `PropertyAspectCondition(UiProperty<TValue>, Func<TValue, bool>, string)` | `ArgumentException` | `diagnosticName` is `null`, empty, or whitespace. |

## Applies to

Cerneala retained UI aspect condition internals used by `AspectCondition.Property<TValue>(UiProperty<TValue>)`.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectPropertyConditionBuilder<TValue>`
- `Cerneala.UI.Aspect.AspectConditionNode`
- `Cerneala.UI.Aspect.AspectConditionDependency`
- `Cerneala.UI.Core.UiProperty<T>`
