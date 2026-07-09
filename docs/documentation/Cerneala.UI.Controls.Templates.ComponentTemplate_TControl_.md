# ComponentTemplate<TControl> Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ComponentTemplate.cs`

Represents a strongly typed component template that builds a `ComponentTemplateInstance` for a specific `Control` subtype.

```csharp
public sealed class ComponentTemplate<TControl> : ComponentTemplate
    where TControl : Control
```

Inheritance:
`object` -> `ComponentTemplate` -> `ComponentTemplate<TControl>`

Type parameters:

| Name | Constraints | Description |
| --- | --- | --- |
| `TControl` | `Control` | The owner control type accepted by the template and exposed through `ComponentTemplateContext<TControl>.Owner`. |

## Examples

Create a typed button component template and apply it through `Control.ComponentTemplate`:

```csharp
using Cerneala.UI.Core;
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

    context.RequirePart("PART_Content", presenter);
    context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
    context.Bind(Control.PaddingProperty, border, Control.PaddingProperty, UiPropertyValueSource.Local);

    return border;
});

button.ApplyTemplate();

ComponentTemplateInstance instance = button.ComponentTemplateInstance!;
```

Use the typed context to register slots and access the owner without casting:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ComponentTemplate<Button> template = new("Button.Modern", context =>
{
    ContentPresenter presenter = new();
    Border border = new() { Child = presenter };

    presenter.ResourceProvider = context.Owner.ResourceProvider;
    context.RegisterSlot(ButtonSlots.Root, border);
    context.RegisterSlot(ButtonSlots.Content, presenter);

    return border;
});
```

## Remarks

`ComponentTemplate<TControl>` is the public typed implementation of `ComponentTemplate`. Its constructor sets the inherited `OwnerType` to `typeof(TControl)`, stores a non-empty template name through the base class, and requires a non-null factory delegate.

The factory receives a `ComponentTemplateContext<TControl>`. When `CreateInstance(Control, ComponentTemplateContext)` is called on the base class, the owner is first validated against `OwnerType`. The typed implementation reuses an already typed context when possible; otherwise it creates a new `ComponentTemplateContext<TControl>` from the supplied owner, aspect environment, states, and variants.

The factory may return `null`. The created `ComponentTemplateInstance` still carries the context's recorded template bindings, token bindings, slot map, and part map. Those bindings are attached later when the instance is attached to the owner.

When assigned to `Control.ComponentTemplate`, the template is applied by `Control.ApplyTemplate()`. Component templates take precedence over the classic `Control.Template` property. Reapplying the same component template keeps the existing generated root; replacing it detaches the previous `ComponentTemplateInstance` before attaching the new one.

## Constructors

| Name | Description |
| --- | --- |
| `ComponentTemplate(string name, Func<ComponentTemplateContext<TControl>, UIElement?> factory)` | Initializes a component template for `TControl` with a non-empty diagnostic name and the factory used to create each `ComponentTemplateInstance`. Throws `ArgumentException` for an empty or whitespace name and `ArgumentNullException` when `factory` is `null`. |

## Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `OwnerType` | `Type` | `ComponentTemplate` | Gets the control type accepted by this template. For `ComponentTemplate<TControl>`, this is `typeof(TControl)`. |
| `Name` | `string` | `ComponentTemplate` | Gets the non-empty template name supplied to the constructor. |

## Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `CreateInstance(Control owner, ComponentTemplateContext context)` | `ComponentTemplateInstance` | `ComponentTemplate` | Creates a component template instance for a compatible owner and build context. Throws `ArgumentNullException` for `null` arguments and `InvalidOperationException` when the owner is not an instance of `OwnerType`. |

## Applies to

Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware templating.

## See also

- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Control.cs`
- `UI/Controls/ButtonTemplates.cs`
