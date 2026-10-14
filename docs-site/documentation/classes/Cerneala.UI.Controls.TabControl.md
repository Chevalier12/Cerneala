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
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `TabControl`

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

Selection can also be changed through the inherited `Selector` input behavior: a left mouse button release on a prepared item container selects that container.

## Constructors

| Name | Description |
| --- | --- |
| `TabControl()` | Initializes a new instance of the `TabControl` class. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedTabItem` | `TabItem?` | Gets the selected `TabItem` item, or the realized tab container prepared for the selected item, or `null` when there is no valid selected tab item. |

## Relevant Inherited Properties

| Name | Declared by | Type | Description |
| --- | --- | --- | --- |
| `SelectedIndex` | `Selector` | `int` | Gets or sets the selected item index. The default value is `-1`; values must be greater than or equal to `-1`. |
| `SelectedItem` | `Selector` | `object?` | Gets the item at `SelectedIndex`, or `null` when the selected index is invalid. |
| `SelectionModel` | `Selector` | `SelectionModel` | Gets the selection model used by the selector. |
| `Items` | `ItemsControl` | `ItemCollection` | Gets the local item collection used when `ItemsSource` is not the active source. |
| `ItemsSource` | `ItemsControl` | `IEnumerable?` | Gets or sets an enumerable source for items. |
| `ItemsPanel` | `ItemsControl` | `Panel?` | Gets or sets the retained panel used to lay out item containers. |

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
