# AspectPropertyConditionBuilder<TValue> Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectCondition.cs`

Builds `AspectCondition` instances that match against the current value of a typed `UiProperty<TValue>`.

```csharp
public sealed class AspectPropertyConditionBuilder<TValue>
```

Inheritance:
`object` -> `AspectPropertyConditionBuilder<TValue>`

## Examples

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;

AspectCondition pressedCondition =
    AspectCondition.Property(ButtonBase.IsPressedProperty).Is(true);

AspectRuleSet pressedRule = new(
    "pressed",
    AspectLayer.App,
    new AspectTarget(typeof(Button), conditions: [pressedCondition]),
    declarations: [],
    declarationOrder: 0);
```

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition visibleTextCondition =
    AspectCondition.Property(TextBlock.TextProperty)
        .Matches(text => !string.IsNullOrWhiteSpace(text), "text is visible");
```

## Remarks

`AspectPropertyConditionBuilder<TValue>` is returned by `AspectCondition.Property<TValue>(UiProperty<TValue>)`. The builder keeps the target property and creates property-backed aspect conditions.

`Is` compares the element's current property value with the expected value by using `property.Metadata.EqualityComparer`. `Matches` stores the supplied predicate and diagnostic name directly.

The resulting `AspectCondition` evaluates against `AspectMatchContext.Element.GetValue(property)`. It reports one `AspectConditionDependency` with `AspectConditionDependencyKind.UiProperty`, allowing the aspect engine to reapply rules when that property affects the match result.

Property conditions contribute one property point to aspect specificity through their internal condition node.

## Methods

| Name | Description |
| --- | --- |
| `Is(TValue)` | Creates an `AspectCondition` that matches when the target UI property equals the supplied value according to the property's metadata equality comparer. |
| `Matches(Func<TValue, bool>, string)` | Creates an `AspectCondition` that matches when the supplied predicate returns `true` for the target UI property's current value. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Matches(Func<TValue, bool>, string)` | `ArgumentNullException` | `predicate` is `null`. |
| `Matches(Func<TValue, bool>, string)` | `ArgumentException` | `diagnosticName` is `null`, empty, or whitespace. |

## Applies to

Cerneala retained UI aspect condition system.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectConditionResult`
- `Cerneala.UI.Aspect.AspectConditionDependency`
- `Cerneala.UI.Core.UiProperty<T>`
