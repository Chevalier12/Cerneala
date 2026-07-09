# ControlTemplate<TControl> Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ControlTemplate{TControl}.cs`

Represents a strongly typed classic control template that creates a retained template root for a specific `Control` subtype.

```csharp
public sealed class ControlTemplate<TControl> : ControlTemplate
    where TControl : Control
```

Inheritance:
`object` -> `ControlTemplate` -> `ControlTemplate<TControl>`

Type parameters:

| Name | Constraints | Description |
| --- | --- | --- |
| `TControl` | `Control` | The owner control type accepted by the template and exposed through `TemplateContext<TControl>.Owner`. |

## Examples

Create a typed template for a `Button` and apply it:

```csharp
using Cerneala.UI.Controls;

Button button = new()
{
    Content = "Save"
};

button.Template = new ControlTemplate<Button>(context =>
{
    return new ContentPresenter
    {
        Content = context.Owner.Content
    };
});

button.ApplyTemplate();

TemplateInstance? instance = button.TemplateInstance;
ContentPresenter? root = instance?.Root as ContentPresenter;
```

Create a template that records a binding in the template context:

```csharp
using Cerneala.UI.Controls;

Control control = new();
Control child = new();

control.Template = new ControlTemplate<Control>(context =>
{
    context.Bind(Control.FontSizeProperty, child, Control.FontSizeProperty);
    return child;
});

control.ApplyTemplate();
```

## Remarks

`ControlTemplate<TControl>` wraps a `Func<TemplateContext<TControl>, UIElement?>`. When the inherited `CreateInstance(Control)` method is called, the base `ControlTemplate` verifies that the supplied owner is compatible with `OwnerType`. The typed implementation then casts the owner to `TControl`, creates a `TemplateContext<TControl>`, invokes the factory, and returns a `TemplateInstance` containing the produced root and any bindings collected by the context.

The factory may return `null`. In that case the created `TemplateInstance` has no root, but it can still contain bindings added through the context. Passing `null` as the factory argument to the constructor throws `ArgumentNullException`.

When assigned to `Control.Template`, the template is applied by `Control.ApplyTemplate()`. The resulting `TemplateInstance` attaches its root, when present, as both a logical and visual child of the owner, and attaches each recorded `TemplateBinding`. Reapplying the same template keeps the existing template instance; replacing the template detaches the previous root before attaching the new one.

Use this generic template when the factory needs direct access to properties or members on a specific control type. The non-generic base class still provides the public `OwnerType` and `CreateInstance(Control)` surface used by the retained template pipeline.

## Constructors

| Name | Description |
| --- | --- |
| `ControlTemplate(Func<TemplateContext<TControl>, UIElement?> factory)` | Initializes a template for `TControl` and stores the factory used to create each `TemplateInstance`. Throws `ArgumentNullException` when `factory` is `null`. |

## Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `OwnerType` | `Type` | `ControlTemplate` | Gets the control type accepted by this template. For `ControlTemplate<TControl>`, this is `typeof(TControl)`. |

## Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `CreateInstance(Control owner)` | `TemplateInstance` | `ControlTemplate` | Creates a template instance for a compatible owner. Throws `ArgumentNullException` for a `null` owner and `InvalidOperationException` when the owner is not an instance of `OwnerType`. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/ControlTemplate{TControl}.cs`
- `UI/Controls/ControlTemplate.cs`
- `UI/Controls/TemplateContext.cs`
- `UI/Controls/TemplateInstance.cs`
- `UI/Controls/Control.cs`
