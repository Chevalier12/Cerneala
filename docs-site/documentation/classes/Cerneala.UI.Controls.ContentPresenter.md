# ContentPresenter Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ContentPresenter.cs`

Displays a single content value by materializing it as a child `UIElement`.

```csharp
public class ContentPresenter : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentPresenter`

## Examples

Present an existing element directly:

```csharp
using Cerneala.UI.Controls;

TextBlock text = new() { Text = "Hello" };
ContentPresenter presenter = new()
{
    Content = text
};
```

Create a presented element from data with a `DataTemplate`:

```csharp
using Cerneala.UI.Controls;

ContentPresenter presenter = new()
{
    Content = "Ada",
    ContentTemplate = new DataTemplate<string>(name => new TextBlock { Text = name })
};
```

Use a modern content template from the local template registry:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateRegistry registry = new();
registry.Register(new ContentTemplate<string>(
    "NameText",
    key: "name",
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty }));

ContentPresenter presenter = new()
{
    Content = "Ada",
    ContentTemplateKey = "name",
    LocalTemplateRegistry = registry
};
```

## Remarks

`ContentPresenter` is a lightweight control for turning `Content` into one realized child. It adds the realized child to both `LogicalChildren` and `VisualChildren`, exposes it through `PresentedChild`, and measures and arranges that child to fill the presenter's layout slot.

Content is resolved in this order:

| Step | Behavior |
| --- | --- |
| `ContentTemplate` | If set, creates the presented child with `DataTemplate.CreateElement(Content)`. |
| `ModernContentTemplate` | If set, creates the child with a `ContentTemplateContext` containing `Content`, this presenter, and `ContentIndex`. |
| `LocalTemplateRegistry` | If set and a template resolves for `Content` and `ContentTemplateKey`, creates the child from that template. |
| `UIElement` content | Hosts the element directly. The element must not already have a logical or visual parent. |
| `string` content | Creates or reuses a generated `TextBlock` and copies the presenter's `FontFamily`, `FontSize`, `Foreground`, `ResourceProvider`, and `FontResourceId`. |
| Other content | Produces no child unless a template handles the content first. |

Changing `Content`, `ContentTemplate`, `ContentTemplateKey`, or `ModernContentTemplate` marks the presentation dirty and refreshes the child. `Content` uses reference equality for change detection, so two different object instances that compare equal still rematerialize the presented child. Generated text content is special: when the presenter already owns a generated `TextBlock`, string changes update and reuse that `TextBlock`.

`LocalTemplateRegistry` is not a UI property. Assigning it refreshes the presented child and invalidates measure, arrange, and render. `ContentIndex` marks the presentation dirty and is supplied to modern template creation contexts; it does not itself perform layout invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `ContentPresenter()` | Creates a new presenter with no content and no presented child. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ContentProperty` | `UiProperty<object?>` | Identifies the `Content` UI property. The default value is `null`; metadata affects measure and render and uses reference equality. |
| `ContentTemplateProperty` | `UiProperty<DataTemplate?>` | Identifies the `ContentTemplate` UI property. The default value is `null`; metadata affects measure and render. |
| `ContentTemplateKeyProperty` | `UiProperty<string?>` | Identifies the `ContentTemplateKey` UI property. The default value is `null`; metadata affects measure and render. |
| `ModernContentTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `ModernContentTemplate` UI property. The default value is `null`; metadata affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Content` | `object?` | Gets or sets the value to present. UI elements can be hosted directly; strings can be presented as generated `TextBlock` instances; other values require a template to produce a child. |
| `ContentTemplate` | `DataTemplate?` | Gets or sets the data template used before all other presentation paths. |
| `ContentTemplateKey` | `string?` | Gets or sets the key requested from `LocalTemplateRegistry`. |
| `ModernContentTemplate` | `ContentTemplate?` | Gets or sets the modern content template used after `ContentTemplate` and before `LocalTemplateRegistry`. |
| `LocalTemplateRegistry` | `ContentTemplateRegistry?` | Gets or sets the local registry used to resolve a `ContentTemplate` for the current content and key. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the font resource copied to generated `TextBlock` children. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the resource provider copied to generated `TextBlock` children. |
| `ContentIndex` | `int` | Gets or sets the item index passed to modern content template creation contexts. The default is `-1`. |
| `PresentedChild` | `UIElement?` | Gets the currently realized child, or `null` when the content does not produce a child. |

## Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Refreshes the presented child and returns the child's desired size, or `LayoutSize.Zero` when there is no child. |
| `ArrangeCore(ArrangeContext)` | Refreshes and arranges the presented child, then returns the final layout rectangle. |
| `OnPropertyChanged(UiPropertyChangedEventArgs)` | Refreshes presentation when one of the content or template UI properties changes. |

## Applies To

`Cerneala` retained UI controls and template infrastructure.

## See Also

- `Cerneala.UI.Controls.ContentControl`
- `Cerneala.UI.Controls.Templates.DataTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
- `Cerneala.UI.Controls.TextBlock`
