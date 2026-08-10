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

The default template uses an `Overlay`; opening the drop-down does not create a native window or a second `UIRoot`. The list is projected above normal root content and is removed from hit testing while closed. The drop-down matches the control width, sizes to its content up to `MaxDropDownHeight`, and scrolls when the content exceeds that limit. The closed field and arrow glyph, as well as the drop-down background, border, item foreground, and item font, follow the corresponding `ComboBox` properties. The owner palette bindings take precedence over ambient aspects targeting the internal `TextBox`, `ToggleButton`, or `Border` parts, while direct local values on those parts can still override the bindings. The default background is white and the default foreground is black, both assigned as base aspect values so an inherited foreground does not make the default field unreadable. Field text is left-aligned and vertically centered in both editable and non-editable modes.

`MaxDropDownHeight` defaults to `300`. A custom component template can set `Height` on `PART_DropDownOverlay` to request an exact projected height instead; an explicit height overrides the automatic maximum but remains clamped to the available viewport side.

The default `ItemContainerAspect` assigns `Padding="6"` to each `ComboBoxItem`. Assign another `ItemContainerAspect` to replace this default.

Programmatic selection updates `SelectedIndex`, `SelectedItem`, and `Text` immediately. In editable mode, text may differ from every item. A differing value clears selection when no drop-down transaction is active.

Text search is enabled by default. In editable mode, user input is matched against the first item whose primary text starts with the entered prefix. A match completes the editor text and selects the appended suffix so the next character replaces it. Outside an open drop-down, the match updates selection immediately. During an open drop-down, it updates only the preview until the transaction is committed. `ShouldPreserveUserEnteredPrefix` keeps the exact casing entered by the user; otherwise the matched item text replaces the whole prefix. Completion runs only while the caret is at the end of inserted text and is suppressed for `Backspace` and `Delete`, so deleting a completion suffix does not immediately add it again. Assigning `ComboBox.Text` programmatically uses exact matching and does not perform prefix completion.

In non-editable mode, text input accumulates a short-lived prefix and selects the first matching item without opening the drop-down. Search is culture-aware and case-insensitive unless `IsTextSearchCaseSensitive` is `true`. Set `IsTextSearchEnabled` to `false` to retain free editable text without automatic matching.

Primary search text is resolved in this order: an explicit `TextSearch.Text` on an item, `TextSearch.TextPath` on the `ComboBox`, `DisplayMemberPath`, then `ToString()`.

Editable mode and text filtering are enabled by default. Typing opens the drop-down and builds an internal item view without changing `Items`, `ItemsSource`, or source-based item indices. Clicking the drop-down toggle while that filtered view is open clears the filter and keeps the drop-down open with all items; a subsequent toggle click closes it normally. Set `IsEditable` or `IsTextFilterEnabled` to `false` to opt out of the corresponding behavior. Results are ranked by exact match, prefix match, contained text position, and finally bounded Damerau-Levenshtein distance. Adjacent transpositions count as one edit, and equal results keep source order. Fuzzy matching begins with three-character queries so short input does not produce an excessively broad result set. Prefix completion and filtering use the same culture and `IsTextSearchCaseSensitive` policy.

Selection is transactional while the drop-down is open. Arrow keys, `Home`, `End`, autocomplete, and fuzzy results change the visual preview without changing public `SelectedIndex`, `SelectedItem`, or `Text`. `Enter`, `Tab`, and item clicks commit the preview and collapse any selected autocomplete suffix to a caret at the end of the committed text. `Tab` then continues normal keyboard focus navigation out of the composite control. `Escape`, `F4` close, `Alt+Up`, light-dismiss, disabling, detaching, and other external closes cancel the preview and restore the committed editor text. Committing unmatched editable text clears selection and retains that text.

`IsReadOnly` applies to the editable text box. Read-only mode still permits selection, caret movement, copying, and selection changes through the `ComboBox` API, but rejects text insertion, deletion, cut, paste, undo, and redo from the editor.

Without an `ItemTemplate`, each item is presented through its display text. An empty `DisplayMemberPath` uses `ToString()`, including for enum and other non-string values.

The default items panel is a `VirtualizingStackPanel`. The drop-down realizes the visible uniform-height item window plus a one-item cache and recycles containers as the vertical offset changes. When filtering changes the item count, the initial realization window covers the content that can fit up to `MaxDropDownHeight`; small filtered lists are therefore fully realized and auto-size without empty reserved rows. Replacing `Items` or changing an observable `ItemsSource` clears containers from the previous collection immediately, including while the drop-down is closed. Replacing `ItemsPanel` opts out of this default ComboBox virtualization policy.

Keyboard commands include `F4`, `Alt+Down`, `Alt+Up`, `Escape`, `Enter`, `Tab`, arrow keys, `Home`, and `End`. The drop-down also closes after pointer selection, light-dismiss, disabling, or detaching. `Enter`, `Tab`, and pointer selection commit a pending preview.

The template must provide `PART_SelectionPresenter`, `PART_EditableTextBox`, `PART_DropDownToggle`, `PART_DropDownOverlay`, and `PART_ItemsPresenter`.

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
