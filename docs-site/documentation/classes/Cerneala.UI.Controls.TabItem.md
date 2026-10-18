# TabItem Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TabItem.cs`

Represents a selectable tab container that shows its `Header`; its owning `TabControl` shows its `Content` while the tab is selected.

```csharp
public class TabItem : ContentControl, ISelectableItemContainer
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentControl` -> `TabItem`

Implements:
`ISelectableItemContainer`

## Examples

Create a tab item directly and select it.

```csharp
using Cerneala.UI.Controls;

TabItem tabItem = new()
{
    Header = "Settings",
    Content = "Preferences",
    IsSelected = true
};
```

Use a `TabItem` as an item in a `TabControl`.

```csharp
using Cerneala.UI.Controls;

TabControl tabControl = new();
TabItem tabItem = new()
{
    Header = "General",
    Content = "General settings"
};

tabControl.SetItems(new[] { tabItem });
tabControl.SelectedIndex = 0;

TabItem? selected = tabControl.SelectedTabItem;
// selected is the same instance as tabItem: a TabItem item is its own container.
// After the next layout pass the tab shows the text "General" in the header row,
// and the TabControl's content area shows "General settings".
```

Build the header and the content with templates.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

TabItem tabItem = new()
{
    Header = "Report",
    HeaderTemplate = new ContentTemplate<string>("bold-header", null, 0, context =>
        new TextBlock { Text = context.Data?.ToUpperInvariant() ?? string.Empty }),
    Content = "Q3 numbers",
    ContentTemplate = new ContentTemplate<string>("page", null, 0, context =>
        new TextBlock { Text = "Page: " + context.Data })
};
// In a TabControl, the header shows "REPORT"; while the tab is selected,
// the content area shows "Page: Q3 numbers", even when TabControl.ContentTemplate is set.
```

## Remarks

`TabItem` derives from `ContentControl`, so its page content is stored in the inherited `Content` property. The class adds a `Header` property for the tab header, `HeaderTemplate` and `HeaderTemplateKey` for presenting the header, `ContentTemplate` for presenting the content, and an `IsSelected` property used by selection controls.

`TabControl` uses `TabItem` as its default item container. When containers are prepared through `ItemsControl`, the `ISelectableItemContainer` members are updated with the realized item index, source item, and selection state. Clearing the container resets `ItemIndex` to `-1`, `Item` to `null`, and `IsSelected` to `false`. For a tab that `TabControl` generates for a data item, it also sets `Header` and `Content` from the item, `HeaderTemplate` to `ItemTemplate` only when `ItemTemplate` is not `null`, and `HeaderTemplateKey` to `ItemTemplateKey` only when `ItemTemplateKey` is not `null`, as described on `TabControl`. When `ItemTemplate` is `null`, a `HeaderTemplate` from the control's `ItemContainerAspect` applies. Clearing the container sets `Header` to `null` and removes the local `HeaderTemplate` and `HeaderTemplateKey` values (`ClearValue`), so it never writes a local `null` that would hide the aspect value.

### Header

A `TabItem` shows only its header. When it has no `ComponentTemplate`:

- When `Header` is a `UIElement` and both `HeaderTemplate` and `HeaderTemplateKey` are `null`, the `TabItem` hosts that element directly as its logical and visual child.
- Any other non-null `Header` is shown by a `ContentPresenter` that the `TabItem` hosts. `HeaderTemplate` builds the header when it is set; otherwise `HeaderTemplateKey` and data-type templates are resolved (from the `TabControl.Templates` of a generated tab, otherwise from the aspect content templates); otherwise a string is shown as a `TextBlock`. A header that is neither a string nor a `UIElement`, without a template, shows nothing. A `UIElement` header with a `HeaderTemplate` is passed to the template as data and is not hosted itself.

Setting or clearing `HeaderTemplate` or `HeaderTemplateKey` moves a `UIElement` header between the two cases: for example, after `tab.HeaderTemplate = template`, the element's `VisualParent` is `null`, and after `tab.HeaderTemplate = null` the `TabItem` hosts it again. Layout measures and arranges the header inside the control insets; the desired size is the header size plus the insets. When a `ComponentTemplate` is present, layout is delegated to the template and the `TabItem` hosts no header itself.

### Content

A `TabItem` never hosts its `Content`, also when it is a `UIElement`. The `TabControl` that owns the tab shows the content of the selected tab in its content area, with `ContentTemplate`, or `TabControl.ContentTemplate` when `ContentTemplate` is `null`. A `TabItem` outside a `TabControl`, or a tab that is not selected, shows no content. A `UIElement` content of such a tab is still owned: its `LogicalParent` is the `TabItem` and it has no visual parent. So, while `second.Content = button` and `second` is not selected, `panel.LogicalChildren.Add(button)` and `third.Content = button` throw `InvalidOperationException`, and `button.LogicalParent` is `second`. As in WPF, only the content of the selected tab is loaded: the held element is not in the `TabItem`'s `LogicalChildren` and is not attached to the root, so `button.IsLoaded` is `false` and `button` raises no `Loaded` event while its tab is not selected. While the tab is selected, the content area of the `TabControl` owns the element instead, as its logical and visual child. When the `TabControl` has a `ComponentTemplate`, the selected tab lets go of the element, so a `ContentPresenter` in the template that shows `TabControl.SelectedContent` can own it; see `TabControl`. Changing `Content` or `ContentTemplate` of the selected tab makes its `TabControl` show the change on the next layout pass.

Assigning a `UIElement` to `Header` uses the same ownership validation as `ContentControl.Content`: the element cannot be self-owned, cannot be an ancestor, cannot already have a logical or visual parent, and cannot belong to a different root.

## Constructors

| Name | Description |
| --- | --- |
| `TabItem()` | Initializes a new `TabItem` instance. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ContentTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `ContentTemplate` UI property. The default value is `null`; metadata affects measure and render. |
| `HeaderProperty` | `UiProperty<object?>` | Identifies the `Header` UI property. The default value is `null`; metadata affects measure and render. |
| `HeaderTemplateKeyProperty` | `UiProperty<string?>` | Identifies the `HeaderTemplateKey` UI property. The default value is `null`; metadata affects measure and render. |
| `HeaderTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `HeaderTemplate` UI property. The default value is `null`; metadata affects measure and render. |
| `IsSelectedProperty` | `UiProperty<bool>` | Identifies the `IsSelected` UI property. The default value is `false`; metadata affects render, input visuals, and aspect. |
| `SelectedEvent` | `RoutedEvent` | Identifies the bubbling `Selected` event. Added as an owner of `Selector.SelectedEvent`, so it is the same routed event. |
| `UnselectedEvent` | `RoutedEvent` | Identifies the bubbling `Unselected` event. Added as an owner of `Selector.UnselectedEvent`, so it is the same routed event. |

## Properties

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `ContentTemplate` | `ContentTemplate?` | `null` | Gets or sets the template the owning `TabControl` uses for this tab's `Content` while the tab is selected. It wins over `TabControl.ContentTemplate`. |
| `Header` | `object?` | `null` | Gets or sets the tab header content. A `UIElement` header without a header template is hosted directly; any other header is presented by a `ContentPresenter`. |
| `HeaderTemplate` | `ContentTemplate?` | `null` | Gets or sets the template that builds the header from `Header`. `TabControl` sets it to `ItemTemplate` on a generated tab when `ItemTemplate` is not `null`; otherwise it leaves it unset, so a value from `ItemContainerAspect` applies. |
| `HeaderTemplateKey` | `string?` | `null` | Gets or sets the key of the template that presents `Header` when `HeaderTemplate` is `null`. `TabControl` sets it to `ItemTemplateKey` on a generated tab when `ItemTemplateKey` is not `null`; otherwise it leaves it unset, so a value from `ItemContainerAspect` applies. |
| `IsSelected` | `bool` | `false` | Gets or sets whether the tab item is selected. |
| `Item` | `object?` | `null` | Gets or sets the item associated with this generated or prepared container. |
| `ItemIndex` | `int` | `-1` | Gets or sets the item index associated with this generated or prepared container. |

## Methods

`TabItem` does not declare public methods. It overrides protected layout and property-change members to coordinate header hosting, template changes, and direct layout.

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `Selected` | `RoutedEventHandler` | Raised by the tab item (`Source` = the tab item) when `IsSelected` changes to `true`. Bubbles. |
| `Unselected` | `RoutedEventHandler` | Raised by the tab item (`Source` = the tab item) when `IsSelected` changes to `false`. Bubbles. |

## Applies to

Cerneala UI controls.

## See also

- `ContentControl`
- `TabControl`
- `ISelectableItemContainer`
