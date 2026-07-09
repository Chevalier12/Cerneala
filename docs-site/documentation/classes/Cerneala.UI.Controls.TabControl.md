# TabControl Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TabControl.cs`

Represents an item selector that uses `TabItem` containers and exposes the currently selected tab item.

```csharp
public class TabControl : Selector
```

Inheritance:
`object` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `TabControl`

## Examples

```csharp
using Cerneala.UI.Controls;

TabItem first = new() { Header = "First" };
TabItem second = new() { Header = "Second" };

TabControl tabs = new();
tabs.SetItems(new[] { first, second });

tabs.SelectedIndex = 1;

TabItem? selected = tabs.SelectedTabItem;
// selected is the same instance as second.
```

## Remarks

`TabControl` derives from `Selector`, so selection is driven by the inherited `SelectedIndex`, `SelectedItem`, and `SelectionModel` APIs. The selected index defaults to `-1`, meaning no item is selected.

The control uses `TabItem` as its default item container. If an item is already a `TabItem`, the control reuses that item as the container. Otherwise, it creates a new `TabItem` container for the item.

`SelectedTabItem` is a computed property. It returns `null` when `SelectedIndex` is outside the current item range. When the selected container has already been realized, it returns that realized `TabItem`; otherwise, it returns the selected item only if that item is already a `TabItem`.

Selection can also be changed through the inherited `Selector` input behavior: a left mouse button release on a prepared item container selects that container.

## Constructors

| Name | Description |
| --- | --- |
| `TabControl()` | Initializes a new instance of the `TabControl` class. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedTabItem` | `TabItem?` | Gets the selected tab item or realized tab container, or `null` when there is no valid selected tab item. |

## Relevant Inherited Properties

| Name | Declared by | Type | Description |
| --- | --- | --- | --- |
| `SelectedIndex` | `Selector` | `int` | Gets or sets the selected item index. The default value is `-1`; values must be greater than or equal to `-1`. |
| `SelectedItem` | `Selector` | `object?` | Gets the item at `SelectedIndex`, or `null` when the selected index is invalid. |
| `SelectionModel` | `Selector` | `SelectionModel` | Gets the selection model used by the selector. |
| `Items` | `ItemsControl` | `ItemCollection` | Gets the local item collection used when `ItemsSource` is not the active source. |
| `ItemsSource` | `ItemsControl` | `IEnumerable?` | Gets or sets an enumerable source for items. |
| `ItemContainerGenerator` | `ItemsControl` | `ItemContainerGenerator` | Gets the generator that realizes item containers. |
| `ItemsPanel` | `ItemsControl` | `ItemsPanelTemplate?` | Gets or sets the panel template used to lay out item containers. |

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
