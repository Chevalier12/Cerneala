# ComponentTemplate Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ComponentTemplate.cs`

Defines the non-generic base contract for modern component templates that create a `ComponentTemplateInstance` for a compatible `Control` owner.

```csharp
public abstract class ComponentTemplate
```

Inheritance:
`object` -> `ComponentTemplate`

Derived:
`ComponentTemplate<TControl>`, `ControlTemplateAdapter`

## Examples
Create a typed component template and apply it to a button:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new()
{
    Content = "Save"
};

button.ComponentTemplate = new ComponentTemplate<Button>("Button.Simple", context =>
{
    ContentPresenter presenter = new();
    Border border = new() { Child = presenter };

    context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
    context.RequirePart("PART_Content", presenter);

    return border;
});

button.ApplyTemplate();

ComponentTemplateInstance instance = button.ComponentTemplateInstance!;
```

Create an instance directly when the owner and context are known:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new();
ComponentTemplate template = new ComponentTemplate<Button>("Button.Root", _ => new Border());
ComponentTemplateContext context = new(button, new AspectEnvironment("template"));

ComponentTemplateInstance instance = template.CreateInstance(button, context);
```

## Remarks
`ComponentTemplate` stores the accepted owner type in `OwnerType` and a diagnostic template name in `Name`. Its public creation entry point is `CreateInstance(Control, ComponentTemplateContext)`, which rejects `null` arguments and verifies that the supplied owner is an instance of `OwnerType` before delegating to the derived implementation.

The usual public implementation is `ComponentTemplate<TControl>`. It sets `OwnerType` to `typeof(TControl)`, invokes a `Func<ComponentTemplateContext<TControl>, UIElement?>` factory, and returns a `ComponentTemplateInstance` containing the generated root plus the bindings, token bindings, slots, and required parts collected by the context.

`Control.ApplyTemplate()` prefers `Control.ComponentTemplate` over the classic `Control.Template`. Applying a component template detaches any existing classic template instance, creates a `ComponentTemplateContext` from the control's current aspect state, variants, and theme environment, attaches the resulting component instance, and stores it in `Control.ComponentTemplateInstance`. Reapplying the same template instance is stable and does not recreate the generated root.

`ControlTemplateAdapter` is a bridge from a classic `ControlTemplate` into the component template pipeline. It preserves the legacy root and bindings but creates empty component slot and part maps.

The base constructor and `CreateInstanceCore` override point are `private protected`, so external consumers normally use `ComponentTemplate<TControl>` or adapter types provided by the `Cerneala` assembly instead of deriving from `ComponentTemplate` outside the assembly.

## Constructors
This class does not expose public constructors.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `OwnerType` | `Type` | Gets the `Control` type that this template can be applied to. |
| `Name` | `string` | Gets the non-empty template name supplied by the derived template. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `ComponentTemplateInstance` | Creates a component template instance for a compatible owner and build context. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `ComponentTemplate(Type ownerType, string name)` | `ArgumentException` | `ownerType` does not derive from `Control`, or `name` is `null`, empty, or whitespace. |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `ArgumentNullException` | `owner` or `context` is `null`. |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `InvalidOperationException` | `owner` is not an instance of `OwnerType`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, modern component templates, aspect-aware templating.

## See Also
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Templates/ControlTemplateAdapter.cs`
- `UI/Controls/Control.cs`
- `UI/Controls/Buttons/ButtonTemplates.cs`
