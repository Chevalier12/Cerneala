# ComponentTemplateInstance Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ComponentTemplateInstance.cs`

Represents one materialized component template, including its generated root element, recorded template bindings, token bindings, slots, and required parts.

```csharp
public sealed class ComponentTemplateInstance : IDisposable
```

Inheritance:
`object` -> `ComponentTemplateInstance`

Implements:
`IDisposable`

## Examples

Create, inspect, and attach a component template instance:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new();
AspectSlot<Button, Border> rootSlot = AspectSlot.For<Button, Border>("Root");

ComponentTemplate<Button> template = new("Button.Root", context =>
{
    Border root = new();
    context.RegisterSlot(rootSlot, root);
    context.RequirePart("PART_Root", root);
    return root;
});

ComponentTemplateContext context = new(button, new AspectEnvironment("template"));
using ComponentTemplateInstance instance = template.CreateInstance(button, context);

Border root = (Border)instance.Slots[rootSlot];
Border part = (Border)instance.Parts["PART_Root"];

instance.Attach(button);
instance.Detach();
```

Apply a component template through a control:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;

Button button = new()
{
    ComponentTemplate = new ComponentTemplate<Button>("Button.Chrome", _ => new Border())
};

button.ApplyTemplate();

ComponentTemplateInstance instance = button.ComponentTemplateInstance!;
UIElement? root = instance.Root;
```

## Remarks

`ComponentTemplateInstance` is the retained runtime object produced by `ComponentTemplate.CreateInstance(Control, ComponentTemplateContext)`. `ComponentTemplate<TControl>` creates it from the template factory root and the `ComponentTemplateContext` collections gathered while the factory ran.

The `Root` property exposes the factory result, which may be `null`. `Slots` and `Parts` expose the same slot and required-part maps populated through `ComponentTemplateContext.RegisterSlot` and `ComponentTemplateContext.RequirePart`. The recorded `TemplateBinding` and `TemplateTokenBinding` collections are internal implementation details; they are copied into the instance during construction and attached later by `Attach(Control)`.

`Attach(Control)` connects the instance to exactly one owner. It adds `Root` to the owner's template child collections through the template child ownership helper, then attaches regular template bindings and token bindings. If attachment fails after any step, the instance calls `Detach()` and rethrows the original exception.

`Detach()` is idempotent when the instance has no owner. When attached, it detaches token bindings first, detaches regular template bindings, removes `Root` from the owner, and clears the owner reference.

`Dispose()` detaches the instance once and prevents later attachment. Calling `Attach(Control)` after disposal throws `ObjectDisposedException`. Calling `Attach(Control)` while the instance is already attached throws `InvalidOperationException`.

`Control.ApplyTemplate()` stores the currently attached component instance in `Control.ComponentTemplateInstance`. Component templates take precedence over classic `Control.Template`; replacing the component template detaches the old `ComponentTemplateInstance` before attaching the new one.

## Constructors

| Name | Description |
| --- | --- |
| `ComponentTemplateInstance(UIElement? root, IEnumerable<TemplateBinding>? bindings, IEnumerable<TemplateTokenBinding>? tokenBindings, TemplateSlotMap slots, TemplatePartMap parts)` | Initializes a component template instance with an optional root, optional template bindings, optional token bindings, and the slot and part maps captured from the template context. Binding sequences are copied into the instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Root` | `UIElement?` | Gets the root element created by the component template factory, or `null` when the template has no root. |
| `Slots` | `TemplateSlotMap` | Gets the aspect slot map populated while the template factory ran. |
| `Parts` | `TemplatePartMap` | Gets the named required-part map populated while the template factory ran. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Attach(Control templateOwner)` | `void` | Attaches the root, template bindings, and token bindings to `templateOwner`. |
| `Detach()` | `void` | Detaches token bindings, template bindings, and the root from the current owner, if any. |
| `Dispose()` | `void` | Detaches the instance and prevents future attachment. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Attach(Control templateOwner)` | `ObjectDisposedException` | The instance has already been disposed. |
| `Attach(Control templateOwner)` | `ArgumentNullException` | `templateOwner` is `null`. |
| `Attach(Control templateOwner)` | `InvalidOperationException` | The instance is already attached to an owner. |

## Applies To

Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware templating.

## See Also

- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/TemplateSlotMap.cs`
- `UI/Controls/Templates/TemplatePartMap.cs`
- `UI/Controls/Templates/TemplateTokenBinding.cs`
- `UI/Controls/Control.cs`
