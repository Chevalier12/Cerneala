# ComponentTemplateContext<TControl> Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ComponentTemplateContext.cs`

Provides a strongly typed component-template build context whose `Owner` property exposes the concrete control type used by the template factory.

```csharp
public sealed class ComponentTemplateContext<TControl> : ComponentTemplateContext
    where TControl : Control
```

Inheritance:
`object` -> `ComponentTemplateContext` -> `ComponentTemplateContext<TControl>`

Type parameters:

| Name | Constraints | Description |
| --- | --- | --- |
| `TControl` | `Control` | The concrete owner control type exposed by the strongly typed `Owner` property. |

## Examples

Create a button component template and read the owner as `Button` without casting:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new()
{
    Content = "Save"
};

button.ComponentTemplate = new ComponentTemplate<Button>("Button.Content", context =>
{
    ContentPresenter presenter = new()
    {
        ResourceProvider = context.Owner.ResourceProvider,
        FontResourceId = context.Owner.FontResourceId
    };

    context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);
    context.RequirePart("PART_Content", presenter);

    return presenter;
});

button.ApplyTemplate();
```

Register component slots and token bindings while building a template root:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Layout;

AspectToken<Thickness> paddingToken = AspectToken.Thickness("button.padding");

ComponentTemplate<Button> template = new("Button.Modern", context =>
{
    ContentPresenter presenter = new();
    Border border = new() { Child = presenter };

    context.RegisterSlot(ButtonSlots.Root, border);
    context.RegisterSlot(ButtonSlots.Content, presenter);
    context.BindToken(paddingToken, border, Control.PaddingProperty);
    context.Bind(Control.ForegroundProperty, presenter, Control.ForegroundProperty, UiPropertyValueSource.TemplateBinding);

    return border;
});
```

## Remarks

`ComponentTemplateContext<TControl>` is the typed counterpart of `ComponentTemplateContext`. It keeps the base context's environment, state set, variant set, slot map, part map, template bindings, token bindings, and registered lifetimes, but hides the base `Owner` property with a strongly typed `TControl` owner.

`ComponentTemplate<TControl>` supplies this context to its factory. If the public creation path receives a non-generic `ComponentTemplateContext`, the template creates a typed context from the same owner, `Environment`, `States`, and `Variants` before invoking the factory.

The context records template work; it does not attach anything by itself. `ComponentTemplate<TControl>` copies the recorded `Bindings`, `TokenBindings`, `Slots`, and `Parts` into a `ComponentTemplateInstance`. The instance attaches the root element and recorded bindings when `ComponentTemplateInstance.Attach(Control)` is called by `Control.ApplyTemplate()`.

Use the typed context when template construction needs members on a specific control type. Use inherited helpers to register aspect slots, required named parts, owner-to-child property bindings, and aspect token bindings while keeping the factory focused on creating the component tree.

## Constructors

| Name | Description |
| --- | --- |
| `ComponentTemplateContext(TControl owner, AspectEnvironment environment, AspectStateSet? states = null, AspectVariantSet? variants = null)` | Initializes a typed component-template context for `owner`, `environment`, and optional aspect state and variant sets. |

## Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Owner` | `TControl` | `ComponentTemplateContext<TControl>` | Gets the owner control using the specific component template owner type. |
| `Owner` | `Control` | `ComponentTemplateContext` | Gets the owner control as the base `Control` type. Hidden by the generic `Owner` property. |
| `Environment` | `AspectEnvironment` | `ComponentTemplateContext` | Gets the aspect environment used to resolve token values for the template. |
| `States` | `AspectStateSet` | `ComponentTemplateContext` | Gets the aspect states captured for the owner when the context was created, or `AspectStateSet.Empty` when none were supplied. |
| `Variants` | `AspectVariantSet` | `ComponentTemplateContext` | Gets the aspect variants captured for the owner when the context was created, or `AspectVariantSet.Empty` when none were supplied. |
| `Bindings` | `IReadOnlyList<TemplateBinding>` | `ComponentTemplateContext` | Gets the template bindings recorded through `Bind`. |
| `TokenBindings` | `IReadOnlyList<TemplateTokenBinding>` | `ComponentTemplateContext` | Gets the aspect token bindings recorded through `BindToken`. |
| `Slots` | `TemplateSlotMap` | `ComponentTemplateContext` | Gets the slot map populated through `RegisterSlot`. |
| `Parts` | `TemplatePartMap` | `ComponentTemplateContext` | Gets the named part map populated through `RequirePart`. |

## Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `RegisterSlot(AspectSlot slot, UIElement element)` | `void` | `ComponentTemplateContext` | Registers a generated element for an aspect slot. |
| `RequirePart<TElement>(string name, TElement? element)` | `TElement` | `ComponentTemplateContext` | Registers a required named part and returns it, or throws when `element` is `null`. |
| `Bind(UiProperty sourceProperty, UIElement target, UiProperty targetProperty, UiPropertyValueSource targetSource)` | `void` | `ComponentTemplateContext` | Records a non-generic owner property binding to a generated target element property. |
| `Bind<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty, UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)` | `void` | `ComponentTemplateContext` | Records a typed owner property binding to a generated target element property. |
| `BindToken<T>(AspectToken<T> token, UIElement target, UiProperty<T> targetProperty)` | `void` | `ComponentTemplateContext` | Records an aspect token binding from the context environment to a generated target element property. |
| `RegisterLifetime(IDisposable lifetime)` | `void` | `ComponentTemplateContext` | Transfers a disposable subscription or controller to the resulting component template instance. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ComponentTemplateContext(TControl owner, AspectEnvironment environment, AspectStateSet? states = null, AspectVariantSet? variants = null)` | `ArgumentNullException` | `owner` or `environment` is `null`. |
| `RequirePart<TElement>(string name, TElement? element)` | `InvalidOperationException` | `element` is `null`. The exception message includes the missing part name. |

## Applies To

Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware templating.

## See Also

- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Buttons/ButtonTemplates.cs`
- `UI/Controls/Control.cs`
