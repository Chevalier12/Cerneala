# TemplatePartMap Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/TemplatePartMap.cs`

Stores the named `UIElement` parts collected while a component template is built.

```csharp
public sealed class TemplatePartMap
```

Inheritance:
`object` -> `TemplatePartMap`

## Examples
Register a required part while building a component template and retrieve it from the created instance:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Button button = new();

ComponentTemplate<Button> template = new("Button.Content", context =>
{
    ContentPresenter presenter = new();
    context.RequirePart("PART_Content", presenter);
    return presenter;
});

ComponentTemplateContext context = new(button, new AspectEnvironment("template"));
ComponentTemplateInstance instance = template.CreateInstance(button, context);

ContentPresenter contentPart = (ContentPresenter)instance.Parts["PART_Content"];
```

Register a part directly when constructing a map:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

TemplatePartMap parts = new();
Border root = new();

parts.Register("PART_Root", root);

Border sameRoot = (Border)parts["PART_Root"];
```

## Remarks
`TemplatePartMap` is the runtime lookup table for named template parts. `ComponentTemplateContext.RequirePart<TElement>(string, TElement?)` registers required parts into the context's map, and `ComponentTemplate<TControl>` passes that same map to the resulting `ComponentTemplateInstance`.

Part names are matched with `StringComparer.Ordinal`, so lookups are case-sensitive and culture-insensitive. Registering the same name again replaces the previous element for that name.

`Register` requires a non-empty, non-whitespace name and a non-null `UIElement`. The indexer returns the element for an existing name and uses the underlying dictionary lookup, so a missing name throws `KeyNotFoundException`.

`ControlTemplateAdapter` creates an empty `TemplatePartMap` when adapting classic `ControlTemplate` instances into the component template pipeline.

## Constructors
| Name | Description |
| --- | --- |
| `TemplatePartMap()` | Initializes an empty named template-part map. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `this[string name]` | `UIElement` | Gets the registered element for `name`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Register(string name, UIElement element)` | `void` | Registers or replaces the element associated with the specified part name. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Register(string name, UIElement element)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `Register(string name, UIElement element)` | `ArgumentNullException` | `element` is `null`. |
| `this[string name]` | `KeyNotFoundException` | No element is registered for `name`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, component templates, named template parts.

## See Also
- `UI/Controls/Templates/TemplatePartMap.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/TemplatePartAttribute.cs`
