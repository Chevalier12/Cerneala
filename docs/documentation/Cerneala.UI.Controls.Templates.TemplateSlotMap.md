# TemplateSlotMap Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplateSlotMap.cs`

Stores the `UIElement` instances registered for aspect slots while a component template is built.

```csharp
public sealed class TemplateSlotMap
```

Inheritance:
`object` -> `TemplateSlotMap`

## Examples
Register an aspect slot while building a component template and retrieve the generated element from the created instance:

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
    return root;
});

ComponentTemplateContext context = new(button, new AspectEnvironment("template"));
ComponentTemplateInstance instance = template.CreateInstance(button, context);

Border registeredRoot = (Border)instance.Slots[rootSlot];
```

Register a slot directly when constructing a map:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

AspectSlot<Button, Border> rootSlot = AspectSlot.For<Button, Border>("Root");
TemplateSlotMap slots = new();
Border root = new();

slots.Register(rootSlot, root);

Border sameRoot = (Border)slots[rootSlot];
```

## Remarks
`TemplateSlotMap` is the runtime lookup table for aspect-aware template slots. `ComponentTemplateContext.RegisterSlot(AspectSlot, UIElement)` registers generated template elements into the context's map, and `ComponentTemplate<TControl>` passes that same map to the resulting `ComponentTemplateInstance`.

Slots are keyed by `AspectSlot`. Slot equality uses the slot name, owner type, and target type, so two slot instances with the same values address the same map entry. Registering the same slot again replaces the previous element for that slot.

`Register` requires a non-null slot and a non-null `UIElement`. The indexer returns the element for an existing slot and uses the underlying dictionary lookup, so a missing slot throws `KeyNotFoundException`.

`ControlTemplateAdapter` creates an empty `TemplateSlotMap` when adapting classic `ControlTemplate` instances into the component template pipeline.

## Constructors
| Name | Description |
| --- | --- |
| `TemplateSlotMap()` | Initializes an empty aspect slot map. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `this[AspectSlot slot]` | `UIElement` | Gets the registered element for `slot`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Register(AspectSlot slot, UIElement element)` | `void` | Registers or replaces the element associated with the specified aspect slot. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Register(AspectSlot slot, UIElement element)` | `ArgumentNullException` | `slot` or `element` is `null`. |
| `this[AspectSlot slot]` | `KeyNotFoundException` | No element is registered for `slot`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, component templates, aspect-aware template slots.

## See Also
- `UI/Controls/Templates/TemplateSlotMap.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/TemplatePartMap.cs`
- `UI/Aspect/AspectSlot.cs`
