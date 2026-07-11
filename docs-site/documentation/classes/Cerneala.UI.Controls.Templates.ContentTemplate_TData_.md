# ContentTemplate<TData> Class

## Definition

Namespace: `Cerneala.UI.Controls.Templates`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Templates/ContentTemplate.cs`

Represents a strongly typed content template that creates a `UIElement` for content whose runtime value is assignable to `TData`.

```csharp
public sealed class ContentTemplate<TData> : ContentTemplate
```

Inheritance:
`object` -> `ContentTemplate` -> `ContentTemplate<TData>`

Type parameters:

| Name | Constraints | Description |
| --- | --- | --- |
| `TData` | None | The content data type matched by this template and exposed through `ContentTemplateContext<TData>.Data`. |

## Examples

Register a typed template and let a `ContentPresenter` resolve it from a local registry:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

ContentTemplateRegistry registry = new();
registry.Register(new ContentTemplate<string>(
    "PlainText.Card",
    key: null,
    priority: 0,
    context => new Border
    {
        Padding = new Thickness(8),
BorderBrush = new Cerneala.UI.Media.SolidColorBrush(new Color(148, 163, 184)),
        BorderThickness = new Thickness(1),
        Child = new TextBlock { Text = context.Data ?? string.Empty }
    }));

ContentPresenter presenter = new()
{
    Content = "Hello",
    LocalTemplateRegistry = registry
};
```

Use a key and predicate for a more specific template:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

record UserCard(string Name, bool Important);

ContentTemplate<UserCard> template = new(
    "UserCard.Important",
    key: "compact",
    priority: 10,
    context => new TextBlock { Text = context.Data?.Name ?? string.Empty },
    predicate: context => context.Data is UserCard { Important: true });
```

## Remarks

`ContentTemplate<TData>` is the typed public wrapper over `ContentTemplate`. Its constructor passes `typeof(TData)` to the base class, so registry matching uses the same type assignability rules as `ContentTemplate.CanApply(ContentTemplateMatchContext)`: the template can apply when the content value is non-null and `typeof(TData).IsInstanceOfType(context.Data)` returns `true`.

The factory receives a `ContentTemplateContext<TData>`, which copies the untyped content template context and exposes `Data` as `TData?`. When the supplied untyped context does not contain a `TData` value, the typed `Data` property is `default`. Normal registry resolution calls `CanApply` before `Create`, but direct calls through `ContentPresenter.ContentTemplate` invoke `Create` without resolving through a registry.

Keys are matched with ordinal string comparison. A keyed template does not match an unkeyed request, and an unkeyed template does not match a keyed request. When a `ContentTemplateRegistry` resolves multiple matches, keyed matches, predicate templates, higher priority, nearer data types, and registration order determine the winner.

Factories may return `null`. `ContentPresenter` treats a `null` result as no presented child.

## Constructors

| Name | Description |
| --- | --- |
| `ContentTemplate(string name, string? key, int priority, Func<ContentTemplateContext<TData>, UIElement?> factory, Func<ContentTemplateMatchContext, bool>? predicate = null)` | Initializes a typed template with a non-empty name, optional key, priority, factory, and optional match predicate. Throws `ArgumentException` for an empty or whitespace name and `ArgumentNullException` when `factory` is `null`. |

## Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `DataType` | `Type?` | `ContentTemplate` | Gets the data type used for matching. For `ContentTemplate<TData>`, this is `typeof(TData)`. |
| `HasPredicate` | `bool` | `ContentTemplate` | Gets whether the template has an additional match predicate. |
| `Key` | `string?` | `ContentTemplate` | Gets the optional template key that must match `ContentTemplateMatchContext.RequestedKey`. |
| `Name` | `string` | `ContentTemplate` | Gets the non-empty diagnostic template name supplied to the constructor. |
| `Priority` | `int` | `ContentTemplate` | Gets the priority used by `ContentTemplateRegistry` when ordering matching templates. |

## Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `CanApply(ContentTemplateMatchContext context)` | `bool` | `ContentTemplate` | Returns whether the template matches the requested key, content value, data type, and optional predicate. Throws `ArgumentNullException` when `context` is `null`. |
| `Create(ContentTemplateContext context)` | `UIElement?` | `ContentTemplate` | Invokes the template factory. For `ContentTemplate<TData>`, the factory receives a `ContentTemplateContext<TData>` wrapper. |

## Applies to

Project: `Cerneala`

UI area: retained controls, content presenters, item containers, content template registries.

## See also

- `UI/Controls/Templates/ContentTemplate.cs`
- `UI/Controls/Templates/ContentTemplateContext.cs`
- `UI/Controls/Templates/ContentTemplateMatchContext.cs`
- `UI/Controls/Templates/ContentTemplateRegistry.cs`
- `UI/Controls/ContentPresenter.cs`
- `UI/Controls/ItemsControl.cs`
