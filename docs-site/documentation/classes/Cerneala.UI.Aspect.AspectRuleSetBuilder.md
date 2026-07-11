# AspectRuleSetBuilder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectRuleSetBuilder.cs`

Collects aspect declarations through a fluent API and materializes them as an `AspectRuleSet`.

```csharp
public sealed class AspectRuleSetBuilder
```

Inheritance:
`object` -> `AspectRuleSetBuilder`

## Examples

Create a rule set for `Button` controls and add a literal property declaration:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectRuleSet rule = new AspectRuleSetBuilder(
        "button.background",
        AspectLayer.App,
        new AspectTarget(typeof(Button)),
        declarationOrder: 0)
    .Set(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))
    .Build();
```

Supply a diagnostic name for the generated declaration:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectRuleSet rule = new AspectRuleSetBuilder(
        "button.foreground",
        AspectLayer.App,
        new AspectTarget(typeof(Button)),
        declarationOrder: 1)
    .Set(
        Control.ForegroundProperty,
        AspectValue<Color>.Literal(Color.Black),
        diagnosticName: "Button foreground")
    .Build();
```

## Remarks

`AspectRuleSetBuilder` stores the rule set metadata supplied to its constructor and accumulates declarations in call order. Each `Set<T>` call creates an `AspectDeclaration` for a `UiProperty<T>` and an `AspectValue<T>`, optionally carrying the supplied diagnostic name.

`Set<T>` returns the same builder instance, so multiple property declarations can be chained before `Build()` is called. `Build()` copies the current declaration list with `ToArray()` and passes it to the `AspectRuleSet` constructor together with `Name`, `Layer`, `Target`, and `DeclarationOrder`.

The builder constructor does not validate its arguments directly. Validation happens through the objects it creates or receives: `Set<T>` uses `AspectDeclaration`, which rejects `null` properties, `null` values, and property/value type mismatches; `Build()` uses `AspectRuleSet`, which validates the rule name, layer, target, and declarations collection.

## Constructors

| Name | Description |
| --- | --- |
| `AspectRuleSetBuilder(string name, AspectLayer layer, AspectTarget target, int declarationOrder)` | Initializes a builder with the metadata that will be assigned to the built `AspectRuleSet`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the rule set name that will be passed to `AspectRuleSet`. |
| `Layer` | `AspectLayer` | Gets the cascade layer that will be assigned to the built rule set. |
| `Target` | `AspectTarget` | Gets the aspect target that will decide whether the built rule set matches a context. |
| `DeclarationOrder` | `int` | Gets the cascade declaration order that will be assigned to the built rule set. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Set<T>(UiProperty<T> property, AspectValue<T> value, string? diagnosticName = null)` | `AspectRuleSetBuilder` | Adds a declaration for `property` with the supplied aspect value and optional diagnostic name, then returns this builder. |
| `Build()` | `AspectRuleSet` | Creates an `AspectRuleSet` from the builder metadata and the declarations collected so far. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Set<T>(UiProperty<T>, AspectValue<T>, string?)` | `ArgumentNullException` | `property` or `value` is `null`. |
| `Build()` | `ArgumentException` | `Name` is `null`, empty, or whitespace when the `AspectRuleSet` is created. |
| `Build()` | `ArgumentNullException` | `Layer` or `Target` is `null` when the `AspectRuleSet` is created. |

## Applies to

Cerneala UI aspect rule construction and fluent aspect package setup.

## See also

- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectDeclaration`
- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Aspect.ComponentAspectBuilder`
