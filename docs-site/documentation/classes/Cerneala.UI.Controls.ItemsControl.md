# ItemsControl Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ItemsControl.cs`

Displays generated item containers from a local collection or external enumerable source.

```csharp
public class ItemsControl : Control
```

## Examples

```csharp
using Cerneala.UI.Controls;

var people = new[] { new { Name = "Bucharest" } };
ItemsControl control = new()
{
    ItemsSource = people,
    DisplayMemberPath = "Name",
    ItemsPanel = new StackPanel()
};
```

```xml
<ItemsControl ItemsSource="$DataContext.Rows">
    <ItemsControl.ItemsPanel>
        <VirtualizingStackPanel />
    </ItemsControl.ItemsPanel>
    @templates
    {
        <ContentTemplate DataType="models:TextRow">
            <TextBlock Text="$DataContext.Text" />
        </ContentTemplate>
        <ContentTemplate DataType="models:BooleanRow">
            <CheckBox IsChecked="$DataContext.Value:TwoWay" />
        </ContentTemplate>
    }
</ItemsControl>
```

## Remarks

Without a component template, `ItemsControl` uses its built-in `ItemsPresenter`. A derived templated control can activate an `ItemsPresenter` supplied by its template; the fallback remains available when that template is removed.

When no `ItemsPanel` is assigned, the presenter uses one retained vertical `StackPanel`. Assign a panel directly in C# or through `ItemsControl.ItemsPanel` markup; no factory or template wrapper is required. The assigned panel is retained across item refreshes.

`ItemTemplate` has priority for item visuals. Otherwise, `DisplayMemberPath` resolves a public readable property path. Paths may contain dots, an intermediate `null` produces an empty/default presentation, and an invalid segment throws `InvalidOperationException`. An empty path uses the item itself and therefore `ToString()` for textual presentation. Accessor chains are cached per item type and path.

`ItemsSource` takes precedence over `Items`. Each assigned source is materialized once into an indexed snapshot, avoiding repeated enumeration of lazy `IEnumerable` values. Sources implementing either Cerneala `IObservableList` or standard `INotifyCollectionChanged`, including `ObservableCollection<T>`, refresh the snapshot while attached. Generated containers and recycling remain framework-managed.

Virtualization is automatic when `ItemsControl` is the content of a `ScrollViewer` and its `ItemsPanel` implements `IItemsVirtualizingPanel`. `VirtualizingStackPanel` estimates unknown rows, records measured variable heights, corrects the realization window, and receives viewport and offset updates from the viewer. Ordinary panels continue to realize every item.

A `VirtualizingStackPanel` assigned to `ItemsPanel` later receives the viewer's current viewport and offset in its first layout, also after an ordinary panel presented the items, or after an explicit `ItemsPresenter.VirtualizationContext` was set to `null` again. Once the control stops being the viewer's content, or the viewer's `VerticalScrollBarVisibility` becomes `Disabled`, the viewer no longer drives it, and a new `VirtualizingStackPanel` assigned after that realizes every item. For example, an `ItemsControl` with 2000 rows of height 20 in a `ScrollViewer` with a 200-pixel viewport, scrolled to offset 20000, realizes rows 999 to 1010 when `ItemsPanel` becomes a `VirtualizingStackPanel`; the same control removed from the viewer with `viewer.Content = null` and placed directly in the root realizes all 2000 rows.

`ItemContainerAspect` is assigned to every prepared container through `UIElement.AspectProperty` at the framework `TemplateBinding` source and removed when the container is cleared or recycled. This keeps the engine-owned `AspectBase` source exclusive to resolved declarations. The assigned `ElementAspect` is then resolved by the root `AspectProcessor`; it does not write container properties directly. An aspect assigned directly on the item at a higher UI-property source wins. Changing the policy refreshes realized containers, and containers created during measure are resolved in the following Aspect phase.

As in WPF, where `ContentPresenter.PrepareContentPresenter` sets `ContentTemplate` only when the `ItemsControl` has an `ItemTemplate`, preparing a generated `ContentPresenter` sets its `ContentTemplate` only when `ItemTemplate` is not `null`, and its `ContentTemplateKey` only when `ItemTemplateKey` is not `null`. So a `ContentTemplate` or `ContentTemplateKey` that `ItemContainerAspect` sets on the generated presenters applies when the matching item property is `null`. When `ItemTemplate` is set, it wins over the aspect value, because the local value has the higher source. When the container is cleared, for example when it is recycled or when `ItemTemplate` changes, the local `ContentTemplate` and `ContentTemplateKey` values are removed (`ClearValue`), so a recycled presenter prepared again without `ItemTemplate` uses the aspect's template again.

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Layout;

ContentTemplate<string> row = new("aspect-row", null, 0, context =>
    new Border { Padding = new Thickness(8, 4, 8, 4), Child = new TextBlock { Text = "Row " + context.Data } });

ItemsControl list = new()
{
    ItemsSource = new[] { "A", "B" },
    ItemContainerAspect = new ElementAspect(new[]
    {
        new ElementAspectValue(ContentPresenter.ContentTemplateProperty, row)
    })
};
// After the next layout pass: each generated ContentPresenter has ContentTemplate == row, row is called 2 times,
// and the list shows "Row A" and "Row B".
// With list.ItemTemplate = itemTemplate as well, each generated ContentPresenter has ContentTemplate == itemTemplate instead.
```

`ItemsControl` does not discover `ContentTemplate` values from resource collections. Assign one template explicitly through `ItemTemplate`, or add several owned templates to `Templates` for heterogeneous item sources. `ItemTemplate` has priority when both paths are configured.

In `.crn` markup, declare the owned collection exclusively with `@templates { ... }`. The legacy `ItemsControl.Templates` property-element wrapper is rejected by the source generator.

## Constructors

| Name | Description |
| --- | --- |
| `ItemsControl()` | Creates the local collection, generator, and fallback presenter. |

## Fields

| Name | Description |
| --- | --- |
| `DisplayMemberPathProperty` | Identifies `DisplayMemberPath`; default empty string. |
| `ItemTemplateProperty` | Identifies `ItemTemplate`; default `null`. |
| `ItemTemplateKeyProperty` | Identifies `ItemTemplateKey`; default `null`. |
| `ItemContainerAspectProperty` | Identifies `ItemContainerAspect`; default `null`. |
| `ItemsPanelProperty` | Identifies `ItemsPanel`; default `null`. |
| `ItemsSourceProperty` | Identifies `ItemsSource`; default `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DisplayMemberPath` | `string` | Dotted public property path used by default presentation. |
| `Items` | `ItemCollection` | Local items used when `ItemsSource` is null. |
| `Templates` | `Collection<ContentTemplate>` | Owned templates resolved by data type and key when `ItemTemplate` is null. |
| `ItemsSource` | `IEnumerable?` | External item source. |
| `ItemCount` | `int` | Number of items in the active source. |
| `ItemTemplate` | `ContentTemplate?` | Explicit visual template with priority over `DisplayMemberPath`. |
| `ItemContainerAspect` | `ElementAspect?` | Aspect applied to generated or prepared item containers. |
| `ItemsPanel` | `Cerneala.UI.Layout.Panels.Panel?` | Retained panel used to lay out generated item containers. |
| `RealizedItemCount` | `int` | Advanced diagnostic count of currently realized containers. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable?)` | Replaces the local item collection. |
| `GetItemAt(int)` | Returns an item from the active source. |

## Protected Members

| Name | Description |
| --- | --- |
| `DefaultContainerType` | Container type used for items that are not `UIElement` values, or for every item when `ItemTemplate` is set. Default `typeof(ContentPresenter)`. |
| `GetContainerTypeForItem(object?)` | Returns `DefaultContainerType` when `ItemTemplate` is set; otherwise the item's own type for a `UIElement` item, or `DefaultContainerType`. |
| `CreateItemContainer(int, object?)` | Returns a new `ContentPresenter` when `ItemTemplate` is set; otherwise the item itself for a `UIElement` item, or a new `ContentPresenter`. |
| `PrepareItemContainer(UIElement, int, object?)` | Applies `ItemContainerAspect`, records index, item and selection state, updates an `ISelectableItemContainer`, and calls `PrepareItemContent` unless the container is the item itself. |
| `PrepareItemContent(UIElement, int, object?)` | Applies default content/template presentation to a `ContentPresenter` or `ContentControl` container and can be overridden by specialized containers. A `ContentPresenter` gets `ContentTemplate` and `ContentTemplateKey` only when `ItemTemplate` and `ItemTemplateKey` are not `null`, so values from `ItemContainerAspect` apply otherwise. |
| `ClearItemContainer(UIElement)` | Removes the container aspect, clears the recorded item state, resets an `ISelectableItemContainer` to index `-1`, and clears presenter or content-control content unless the container is the item itself. A `ContentPresenter` container also loses its local `ContentTemplate` and `ContentTemplateKey` values (`ClearValue`). A `UIElement` item that is its own container, such as a `Button` or `TabItem` item, keeps its `Content` when it is recycled or when other items are inserted, removed or moved. |
| `IsItemSelected(int)` | Returns whether the item at an index is selected. The base implementation returns `false`. |
| `OnItemContainerPrepared(UIElement, int)` | Called after a container is prepared. The base implementation does nothing. |
| `ActivateItemsPresenter(ItemsPresenter?)` | Activates a template presenter or restores the fallback presenter. |

## Applies to

Project: `Cerneala`

## See also

- `IItemsVirtualizingPanel`
- `VirtualizingStackPanel`
- `ComboBox`
