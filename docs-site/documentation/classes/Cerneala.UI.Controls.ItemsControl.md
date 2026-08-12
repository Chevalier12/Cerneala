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
using Cerneala.UI.Layout.Panels;

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
    <ItemsControl.Templates>
        <ContentTemplate DataType="models:TextRow">
            <TextBlock Text="$DataContext.Text" />
        </ContentTemplate>
        <ContentTemplate DataType="models:BooleanRow">
            <CheckBox IsChecked="$DataContext.Value:TwoWay" />
        </ContentTemplate>
    </ItemsControl.Templates>
</ItemsControl>
```

## Remarks

Without a component template, `ItemsControl` uses its built-in `ItemsPresenter`. A derived templated control can activate an `ItemsPresenter` supplied by its template; the fallback remains available when that template is removed.

When no `ItemsPanel` is assigned, the presenter uses one retained vertical `StackPanel`. Assign a panel directly in C# or through `ItemsControl.ItemsPanel` markup; no factory or template wrapper is required. The assigned panel is retained across item refreshes.

`ItemTemplate` has priority for item visuals. Otherwise, `DisplayMemberPath` resolves a public readable property path. Paths may contain dots, an intermediate `null` produces an empty/default presentation, and an invalid segment throws `InvalidOperationException`. An empty path uses the item itself and therefore `ToString()` for textual presentation. Accessor chains are cached per item type and path.

`ItemsSource` takes precedence over `Items`. Each assigned source is materialized once into an indexed snapshot, avoiding repeated enumeration of lazy `IEnumerable` values. Sources implementing either Cerneala `IObservableList` or standard `INotifyCollectionChanged`, including `ObservableCollection<T>`, refresh the snapshot while attached. Generated containers and recycling remain framework-managed.

Virtualization is automatic when `ItemsControl` is the content of a `ScrollViewer` and its `ItemsPanel` implements `IItemsVirtualizingPanel`. `VirtualizingStackPanel` estimates unknown rows, records measured variable heights, corrects the realization window, and receives viewport and offset updates from the viewer. Ordinary panels continue to realize every item.

`ItemContainerAspect` is applied to every prepared container at the `AspectBase` value source and removed when the container is cleared or recycled. An aspect assigned directly to an item container has higher precedence. Changing the property refreshes realized containers.

`ItemsControl` does not discover `ContentTemplate` values from resource collections. Assign one template explicitly through `ItemTemplate`, or add several owned templates to `Templates` for heterogeneous item sources. `ItemTemplate` has priority when both paths are configured.

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
| `ContentTemplateRegistry` | `ContentTemplateRegistry` | Registry used when resolving generated content templates; cannot be `null`. |
| `ItemsPanel` | `Panel?` | Retained panel used to lay out generated item containers. |
| `RealizedItemCount` | `int` | Advanced diagnostic count of currently realized containers. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable?)` | Replaces the local item collection. |
| `GetItemAt(int)` | Returns an item from the active source. |

## Protected Members

| Name | Description |
| --- | --- |
| `PrepareItemContent(...)` | Applies default content/template presentation and can be overridden by specialized containers. |
| `ActivateItemsPresenter(ItemsPresenter?)` | Activates a template presenter or restores the fallback presenter. |

## Applies to

Project: `Cerneala`

## See also

- `IItemsVirtualizingPanel`
- `VirtualizingStackPanel`
- `ComboBox`
