# ComponentAspectBuilder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectPackageBuilder.cs`

Collects component aspect rule sets and component template definitions while configuring an `AspectPackageBuilder`.

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

The builder appends the supplied `AspectRuleSet` and `ComponentTemplateDefinition` instances to the package currently being configured. It does not clone or validate the rule or template beyond rejecting `null`; construction-time validation belongs to the supplied objects themselves.

Both public methods return the same builder instance, which allows chained calls inside the callback. When `AspectPackageBuilder.Build()` runs, the accumulated rules and component templates are copied into the resulting `AspectPackage`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `AddRule(AspectRuleSet rule)` | `ComponentAspectBuilder` | Adds a non-null aspect rule set to the package's component rules and returns this builder. |
| `AddTemplate(ComponentTemplateDefinition template)` | `ComponentAspectBuilder` | Adds a non-null component template definition to the package's component templates and returns this builder. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AddRule(AspectRuleSet rule)` | `ArgumentNullException` | `rule` is `null`. |
| `AddTemplate(ComponentTemplateDefinition template)` | `ArgumentNullException` | `template` is `null`. |

## Applies to

Cerneala UI aspect package construction, component aspect rule registration, and component template contribution.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectRuleSet`
- `Cerneala.UI.Controls.Templates.ComponentTemplateDefinition`
