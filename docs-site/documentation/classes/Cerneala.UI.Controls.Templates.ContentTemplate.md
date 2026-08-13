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
Declare typed templates directly on the `ItemsControl` that resolves them:

```xml
<ItemsControl
    xmlns:sample="clr-namespace:Sample"
    ItemsSource="$DataContext.Rows">
    <ItemsControl.Templates>
        <ContentTemplate DataType="sample:PersonRow">
            <TextBlock Text="$DataContext.Name" />
        </ContentTemplate>
        <ContentTemplate DataType="sample:ToggleRow">
            <CheckBox IsChecked="$DataContext.Value:TwoWay" />
        </ContentTemplate>
    </ItemsControl.Templates>
</ItemsControl>
```

Declare a single template inline on the property that owns it:

```xml
<UserControl>
    <ItemsControl ItemsSource="$DataContext.People">
        <ItemsControl.ItemTemplate>
            <ContentTemplate DataType="Sample.PersonRow">
                <TextBlock Text="$DataContext.Name" />
            </ContentTemplate>
        </ItemsControl.ItemTemplate>
    </ItemsControl>
</UserControl>
```

Scope descendant bindings to a nested object by assigning `DataContext` on an element:

```xml
<ContentTemplate DataType="sample:PersonRow">
    <StackPanel DataContext="$DataContext.Details">
        <TextBlock Text="$DataContext.Name" />
        <TextBlock Text="$DataContext.Description" />
    </StackPanel>
</ContentTemplate>
```

The `StackPanel.DataContext` expression is validated against `PersonRow`. Inside the panel, `$DataContext` is validated against the type of `PersonRow.Details`. Sibling elements outside that panel keep the surrounding `PersonRow` context.

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

The `.cui.xml` source generator accepts `ContentTemplate` only as the inline value of a content-template property or inside `ItemsControl.Templates`. A `ContentTemplate` declaration inside any `Resources` collection is rejected, regardless of whether it has a `Name`; templates must have an explicit owning control or property. `DataType` accepts a fully qualified metadata name or a scoped XML alias declared as `xmlns:prefix="clr-namespace:Namespace"`. References to another assembly may use `clr-namespace:Namespace;assembly=AssemblyName`. `DataType` is required, while `Key` and `Priority` are optional. Each declaration must contain exactly one visual root. Names inside the repeated visual tree are rejected until per-realization name scopes are supported.

By default, the generated factory assigns the item to the visual root's `DataContext`, and `$DataContext` bindings are validated statically against the template's `DataType`. An element may override that inherited context with a direct binding such as `DataContext="$DataContext.Details"`. The override expression is validated against the surrounding context type; descendant `$DataContext` expressions are then validated against the override expression's result type. The new context applies only to that element's subtree, so later siblings resume the surrounding context. Runtime bindings observe both the path that supplies the local `DataContext` and descendant paths. Replacing an intermediate object retargets the local context and its descendant bindings. When the template root declares `DataContext` explicitly, the generated factory does not overwrite it with the original item.

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
