# ControlTemplateAdapter Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ControlTemplateAdapter.cs`

Adapts a classic `ControlTemplate` so it can be used through the modern `ComponentTemplate` creation pipeline.

```csharp
public sealed class ControlTemplateAdapter : ComponentTemplate
```

Inheritance:
`object` -> `ComponentTemplate` -> `ControlTemplateAdapter`

## Examples
Wrap a classic button template and create a component template instance from it:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new();
ControlTemplate legacyTemplate = new ControlTemplate<Button>(_ => new Border());
ComponentTemplate adaptedTemplate = new ControlTemplateAdapter(legacyTemplate);

ComponentTemplateContext context = new(button, new AspectEnvironment("template"));
ComponentTemplateInstance instance = adaptedTemplate.CreateInstance(button, context);

instance.Attach(button);
instance.Detach();
```

## Remarks
`ControlTemplateAdapter` is a compatibility bridge between the classic `ControlTemplate` API and the aspect-aware `ComponentTemplate` API. The adapter uses the wrapped template's `OwnerType` as its own `OwnerType` and gives the component template the diagnostic name `legacy.{OwnerType.Name}`.

When `CreateInstance(Control, ComponentTemplateContext)` is called, the inherited `ComponentTemplate` validation checks that the owner and context are not `null` and that the owner is compatible with `OwnerType`. The adapter then creates the classic `TemplateInstance` by calling the wrapped `ControlTemplate.CreateInstance(Control)`.

The resulting `ComponentTemplateInstance` preserves the classic instance's `Root` and `Bindings`. Component-specific token bindings, slots, and required parts are created as empty collections because classic control templates do not record those component-template features.

The supplied `ComponentTemplateContext` is required by the `ComponentTemplate` contract and participates in validation, but the adapter does not read values from it while creating the legacy instance.

An architecture test keeps production code from taking new dependencies on `ControlTemplateAdapter` outside its own file after the modern template migration.

## Constructors
| Name | Description |
| --- | --- |
| `ControlTemplateAdapter(ControlTemplate template)` | Initializes an adapter for `template`, using the wrapped template's owner type and a `legacy.{OwnerType.Name}` component template name. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `OwnerType` | `Type` | Gets the `Control` type accepted by the wrapped classic template. Inherited from `ComponentTemplate`. |
| `Name` | `string` | Gets the adapter's diagnostic component template name, formatted as `legacy.{OwnerType.Name}`. Inherited from `ComponentTemplate`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `ComponentTemplateInstance` | Creates a component template instance from the wrapped classic template after validating the owner and context. Inherited from `ComponentTemplate`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `ControlTemplateAdapter(ControlTemplate template)` | `ArgumentNullException` | `template` is `null`. |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `ArgumentNullException` | `owner` or `context` is `null`. |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `InvalidOperationException` | `owner` is not an instance of `OwnerType`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, classic control templates, modern component template compatibility.

## See Also
- `UI/Controls/Templates/ControlTemplateAdapter.cs`
- `UI/Controls/ControlTemplate.cs`
- `UI/Controls/ControlTemplate{TControl}.cs`
- `UI/Controls/TemplateInstance.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
