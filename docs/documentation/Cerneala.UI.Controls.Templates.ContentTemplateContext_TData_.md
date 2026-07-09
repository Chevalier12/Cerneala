# ContentTemplateContext<TData> Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplateContext.cs`

Provides a strongly typed wrapper around a content-template creation context.

```csharp
public sealed class ContentTemplateContext<TData> : ContentTemplateContext
```

Inheritance:
`object` -> `ContentTemplateContext` -> `ContentTemplateContext<TData>`

Type parameters:

| Name | Constraints | Description |
| --- | --- | --- |
| `TData` | None | The expected content data type exposed by the typed `Data` property. |

## Examples

Create a typed content template and read the data without casting:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplate<string> template = new(
    "Text.Item",
    key: null,
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty });
```

Wrap an existing untyped context when invoking a typed factory directly:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentPresenter presenter = new() { Content = "Ada", ContentIndex = 2 };
ContentTemplateContext untyped = new(presenter.Content, presenter, index: presenter.ContentIndex);
ContentTemplateContext<string> typed = new(untyped);

string text = typed.Data ?? string.Empty;
int index = typed.Index;
```

## Remarks

`ContentTemplateContext<TData>` is the context type supplied to `ContentTemplate<TData>` factories. It copies the untyped context's `Data`, `Presenter`, `Environment`, `Variants`, `Index`, and `Owner` values into the base `ContentTemplateContext`.

The typed `Data` property hides the base `Data` property. During construction, the untyped data value is assigned to `Data` only when `context.Data is TData`; otherwise the typed value is `default`. Normal registry resolution checks template applicability before creation, but direct template creation can still produce a typed context whose `Data` is `default`.

Use the inherited `Presenter`, `Index`, `Environment`, `Variants`, and `Owner` values when the factory needs information about the presenter, item position, aspect environment, aspect variants, or owner object that caused the content template to run.

## Constructors

| Name | Description |
| --- | --- |
| `ContentTemplateContext(ContentTemplateContext context)` | Initializes a typed context by copying values from an existing untyped content-template context. |

## Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Data` | `TData?` | `ContentTemplateContext<TData>` | Gets the content value as `TData` when the untyped context data is assignable to `TData`; otherwise `default`. |
| `Data` | `object?` | `ContentTemplateContext` | Gets the original untyped content value. Hidden by the generic `Data` property. |
| `Presenter` | `ContentPresenter?` | `ContentTemplateContext` | Gets the presenter that is creating the content element, when available. |
| `Environment` | `AspectEnvironment` | `ContentTemplateContext` | Gets the aspect environment associated with the template context. |
| `Variants` | `AspectVariantSet` | `ContentTemplateContext` | Gets the aspect variants associated with the template context. |
| `Index` | `int` | `ContentTemplateContext` | Gets the item index passed to the content template, or `-1` when no index was supplied. |
| `Owner` | `object?` | `ContentTemplateContext` | Gets the owner object associated with the content template, when available. |

## Applies To

Project: `Cerneala`

UI area: retained controls, content presenters, modern content templates, item presentation.

## See Also

- `UI/Controls/Templates/ContentTemplateContext.cs`
- `UI/Controls/Templates/ContentTemplate.cs`
- `UI/Controls/Templates/ContentTemplateRegistry.cs`
- `UI/Controls/ContentPresenter.cs`
- `docs/documentation/Cerneala.UI.Controls.Templates.ContentTemplate_TData_.md`
