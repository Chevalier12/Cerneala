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

Part names are matched with `StringComparer.Ordinal`, so lookups are case-sensitive and culture-insensitive. Registering the same name again throws `InvalidOperationException` and keeps the element registered first. For example, `Register("PART_Text", a)` followed by `Register("PART_Text", b)` throws, and `map["PART_Text"]` still returns `a`.

`Register` requires a non-empty, non-whitespace name and a non-null `UIElement`. The indexer returns the element for an existing name and uses the underlying dictionary lookup, so a missing name throws `KeyNotFoundException`. `TryGetValue` returns `false` and a `null` element for a missing name instead of throwing.

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
| `Register(string name, UIElement element)` | `void` | Registers the element for the specified part name. A name can be registered only once. |
| `TryGetValue(string name, out UIElement? element)` | `bool` | Returns `true` and the registered element when `name` exists; otherwise `false` and `null`. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Register(string name, UIElement element)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `Register(string name, UIElement element)` | `ArgumentNullException` | `element` is `null`. |
| `Register(string name, UIElement element)` | `InvalidOperationException` | An element is already registered for `name`. |
| `this[string name]` | `KeyNotFoundException` | No element is registered for `name`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, component templates, named template parts.

## See Also
- `UI/Controls/Templates/TemplatePartMap.cs`
- `UI/Controls/Templates/ComponentTemplateContext.cs`
- `UI/Controls/Templates/ComponentTemplateInstance.cs`
- `UI/Controls/Templates/ComponentTemplate.cs`
- `UI/Controls/Templates/TemplatePartAttribute.cs`
