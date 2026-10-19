# TabControl Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TabControl.cs`

Represents an item selector that shows one `TabItem` header per item in a header row and the content of the selected tab in a content area below it.

```csharp
public class TabControl : Selector
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `TabControl`

## Examples

```csharp
using Cerneala.UI.Controls;

TabItem first = new() { Header = "First", Content = "First page" };
TabItem second = new() { Header = "Second", Content = "Second page" };

TabControl tabs = new();
tabs.SetItems(new[] { first, second });

tabs.SelectedIndex = 1;

TabItem? selected = tabs.SelectedTabItem;
// selected is the same instance as second.
// tabs.SelectedContent is "Second page". After the next layout pass the header row shows
// "First" and "Second" from left to right, and the content area below it shows "Second page".
```

Data items with an item template. As in WPF, `ItemTemplate` is the template of each generated tab's header, and `ContentTemplate` is the template of the content area.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

ContentTemplate<string> header = new("tab-header", null, 0, context =>
    new Border { Padding = new Thickness(8, 4, 8, 4), Child = new TextBlock { Text = context.Data ?? string.Empty } });
ContentTemplate<string> page = new("tab-page", null, 0, context =>
    new TextBlock { Text = "Page of " + context.Data });

TabControl tabs = new()
{
    ItemTemplate = header,
    ContentTemplate = page,
    ItemsSource = new[] { "One", "Two" },
    SelectedIndex = 0
};
// After the next layout pass: two headers built by header (header is called 2 times),
// each generated TabItem has Header == item, Content == item, and HeaderTemplate == header.
// The content area holds the TextBlock "Page of One". SelectedIndex = 1 changes it to "Page of Two".
```

## Remarks

### Header row and content area

Without a `ComponentTemplate`, `TabControl` lays out two parts:

- The header row at the top: the item containers (`TabItem`) in the `ItemsPanel`. The default `ItemsPanel` is a `StackPanel` with `Orientation.Horizontal`, so headers go from left to right. Setting `ItemsPanel` replaces it. The header row gets its desired height (at most the available height) and the full width.
- The content area below it: a `ContentPresenter` owned by the `TabControl`. It gets the full width and the remaining height. It shows `SelectedContent` with `SelectedContentTemplate`. A `TabItem` never shows its own `Content`; only the selected tab's content is shown, here.

The desired size is the wider of the two parts, and the header row height plus the content area height. Neither part applies `Padding` or `BorderThickness`, and `TabControl` draws no background itself. When the control has a `ComponentTemplate`, layout goes to the template, and the built-in header row and content area are not laid out.

The content area is refreshed during measure, after the tabs are realized. Changing `SelectedIndex`, `ContentTemplate`, or the `Content` or `ContentTemplate` of the selected tab invalidates measure, so the next layout pass shows the change. `SelectedContent` and `SelectedContentTemplate` are computed from the selected tab, so they return the new values at once.

When `Content` is a `UIElement`, that element is a child of the content area while its tab is selected. When another tab is selected, the next layout pass removes it from the content area and gives it back to its `TabItem`, which owns it as its `LogicalParent` until its tab is selected again. As in WPF, only the content of the selected tab is loaded: the element raises `Loaded` when its tab is shown and `Unloaded` once when another tab is selected, and the content of a tab that is not selected raises neither. For example, with `first.Content = button` and `SelectedIndex = 0`, `button` is under the `TabControl` after layout and has raised `Loaded` once; after `SelectedIndex = 1` and the next layout pass, `button` has raised `Unloaded` once, `button.IsLoaded` is `false`, `button.VisualParent` is `null` and `button.LogicalParent` is `first`, so adding `button` to another parent throws `InvalidOperationException`.

### Element content with a ComponentTemplate

When the control has a `ComponentTemplate` (or a `ComponentTemplateKey`), the template shows the selected tab's content, for example with a `ContentPresenter` whose `Content` is `SelectedContent`. Then the selected tab lets go of a `UIElement` content, so a `ContentPresenter` in the template can take it as its logical and visual child. The built-in content area is emptied when the template is set, and the selected tab lets go of its content before the template is built. When another tab is selected, the newly selected tab lets go of its content at once, and every other tab owns its `UIElement` content again (as its `LogicalParent`) as soon as nothing shows it: at the selection change, or at the next layout pass when the template still showed it then. When the template is replaced or removed, the old template lets go of the tabs' content, and without a template the content area shows the selected tab's content again on the next layout pass.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

Border page1 = new() { Width = 50, Height = 20 };
Border page2 = new() { Width = 50, Height = 20 };
TabItem first = new() { Header = "1", Content = page1 };
TabItem second = new() { Header = "2", Content = page2 };
TabControl tabs = new();
tabs.SetItems(new[] { first, second });
tabs.SelectedIndex = 0;

ContentPresenter? presenter = null;
tabs.ComponentTemplate = new ComponentTemplate<TabControl>("tabs", context =>
    presenter = new ContentPresenter { Content = context.Owner.SelectedContent });
tabs.SelectionChanged += (_, _) => presenter!.Content = tabs.SelectedContent;
// After the next layout pass: page1.LogicalParent and page1.VisualParent are presenter,
// page2.LogicalParent is second and page2.VisualParent is null.

tabs.SelectedIndex = 1;
// After the next layout pass: page2 is under presenter, page1.LogicalParent is first and
// page1.VisualParent is null, so adding page1 to another parent throws InvalidOperationException.
```

The selected tab lets go of its content only for the template to take it. When nothing in the template took it by the end of the next layout pass, the selected `TabItem` owns it again as its `LogicalParent`. The same happens when an aspect template for `ComponentTemplateKey` replaces the old template: the selected tab lets go of its content before the new template is built. For example, with `tabs.ComponentTemplate = new ComponentTemplate<TabControl>("plain", _ => new Border())`, `SelectedIndex = 1` and the next layout pass, `page2.LogicalParent` is `second` and `page2.VisualParent` is `null`, so `other.Children.Add(page2)` throws `InvalidOperationException` with the message `Element must be removed from its current parent before reparenting.`

The content area owns the element only when it shows it. When the template of the content area (`SelectedContentTemplate`, or an aspect content template for the content) builds an element that does not contain the selected tab's `UIElement` content, the selected `TabItem` keeps that content as its logical child after layout. For example, with `tabs.ContentTemplate = new ContentTemplate("page", typeof(Border), null, 0, _ => new Border { Width = 10, Height = 10 })`, `first.Content = border` and `SelectedIndex = 0`, the content area shows the new 10 x 10 `Border`; `border.LogicalParent` is `first` and `border.VisualParent` is `null`, so adding `border` to another parent, or making it the `Content` of another tab, throws `InvalidOperationException`. After `tabs.ContentTemplate = null` and the next layout pass, the content area shows `border` itself.

The content area resolves templates like a `ContentPresenter` with no local registry: `SelectedContentTemplate` first, then the aspect content templates for the content. A string without a template is shown as a `TextBlock`. Other content without a template, for example an `int`, shows nothing.

### Generated tabs

For an item that is not a `TabItem`, the control generates a `TabItem` and prepares it as WPF prepares a headered container:

| Member of the generated `TabItem` | Value |
| --- | --- |
| `HeaderTemplate` | `ItemTemplate`, as a local value, only when `ItemTemplate` is not `null`. Otherwise it is not set. |
| `HeaderTemplateKey` | `ItemTemplateKey`, as a local value, only when `ItemTemplateKey` is not `null`. Otherwise it is not set. |
| `Header` | the item when `ItemTemplate` is set; otherwise the item's `DisplayMemberPath` value. `null` when the item is a `UIElement`, because an element cannot be both header and content. |
| `Content` | the item |

As in WPF, where `ItemContainerStyle` can set the header template of a generated container when the `ItemsControl` has no `ItemTemplate`, a `HeaderTemplate` or `HeaderTemplateKey` that `ItemContainerAspect` sets on the generated tabs applies when `ItemTemplate` (or `ItemTemplateKey`) is `null`. When `ItemTemplate` is set, it wins over the aspect value, because the local value has the higher source.

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

ContentTemplate<string> header = new("aspect-header", null, 0, context =>
    new Border { Padding = new Thickness(8, 4, 8, 4), Child = new TextBlock { Text = "Tab " + context.Data } });

TabControl tabs = new()
{
    ItemsSource = new[] { "A", "B" },
    ItemContainerAspect = new ElementAspect(new[]
    {
        new ElementAspectValue(TabItem.HeaderTemplateProperty, header),
        new ElementAspectValue(Control.PaddingProperty, new Thickness(6))
    })
};
// After the next layout pass: each generated TabItem has Padding == Thickness(6) and HeaderTemplate == header,
// header is called 2 times, and the header row shows "Tab A" and "Tab B".
// With tabs.ItemTemplate = itemTemplate as well, each generated TabItem has HeaderTemplate == itemTemplate instead.
```

The header of a generated tab resolves `HeaderTemplateKey` and data-type templates from the control's `Templates` collection, as other item containers do. When the container is cleared, for example when it is recycled or when `ItemTemplate` changes, `Header` is reset to `null`, and the local `HeaderTemplate` and `HeaderTemplateKey` values are removed (`ClearValue`), so a recycled tab prepared again without `ItemTemplate` uses the aspect's `HeaderTemplate` again.

A `TabItem` item is its own container: it keeps its own `Header`, `HeaderTemplate`, `HeaderTemplateKey`, and `Content`, and `ItemTemplate` is not applied to it. For example, `new TabItem { Header = "H1", Content = "C1" }` in a `TabControl` with an `ItemTemplate` shows the text `H1` in its header and never calls the item template.

### Selection

`TabControl` derives from `Selector`, so selection is driven by the inherited `SelectedIndex`, `SelectedItem`, and `SelectionModel` APIs. The selected index defaults to `-1`, meaning no item is selected.

The control uses `TabItem` as its default item container. If an item is already a `TabItem`, the control reuses that item as the container. Otherwise, it creates a new `TabItem` container for the item.

`SelectedTabItem` is a computed property. It reads the same active item source as `SelectedItem`: `ItemsSource` when it is set, otherwise `Items`. It returns `null` when `SelectedIndex` is outside the range of `ItemCount`. When the selected item is a `TabItem`, it returns that item, because a `TabItem` item is its own container. For example, with `ItemsSource = new[] { first, second }` of `TabItem` values and `SelectedIndex = 1`, it returns `second`. For any other item, it returns the realized `TabItem` container prepared for the selected item, or `null` when no container has been realized for it.

Realized containers keep their previous indexes until the next layout pass, so `SelectedTabItem` matches the container by its item, not by its old index. With `TabItem` items, it is always the same instance as `SelectedItem`, before and after layout, and never a tab that was removed from `Items`. With other items, it is a container whose recorded item equals `SelectedItem`. When several items are equal, for example two `"A"` strings, the container chosen before the next layout pass can be the one prepared for the other equal item; after the layout pass it is the container realized at `SelectedIndex`.

```csharp
TabControl tabs = new();
tabs.SetItems(new[] { "A", "B", "C" });
// The first layout pass realizes a TabItem container for each string.
tabs.SelectedIndex = 0;

tabs.Items.Insert(0, "Z");
// tabs.SelectedIndex is 1 and tabs.SelectedItem is "A".
// Before the next layout pass, tabs.SelectedTabItem is still the container prepared for "A",
// not the container that the previous layout pass realized at index 1 for "B".
```

Inserting, removing, or moving other tabs keeps the selected tab selected, as described for `Selector`. Removing the selected tab does not leave the control without a selected tab: as in WPF, `Selector` first clears the selection, and `TabControl` then selects the tab that took the removed tab's place. When the removed tab was the last one, it selects the previous tab. The search continues backwards and wraps to the last tab. It skips a `TabItem` item that is disabled (`IsEnabled == false`) or not `Visibility.Visible`. If no tab qualifies, `SelectedIndex` stays `-1`. Each step raises `SelectionChanged`: first with the removed tab in `RemovedItems`, then with the new tab in `AddedItems`. Replacing the selected tab or resetting the items does not select a neighbouring tab.

```csharp
TabItem a = new() { Header = "A" };
TabItem b = new() { Header = "B" };
TabItem c = new() { Header = "C" };
TabControl tabs = new();
tabs.SetItems(new[] { a, b, c });
tabs.SelectedIndex = 1;

tabs.Items.RemoveAt(1);
// tabs.SelectedIndex is 1 and tabs.SelectedItem is c.

tabs.Items.RemoveAt(1);
// c was the last tab: tabs.SelectedIndex is 0 and tabs.SelectedItem is a.
```

This also holds when a container re-preparation throws during the removal. Clearing the selection re-prepares the realized container at the removed tab's old index, and that container can already hold the tab that took its place. When that re-preparation throws, `TabControl` still selects the next tab, and the first exception propagates out of the item change only after that, for example out of `Items.RemoveAt` or out of the `ItemsSource` collection's `Remove`. When selecting the next tab throws too, `SelectedIndex` still holds the next tab's index, as described for `Selector`, and the first exception is the one that propagates. For example, a `TabControl` subclass whose `PrepareItemContent` throws for `"C"` shows `"A"`, `"B"`, and `"C"`, with `SelectedIndex == 1`. `Items.RemoveAt(1)` throws `InvalidOperationException`, and before it throws, `SelectionChanged` is raised twice: first with `RemovedItems == ["B"]` and `AddedItems == []`, then with `RemovedItems == []` and `AddedItems == ["C"]`. Afterwards `SelectedIndex` is `1` and `SelectedItem` is `"C"`, and the next layout pass prepares the tab of `"C"` again.

Selection can also be changed through the inherited `Selector` input behavior: a left mouse button release on a prepared item container selects that container.

## Constructors

| Name | Description |
| --- | --- |
| `TabControl()` | Initializes a new instance of the `TabControl` class with a horizontal `StackPanel` as its `ItemsPanel` and an empty content area. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ContentTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `ContentTemplate` UI property. The default value is `null`; metadata affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ContentTemplate` | `ContentTemplate?` | Gets or sets the template of the content area for a selected tab whose `TabItem.ContentTemplate` is `null`. The default value is `null`. |
| `SelectedContent` | `object?` | Gets the `Content` of `SelectedTabItem`, or `null` when `SelectedTabItem` is `null`. For a data item whose tab has not been realized yet, this is `null` until the next layout pass. |
| `SelectedContentTemplate` | `ContentTemplate?` | Gets the template the content area uses: `SelectedTabItem.ContentTemplate` when it is not `null`, otherwise `ContentTemplate`; `null` when `SelectedTabItem` is `null`. |
| `SelectedTabItem` | `TabItem?` | Gets the selected `TabItem` item, or the realized tab container prepared for the selected item, or `null` when there is no valid selected tab item. |

## Relevant Inherited Properties

| Name | Declared by | Type | Description |
| --- | --- | --- | --- |
| `SelectedIndex` | `Selector` | `int` | Gets or sets the selected item index. The default value is `-1`; values must be greater than or equal to `-1`. |
| `SelectedItem` | `Selector` | `object?` | Gets the item at `SelectedIndex`, or `null` when the selected index is invalid. |
| `SelectionModel` | `Selector` | `SelectionModel` | Gets the selection model used by the selector. |
| `Items` | `ItemsControl` | `ItemCollection` | Gets the local item collection used when `ItemsSource` is not the active source. |
| `ItemsSource` | `ItemsControl` | `IEnumerable?` | Gets or sets an enumerable source for items. |
| `ItemsPanel` | `ItemsControl` | `Panel?` | Gets or sets the retained panel used to lay out the tab headers. `TabControl` sets it to a horizontal `StackPanel`. |
| `ItemTemplate` | `ItemsControl` | `ContentTemplate?` | Gets or sets the template that becomes the `HeaderTemplate` of each generated tab. When it is `null`, the generated tab's `HeaderTemplate` is not set, so a value from `ItemContainerAspect` applies. |
| `ItemTemplateKey` | `ItemsControl` | `string?` | Gets or sets the key that becomes the `HeaderTemplateKey` of each generated tab. When it is `null`, the generated tab's `HeaderTemplateKey` is not set, so a value from `ItemContainerAspect` applies. |
| `Templates` | `ItemsControl` | `Collection<ContentTemplate>` | Gets the templates the headers of generated tabs resolve against. |

## Relevant Inherited Methods

| Name | Declared by | Description |
| --- | --- | --- |
| `SetItems(IEnumerable? items)` | `ItemsControl` | Replaces the local item collection with the supplied items. |
| `GetItemAt(int index)` | `ItemsControl` | Returns the item at the specified index from the active item source. |

## Applies To

`Cerneala` UI controls.

## See Also

- `Cerneala.UI.Controls.TabItem`
- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Controls.ItemsControl`
