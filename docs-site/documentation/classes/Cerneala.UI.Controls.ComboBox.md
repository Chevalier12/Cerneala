# ComboBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ComboBox.cs`

Represents a single-selection control with an optional text editor and an in-root drop-down overlay.

```csharp
public class ComboBox : Selector
```

Inheritance:

`object` -> `UiObject` -> `UIElement` -> `Control` -> `ItemsControl` -> `Selector` -> `ComboBox`

## Examples

```csharp
var people = new[] { new { Name = "Bucharest" } };
ComboBox cities = new()
{
    DisplayMemberPath = "Name",
    IsEditable = true,
    IsTextSearchEnabled = true,
    IsTextFilterEnabled = true,
    ShouldPreserveUserEnteredPrefix = true,
    MaxDropDownHeight = 240
};
cities.SetItems(people);
cities.DropDownOpened += (_, _) => System.Console.WriteLine("opened");
```

## Remarks

The default template uses an `Overlay`; opening the drop-down does not create a native window or a second `UIRoot`. The list is projected above normal root content and is removed from hit testing while closed. The drop-down matches the control width, sizes to its content up to `MaxDropDownHeight`, and scrolls when the content exceeds that limit. The closed field and arrow glyph, as well as the drop-down background, border, item foreground, and item font, follow the corresponding `ComboBox` properties. The owner palette bindings take precedence over ambient aspects targeting the internal `TextBox`, `ToggleButton`, or `Border` parts, while direct local values on those parts can still override the bindings. The default background is white and the default foreground is black, both stored as framework defaults at the `Default` value source. `Foreground` inherits, so an inherited, Aspect, or local `Foreground` replaces the default black; for example, a parent with a white `Foreground` makes the field text white. Field text is left-aligned and vertically centered in both editable and non-editable modes.

`MaxDropDownHeight` defaults to `300`. A custom component template can set `Height` on `PART_DropDownOverlay` to request an exact projected height instead; an explicit height overrides the automatic maximum but remains clamped to the available viewport side.

The default `ItemContainerAspect` assigns `Padding="6"` to each `ComboBoxItem`. Assign another `ItemContainerAspect` to replace this default.

Programmatic selection updates `SelectedIndex`, `SelectedItem`, and `Text` immediately. In editable mode, text may differ from every item. Assigning a `Text` value that differs from the selected item text clears the selection (`SelectedIndex` becomes `-1` and the text is kept), also while a drop-down transaction is open.

Text search is enabled by default. In editable mode, user input is matched against the first item whose primary text starts with the entered prefix. A match completes the editor text and selects the appended suffix so the next character replaces it. When `IsTextFilterEnabled` is `false` and the drop-down is closed, the match updates selection immediately. With the default `IsTextFilterEnabled = true`, typing first opens the drop-down, which starts a selection transaction, so the match updates only the preview until the transaction is committed. `ShouldPreserveUserEnteredPrefix` keeps the exact casing entered by the user; otherwise the matched item text replaces the whole prefix. Completion runs only while the caret is at the end of inserted text and is suppressed for `Backspace` and `Delete`, so deleting a completion suffix does not immediately add it again. Assigning `ComboBox.Text` programmatically uses exact matching and does not perform prefix completion.

In non-editable mode, text input accumulates a short-lived prefix and selects the first matching item without opening the drop-down. Search is culture-aware and case-insensitive unless `IsTextSearchCaseSensitive` is `true`. Set `IsTextSearchEnabled` to `false` to retain free editable text without automatic matching.

Primary search text is resolved in this order: an explicit `TextSearch.Text` on an item, `TextSearch.TextPath` on the `ComboBox`, `DisplayMemberPath`, then the item itself. The value found is converted to text as in WPF: a content element such as a `ComboBoxItem` gives the text of its `Content`, a `TextBlock` gives its `Text`, and any other value gives its `ToString()`. The primary text is also the value of `Text` when the item is selected. For example, with the items `new ComboBoxItem { Content = "Alpha" }` and `new ComboBoxItem { Content = "Beta" }`, selecting the first item sets `Text` to `"Alpha"` and the closed field shows `"Alpha"`, and typing `"Be"` in editable mode finds `"Beta"`. A `ComboBoxItem` whose `Content` is `new TextBlock { Text = "Gamma" }` has the primary text `"Gamma"`. An explicit `TextSearch.Text = "X"` on an item still wins over its `Content`.

Editable mode and text filtering are enabled by default. Typing opens the drop-down and builds an internal item view without changing `Items`, `ItemsSource`, or source-based item indices. Clicking the drop-down toggle while that filtered view is open clears the filter and keeps the drop-down open with all items; a subsequent toggle click closes it normally. Set `IsEditable` or `IsTextFilterEnabled` to `false` to opt out of the corresponding behavior. Results are ranked by exact match, prefix match, contained text position, and finally bounded Damerau-Levenshtein distance. Adjacent transpositions count as one edit, and equal results keep source order. Fuzzy matching begins with three-character queries so short input does not produce an excessively broad result set. Prefix completion and filtering use the same culture and `IsTextSearchCaseSensitive` policy.

Selection is transactional while the drop-down is open. Arrow keys, `Home`, `End`, autocomplete, and fuzzy results change the visual preview without changing public `SelectedIndex`, `SelectedItem`, or `Text`. `Enter`, `Tab`, and item clicks commit the preview and collapse any selected autocomplete suffix to a caret at the end of the committed text. A click on a button inside a drop-down item, such as a `CheckBox` from the `ItemTemplate`, is not an item click: it only clicks that button, the selection does not change, and the drop-down stays open (see `Selector`). A release on that button after a press on the empty part of the item completes no click of the button, so it is an item click: for example, a press on the empty part of item `"two"` followed by a release on its `CheckBox` selects `"two"`, closes the drop-down, raises no `Click`, and keeps `IsChecked == false`. `Tab` then continues normal keyboard focus navigation out of the composite control. `Escape`, `F4` close, `Alt+Up`, light-dismiss, disabling, detaching, and other external closes cancel the preview and restore the committed editor text. Committing unmatched editable text clears selection and retains that text.

The preview follows its item when the items change while the drop-down is open, through `Items`, through an `ItemsSource` that raises `INotifyCollectionChanged` or `IObservableList` notifications, or through a new `ItemsSource`, as the committed selection does (see `Selector`). With items `a`, `b`, `c` and `c` highlighted, inserting `z` at index `0` or removing `a` keeps `c` highlighted, and `Enter` commits `SelectedItem == "c"` with `Text == "c"`. A move or a reset keeps the same item highlighted while it is still present. Assigning a new `ItemsSource` is a reset: with `c` highlighted, `ItemsSource = new[] { "x", "c" }` keeps `c` highlighted, and `ItemsSource = new[] { "x", "y" }` highlights `x`. When the highlighted item is removed or replaced, the first visible item (the first filtered result while a filter is active) becomes the preview, or no item when none is visible. The editor keeps its text, and committing that preview sets `Text` to the text of the newly highlighted item, so `Text` and `SelectedItem` name the same item.

A failed re-preparation does not stop a preview change. When the drop-down opens, the preview moves, or the preview is committed or canceled, `ComboBox` re-prepares the realized containers of the previously and newly highlighted items. When one of those re-preparations throws, `ComboBox` still re-prepares the other container, still updates the editor text, the caret, and `PART_SelectionPresenter`, and only then rethrows the first exception, as `Selector` does for the committed selection. For example, a non-editable `ComboBox` subclass whose `PrepareItemContent` override throws for `"one"` shows `"one"`, `"two"`, and `"three"`, with `"one"` selected and highlighted in the open drop-down. `Down` throws, but before it throws, the row of `"two"` is re-prepared as highlighted and the selection box shows `"two"` with `ContentIndex == 1`. With `"two"` highlighted and an override that throws for `"two"`, `Escape` throws, but before it throws, the row of `"one"` is re-prepared as selected and both the editor text and the selection box show `"one"` again. The container whose re-preparation threw is prepared again by the next layout pass, as described for `Selector`.

Setting `SelectedIndex` while the drop-down is open also moves the preview to the new item. When a re-preparation of that preview change throws, the selection change still completes first: `Selector` still re-prepares the old and new committed containers and still raises `SelectionChanged`, and only then the first exception propagates out of the `SelectedIndex` setter, as for a nested selection change (see `Selector`). For example, with the same subclass throwing for `"two"`, `"one"` selected, and the drop-down open, `SelectedIndex = 1` throws `InvalidOperationException`, but before it throws, `SelectionChanged` is raised once with `RemovedItems == ["one"]` and `AddedItems == ["two"]`. Afterwards `SelectedIndex` is `1`, the selection box shows `"two"`, and the row of `"one"` shows `IsSelected == false`. The same holds when application code opens, closes, or commits the drop-down while `SelectedIndex` changes, for example from a `PropertyChanged` handler for `SelectedIndexProperty`: a failed re-preparation of that drop-down change propagates only after the selection change has raised `SelectionChanged`. For example, with the same subclass throwing for `"two"`, `"one"` selected, the drop-down open, and a handler that sets `IsDropDownOpen = false` when `SelectedIndex` changes, `SelectedIndex = 1` throws `InvalidOperationException`, but before it throws, `SelectionChanged` is raised once with `RemovedItems == ["one"]` and `AddedItems == ["two"]`. Afterwards `SelectedIndex` is `1`, `IsDropDownOpen` is `false`, and the overlay is closed. Opening or closing the drop-down outside a selection change, for example with `Escape`, light-dismiss, or `IsDropDownOpen = false`, still rethrows the failure at once.

A failed re-preparation also does not stop the command that caused it. `Enter`, `Tab`, and item clicks still commit the preview and still close the drop-down, and a change of `IsDropDownOpen` still opens or closes `PART_DropDownOverlay` to match the new value, before the first exception propagates. An open drop-down therefore always has a pending preview, and arrow keys never change `SelectedIndex` while it is open. For example, with the same subclass, `"one"` selected and highlighted, and an override that throws for `"one"`, `Enter` throws, but `IsDropDownOpen` is already `false` and the overlay is closed, and a click on `"two"` throws, but `SelectedIndex` is already `1` and the drop-down is closed. With `"two"` highlighted and an override that throws for `"two"`, `Escape` throws, and the overlay is already closed. After the drop-down was opened and closed once, `F4` with an override that throws for `"one"` throws, but `IsDropDownOpen` is `true` and the overlay is open.

A commit that selects a different item also changes `SelectedIndex`, so `Selector` re-prepares the containers of the previously and newly selected items, and a failure there propagates out of the `SelectedIndex` setter (see `Selector`). That failure does not stop the commit either: `ComboBox` still sets `Text` to the committed text, still clears the text filter, still collapses the autocomplete suffix to a caret at the end of the editor text, and still updates `PART_SelectionPresenter`, and only then rethrows the first exception. For example, an editable `ComboBox` subclass whose `PrepareItemContent` override throws for `"two"` shows `"one"`, `"two"`, and `"three"`, with `"one"` selected. Typing `tw` opens the drop-down filtered to `"two"`, and autocomplete shows `"two"` with the suffix `"o"` selected. `Enter` throws, but `SelectedIndex` is already `1`, `Text` and the editor text are `"two"`, the caret is at index `3` with no text selected, and the filter no longer hides `"one"` and `"three"`. The same holds for `Tab`, and for `Enter`, `Tab`, or a click on `"two"` when the override throws for the previously selected `"one"` instead.

`IsReadOnly` applies to the editable text box. Read-only mode still permits selection, caret movement, copying, and selection changes through the `ComboBox` API, but rejects text insertion, deletion, cut, paste, undo, and redo from the editor.

Without an `ItemTemplate`, each item is presented through its display text. An empty `DisplayMemberPath` uses `ToString()`, including for enum and other non-string values.

The default items panel is a `VirtualizingStackPanel`. The drop-down realizes the visible uniform-height item window plus a one-item cache and recycles containers as the vertical offset changes. When filtering changes the item count, the initial realization window covers the content that can fit up to `MaxDropDownHeight`; small filtered lists are therefore fully realized and auto-size without empty reserved rows. Replacing `Items` or changing an observable `ItemsSource` clears containers from the previous collection immediately, including while the drop-down is closed. Replacing `ItemsPanel` opts out of this default ComboBox virtualization policy. The policy also requires a virtualizing panel in the tree. When a template sets `PART_ItemsPresenter.ItemsPanel` to a non-virtualizing panel such as `StackPanel`, that panel realizes every item, before and after the drop-down scrolls. A template that sets it to a `VirtualizingStackPanel` keeps the policy.

The pointer area that opens the drop-down depends on the mode. In non-editable mode the whole closed field acts as the drop-down button, as in the WPF default template: a left click on the selection box, on the empty part of the field, or on `PART_DropDownToggle` opens a closed drop-down and closes an open one. The click completes on release, as a `ToggleButton` click does, and the press and the release must hit the same element of the field. In the default template `PART_SelectionPresenter` is not hit-testable, so a press on the selection box hits the field itself, the same element as a press on the empty part: a press on the text `"one"` followed by a release on the empty part of the field is one click and opens a closed drop-down. `PART_DropDownToggle` is a different element: a press on the empty part of the field followed by a release on `PART_DropDownToggle`, or a press on `PART_DropDownToggle` followed by a release on the empty part of the field, is not a click and leaves the drop-down unchanged. For example, a non-editable `ComboBox` with `Width = 180` that shows `"one"`, `"two"`, and `"three"`, with `"one"` selected, opens its drop-down (`IsDropDownOpen == true`) when the user clicks the text `"one"` or the middle of the field, and a second click at the same point closes it. As the WPF template's `ToggleButton` handles the field's click, a click that opens or closes the drop-down marks its `MouseUp` event handled, so it does not also click a command source that contains the `ComboBox`. For example, when that `ComboBox` is the `Content` of a `Button`, a click on the middle of the field sets `IsDropDownOpen == true`, and the `Button` raises no `Click` and does not run its `Command`; a click on the `Button` outside the `ComboBox` still raises `Click` once. A press on the `Button` outside the `ComboBox` followed by a release on the middle of the field is not a field click, because the press did not hit the field: `IsDropDownOpen` stays `false`, and the `Button` raises `Click` once and runs its `Command` once, as in WPF, where the `Button` takes that press and the template's `ToggleButton` never sees it. As the WPF `ToggleButton` also handles the field's `MouseLeftButtonDown`, the field takes the press too: a press on the field does not press, start, or click a command source that contains the `ComboBox`. For example, while the middle of that field is held, the outer `Button` keeps `IsPressed == false`, and a press on the middle of the field followed by a release on the `Button` outside the `ComboBox` raises no `Click`, runs no `Command`, and leaves `IsDropDownOpen == false`. When the `ComboBox` is the `Content` of a `RepeatButton` with `Delay = 500` and `Interval = 100`, holding the left button on the middle of the field or on `PART_DropDownToggle` for 1 second makes the `RepeatButton` raise no `Click` and run no `Command`, and the release opens the drop-down; holding the `RepeatButton` outside the `ComboBox` still clicks and repeats. As in the WPF default template, `PART_SelectionPresenter` is not hit-testable (`IsHitTestVisible == false`), so content that an `ItemTemplate` shows in the closed field never receives the click. For example, with an `ItemTemplate` that creates `new CheckBox { Content = context.Data }`, a click on the `CheckBox` `"one"` in the closed field opens the drop-down (`IsDropDownOpen == true`), and that `CheckBox` raises no `Click` and keeps `IsChecked == false`. A custom template whose `PART_SelectionPresenter` stays hit-testable lets that content receive the click. A click on interactive content belongs to that content alone and leaves `IsDropDownOpen` unchanged. Interactive content is content that the input bridge gives its own pointer input: a command source (`IInputCommandSource`), such as a `Button` or a `CheckBox`, and a pointer drag source (`IPointerDragSource`), such as a `TextBox`, a `PasswordBox`, or a `Thumb`. A click on other content, such as the text of a `TextBlock`, or on the empty part of the field still opens or closes the drop-down. For example, with that `CheckBox` template and a hit-testable `PART_SelectionPresenter`, a click on the `CheckBox` `"one"` raises its `Click` once, sets `IsChecked == true`, and keeps `IsDropDownOpen == false`. With an `ItemTemplate` that creates `new TextBox { Text = context.Data }` and a hit-testable `PART_SelectionPresenter`, a click on the `TextBox` `"one"` gives that `TextBox` keyboard focus and keeps `IsDropDownOpen == false`. The field decides which content the click hit with the elements and visual parents that the input bridge routed the `MouseUp` event with, so a handler that runs earlier on that route and removes the clicked content does not change the result. For example, with a hit-testable `PART_SelectionPresenter` and an `ItemTemplate` that creates a `Border` whose `MouseUp` handler removes that `Border` from its panel, a click on the `Border` still opens the drop-down (`IsDropDownOpen == true`), as it does when the handler removes nothing; a `CheckBox` whose `MouseUp` handler removes it still keeps `IsDropDownOpen == false`. A click on an item in the open drop-down selects that item and closes the drop-down. That click marks its `MouseUp` event handled, so a `Button` or `RadioButton` that contains the `ComboBox` is not clicked or checked by it (see `Selector`). In editable mode only `PART_DropDownToggle` opens or closes the drop-down by pointer; a click on the editor focuses the editor and leaves `IsDropDownOpen` unchanged.

Keyboard commands include `F4`, `Alt+Down`, `Alt+Up`, `Escape`, `Enter`, `Tab`, arrow keys, `Home`, and `End`. The drop-down also closes after pointer selection, light-dismiss, disabling, or detaching. `Enter`, `Tab`, and pointer selection commit a pending preview.

The template must provide `PART_SelectionPresenter`, `PART_EditableTextBox`, `PART_DropDownToggle`, `PART_DropDownOverlay`, and `PART_ItemsPresenter`.

`AspectState.Expanded` is active while `IsDropDownOpen` is `true`. The property includes `AffectsAspect` metadata, so opening or closing the drop-down also queues aspect resolution for the attached control on the next processed frame.

## Constructors

| Name | Description |
| --- | --- |
| `ComboBox()` | Initializes the default white background, black foreground, template, virtualizing vertical items panel, item-container padding, focus behavior, and keyboard commands. |

## Fields

| Name | Description |
| --- | --- |
| `IsDropDownOpenProperty` | Identifies `IsDropDownOpen`; default `false`. |
| `IsEditableProperty` | Identifies `IsEditable`; default `true`. |
| `IsReadOnlyProperty` | Identifies `IsReadOnly`; default `false`. |
| `IsTextSearchEnabledProperty` | Identifies `IsTextSearchEnabled`; default `true`. |
| `IsTextSearchCaseSensitiveProperty` | Identifies `IsTextSearchCaseSensitive`; default `false`. |
| `ShouldPreserveUserEnteredPrefixProperty` | Identifies `ShouldPreserveUserEnteredPrefix`; default `false`. |
| `IsTextFilterEnabledProperty` | Identifies `IsTextFilterEnabled`; default `true`. |
| `TextProperty` | Identifies `Text`; default empty string. |
| `MaxDropDownHeightProperty` | Identifies `MaxDropDownHeight`; default `300`. |
| `DropDownOpenedEvent` | Identifies the bubbling open event. |
| `DropDownClosedEvent` | Identifies the bubbling close event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDropDownOpen` | `bool` | Gets or sets the requested drop-down state. |
| `IsEditable` | `bool` | Selects text-editor or selection-presenter mode. Defaults to `true`. |
| `IsReadOnly` | `bool` | Prevents user edits in editable mode while preserving selection and copy behavior. |
| `IsTextSearchEnabled` | `bool` | Enables prefix search and editable completion. Defaults to `true`. |
| `IsTextSearchCaseSensitive` | `bool` | Selects culture-aware case-sensitive matching. Defaults to `false`. |
| `ShouldPreserveUserEnteredPrefix` | `bool` | Preserves the exact user-entered prefix when appending a matched suffix. Defaults to `false`. |
| `IsTextFilterEnabled` | `bool` | Enables the editable filtered and fuzzy-ranked item view. Defaults to `true`. |
| `Text` | `string` | Gets or sets the displayed/editor text. |
| `MaxDropDownHeight` | `float` | Limits automatically sized projected drop-down height. Defaults to `300`; must be positive and not `NaN`. |
| `DisplayMemberPath` | `string` | Inherited dotted path used for default text and visuals. |

## Events

| Name | Description |
| --- | --- |
| `DropDownOpened` | Raised once the overlay is actually projected. |
| `DropDownClosed` | Raised once the projected overlay is withdrawn. |
| `SelectionChanged` | Inherited event raised when selection changes, including when a drop-down preview is committed. |

## Applies to

Project: `Cerneala`

## See also

- `ComboBoxItem`
- `ItemsControl`
- `Overlay`
- `Selector`
- `TextSearch`
