# ComponentAspectBuilder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/ComponentAspectBuilder.cs`

Collects component aspect rule sets, non-style behaviors, and component template definitions while configuring an `AspectPackageBuilder`.

```csharp
public sealed class ComponentAspectBuilder
```

Inheritance:
`object` -> `ComponentAspectBuilder`

## Examples

Add a component template to a package:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ComponentTemplateDefinition componentTemplate =
    new("button.modern", typeof(Button), template: null);

AspectPackage package = AspectPackage.Create("App")
    .Components(components => components.AddTemplate(componentTemplate));
```

Add aspect rules to a package:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectRuleSet rule = new(
    "button.base",
    AspectLayer.App,
    new AspectTarget(typeof(Button)),
    [new AspectDeclaration(Control.BackgroundProperty, AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))],
    declarationOrder: 0);

AspectPackage package = AspectPackage.Create("App")
    .Components(components => components.AddRule(rule));
```

## Remarks

`ComponentAspectBuilder` is created by `AspectPackageBuilder.Components(Action<ComponentAspectBuilder>)`. Its constructor is internal, so callers normally receive it only inside the `Components` callback.

The builder appends standalone `AspectRuleSet`, `AspectBehavior`, and `ComponentTemplateDefinition` instances to the package currently being configured, rejecting `null`. Rules constructed through the public constructor or `AspectRuleSetBuilder` have already passed standalone ownership validation and are retained by reference.

Diagnostics can expose a rule projected internally from an `ElementAspect`, whose properties were validated against the actual assigned element rather than its declared target type. `AddRule` reconstructs such a rule with standalone construction-time validation before adding it to a public package. A compatible projection becomes a separate standalone rule; an incompatible property or `UIElement.AspectProperty` causes `ArgumentException` with parameter name `declarations`, naming the rule, target, property, and property owner. The original local diagnostic snapshot is unchanged. `ElementAspect` uses a separate internal add path to preserve its validated-at-assignment policy.

The public add methods return the same builder instance, which allows chained calls inside the callback. When `AspectPackageBuilder.Build()` runs, the accumulated rules and component templates are copied into the resulting `AspectPackage`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AddRule(AspectRuleSet rule)` | `ComponentAspectBuilder` | Adds a non-null aspect rule set to the package's component rules and returns this builder. |
| `AddBehavior(AspectBehavior behavior)` | `ComponentAspectBuilder` | Adds a non-null target-typed non-style behavior to the package and returns this builder. |
| `AddTemplate(ComponentTemplateDefinition template)` | `ComponentAspectBuilder` | Adds a non-null component template definition to the package's component templates and returns this builder. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AddRule(AspectRuleSet rule)` | `ArgumentNullException` | `rule` is `null`. |
| `AddRule(AspectRuleSet rule)` | `ArgumentException` | A reused local-aspect projection cannot satisfy standalone property ownership validation against its declared element type or slot child type, or declares `UIElement.AspectProperty`. |
| `AddBehavior(AspectBehavior behavior)` | `ArgumentNullException` | `behavior` is `null`. |
| `AddTemplate(ComponentTemplateDefinition template)` | `ArgumentNullException` | `template` is `null`. |

## Applies to

Cerneala UI aspect package construction, component aspect rule registration, and component template contribution.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Aspect.AspectBehavior`
- `Cerneala.UI.Controls.Templates.ComponentTemplateDefinition`
