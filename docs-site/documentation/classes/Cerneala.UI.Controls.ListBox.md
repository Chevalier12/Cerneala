# ListBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ListBox.cs`

Displays an item list with single selection behavior and `ListBoxItem` containers.

```csharp
public class ListBox : Selector
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `ListBox`

## Examples

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new();
listBox.SetItems(new[] { "Draft", "Review", "Published" });

listBox.SelectedIndex = 1;

object? selected = listBox.SelectedItem;
```

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new()
{
    ItemsSource = new[] { "One", "Two", "Three" }
};
```

## Remarks

`ListBox` is a concrete `Selector` that configures item presentation for a vertical list. Its constructor sets `ItemsPanel` to an `ItemsPanelTemplate` that creates a `StackPanel`.

Each data item is wrapped in a `ListBoxItem` container. If an item is already a `ListBoxItem`, that same element is used as its own container. Selection is inherited from `Selector`: setting `SelectedIndex` updates the selection model, and a left mouse button release on a prepared item container selects that item.

`ListBoxItem` receives `ItemIndex`, `Item`, and `IsSelected` during container preparation. When a selected realized container is invalidated, the container render and input visual state are refreshed.

## Constructors

| Name | Description |
| --- | --- |
| `ListBox()` | Initializes a new list box and sets the default items panel to a `StackPanel`. |

## Fields

| Name | Description |
| --- | --- |
| `SelectedIndexProperty` | Inherited from `Selector`. Identifies the `SelectedIndex` UI property. |
| `ItemTemplateProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplate` UI property. |
| `ItemsPanelProperty` | Inherited from `ItemsControl`. Identifies the `ItemsPanel` UI property. |
| `ItemTemplateKeyProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplateKey` UI property. |
| `ItemsSourceProperty` | Inherited from `ItemsControl`. Identifies the `ItemsSource` UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedIndex` | `int` | Inherited from `Selector`. Gets or selects the current item index. The default is `-1`; values below `-1` are rejected by the selection model/property validation. |
| `SelectedItem` | `object?` | Inherited from `Selector`. Gets the item at `SelectedIndex`, or `null` when the selected index is outside the current item range. |
| `SelectionModel` | `SelectionModel` | Inherited from `Selector`. Gets the single-selection model used by the selector. |
| `Items` | `ItemCollection` | Inherited from `ItemsControl`. Gets the local item collection used when `ItemsSource` is not set. |
| `ItemsSource` | `IEnumerable?` | Inherited from `ItemsControl`. Gets or sets an external item source. |
| `ItemCount` | `int` | Inherited from `ItemsControl`. Gets the count from the observable item source, `ItemsSource`, or `Items`. |
| `ItemTemplate` | `DataTemplate?` | Inherited from `ItemsControl`. Gets or sets the template used to display non-container items. |
| `ItemTemplateKey` | `string?` | Inherited from `ItemsControl`. Gets or sets the template key used by content presentation. |
| `ContentTemplateRegistry` | `ContentTemplateRegistry` | Inherited from `ItemsControl`. Gets or sets the registry used to resolve content templates. |
| `ItemsPanel` | `ItemsPanelTemplate?` | Inherited from `ItemsControl`. Gets or sets the panel template used by the items presenter. `ListBox` initializes it to a `StackPanel` template. |
| `ItemContainerGenerator` | `ItemContainerGenerator` | Inherited from `ItemsControl`. Gets the generator that realizes and tracks item containers. |
| `ItemsPresenter` | `ItemsPresenter` | Inherited from `ItemsControl`. Gets the presenter that hosts generated item containers. |

## Methods

| Name | Description |
| --- | --- |
| `SetItems(IEnumerable? items)` | Inherited from `ItemsControl`. Replaces the local `Items` collection. |
| `GetItemAt(int index)` | Inherited from `ItemsControl`. Gets an item from the observable source, `ItemsSource`, or local `Items`. |
| `SetVirtualizationContext(VirtualizationContext? context)` | Inherited from `ItemsControl`. Applies a virtualization context to the items presenter and invalidates items. |
| `UpdateVirtualizationFromScrollInfo(IScrollInfo scrollInfo, float itemExtent, int cacheItems = 0)` | Inherited from `ItemsControl`. Updates scroll-based virtualization and invalidates items when the realization window changes. |

## Applies to

`Cerneala` UI controls.

## See also

- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.ListBoxItem`
