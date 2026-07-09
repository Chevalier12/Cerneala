# ContentTemplateRegistry.RegisteredTemplate Record

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateRegistry.cs`

Stores one registered content template together with the insertion order used as the final tie-breaker during registry resolution.

```csharp
private sealed record RegisteredTemplate(ContentTemplate Template, int Order);
```

Containing type:
`Cerneala.UI.Controls.Templates.ContentTemplateRegistry`

Inheritance:
`object` -> `ContentTemplateRegistry.RegisteredTemplate`

## Examples
`RegisteredTemplate` is an internal implementation detail of `ContentTemplateRegistry`; callers do not create it directly. It is created when a template is registered:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateRegistry registry = new();
ContentTemplate<string> template = new(
    "Messages.Text",
    key: null,
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty });

registry.Register(template);
```

The registry keeps the template and its order so equal matches resolve to the template registered first.

## Remarks
`RegisteredTemplate` is a private nested record used only by `ContentTemplateRegistry`. `Register(ContentTemplate)` creates a new entry with the supplied `ContentTemplate` and the current registration order, then increments the next-order counter.

`TryResolve(ContentTemplateMatchContext, out ContentTemplate)` evaluates the stored entries, filters them through `ContentTemplate.CanApply(ContentTemplateMatchContext)`, and orders candidates by key match, predicate presence, priority, type specificity, and finally `Order`. The `Order` value preserves stable first-registered-wins behavior when all stronger match rules are equal.

The record is not part of the public API surface. Its generated value equality is not used by the registry's removal path; `Unregister(ContentTemplate)` removes entries by reference comparison against the stored `Template`.

## Constructors
| Name | Description |
| --- | --- |
| `RegisteredTemplate(ContentTemplate template, int order)` | Initializes an entry with the registered template and its insertion order. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Template` | `ContentTemplate` | Gets the registered template instance used for matching and creation. |
| `Order` | `int` | Gets the zero-based insertion order assigned by `ContentTemplateRegistry.Register(ContentTemplate)`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, content presenters, item presentation, modern content-template resolution.

## See Also
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplateMatchContext`
