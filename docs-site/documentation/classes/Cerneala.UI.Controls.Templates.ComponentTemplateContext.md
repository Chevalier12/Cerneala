# ComponentTemplateContext Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ComponentTemplateContext.cs`

Collects the owner, aspect inputs, generated slot and part maps, and deferred bindings while a component template factory builds its retained root.

```csharp
public class ComponentTemplateContext
```

Inheritance:
`object` -> `ComponentTemplateContext`

Derived:
`ComponentTemplateContext<TControl>`

## Examples
Create a component template that registers slots, requires a named part, and binds owner properties to generated elements:

```csharp
using Cerneala.UI.Core;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new()
{
    Content = "Save"
};

button.ComponentTemplate = new ComponentTemplate<Button>("Button.Custom", context =>
{
    ContentPresenter presenter = new();
    Border root = new() { Child = presenter };

    context.RegisterSlot(ButtonSlots.Root, root);
    context.RegisterSlot(ButtonSlots.Content, presenter);
    context.RequirePart("PART_Content", presenter);
    context.Bind(Control.BackgroundProperty, root, Control.BackgroundProperty, UiPropertyValueSource.Local);
    context.Bind(ContentControl.ContentProperty, presenter, ContentPresenter.ContentProperty);

    return root;
});

button.ApplyTemplate();
ComponentTemplateInstance instance = button.ComponentTemplateInstance!;
```

Bind a template child property to an aspect token from the context environment:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

AspectToken<Thickness> paddingToken = AspectToken.Thickness("button.padding");
AspectEnvironment environment = new("template");
environment.Set(paddingToken, new Thickness(8));

Button owner = new();
Border root = new();
ComponentTemplateContext context = new(owner, environment);

context.BindToken(paddingToken, root, Control.PaddingProperty);
```

## Remarks
`ComponentTemplateContext` is the mutable build-time context used by `ComponentTemplate<TControl>`. The template factory records information on the context, and the template pipeline copies `Bindings`, `TokenBindings`, `Slots`, and `Parts` into the returned `ComponentTemplateInstance`.

The context does not attach bindings, token bindings, slots, or parts by itself. `ComponentTemplateInstance.Attach(Control)` attaches the recorded template bindings to the owner, applies token bindings from the captured `AspectEnvironment`, and attaches the generated root as a template child.

`Owner` and `Environment` are required constructor arguments. When `states` or `variants` are omitted, they default to `AspectStateSet.Empty` and `AspectVariantSet.Empty`.

Use `RegisterSlot` to expose generated child elements through aspect slots, and use `RequirePart` for named template parts that must be present. `RequirePart` registers the supplied element and returns it, but throws an `InvalidOperationException` with the part name when the supplied element is `null`.

`Bind` records a `TemplateBinding` from an owner property to a generated target element property. The typed overload defaults the target value source to `UiPropertyValueSource.TemplateBinding`; the non-generic overload requires the caller to choose the target source. `BindToken` records a `TemplateTokenBinding<T>` that reads from `Environment` and writes the value to the target property when the component template instance is attached.

Use `ComponentTemplateContext<TControl>` when a component template factory needs a strongly typed `Owner`. The non-generic context is the shared base used by the component template creation pipeline.

## Constructors
| Name | Description |
| --- | --- |
| `ComponentTemplateContext(Control owner, AspectEnvironment environment, AspectStateSet? states = null, AspectVariantSet? variants = null)` | Initializes a context for the template owner, aspect environment, optional states, and optional variants. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `Control` | Gets the control that owns the component template being built. |
| `Environment` | `AspectEnvironment` | Gets the aspect environment used for token resolution while the template is built and attached. |
| `States` | `AspectStateSet` | Gets the aspect states available to the template context. |
| `Variants` | `AspectVariantSet` | Gets the aspect variants available to the template context. |
| `Bindings` | `IReadOnlyList<TemplateBinding>` | Gets a read-only view of the template bindings recorded through `Bind`. |
| `TokenBindings` | `IReadOnlyList<TemplateTokenBinding>` | Gets a read-only view of the token bindings recorded through `BindToken`. |
| `Slots` | `TemplateSlotMap` | Gets the slot map populated by `RegisterSlot`. |
| `Parts` | `TemplatePartMap` | Gets the named part map populated by `RequirePart`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `RegisterSlot(AspectSlot slot, UIElement element)` | `void` | Registers a generated element for an aspect slot in `Slots`. |
| `RequirePart<TElement>(string name, TElement? element)` | `TElement` | Registers a required named part in `Parts` and returns the element. |
| `Bind(UiProperty sourceProperty, UIElement target, UiProperty targetProperty, UiPropertyValueSource targetSource)` | `void` | Records a non-generic owner-to-target template binding with the specified target value source. |
| `Bind<T>(UiProperty<T> sourceProperty, UIElement target, UiProperty<T> targetProperty, UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)` | `void` | Records a typed owner-to-target template binding. |
| `BindToken<T>(AspectToken<T> token, UIElement target, UiProperty<T> targetProperty)` | `void` | Records a token binding that applies a value from `Environment` to the target property when attached. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `ComponentTemplateContext(Control, AspectEnvironment, AspectStateSet?, AspectVariantSet?)` | `ArgumentNullException` | `owner` or `environment` is `null`. |
| `RegisterSlot(AspectSlot, UIElement)` | `ArgumentNullException` | `slot` or `element` is `null`. |
| `RequirePart<TElement>(string, TElement?)` | `InvalidOperationException` | `element` is `null`. |
| `RequirePart<TElement>(string, TElement?)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `Bind(UiProperty, UIElement, UiProperty, UiPropertyValueSource)` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Bind(UiProperty, UIElement, UiProperty, UiPropertyValueSource)` | `ArgumentException` | Source and target property value types do not match, or the target property is read-only. |
| `Bind<T>(UiProperty<T>, UIElement, UiProperty<T>, UiPropertyValueSource)` | `ArgumentNullException` | `sourceProperty`, `target`, or `targetProperty` is `null`. |
| `Bind<T>(UiProperty<T>, UIElement, UiProperty<T>, UiPropertyValueSource)` | `ArgumentException` | Source and target property value types do not match, or the target property is read-only. |
| `BindToken<T>(AspectToken<T>, UIElement, UiProperty<T>)` | `ArgumentNullException` | `token`, `target`, or `targetProperty` is `null`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware templating.

## See Also
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Templates/TemplateSlotMap.cs`
- `UI/Controls/Templates/TemplatePartMap.cs`
- `UI/Controls/Templates/TemplateTokenBinding.cs`
- `UI/Controls/Buttons/ButtonTemplates.cs`
