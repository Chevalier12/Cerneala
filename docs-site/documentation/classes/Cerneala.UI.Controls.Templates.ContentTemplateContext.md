# ContentTemplateContext Class

## Definition
Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateContext.cs`

Carries the data, presenter, aspect environment, variants, item index, and owner supplied to a `ContentTemplate` factory.

```csharp
public class ContentTemplateContext
```

Inheritance:
`object` -> `ContentTemplateContext`

Derived:
`ContentTemplateContext<TData>`

## Examples
Create a direct modern content template for a presenter:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentPresenter presenter = new()
{
    Content = "Saved",
    ContentTemplate = new ContentTemplate(
        "Status.Text",
        dataType: typeof(string),
        key: null,
        priority: 0,
        factory: context => new TextBlock
        {
            Text = (string?)context.Data ?? string.Empty
        })
};
```

Read the item index passed by an `ItemsControl` container:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

int observedIndex = -1;

ContentTemplate<string> template = new(
    "Items.Text",
    key: null,
    priority: 0,
    context =>
    {
        observedIndex = context.Index;
        return new TextBlock { Text = context.Data ?? string.Empty };
    });
```

## Remarks
`ContentTemplateContext` is the non-generic context object passed to `ContentTemplate.Create(ContentTemplateContext)` and to the factory supplied to the non-generic `ContentTemplate` constructor. It is an immutable data holder; it does not resolve templates, attach children, or perform layout.

`ContentPresenter` creates this context when it invokes either `ContentTemplate` or a template resolved from `LocalTemplateRegistry`. In that path, `Data` is the presenter content, `Presenter` is the presenter that is creating the child, and `Index` is copied from `ContentPresenter.ContentIndex`. Item presenters prepared by `ItemsControl` use that index so item templates can know their realized item position.

When no `AspectEnvironment` is supplied, the constructor creates a new environment named `content-template`. When no `AspectVariantSet` is supplied, `Variants` is `AspectVariantSet.Empty`. `Index` defaults to `-1`, which is the same default used by `ContentPresenter` before an item container assigns an index. `Owner` is optional and is not assigned by the current `ContentPresenter` creation path.

Use `ContentTemplateContext<TData>` when a typed template factory needs `Data` exposed as `TData?`. The typed context copies all values from the untyped context and shadows `Data` with a typed property.

## Constructors
| Name | Description |
| --- | --- |
| `ContentTemplateContext(object? data, ContentPresenter? presenter = null, AspectEnvironment? environment = null, AspectVariantSet? variants = null, int index = -1, object? owner = null)` | Initializes a content template context with optional presenter, aspect environment, variants, item index, and owner. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Data` | `object?` | Gets the content data passed to the template factory. |
| `Presenter` | `ContentPresenter?` | Gets the presenter creating the templated child, when one was supplied. |
| `Environment` | `AspectEnvironment` | Gets the aspect environment available during content template creation. |
| `Variants` | `AspectVariantSet` | Gets the aspect variants available to the content template. |
| `Index` | `int` | Gets the item or content index supplied by the presenter; the default is `-1`. |
| `Owner` | `object?` | Gets the optional owner associated with this template context. |

## Applies To
Project: `Cerneala`

UI area: retained controls, content presenters, item presentation, modern content-template factories.

## See Also
- `UI/Controls/Templates/ContentTemplateContext.cs`
- `UI/Controls/Templates/ContentTemplate.cs`
- `UI/Controls/Templates/ContentTemplateRegistry.cs`
- `UI/Controls/Templates/ContentTemplateMatchContext.cs`
- `UI/Controls/ContentPresenter.cs`
- `UI/Controls/ItemsControl.cs`
