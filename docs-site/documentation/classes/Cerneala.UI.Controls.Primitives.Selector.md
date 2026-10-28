# Selector Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Primitives/Selector.cs`

Provides selection behavior for item controls, with one or more selected items, selected item lookup, and mouse-up selection of realized item containers.

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

Every selected item follows its item. Here a `ListBox` selects `"b"` and then `"d"`, and the first item moves to the end:

```csharp
using System.Collections.ObjectModel;
using Cerneala.UI.Controls;

ObservableCollection<string> items = new(["a", "b", "c", "d"]);
ListBox listBox = new() { SelectionMode = SelectionMode.Multiple, ItemsSource = items };
listBox.SelectionModel.Select(1);
listBox.SelectionModel.Add(3);

items.Move(0, 3);
// Items: "b", "c", "d", "a"
// listBox.SelectionModel.SelectedIndices is [0, 2], listBox.SelectedItems is ["b", "d"],
// and no SelectionChanged is raised.
```

## Remarks

`Selector` builds on `ItemsControl` and keeps a private `SelectionModel` as the selection source of truth. Setting `SelectedIndex` calls `SelectionModel.Select`; when the model changes, `Selector` updates `SelectedIndexProperty`, invalidates the realized containers of every index that left or entered the selection, and re-prepares those containers so `ISelectableItemContainer` state is refreshed.

When that re-preparation throws, the exception propagates out of the `SelectedIndex` setter, and `SelectedIndex` already holds the new index. The container stays realized, `ItemContainerGenerator.GetItemIndex` returns `-1` for it, and the items stay dirty, so the next layout pass prepares it again, the same way as a failed preparation during realization. For example, a `ListBox` subclass whose `PrepareItemContent` override shows a selected item as `"a (selected)"` realizes `"a"` and `"b"`. `SelectedIndex = 0` runs while that override throws once: the setter throws, `SelectedIndex` is `0`, the row still shows `"a"`, and `GetItemIndex` returns `-1` for its container. After the next measure and arrange, with no item change, the row shows `"a (selected)"` and `GetItemIndex` returns `0`.

A failed re-preparation does not stop the rest of the selection change. `Selector` still re-prepares the other container and still raises `SelectionChanged`, and only then rethrows the first exception. For example, a `ListBox` subclass whose `PrepareItemContent` throws for `"b"` shows `"a"` and `"b"`, with `SelectedIndex == 0`. `SelectedIndex = 1` throws, but before it throws, the row of `"a"` is re-prepared with `IsSelected == false`, and `SelectionChanged` is raised once with `RemovedItems == ["a"]` and `AddedItems == ["b"]`. When only the index moves because the items changed, no `SelectionChanged` is raised, also when the re-preparation throws.

A selection change made while `Selector` sets `SelectedIndexProperty` for another selection change, for example from an `OnPropertyChanged` override that sets `SelectedIndex` again, is nested in that outer change. A failed re-preparation in the nested change does not leave the nested `SelectedIndex` setter call. The outer change still re-prepares its own containers and raises its own `SelectionChanged`, and only then the first exception propagates out of the outer `SelectedIndex` setter. For example, a `ListBox` subclass sets `SelectedIndex = 2` from `OnPropertyChanged` whenever `SelectedIndex` becomes `1`, and its `PrepareItemContent` throws for `"b"`. It shows `"a"`, `"b"`, and `"c"`, with `SelectedIndex == 0`. `SelectedIndex = 1` throws `InvalidOperationException`, and before it throws, `SelectionChanged` is raised twice, first with `RemovedItems == ["b"]` and `AddedItems == ["c"]`, then with `RemovedItems == ["a"]` and `AddedItems == ["b"]`. Afterwards `SelectedIndex` is `2`, the row of `"a"` shows `IsSelected == false`, and the row of `"c"` shows `IsSelected == true`. The same holds for a built-in subclass that re-prepares its own containers while `SelectedIndexProperty` changes, such as `ComboBox` moving its open drop-down preview to the new item, or opening, closing, or committing its drop-down because a handler of that change sets `IsDropDownOpen` or presses `Enter`: a failure there propagates only after the selection change has re-prepared its containers and raised `SelectionChanged`.

`SetValue(Selector.SelectedIndexProperty, n)`, `ClearValue`, a binding, or an aspect that changes `SelectedIndex` behaves exactly like `SelectedIndex = n`, as WPF requires of a dependency property and its CLR wrapper. `SelectionModel` selects the new index first, and the `SelectedIndexProperty` change is then notified inside that selection change: every `PropertyChanged` handler and every `OnPropertyChanged` override runs, the old and the new containers are re-prepared, `SelectionChanged` is raised, and only then a failed re-preparation propagates out of `SetValue`. For example, a `ComboBox` subclass whose `PrepareItemContent` throws for `"two"` shows `"one"`, `"two"`, and `"three"`, its rows are realized, and `"one"` is selected. A `PropertyChanged` handler sees `SelectionModel.SelectedIndex == 1` when `SelectedIndex` becomes `1`. `comboBox.SetValue(Selector.SelectedIndexProperty, 1)` throws `InvalidOperationException`, and before it throws, `SelectionChanged` is raised once with `RemovedItems == ["one"]` and `AddedItems == ["two"]`. Afterwards `SelectionModel.SelectedIndex` is `1`, `Text` is `"two"`, and the selection box shows `"two"`, with the drop-down open or closed. When a `PropertyChanged` handler closes the open drop-down because `SelectedIndex` changed, the drop-down is also closed afterwards.

The selection can hold several items. `SelectionModel.SelectedIndices` lists the selected indices in selection order, and `SelectedIndex` is the first of them, as WPF's `SelectedIndex` is the first item of `SelectedItems`. Setting `SelectedIndex` makes that index the only selected index, also when the new value equals the current `SelectedIndex`, as the Microsoft Learn remarks of WPF's `Selector.SelectedIndex` say: setting it in a selector that supports multiple selections clears the existing selected items. (WPF's own dependency property system skips a write that keeps the value, so WPF itself keeps the selection in that case.) `SetValue(Selector.SelectedIndexProperty, n)` and a `UiPropertyBinding` that transfers a new source value do the same when their local value is the effective value. For example, a `ListBox` with `SelectionMode.Multiple` has `"c"` and then `"a"` selected, so `SelectionModel.SelectedIndices` is `[2, 0]` and `SelectedIndex` is `2`. `SelectedIndex = 2` and `listBox.SetValue(Selector.SelectedIndexProperty, 2)` each make the selection `[2]` and raise `SelectionChanged` once with `RemovedItems == ["a"]`. A write that keeps `SelectedIndex` at any other value source changes nothing: a write below the effective source, such as `SetValue(Selector.SelectedIndexProperty, 2, UiPropertyValueSource.AspectBase)` while the local value is effective, and a refresh of the effective value at its own non-local source, such as a markup binding or a markup condition writing `2` again at `UiPropertyValueSource.MarkupBase`. A `UiPropertyBinding` that is activated again, for example when the `ListBox` is removed from its root and added again, writes nothing while the local value already equals the source value, so the selection stays `[2, 0]`. `SelectionModel.Add`, `Remove`, `SelectRange`, and `AddRange` change the selection without clearing it, and `Selector` handles those changes the same way as a `SelectedIndex` change. `ListBox` lets the user select several items through `ListBox.SelectionMode` and exposes them through `ListBox.SelectedItems`; `ComboBox` and `TabControl` let the user select one item. The valid no-selection value is `-1`; lower values are rejected by `SelectionModel.Select`.

`SelectionChanged` reports every item that left the selection in `RemovedItems` and every item that entered it in `AddedItems`, so each list can hold more than one item. For example, a `ListBox` with `SelectionMode.Extended` shows `"Apple"`, `"Banana"`, and `"Cherry"`, with nothing selected, and `SelectAll()` selects every item in that order. A plain click on `"Banana"` raises `SelectionChanged` once with `RemovedItems == ["Apple", "Cherry"]` and `AddedItems == []`.

Every selected item follows its item when the items change, through `Items` or through an `ItemsSource` that raises `INotifyCollectionChanged` or `IObservableList` notifications. Each row below applies to each selected item:

| Change | Result |
| --- | --- |
| Items are inserted or removed before the selected item. | Its selected index moves by the number of inserted or removed items. The item stays selected. |
| Items are inserted or removed after the selected item. | Nothing changes for that item. |
| The selected item is removed or replaced. | That item is deselected, and `SelectionChanged` reports it in `RemovedItems` with an empty `AddedItems`. The other selected items stay selected. When the last selected item was removed, `SelectedIndex` becomes `-1`, and `TabControl` then selects a neighbouring tab. |
| The selected item, or items around it, are moved. | Its selected index becomes the item's new index. |
| The items are reset (`SetItems`, `Items.Clear`, a new `ItemsSource`, or a reset notification). | If the item is still present (compared with `object.Equals`), its selected index becomes the item's index, preferring the same index when that position still holds an equal item. Otherwise the item is deselected, and `SelectionChanged` reports it in `RemovedItems`. Each selected item takes its own equal item, so equal items are matched one to one (see below). |

The selected items keep their selection order. For example, a `ListBox` with `SelectionMode.Multiple` shows `"a"`, `"b"`, `"c"`, and `"d"`, with `"b"` and then `"d"` selected (`SelectedIndices == [1, 3]`). `SetItems(new[] { "d", "x", "b" })` leaves `SelectedIndices == [2, 0]` and `SelectedItems == ["b", "d"]`. When a `ListBox` with `SelectionMode.Multiple` shows `"Apple"`, `"Banana"`, and `"Cherry"`, all selected, `Items.RemoveAt(1)` raises `SelectionChanged` once with `RemovedItems == ["Banana"]` and `AddedItems == []`, and leaves `SelectedItems == ["Apple", "Cherry"]` at the indices `[0, 1]`.

A reset matches equal items one to one. First, every selected item whose index still holds an equal item keeps that index. Then each other selected item, in selection order, takes the first equal item that no selected item took yet. A selected item that finds no free equal item is deselected and reported. For example, a `ListBox` with `SelectionMode.Multiple` shows `"a"`, `"a"`, and `"b"`, with both `"a"` items selected (`SelectedIndices == [0, 1]`). `SetItems(new[] { "b", "a", "a" })` leaves `SelectedIndices == [2, 1]` and `SelectedItems == ["a", "a"]`, without `SelectionChanged`: the second `"a"` keeps index `1`, which still holds an `"a"`, and the first `"a"` takes the free `"a"` at index `2`. `SetItems(new[] { "a", "b" })` instead leaves `SelectedIndices == [0]` and `SelectedItems == ["a"]`, and raises `SelectionChanged` once with `RemovedItems == ["a"]` and `AddedItems == []`. A selected index that refers to no item keeps its position (see below), so no selected item takes it: when `"a"` is selected at index `0` together with index `3` of a one-item list, `SetItems(new[] { "x", "x", "x", "a" })` leaves `SelectedIndices == [3]` and reports `"a"` in `RemovedItems`.

When the item change only moves selected indices and every selected item stays selected, `Selector` updates `SelectedIndex`, the selected indices, and the affected containers but does not raise the routed `SelectionChanged` event, because the selection did not change.

When the selection follows its item and the re-preparation of its container throws, the exception propagates out of the item change, for example out of `Items.RemoveAt` or out of the `ItemsSource` collection's `Remove` or `Move`. The items are already changed and `SelectedIndex` already holds the new index. The control's item view still follows the change, and the items stay dirty, so the next layout pass realizes the current items and prepares that container again. For example, an editable `ComboBox` subclass whose `PrepareItemContent` override throws for `"b2"` shows `"x"`, `"b1"`, `"c"`, and `"b2"`, the user typed `"b"`, so the drop-down shows `"b1"` and `"b2"`, and `"b2"` is selected. `Items.RemoveAt(0)` runs while that override throws once: the call throws, `SelectedIndex` is `2`, and the filtered drop-down maps to the source indexes `0` and `2`. The next layout pass completes without an exception and shows `"b1"` and `"b2"`.

A `SelectedIndex` set before its item exists refers to no item, so item changes do not move it. For example, setting `SelectedIndex = 1` on an empty selector and then adding `"a"` and `"b"` leaves `SelectedIndex == 1` and `SelectedItem == "b"`.

During item container preparation, `Selector` installs a left-button `MouseUp` handler on the container, and a `MouseDown` handler registered with `handledEventsToo` that records the left press on the container. A command source in the container, such as a `Button` or `CheckBox` that the `ItemTemplate` creates, owns its click, as WPF's `ButtonBase` marks the mouse button events handled: the handler ignores a release that completes a click (`MouseButtonEventArgs.ClickCount` greater than `0`) and whose source lies inside an `IInputCommandSource` below the container. For example, a `ListBox` with a `CheckBox` `ItemTemplate` shows `"one"` and `"two"` with nothing selected. One click on the `CheckBox` `"two"` raises its `Click` once and sets `IsChecked` to `true`, and `SelectedIndex` stays `-1`. The handler finds the command source on the route that the input system resolved the click with before any handler ran, so the click stays the command source's when its `Click` handler changes the tree. For example, when a `ListBoxItem` holds a `CheckBox` whose `Click` handler sets the row's `Content` to `"removed"`, one click on the `CheckBox` raises `Click` once, the row shows `"removed"`, `SelectedIndex` stays `-1`, and no `SelectionChanged` is raised; the same holds when the `Click` handler removes the `ListBox` from its parent. A click on text or on the empty part of the row still selects it. A release on the `CheckBox` that completes no click is not the `CheckBox`'s click, so it still selects the row, as the press on the row does in WPF: a press on the empty part of row `"two"` followed by a release on its `CheckBox` sets `SelectedIndex` to `1`, raises no `Click`, and keeps `IsChecked == false`. For any other release, the handler calls `SelectContainer`, which selects the index at which the selector's own item container generator currently realizes the container, and marks that `MouseUp` event handled, also when the selection does not change. `ListBox` calls `SelectContainer` only for a row that can take the keyboard focus, as a WPF `ListBoxItem` selects itself only when its `Focus()` succeeds: a release on a row whose `Focusable` is `false` is still handled but selects nothing (see `ListBox`). That check runs at the left press on the row, before the press moves the focus, and the release of the same press uses its result; a release whose press was not on the row checks at the release. `ComboBox` and `TabControl` select on every such release. As WPF's `ListBoxItem` and `ComboBoxItem` handle the mouse button event of their click, the release that selects an item therefore does not also click, check, or run the `Command` of a command source that contains the selector; handlers registered with `handledEventsToo` still receive it. For example, when a `ListBox` that shows `"One"` and `"Two"` is the `Content` of a `Button`, one click on `"Two"` sets `SelectedIndex` to `1`, and the `Button` raises no `Click` and does not run its `Command`; a click on the `Button` outside the `ListBox` still raises `Click` once. When a `ComboBox` is the `Content` of a `RadioButton`, a click on `"Two"` in the open drop-down selects it and closes the drop-down, and `IsChecked` stays `false`. The selected index follows inserted, removed, and moved items at once, before the next layout pass. For example, a `ListBox` shows `e0`, `e1`, `e2`, and `e3`, and a `PreviewMouseUp` handler calls `listBox.Items.RemoveAt(0)`. When the user releases the left button on the row that shows `e2`, `SelectedIndex` becomes `1` and `SelectedItem` is `e2`, although `ItemContainerGenerator.GetItemIndex` still returns `2` for that container until the next layout pass prepares it again. A container whose item was replaced keeps showing the replaced item until the next layout pass prepares it for the new item, and a click on it in that window selects nothing. For example, a `ListBox` shows `"a"`, `"b"`, and `"c"`, and `listBox.Items[1] = "x"` runs before the next layout pass. When the user releases the left button on the row that still shows `"b"`, the selection does not change: `SelectedIndex` stays `-1` and no `SelectionChanged` is raised. After the next layout pass, that row shows `"x"`, and a click on it selects index `1` with `SelectedItem == "x"`. A container whose preparation threw has not been prepared, so a click on it selects nothing until a later layout pass prepares it. For example, a `ListBox` subclass whose `PrepareItemContent` throws for `"x"` shows `"a"`, `"b"`, and `"c"`. After `listBox.Items[1] = "x"`, the next layout pass throws, and the row still shows `"b"`. When the user releases the left button on that row, `SelectedIndex` stays `-1`, no `SelectionChanged` is raised, and no exception escapes from the click, both when the preparation threw once and while it keeps throwing. After a layout pass that prepares the row for `"x"`, a click on it selects index `1` with `SelectedItem == "x"`. `ComboBox` item clicks resolve the index the same way. Container handler registrations are stored in a `ConditionalWeakTable`, so re-preparing a container does not duplicate handlers. When a container is cleared, the handler owner is detached only if it still belongs to that `Selector`, which keeps reused containers from selecting an old owner.

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
| `SelectedIndex` | `int` | Gets the first selected index in selection order, or `-1`; sets the only selected index. Setting the property selects that index through `SelectionModel`, and `SetValue(SelectedIndexProperty, n)`, a binding, or an aspect does the same. When items change, every selected index follows its item. |
| `SelectedItem` | `object?` | Gets the object at `SelectedIndex` from the active item source (`ItemsSource` when set, otherwise `Items`) when `SelectedIndex` is valid; otherwise, `null`. Inserting, removing, or moving other items does not change it. |
| `SelectionModel` | `SelectionModel` | Gets the selector's internal selection model, which holds every selected index in selection order. |

## Methods

| Name | Description |
| --- | --- |
| `IsItemSelected(int index)` | Returns whether `index` is one of the selected indices of the internal `SelectionModel`. |
| `PrepareItemContainer(UIElement container, int index, object? item)` | Prepares the inherited item container state and associates a left-button mouse-up selection handler with the container. While the container is enabled, it owns the pointer presses on it, as a WPF `ListBoxItem` handles `MouseLeftButtonDown`: an enclosing `ButtonBase`, for example a `Button` whose `Content` is the `ListBox`, is not pressed, does not repeat, and is not clicked for a press on the container. |
| `ClearItemContainer(UIElement container)` | Clears inherited item container state, detaches this selector as the handler owner for the container, and ends the container's ownership of its pointer presses. |
| `SelectContainer(UIElement container)` | Selects the index at which the selector's own item container generator currently realizes `container`; this index already follows inserted, removed, and moved items. Does nothing when the selector has not realized `container`, or when `container` still shows an item that was replaced and has not been prepared for the new item yet, or when the last preparation of `container` threw and no later preparation has succeeded. |
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
