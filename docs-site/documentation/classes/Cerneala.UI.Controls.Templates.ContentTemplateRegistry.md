# ContentTemplateRegistry Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateRegistry.cs`

Stores registered `ContentTemplate` instances and resolves the best matching template for a `ContentTemplateMatchContext`.

```csharp
public sealed class ContentTemplateRegistry
```

Inheritance:
`object` -> `ContentTemplateRegistry`

## Examples
Register templates and resolve the most specific match:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateRegistry registry = new();

ContentTemplate<object> fallback = new(
    "Fallback",
    key: null,
    priority: 0,
    context => new TextBlock { Text = context.Data?.ToString() ?? string.Empty });

ContentTemplate<string> text = new(
    "Text",
    key: null,
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty });

registry.Register(fallback);
registry.Register(text);

bool found = registry.TryResolve(
    new ContentTemplateMatchContext("hello"),
    out ContentTemplate selected);
```

Use a keyed registry from a `ContentPresenter`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateRegistry registry = new();
registry.Register(new ContentTemplate<string>(
    "CompactText",
    key: "compact",
    priority: 10,
    context => new TextBlock { Text = context.Data ?? string.Empty }));

ContentPresenter presenter = new()
{
    Content = "Ada",
    ContentTemplateKey = "compact",
    LocalTemplateRegistry = registry
};
```

## Remarks
`ContentTemplateRegistry` is used by the content-template pipeline. `ContentPresenter.LocalTemplateRegistry` asks the registry to resolve a `ContentTemplate` when no explicit `ContentTemplate` is set. `ItemsControl.ContentTemplateRegistry` supplies the registry assigned to generated item presenters.

Registering a template appends it with a registration order, clears the resolve cache, and increments `Version`. Unregistering removes the exact same template instance by reference, clears the cache, increments `Version`, and returns whether a template was removed.

Resolution filters templates through `ContentTemplate.CanApply(ContentTemplateMatchContext)` and then selects the first template after applying these ordering rules:

| Order | Rule |
| --- | --- |
| 1 | A keyed template wins when both the template key and requested key are non-null. |
| 2 | Templates with predicates are preferred over templates without predicates. |
| 3 | Higher `ContentTemplate.Priority` values win. |
| 4 | More specific data types win. An exact data-type match outranks assignable base types, and nearer base types outrank farther base types. |
| 5 | Earlier registration order wins when all other factors are equal. |

The registry caches successful resolutions by requested key and data type only when no registered template has a predicate. If any registered template has a predicate, every resolve attempt is treated as a cache miss because the predicate can depend on the full match context.

`CacheHits` and `CacheMisses` are diagnostic counters for resolve attempts. `TryResolve` increments `CacheHits` only when it returns a cached template. It increments `CacheMisses` for uncached resolution attempts, including attempts that return `false`.

## Constructors
| Name | Description |
| --- | --- |
| `ContentTemplateRegistry()` | Creates an empty registry with version and cache counters set to zero. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Version` | `int` | Gets the number of successful registry mutations. Registering a template or unregistering an existing template increments this value. |
| `CacheHits` | `int` | Gets the number of successful cache lookups performed by `TryResolve`. |
| `CacheMisses` | `int` | Gets the number of uncached resolve attempts performed by `TryResolve`. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `Register(ContentTemplate template)` | `void` | Adds a template to the registry, clears cached resolutions, and increments `Version`. |
| `Unregister(ContentTemplate template)` | `bool` | Removes the registered template instance by reference, clears cached resolutions, increments `Version` when removed, and returns whether a template was removed. |
| `TryResolve(ContentTemplateMatchContext context, out ContentTemplate template)` | `bool` | Resolves the best matching template for the supplied context and returns `true` when one is found. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `Register(ContentTemplate template)` | `ArgumentNullException` | `template` is `null`. |
| `TryResolve(ContentTemplateMatchContext context, out ContentTemplate template)` | `ArgumentNullException` | `context` is `null`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, content presenters, item presentation, modern content-template resolution.

## See Also
- `Cerneala.UI.Controls.ContentPresenter`
- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplateContext`
- `Cerneala.UI.Controls.Templates.ContentTemplateMatchContext`
