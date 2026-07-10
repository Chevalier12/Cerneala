# ContentTemplate Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplate.cs`

Represents a modern content template that matches content data, an optional template key, and an optional predicate before creating a retained `UIElement`.

```csharp
public class ContentTemplate
```

Inheritance:
`object` -> `ContentTemplate`

Derived:
`ContentTemplate<TData>`

## Examples
Register a template and resolve it for matching content:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;

ContentTemplate template = new(
    "Messages.Text",
    dataType: typeof(string),
    key: "message",
    priority: 10,
    factory: context => new TextBlock { Text = (string?)context.Data ?? string.Empty });

ContentTemplateRegistry registry = new();
registry.Register(template);

bool resolved = registry.TryResolve(
    new ContentTemplateMatchContext("Saved", requestedKey: "message"),
    out ContentTemplate selected);

if (resolved)
{
    ContentPresenter presenter = new() { Content = "Saved" };
    UIElement? child = selected.Create(new ContentTemplateContext(presenter.Content, presenter));
}
```

Use a predicate to narrow a template beyond its data type:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplate importantMessage = new(
    "Messages.Important",
    dataType: typeof(MessageViewModel),
    key: null,
    priority: 20,
    factory: context =>
    {
        MessageViewModel message = (MessageViewModel)context.Data!;
        return new TextBlock { Text = message.Title };
    },
    predicate: context => context.Data is MessageViewModel { IsImportant: true });

public sealed record MessageViewModel(string Title, bool IsImportant);
```

## Remarks
`ContentTemplate` is the non-generic base for the modern content-template pipeline under `Cerneala.UI.Controls.Templates`. It stores a diagnostic `Name`, an optional accepted `DataType`, an optional `Key`, a numeric `Priority`, a required element factory, and an optional match predicate.

`CanApply(ContentTemplateMatchContext)` returns `false` when the requested key does not match the template key. A keyed template only matches the same requested key, and an unkeyed template does not match a non-null requested key. Type matching uses `DataType.IsInstanceOfType(context.Data)`. When `DataType` is `null`, the template matches only `null` data. If the key and type checks pass, the optional predicate decides the final match.

`Create(ContentTemplateContext)` invokes the factory supplied to the constructor and returns the produced `UIElement`, or `null` if the factory returns no element. The base method does not call `CanApply`; callers such as `ContentTemplateRegistry` are responsible for resolving an applicable template before creation.

`ContentTemplateRegistry` orders matching templates by keyed match, predicate presence, priority, data-type specificity, and registration order. The registry disables its cache while any registered template has a predicate because predicate results can depend on the full match context.

`ContentPresenter.ContentTemplate` can apply a template directly. When no explicit template is set, `ContentPresenter.LocalTemplateRegistry` can resolve one from a registry. If neither path produces a template, the presenter falls back to hosting an existing `UIElement`, generating a `TextBlock` for string content, or producing no child.

For typed factories, prefer `ContentTemplate<TData>`. It sets `DataType` to `typeof(TData)` and wraps the untyped context in `ContentTemplateContext<TData>`.

## Constructors
| Name | Description |
| --- | --- |
| `ContentTemplate(string name, Type? dataType, string? key, int priority, Func<ContentTemplateContext, UIElement?> factory, Func<ContentTemplateMatchContext, bool>? predicate = null)` | Initializes a content template with its name, accepted data type, optional key, priority, factory, and optional match predicate. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the non-empty template name. |
| `DataType` | `Type?` | Gets the data type accepted by this template, or `null` for a template that matches `null` data. |
| `Key` | `string?` | Gets the optional template key that must match `ContentTemplateMatchContext.RequestedKey`. |
| `Priority` | `int` | Gets the priority used by `ContentTemplateRegistry` when ordering matching templates. |
| `HasPredicate` | `bool` | Gets whether this template has a predicate that participates in matching. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `CanApply(ContentTemplateMatchContext context)` | `bool` | Returns `true` when the context key, data value, and optional predicate match this template. |
| `Create(ContentTemplateContext context)` | `UIElement?` | Creates the content element by invoking the template factory. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `ContentTemplate(...)` | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `ContentTemplate(...)` | `ArgumentNullException` | `factory` is `null`. |
| `CanApply(ContentTemplateMatchContext context)` | `ArgumentNullException` | `context` is `null`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, content presenters, item presentation, modern content-template registries.

## See Also
- `UI/Controls/Templates/ContentTemplate.cs`
- `UI/Controls/Templates/ContentTemplateContext.cs`
- `UI/Controls/Templates/ContentTemplateMatchContext.cs`
- `UI/Controls/Templates/ContentTemplateRegistry.cs`
- `UI/Controls/ContentPresenter.cs`
- `UI/Controls/ItemsControl.cs`
