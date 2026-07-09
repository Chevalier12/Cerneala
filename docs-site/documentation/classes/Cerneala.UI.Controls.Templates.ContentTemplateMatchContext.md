# ContentTemplateMatchContext Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateMatchContext.cs`

Carries the data and optional lookup details used when a `ContentTemplateRegistry` or `ContentTemplate` decides whether a content template can apply.

```csharp
public sealed class ContentTemplateMatchContext
```

Inheritance:
`object` -> `ContentTemplateMatchContext`

## Examples
Resolve a keyed template for a content value:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateRegistry registry = new();
ContentTemplate<string> template = new(
    "Messages.Compact",
    key: "compact",
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty });

registry.Register(template);

ContentTemplateMatchContext matchContext = new(
    data: "Saved",
    requestedKey: "compact");

bool resolved = registry.TryResolve(matchContext, out ContentTemplate selected);
```

Use the match context in a predicate:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplate importantTemplate = new(
    "Messages.Important",
    dataType: typeof(MessageViewModel),
    key: null,
    priority: 10,
    factory: context =>
    {
        MessageViewModel message = (MessageViewModel)context.Data!;
        return new TextBlock { Text = message.Title };
    },
    predicate: context => context.Data is MessageViewModel { IsImportant: true });

public sealed record MessageViewModel(string Title, bool IsImportant);
```

## Remarks
`ContentTemplateMatchContext` is an immutable context object for template matching. It is passed to `ContentTemplate.CanApply(ContentTemplateMatchContext)` and `ContentTemplateRegistry.TryResolve(ContentTemplateMatchContext, out ContentTemplate)`.

`Data` contains the content value being matched. `DataType` is computed from `Data?.GetType()`, so it is `null` when `Data` is `null`. A `ContentTemplate` with a non-null `DataType` matches only non-null data that is an instance of that type; a template with `DataType` set to `null` matches null data.

`RequestedKey` narrows matching to templates with the same key. In the current `ContentPresenter` registry path, the presenter creates this context from its `Content`, `ContentTemplateKey`, and itself as `Presenter`.

`Owner` and `Index` are optional contextual values for callers that resolve templates outside the default presenter path. The constructor defaults `Index` to `-1`, matching the unset item index convention used by the content-template pipeline.

## Constructors
| Name | Description |
| --- | --- |
| `ContentTemplateMatchContext(object? data, string? requestedKey = null, ContentPresenter? presenter = null, object? owner = null, int index = -1)` | Initializes a match context with content data, an optional requested template key, presenter, owner, and item index. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Data` | `object?` | Gets the content value being matched. |
| `DataType` | `Type?` | Gets the runtime type of `Data`, or `null` when `Data` is `null`. |
| `RequestedKey` | `string?` | Gets the optional key requested by the caller. |
| `Presenter` | `ContentPresenter?` | Gets the presenter that requested template matching, when supplied. |
| `Owner` | `object?` | Gets the optional owner associated with the match. |
| `Index` | `int` | Gets the optional item or content index; the default is `-1`. |

## Applies To
Project: `Cerneala`

UI area: retained controls, content presenters, item presentation, modern content-template registries.

## See Also
- `UI/Controls/Templates/ContentTemplateMatchContext.cs`
- `UI/Controls/Templates/ContentTemplate.cs`
- `UI/Controls/Templates/ContentTemplateContext.cs`
- `UI/Controls/Templates/ContentTemplateRegistry.cs`
- `UI/Controls/ContentPresenter.cs`
