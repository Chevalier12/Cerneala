# ComponentTemplateDefinition Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ComponentTemplateDefinition.cs`

Describes a named component template contribution for an owner control type inside the aspect package catalog.

```csharp
public sealed class ComponentTemplateDefinition
```

Inheritance:
`object` -> `ComponentTemplateDefinition`

## Examples

Register a component template definition in an aspect package:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ComponentTemplateDefinition buttonTemplate = new(
    "App.Button.Modern",
    typeof(Button),
    ButtonTemplates.Modern);

AspectPackage package = AspectPackage.Create("App")
    .Components(components => components.AddTemplate(buttonTemplate));
```

Create a definition that reserves a named contribution without attaching a template object yet:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ComponentTemplateDefinition definition = new(
    "button.modern",
    typeof(Button),
    template: null);
```

## Remarks

`ComponentTemplateDefinition` is a small metadata container used by aspect packages. It records the non-empty `Name` of the contribution, the non-null `OwnerType` that the template targets, and an optional `Template` payload.

`AspectPackageBuilder.Components` exposes `ComponentAspectBuilder.AddTemplate(ComponentTemplateDefinition)`, which stores these definitions in the built `AspectPackage`. When an `AspectRegistry` builds an `AspectCatalog`, the catalog appends component template definitions from registered packages in package registration order.

The constructor validates only the definition name and owner type. It does not require `OwnerType` to derive from `Control`, and it accepts a `null` template payload. Use `ComponentTemplate` or `ComponentTemplate<TControl>` for executable component templates that create `ComponentTemplateInstance` objects.

## Constructors

| Name | Description |
| --- | --- |
| `ComponentTemplateDefinition(string name, Type ownerType, object? template)` | Initializes a component template definition with a non-empty name, a non-null owner type, and an optional template payload. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the non-empty component template definition name. |
| `OwnerType` | `Type` | Gets the control owner type associated with the template contribution. |
| `Template` | `object?` | Gets the optional template payload stored by the definition. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ComponentTemplateDefinition(string name, Type ownerType, object? template)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `ComponentTemplateDefinition(string name, Type ownerType, object? template)` | `ArgumentNullException` | `ownerType` is `null`. |

## Applies to

Cerneala UI aspect packages and component template registration.

## See also

- `Cerneala.UI.Aspect.AspectPackageBuilder`
- `Cerneala.UI.Aspect.ComponentAspectBuilder`
- `Cerneala.UI.Aspect.AspectCatalog`
- `Cerneala.UI.Controls.Templates.ComponentTemplate`
- `Cerneala.UI.Controls.Templates.ComponentTemplate<TControl>`
