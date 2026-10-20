# ListBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ListBox.cs`

Displays a scrollable list of `ListBoxItem` rows in which the user selects one item or, depending on `SelectionMode`, several items with the mouse and the keyboard.

```csharp
[TemplatePart("PART_ScrollViewer", typeof(ScrollViewer))]
[TemplatePart("PART_ItemsPresenter", typeof(ItemsPresenter))]
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

object? selected = listBox.SelectedItem; // "Review"
```

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new()
{
    ItemsSource = new[] { "One", "Two", "Three" }
};
```

With `SelectionMode.Extended`, the user selects a range with Shift+click. Here the user clicks `Apple`, then holds Shift and clicks `Cherry`. Afterwards `SelectedItems` is `["Apple", "Banana", "Cherry"]`, `SelectedIndex` is `0`, and every row's `IsSelected` is `true`. The Shift+click raises one `SelectionChanged` with `AddedItems` `["Banana", "Cherry"]` and empty `RemovedItems`. A plain click on `Banana` after that selects only `Banana`: `SelectedItems` is `["Banana"]`, and one `SelectionChanged` reports `RemovedItems` `["Apple", "Cherry"]`.

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new() { SelectionMode = SelectionMode.Extended };
listBox.SetItems(new[] { "Apple", "Banana", "Cherry" });
```

The keyboard moves the focus and the selection from row to row. In the default `SelectionMode.Single`, the user clicks `Apple` and presses Down: `SelectedIndex` becomes `1` (`Banana`) and the `Banana` row has the keyboard focus. End then selects `Cherry` (`SelectedIndex` `2`), and Home selects `Apple` again (`SelectedIndex` `0`).

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new();
listBox.SetItems(new[] { "Apple", "Banana", "Cherry" });
```

`ScrollIntoView` scrolls the list until a row is visible. Here the list is 100 pixels high and the aspect makes each of its 20 rows 20 pixels high, so the viewport shows the rows `0` to `4`. Row `15` ends at `320`, so after `ScrollIntoView("15")` and the next layout the template's `ScrollViewer` has the vertical offset `220`, and row `15` is the last visible row. A following `ScrollIntoView("2")` scrolls back until row `2` is the first visible row: the offset becomes `40`.

```csharp
using System.Linq;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

ListBox listBox = new()
{
    Width = 150,
    Height = 100,
    ItemContainerAspect = new ElementAspect([new ElementAspectValue(UIElement.HeightProperty, 20f)])
};
listBox.SetItems(Enumerable.Range(0, 20).Select(index => index.ToString()).ToArray());

listBox.ScrollIntoView("15");
```

Type-ahead selects the row whose text starts with the typed letters. In the list `Apple`, `Banana`, `Cherry`, typing `c` selects `Cherry`. Typing `b` and then, within one second, `a` searches for `ba` and keeps `Banana` selected, where `a` alone would select `Apple`. With `IsTextSearchEnabled = false`, typing selects nothing.

```csharp
using Cerneala.UI.Controls;

ListBox listBox = new() { IsTextSearchCaseSensitive = false };
listBox.SetItems(new[] { "Apple", "Banana", "Cherry" });
```

Each row shows its item through `ItemTemplate`. Here the rows read `[Alpha]` and `[Beta]`, and each row's `Content` stays the item string.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ListBox listBox = new()
{
    ItemsSource = new List<string> { "Alpha", "Beta" },
    ItemTemplate = new ContentTemplate<string>("Bracketed", key: null, priority: 0, context =>
        new TextBlock { Text = "[" + context.Data + "]" })
};
```

`Background` fills the whole list area behind the rows, and `BorderBrush` draws a border around it. Here a list arranged at `(3, 4, 80, 60)` draws one red `FillRectangle` at `(3, 4, 80, 60)` first, then one blue `DrawRectangle` (thickness 2) at `(3, 4, 80, 60)`, then the row of `One` on top of them. The row is arranged at `x = 15`, `y = 16` with width 56: 2 + 10 = 12 pixels inside each edge.

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

ListBox listBox = new()
{
    Background = new SolidColorBrush(Color.Red),
    BorderBrush = new SolidColorBrush(Color.Blue),
    BorderThickness = new Thickness(2),
    Padding = new Thickness(10)
};
listBox.Items.Add(new ListBoxItem { Content = "One" });
```

## Remarks

`ListBox` is a concrete `Selector` that configures item presentation for a vertical list. Its constructor assigns a retained vertical `StackPanel` directly to `ItemsPanel` and applies the default component template.

Each data item is wrapped in a `ListBoxItem` container. If an item is already a `ListBoxItem`, that same element is used as its own container. Selection is inherited from `Selector`: setting `SelectedIndex` selects that item only, and `SelectionModel` holds every selected index in selection order. A left mouse button release on a prepared item container selects as described in [Mouse selection](#mouse-selection). A release on a button in the row, such as a `CheckBox` from the `ItemTemplate`, that completes the button's click only clicks that button and does not select the row. A release on the button after a press elsewhere in the row completes no click, so it selects the row and leaves the button unchanged (see `Selector`).

A click selects a row only when the row can take the keyboard focus, as WPF's `ListBoxItem.OnMouseLeftButtonDown` notifies the `ListBox` only when the item's `Focus()` succeeds. A row can take the focus when it is attached, `Focusable`, enabled, and visible. This is decided at the left press, before the press moves the focus, and the release of that press uses that answer: a row that a `GotFocus` handler sets to `Focusable = false` during the press is still selected by the release. A release without a press on the row decides at the release. A click on a row whose `Focusable` is `false` leaves `SelectedIndex`, `SelectedItem`, and every row's `IsSelected` unchanged and raises no `SelectionChanged`. The release is still handled, so an enclosing `Button` is not clicked by it. For example, with rows `a` (`Focusable = false`) and `b`, and `SelectedIndex = 1`, a click on `a` leaves `SelectedIndex` at `1` and `a.IsSelected` at `false`; a click on `b` (focusable, the default) selects and focuses `b`. `ComboBox` does not follow this rule: a click on a `ComboBoxItem`, which is not focusable, still selects it and closes the drop-down, as WPF's `ComboBoxItem` does.

A left press on a row moves keyboard focus to that row, because `ListBoxItem` is focusable, as in WPF. For example, a `Button` has the focus and the user clicks the first row: the row's `IsKeyboardFocused` becomes `true`, the `ListBox`'s `IsKeyboardFocusWithin` becomes `true`, and a Space press after the click does not click the `Button`. The `ListBox` itself does not take the focus from a press on a row, also when its `Focusable` is `true`: a row whose `Focusable` is `false` leaves the focus where it was. A press on a focusable element inside the row, such as a `TextBox` from the `ItemTemplate`, focuses that element. See [`ListBoxItem`](Cerneala.UI.Controls.ListBoxItem.md#keyboard-focus-on-a-click).

`ListBoxItem` receives `ItemIndex`, `Item`, and `IsSelected` during container preparation. When a selected realized container is invalidated, the container render and input visual state are refreshed.

Row content follows `ItemsControl`: without `ItemTemplate`, a row's `Content` is the item's `DisplayMemberPath` value (the item itself when the path is empty); with `ItemTemplate`, it is the item. The row shows a `string` value as text, and `ListBox` passes `ItemTemplate`, `ItemTemplateKey`, and `Templates` to the row, so a template creates the row's visual. For example, `SetItems(new[] { "Apple" })` creates a row whose `Content` is `"Apple"` and whose text reads `Apple`; the row is as tall as that text. See [`ListBoxItem`](Cerneala.UI.Controls.ListBoxItem.md#content-presentation) for every content kind. An item that is already a `ListBoxItem` is its own row and does not use the item template. Any other `UIElement` item is wrapped in a `ListBoxItem` row: with `ItemTemplate`, the row shows the element that the template creates for it (the item is `context.Data` and its index is `context.Index`), as in WPF; without `ItemTemplate`, the row shows the element that a template of `Templates` creates for it when one matches the element and `ItemTemplateKey`, and otherwise hosts the element directly.

### Default template

As the WPF `ListBox` template, the default `ComponentTemplate` is a `Border` around a `ScrollViewer` (the part `PART_ScrollViewer`) around an `ItemsPresenter` (the part `PART_ItemsPresenter`), which hosts the rows. The `Border` receives the list's `Background`, `BorderBrush`, `BorderThickness`, and `Padding` through `UiPropertyValueSource.TemplateOwnerBinding`. The `ScrollViewer` has `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` set to `ScrollBarVisibility.Auto`, so a scroll bar shows only when the rows do not fit on that axis. For example, a list 150 pixels wide and 100 pixels high with three rows of 20 pixels shows no scroll bar, and each row is 150 pixels wide; the same list with 20 rows shows the vertical scroll bar.

`Background` fills the list's arranged bounds, `BorderBrush` draws a border around them (as `Border` does, with the largest side of `BorderThickness` as its thickness), and the rows draw on top. Without `Background` (the default, `null`) the list draws no fill, so the area behind it shows through; without `BorderBrush` or with a zero `BorderThickness` it draws no border. The rows are laid out inside `BorderThickness + Padding`, and `DesiredSize` includes both. With the defaults (`Thickness.Zero`) the rows touch the list edges. `Background` and `BorderBrush` are registered with `AffectsRender`, so changing them redraws the list without a new measure or arrange; `BorderThickness` and `Padding` also affect measure.

The list desires the size of its rows plus `BorderThickness + Padding`, but not more than the available size, because the template's `ScrollContentPresenter` desires the smaller of the available size and the extent on each axis. For example, a `ListBox` whose `ItemTemplate` makes two rows of 80 x 10 in a 100 x 100 root desires 80 x 20. When the rows need more than the available size, the list desires the available size and its `ScrollViewer` shows the scroll bar.

`ListBox` reads both parts with `GetOptionalTemplatePart`, so a custom `ComponentTemplate` can provide them. `PART_ItemsPresenter` hosts the rows. `PART_ScrollViewer` is the viewer that `ScrollIntoView`, the arrow keys, and PageUp/PageDown scroll. With `ComponentTemplate = null`, the list has no template: it lays out its rows itself inside `BorderThickness + Padding`, draws `Background` and `BorderBrush` itself as `ItemsControl` does, and does not scroll. For example, such a list arranged at `(3, 4, 40, 30)` with `BorderThickness = 2` and `Padding = 10` arranges its row at `(15, 16)` with width 16, and no `ScrollViewer` is created.

A `ListBox` that is the content of an outer `ScrollViewer`, with a `VirtualizingStackPanel` as `ItemsPanel`, realizes only the rows in the outer viewport, as an `ItemsControl` does, also with the default template; the top `BorderThickness` and `Padding` move the rows down by that amount. For example, 100 rows 10 pixels high in an outer viewer 30 pixels high realize at most 8 rows; at vertical offset `45` row `0` is not realized and row `4` is, or row `2` when `Padding.Top` is `20`.

The `ScrollViewer` of the template also gives its viewport to a `VirtualizingStackPanel`, as WPF's `ScrollContentPresenter` scrolls the panel of its `ItemsPresenter` content. It does so while that `ScrollViewer` was measured with a finite height, for example because the list has a fixed `Height`. Then the list realizes only the rows in the template's viewport, also inside an outer `ScrollViewer`: the template's viewport wins over the outer one. For example, the list below shows 10 rows when each row is 20 pixels high; after the user presses End, only the rows `1989` to `1999` exist, and row `1999` is selected and visible. A list without a fixed `Height` that is the content of an outer `ScrollViewer` measures its template's `ScrollViewer` with an unbounded height, so the outer viewport applies, as in the paragraph above.

```csharp
ListBox listBox = new()
{
    Height = 200,
    ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel()
};
listBox.SetItems(Enumerable.Range(0, 2000).Select(index => index.ToString()));
```

In such a list the keys and `ScrollIntoView` also reach rows that have no container yet, as WPF's `VirtualizingStackPanel.BringIndexIntoView`. End, Home, PageUp, PageDown, the arrow keys, and `ScrollIntoView` first scroll to the place that the panel estimates for the row: above the viewport the row goes to the top, below it to the bottom. After the next arrange, which creates the row, the list scrolls it to its exact place, and a key gives it the focus then. When the rows have different heights, the rows measured at the new place change the estimate, so the list scrolls again after each arrange that follows a scroll, until a scroll does not move the viewport. For example, in a list 100 pixels high with 10,000 rows 10 pixels high, `ScrollIntoView("5000")` and one frame show row `5000` inside the viewport, and at most 20 rows exist.

The list scrolls a row into view as WPF does: it calls `UIElement.BringIntoView` with the row's place in the panel, and each `ScrollViewer` around the row scrolls by the least amount that shows it (see `ScrollContentPresenter.MakeVisible`). So the keys and `ScrollIntoView` also scroll an outer `ScrollViewer`, for example for a list without a fixed `Height`, whose template's `ScrollViewer` cannot scroll. In the example below the outer viewport is 300 pixels high and the 10,000 rows are 10 pixels high. After `ScrollIntoView("9999")` and two frames, the outer `ScrollViewer` has scrolled once, to the vertical offset `99700`, and row `9999` is realized at the bottom of the outer viewport. When the template's `ScrollViewer` and an outer one both move during a scroll, the list scrolls again after the next arrange until none of them moves.

```csharp
ListBox listBox = new()
{
    Width = 150,
    ItemsPanel = new Cerneala.UI.Layout.Panels.VirtualizingStackPanel(),
    ItemsSource = Enumerable.Range(0, 10_000).Select(index => index.ToString()).ToArray()
};
ScrollViewer outer = new() { Content = listBox, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
// The rows are 10 pixels high and the outer viewer is 300 pixels high.
listBox.ScrollIntoView("9999");
// After two frames: outer.Presenter.VerticalOffset is 99700.
```

### Selection modes

`SelectionMode` decides what the user's clicks and keys select. The default is `SelectionMode.Single`. `SelectedItems` lists the selected items in selection order, and `SelectedItem` and `SelectedIndex` are the first of them. For example, in `SelectionMode.Multiple` a click on `Cherry` and then a click on `Apple` gives `SelectedItems` `["Cherry", "Apple"]` and `SelectedIndex` `2`.

Changing `SelectionMode` to `Single` while several items are selected keeps only the first selected item. In the example above, `SelectionMode = SelectionMode.Single` leaves `SelectedItems` `["Cherry"]` and `SelectedIndex` `2`. `SelectionMode` limits only the user's input and `SelectAll`: `SelectionModel.Add` still adds an index in `Single` mode.

`SelectAll()` selects every item when `SelectionMode` is `Multiple` or `Extended`, through `SelectionModel.AddRange(0, ItemCount - 1)`: the items that were selected keep their order, and the others follow in index order. In `Single` mode it throws `NotSupportedException` and leaves the selection unchanged, as WPF's `ListBox.SelectAll` does. `UnselectAll()` deselects every item in every mode; afterwards `SelectedIndex` is `-1` and `SelectedItems` is empty.

### Mouse selection

| Mode | Click | Ctrl+click | Shift+click | Ctrl+Shift+click |
| --- | --- | --- | --- | --- |
| `Single` | Selects only the clicked row. | Deselects the clicked row when it is selected (`SelectedIndex` becomes `-1`); otherwise selects only it. | As a click. | As Ctrl+click. |
| `Multiple` | Selects the clicked row when it is not selected, deselects it when it is; the other rows stay as they were. | As a click. | As a click. | As a click. |
| `Extended` | Selects only the clicked row and makes it the anchor. | Selects or deselects the clicked row, keeps the others, and makes it the anchor. | Selects the rows from the anchor to the clicked row, in place of the selection. | Adds the rows from the anchor to the clicked row to the selection. |

The anchor follows its item when items are inserted, removed, or moved. Without an anchor, the last item in selection order is the anchor, or the first item when nothing is selected. For example, in `Extended` mode with the items `A` to `E`, a click on `A`, a Ctrl+click on `D`, and a Ctrl+Shift+click on `E` give `SelectedItems` `["A", "D", "E"]`. One click raises one `SelectionChanged` with every item it selected in `AddedItems` and every item it deselected in `RemovedItems`.

### Keyboard

The `ListBox` handles the key events that bubble up to it from the focused element inside it, usually its focused row. It ignores a key that is already handled, a key with Alt held, and every key while the list is disabled or has no items.

Up, Down, Home, End, PageUp, and PageDown move from the focused row, or from `SelectedIndex` when no row has the focus; with neither, they move to row `0`. Up on the first row and Down on the last row change nothing. The list scrolls the new row into view, moves the keyboard focus to it, and selects by mode:

| Mode | No modifier | Shift | Ctrl+Shift | Ctrl |
| --- | --- | --- | --- | --- |
| `Single` | Selects the new row. | Selects the new row. | Moves only the focus. | Moves only the focus. |
| `Multiple` | Moves only the focus. | Moves only the focus. | Moves only the focus. | Moves only the focus. |
| `Extended` | Selects only the new row and makes it the anchor. | Selects the rows from the anchor to the new row, in place of the selection. | Adds the rows from the anchor to the new row to the selection. | Moves only the focus. |

For example, in `Extended` mode with the items `A` to `E`, a click on `B` and Shift+Down twice give `SelectedIndices` `[1, 2, 3]` with the focus on `D`; Shift+Up gives `[1, 2]`, and Down without a modifier then gives `[3]`. When rows are 20 pixels high in a list 80 pixels high, Down on the focused row `3`, the last visible row, selects row `4` and scrolls the vertical offset to `20`.

Space acts on the focused row as a click with the same modifiers: in `Multiple` mode it selects or deselects that row, and in `Extended` mode Ctrl+Space selects or deselects it. Ctrl+A calls `SelectAll` in `Multiple` and `Extended` mode; in `Single` mode it changes nothing.

PageDown moves to the last row that is fully visible when the current row is scrolled into view. When the current row already is that row, the list scrolls one viewport down, at most to the end, and moves to the last fully visible row there. PageUp does the same upward with the first fully visible row. For example, in a list 80 pixels high with rows 20 pixels high, the viewport shows exactly four rows: from row `0`, PageDown moves to row `3`, a second PageDown to row `7`, PageUp to row `4`, and a second PageUp to row `0`. Without `PART_ScrollViewer`, or when the current row is not realized, PageDown moves to the last row and PageUp to the first row.

### Type-ahead

While `IsTextSearchEnabled` is `true` (the default), text that the user types moves to the first item whose text starts with the text typed so far, as a Down without a modifier moves to a row: the list scrolls the row into view, focuses it, and selects it in `Single` and `Extended` mode. Text typed within 1000 milliseconds of the previous text extends that text; after a longer pause the search starts again. When the extended text matches no item, the list searches for the newly typed text alone, so typing another first letter starts a new search. Text that matches no item changes nothing.

The text of an item is its `TextSearch.Text` when it is set on the item, otherwise the value at the list's `TextSearch.TextPath`, otherwise its display value (the `DisplayMemberPath` value, or the item itself), converted to text as `TextSearch` describes. The comparison uses the current culture and ignores case unless `IsTextSearchCaseSensitive` is `true`. Changing `TextSearch.TextPath`, `IsTextSearchEnabled`, or `IsTextSearchCaseSensitive` on the list starts the next search from empty text. `ComboBox` uses the same search.

### ScrollIntoView

`ScrollIntoView(item)` finds the first item equal to `item` by `object.Equals`. When its row is above the viewport, the template's `ScrollViewer` scrolls until the row's top is at the top of the viewport; when it is below, until the row's bottom is at the bottom of the viewport. The scroll goes through `UIElement.BringIntoView`, so every `ScrollViewer` around the row scrolls by the same rule, also an outer one. A row that is already in the viewport, an item that the list does not hold, and `null` when no item is `null` change nothing. Before the rows have their position, for example before the first layout, the row scrolls into view after the next arrange of the list. A row that is not realized then, and a list without `PART_ScrollViewer`, do not scroll.

## Constructors

| Name | Description |
| --- | --- |
| `ListBox()` | Initializes a new list box, sets the default items panel to a `StackPanel`, registers the key and text input handlers, and applies the default component template. |

## Fields

| Name | Description |
| --- | --- |
| `SelectionModeProperty` | Identifies the `SelectionMode` UI property. The default is `SelectionMode.Single`; values that are not defined in `SelectionMode` are rejected. |
| `IsTextSearchEnabledProperty` | Identifies the `IsTextSearchEnabled` UI property. The default is `true`. |
| `IsTextSearchCaseSensitiveProperty` | Identifies the `IsTextSearchCaseSensitive` UI property. The default is `false`. |
| `SelectedIndexProperty` | Inherited from `Selector`. Identifies the `SelectedIndex` UI property. |
| `ItemTemplateProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplate` UI property. |
| `ItemsPanelProperty` | Inherited from `ItemsControl`. Identifies the `ItemsPanel` UI property. |
| `ItemTemplateKeyProperty` | Inherited from `ItemsControl`. Identifies the `ItemTemplateKey` UI property. |
| `ItemsSourceProperty` | Inherited from `ItemsControl`. Identifies the `ItemsSource` UI property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `SelectionMode` | `SelectionMode` | Gets or sets what the user's clicks and keys select. The default is `SelectionMode.Single`. A change to `Single` keeps only the first selected item. |
| `SelectedItems` | `IReadOnlyList<object?>` | Gets a new list of the selected items in selection order. `SelectedItem` is its first item. |
| `IsTextSearchEnabled` | `bool` | Gets or sets whether typed text selects the first item whose text starts with it. The default is `true`. |
| `IsTextSearchCaseSensitive` | `bool` | Gets or sets whether type-ahead compares case. The default is `false`. |
| `SelectedIndex` | `int` | Inherited from `Selector`. Gets the first selected index in selection order, or selects only that index. The default is `-1`; values below `-1` are rejected by the selection model/property validation. |
| `SelectedItem` | `object?` | Inherited from `Selector`. Gets the item at `SelectedIndex`, or `null` when the selected index is outside the current item range. |
| `SelectionModel` | `SelectionModel` | Inherited from `Selector`. Gets the selection model that holds every selected index in selection order. |
| `Items` | `ItemCollection` | Inherited from `ItemsControl`. Gets the local item collection used when `ItemsSource` is not set. |
| `ItemsSource` | `IEnumerable?` | Inherited from `ItemsControl`. Gets or sets an external item source. |
| `ItemCount` | `int` | Inherited from `ItemsControl`. Gets the count of the `ItemsSource` snapshot when `ItemsSource` is set; otherwise the count of `Items`. |
| `ItemTemplate` | `ContentTemplate?` | Inherited from `ItemsControl`. Gets or sets the template used to display non-container items. `ListBox` applies it inside each `ListBoxItem` row. |
| `ItemTemplateKey` | `string?` | Inherited from `ItemsControl`. Gets or sets the template key used by content presentation. |
| `ItemsPanel` | `Panel?` | Inherited from `ItemsControl`. Gets or sets the retained panel used by the items presenter. `ListBox` initializes it to a vertical `StackPanel`. |
| `Background` | `Brush?` | Inherited from `Control`. Gets or sets the brush that fills the list's arranged bounds behind its rows. The default is `null` (no fill). The default template gives it to its `Border`; a custom `ComponentTemplate` decides itself. |
| `BorderBrush` | `Brush?` | Inherited from `Control`. Gets or sets the brush of the border around the list's arranged bounds. The default is `null` (no border). The default template gives it to its `Border`; a custom `ComponentTemplate` decides itself. |
| `BorderThickness` | `Thickness` | Inherited from `Control`. Gets or sets the border width; the rows are inset by it. The default is `Thickness.Zero`. The default template gives it to its `Border`; a custom `ComponentTemplate` decides itself. |
| `Padding` | `Thickness` | Inherited from `Control`. Gets or sets the space between the border and the rows. The default is `Thickness.Zero`. The default template gives it to its `Border`; a custom `ComponentTemplate` decides itself. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `SelectionChanged` | `EventHandler<SelectionChangedEventArgs>` | Inherited from `Selector`. Bubbling routed event raised after the selection changes, with every deselected item in `RemovedItems` and every selected item in `AddedItems`. |

## Methods

| Name | Description |
| --- | --- |
| `SelectAll()` | Selects every item. Throws `NotSupportedException` when `SelectionMode` is `Single`. |
| `UnselectAll()` | Deselects every item, in every `SelectionMode`. |
| `ScrollIntoView(object? item)` | Scrolls the template's `ScrollViewer`, and every outer `ScrollViewer`, through `UIElement.BringIntoView` until the row of the first item equal to `item` is in the viewport. Does nothing for an item that the list does not hold or a row already in the viewport; before the rows have their position, scrolls after the next arrange. |
| `SetItems(IEnumerable? items)` | Inherited from `ItemsControl`. Replaces the local `Items` collection. |
| `GetItemAt(int index)` | Inherited from `ItemsControl`. Gets an item from the `ItemsSource` snapshot when `ItemsSource` is set; otherwise from `Items`. |

## Protected Members

| Name | Description |
| --- | --- |
| `DefaultContainerType` | Returns `typeof(ListBoxItem)`. |
| `GetContainerTypeForItem(object? item)` | Returns the item's own type when the item is a `ListBoxItem`; otherwise `typeof(ListBoxItem)`. |
| `CreateItemContainer(int index, object? item)` | Returns the item itself when it is a `ListBoxItem`; otherwise a new `ListBoxItem`. |
| `PrepareItemContent(UIElement container, int index, object? item)` | Passes `ItemTemplate`, `ItemTemplateKey`, `Templates`, and the item index to a `ListBoxItem` row, then sets its `Content` as `ItemsControl` does. |
| `ClearItemContainer(UIElement container)` | Clears the container as `ItemsControl` does, then removes the item template from a `ListBoxItem` row. |
| `OnTemplateApplied(ComponentTemplateInstance? instance)` | Uses the template's `PART_ItemsPresenter` as the host of the rows and remembers its `PART_ScrollViewer`; without a template, the list hosts the rows itself and has no `ScrollViewer`. |
| `ArrangeCore(ArrangeContext context)` | Arranges as `ItemsControl` does, then scrolls the row that `ScrollIntoView` received before the rows had their position. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | Keeps only the first selected item when `SelectionMode` becomes `Single`, and starts the next type-ahead search from empty text when `TextSearch.TextPath`, `IsTextSearchEnabled`, or `IsTextSearchCaseSensitive` changes. |
| `OnRender(RenderContext context)` | Inherited from `ItemsControl`. Without a `ComponentTemplate`, fills the arranged bounds with `Background` and draws `BorderBrush` around them when they are set and the bounds are not empty; with a `ComponentTemplate`, such as the default template, draws nothing, and the template draws the chrome. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `SelectionMode` | `SelectionModeProperty` | `SelectionMode.Single` | `UiPropertyOptions.None`; the value must be defined in `SelectionMode`. |
| `IsTextSearchEnabled` | `IsTextSearchEnabledProperty` | `true` | `UiPropertyOptions.AffectsInputVisual` |
| `IsTextSearchCaseSensitive` | `IsTextSearchCaseSensitiveProperty` | `false` | `UiPropertyOptions.AffectsInputVisual` |

## Applies to

`Cerneala` UI controls.

## See also

- `Cerneala.UI.Controls.Primitives.Selector`
- `Cerneala.UI.Controls.ItemsControl`
- `Cerneala.UI.Controls.ListBoxItem`
- [`Cerneala.UI.Controls.SelectionMode`](Cerneala.UI.Controls.SelectionMode.md)
- [`Cerneala.UI.Controls.TextSearch`](Cerneala.UI.Controls.TextSearch.md)
- `Cerneala.UI.Controls.ScrollViewer`
