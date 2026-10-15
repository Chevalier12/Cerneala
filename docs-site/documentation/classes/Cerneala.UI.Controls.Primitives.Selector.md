# Selector Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Primitives/Selector.cs`

Provides single-index selection behavior for item controls, including selected item lookup and mouse-up selection of realized item containers.

```csharp
public class Selector : ItemsControl
```

Inheritance:  
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector`

Derived:  
`ListBox`

## Examples

```csharp
using Cerneala.UI.Controls.Primitives;

Selector selector = new();
selector.SetItems(new object?[] { "Ink", "Brush", "Paper" });

selector.SelectedIndex = 1;

object? selected = selector.SelectedItem; // "Brush"
bool hasSelection = selector.SelectionModel.HasSelection; // true

selector.Items.Insert(0, "Canvas");
// Items: "Canvas", "Ink", "Brush", "Paper"
// selector.SelectedItem is still "Brush"; selector.SelectedIndex is now 2.

selector.Items.Remove("Brush");
// selector.SelectedIndex is -1 and selector.SelectedItem is null.
```

## Remarks

`Selector` builds on `ItemsControl` and keeps a private `SelectionModel` as the selection source of truth. Setting `SelectedIndex` calls `SelectionModel.Select`; when the model changes, `Selector` updates `SelectedIndexProperty`, invalidates the previously selected and newly selected realized containers, and re-prepares those containers so `ISelectableItemContainer` state is refreshed.

When that re-preparation throws, the exception propagates out of the `SelectedIndex` setter, and `SelectedIndex` already holds the new index. The container stays realized, `ItemContainerGenerator.GetItemIndex` returns `-1` for it, and the items stay dirty, so the next layout pass prepares it again, the same way as a failed preparation during realization. For example, a `ListBox` subclass whose `PrepareItemContent` override shows a selected item as `"a (selected)"` realizes `"a"` and `"b"`. `SelectedIndex = 0` runs while that override throws once: the setter throws, `SelectedIndex` is `0`, the row still shows `"a"`, and `GetItemIndex` returns `-1` for its container. After the next measure and arrange, with no item change, the row shows `"a (selected)"` and `GetItemIndex` returns `0`.

A failed re-preparation does not stop the rest of the selection change. `Selector` still re-prepares the other container and still raises `SelectionChanged`, and only then rethrows the first exception. For example, a `ListBox` subclass whose `PrepareItemContent` throws for `"b"` shows `"a"` and `"b"`, with `SelectedIndex == 0`. `SelectedIndex = 1` throws, but before it throws, the row of `"a"` is re-prepared with `IsSelected == false`, and `SelectionChanged` is raised once with `RemovedItems == ["a"]` and `AddedItems == ["b"]`. When only the index moves because the items changed, no `SelectionChanged` is raised, also when the re-preparation throws.

Selection is single-index only. The valid no-selection value is `-1`; lower values are rejected by `SelectionModel.Select`.

The selection follows the selected item when the items change, through `Items` or through an `ItemsSource` that raises `INotifyCollectionChanged` or `IObservableList` notifications:

| Change | Result |
| --- | --- |
| Items are inserted or removed before the selected item. | `SelectedIndex` moves by the number of inserted or removed items. `SelectedItem` stays the same. |
| Items are inserted or removed after the selected item. | Nothing changes. |
| The selected item is removed or replaced. | The selection is cleared: `SelectedIndex` becomes `-1` and `SelectionChanged` reports the removed item in `RemovedItems`. When the selected item was removed, `TabControl` then selects a neighbouring tab. |
| The selected item, or items around it, are moved. | `SelectedIndex` becomes the selected item's new index. |
| The items are reset (`SetItems`, `Items.Clear`, a new `ItemsSource`, or a reset notification). | If the previous selected item is still present (compared with `object.Equals`), `SelectedIndex` becomes its index, preferring the same index when that position still holds an equal item. Otherwise the selection is cleared. |

When only the index moves and the selected item stays the same, `Selector` updates `SelectedIndex` and the affected containers but does not raise the routed `SelectionChanged` event, because the selection did not change.

When the selection follows its item and the re-preparation of its container throws, the exception propagates out of the item change, for example out of `Items.RemoveAt` or out of the `ItemsSource` collection's `Remove` or `Move`. The items are already changed and `SelectedIndex` already holds the new index. The control's item view still follows the change, and the items stay dirty, so the next layout pass realizes the current items and prepares that container again. For example, an editable `ComboBox` subclass whose `PrepareItemContent` override throws for `"b2"` shows `"x"`, `"b1"`, `"c"`, and `"b2"`, the user typed `"b"`, so the drop-down shows `"b1"` and `"b2"`, and `"b2"` is selected. `Items.RemoveAt(0)` runs while that override throws once: the call throws, `SelectedIndex` is `2`, and the filtered drop-down maps to the source indexes `0` and `2`. The next layout pass completes without an exception and shows `"b1"` and `"b2"`.

A `SelectedIndex` set before its item exists refers to no item, so item changes do not move it. For example, setting `SelectedIndex = 1` on an empty selector and then adding `"a"` and `"b"` leaves `SelectedIndex == 1` and `SelectedItem == "b"`.

During item container preparation, `Selector` installs a left-button `MouseUp` handler on the container. The handler calls `SelectContainer`, which selects the index at which the selector's own item container generator currently realizes the container. That index follows inserted, removed, and moved items at once, before the next layout pass. For example, a `ListBox` shows `e0`, `e1`, `e2`, and `e3`, and a `PreviewMouseUp` handler calls `listBox.Items.RemoveAt(0)`. When the user releases the left button on the row that shows `e2`, `SelectedIndex` becomes `1` and `SelectedItem` is `e2`, although `ItemContainerGenerator.GetItemIndex` still returns `2` for that container until the next layout pass prepares it again. A container whose item was replaced keeps showing the replaced item until the next layout pass prepares it for the new item, and a click on it in that window selects nothing. For example, a `ListBox` shows `"a"`, `"b"`, and `"c"`, and `listBox.Items[1] = "x"` runs before the next layout pass. When the user releases the left button on the row that still shows `"b"`, the selection does not change: `SelectedIndex` stays `-1` and no `SelectionChanged` is raised. After the next layout pass, that row shows `"x"`, and a click on it selects index `1` with `SelectedItem == "x"`. A container whose preparation threw has not been prepared, so a click on it selects nothing until a later layout pass prepares it. For example, a `ListBox` subclass whose `PrepareItemContent` throws for `"x"` shows `"a"`, `"b"`, and `"c"`. After `listBox.Items[1] = "x"`, the next layout pass throws, and the row still shows `"b"`. When the user releases the left button on that row, `SelectedIndex` stays `-1`, no `SelectionChanged` is raised, and no exception escapes from the click, both when the preparation threw once and while it keeps throwing. After a layout pass that prepares the row for `"x"`, a click on it selects index `1` with `SelectedItem == "x"`. `ComboBox` item clicks resolve the index the same way. Container handler registrations are stored in a `ConditionalWeakTable`, so re-preparing a container does not duplicate handlers. When a container is cleared, the handler owner is detached only if it still belongs to that `Selector`, which keeps reused containers from selecting an old owner.

`SelectedItem` reads the active item source when `SelectedIndex` is in range and returns `null` otherwise: the `ItemsSource` snapshot when `ItemsSource` is set, otherwise `Items`. For example, with `ItemsSource = new[] { "A", "B" }` and `SelectedIndex = 1`, `SelectedItem` is `"B"` even when `Items` is empty.

## Constructors

| Name | Description |
| --- | --- |
| `Selector()` | Initializes a selector and subscribes to its internal `SelectionModel.SelectionChanged` event. |

## Fields

| Name | Description |
| --- | --- |
| `SelectedIndexProperty` | Identifies the `SelectedIndex` UI property. The default value is `-1`, and values must be greater than or equal to `-1`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectedIndex` | `int` | Gets or sets the selected item index. Setting the property selects that index through `SelectionModel`. When items change, the index follows the selected item. |
| `SelectedItem` | `object?` | Gets the selected object from the active item source (`ItemsSource` when set, otherwise `Items`) when `SelectedIndex` is valid; otherwise, `null`. Inserting, removing, or moving other items does not change it. |
| `SelectionModel` | `SelectionModel` | Gets the selector's internal single-selection model. |

## Methods

| Name | Description |
| --- | --- |
| `IsItemSelected(int index)` | Returns whether the internal `SelectionModel` currently selects `index`. |
| `PrepareItemContainer(UIElement container, int index, object? item)` | Prepares the inherited item container state and associates a left-button mouse-up selection handler with the container. |
| `ClearItemContainer(UIElement container)` | Clears inherited item container state and detaches this selector as the handler owner for the container. |
| `SelectContainer(UIElement container)` | Selects the index at which the selector's own item container generator currently realizes `container`; this index already follows inserted, removed, and moved items. Does nothing when the selector has not realized `container`, or when `container` still shows an item that was replaced and has not been prepared for the new item yet, or when the last preparation of `container` threw and no later preparation has succeeded. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | Synchronizes direct `SelectedIndexProperty` changes back into the internal `SelectionModel`. |

## Property Information

| Property | Identifier field | Default value | Metadata/options | Validation |
| --- | --- | --- | --- | --- |
| `SelectedIndex` | `SelectedIndexProperty` | `-1` | `UiPropertyOptions.None` | Value must be `>= -1`. |

## Applies to

`Cerneala` targeting `net8.0`.

## See also

- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.Selection.SelectionModel`
- `Cerneala.UI.Controls.ListBox`
- `Cerneala.UI.Controls.ISelectableItemContainer`
