# AspectDeclaration Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectDeclaration.cs`

Represents one property assignment contributed by an aspect rule.

```csharp
public sealed class AspectDeclaration
```

Inheritance:
`object` -> `AspectDeclaration`

## Examples

Create a literal declaration for a control property:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)),
    diagnosticName: "button background");
```

Use a declaration in a rule set:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectRuleSet rule = new(
    "button surface",
    AspectLayer.App,
    new AspectTarget(typeof(Button)),
    [
        new AspectDeclaration(
            Control.BackgroundProperty,
            AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))
    ],
    declarationOrder: 0);
```

## Remarks

`AspectDeclaration` stores the `UiProperty` that should receive an aspect value and the `AspectValue` used to resolve that value. `AspectEngine.Resolve` evaluates declarations from matching `AspectRuleSet` instances, resolves each declaration value, and keeps the winning declaration for each property according to layer order, target specificity, and declaration order.

The constructor requires non-null `Property` and `Value` arguments. It also verifies that `property.ValueType` matches `value.ValueType`; mismatched property/value pairs throw `ArgumentException` before the declaration can enter a rule set.

`Value` can be a literal, token-backed, or computed `AspectValue`. Token dependencies reported by `Value.Dependencies` are collected by `AspectEngine.Resolve` for later invalidation tracking.

`Motion` is optional metadata carried into the resulting `ResolvedAspectValue`. `DiagnosticName` is optional diagnostic metadata; when a declaration is rejected during cascade resolution, diagnostics use `DiagnosticName` when present and fall back to the property name.

## Constructors

| Name | Description |
| --- | --- |
| `AspectDeclaration(UiProperty property, AspectValue value, AspectMotion? motion = null, string? diagnosticName = null)` | Initializes a declaration for `property` using `value`, with optional motion and diagnostic metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Property` | `UiProperty` | Gets the UI property assigned by the declaration. |
| `Value` | `AspectValue` | Gets the aspect value resolved when the declaration participates in aspect resolution. |
| `Motion` | `AspectMotion?` | Gets optional motion metadata associated with the resolved value. |
| `DiagnosticName` | `string?` | Gets optional diagnostic text used to identify the declaration in aspect diagnostics. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AspectDeclaration(UiProperty, AspectValue, AspectMotion?, string?)` | `ArgumentNullException` | `property` or `value` is `null`. |
| `AspectDeclaration(UiProperty, AspectValue, AspectMotion?, string?)` | `ArgumentException` | `value.ValueType` does not match `property.ValueType`. |

## Applies to

Cerneala UI aspect rule declarations and aspect engine resolution.

## See also

- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectValue`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.ResolvedAspectValue`
- `Cerneala.UI.Core.UiProperty`
