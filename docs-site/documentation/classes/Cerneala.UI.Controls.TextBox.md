# TextBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBox.cs`

Represents an extensible plain-text editing control. It edits one line by default and several lines when `AcceptsReturn` or `TextWrapping` is set.

```csharp
[TemplatePart("PART_ContentHost", typeof(ScrollViewer))]
public class TextBox : Control, ITimeSensitiveRenderElement, IPointerDragSource, ITextInputHost
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBox`

Implements:
`ITimeSensitiveRenderElement`, `IPointerDragSource`, `ITextInputHost`

## Examples

Create a text box and bind its concrete `Text` UI property:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> source = new("query");
TextBox textBox = new();

using IDisposable binding = BindingOperations.BindTwoWay(
    textBox,
    TextBox.TextProperty,
    source);
```

Create a multi-line text box. It is at least 3 lines high, grows to 6 lines, and then scrolls vertically with a scroll bar that shows only when needed:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Text;

TextBox notes = new()
{
    AcceptsReturn = true,
    TextWrapping = TextWrapping.Wrap,
    MinLines = 3,
    MaxLines = 6,
    VerticalScrollBarVisibility = ScrollBarVisibility.Auto
};
```

With `AcceptsReturn = true`, typing `a`, Enter, `b` gives `Text == "a" + Environment.NewLine + "b"`, and `LineCount` is `2` after the text box has been laid out. Without `AcceptsReturn`, the same keys give `Text == "ab"` and Enter reaches the ancestors of the text box unhandled.

Limit and change typed text. `MaxLength` and `CharacterCasing` apply to typed and pasted text only:

```csharp
TextBox code = new() { MaxLength = 3, CharacterCasing = CharacterCasing.Upper };

// The user types "abcdef" and then "g":
// Text == "ABC"

code.Text = "abcdef";
// Text == "abcdef": text set by code is neither cut nor changed.
```

Replace the selection, undo it, and use the clipboard:

```csharp
TextBox textBox = new() { Text = "hello world" };

textBox.Select(6, 11);
// textBox.SelectedText == "world"

textBox.SelectedText = "xyz";
// Text == "hello xyz"; Selection.Start == 6; Selection.End == 9; SelectedText == "xyz"

textBox.Undo();
// Text == "hello world"; Selection.Start == 6; Selection.End == 11
```

Derive a control that normalizes accepted input:

```csharp
public sealed class UpperTextBox : TextBox
{
    protected override string NormalizeTextInput(string text) =>
        base.NormalizeTextInput(text).ToUpperInvariant();
}
```

## Remarks

`TextBox` owns its `TextProperty`, routed change events, selection and caret API. Editing state is encapsulated; the mutable editor and document are not public or protected.

### Template

As in WPF, the text box draws through its template. The default template is a `Border` that contains a `ScrollViewer` named `PART_ContentHost`, and that `ScrollViewer` contains an internal text view. The text view lays out and draws the text, the selection and the blinking caret. The text view is not public.

- The `Border` takes `Background`, `BorderBrush` and `BorderThickness` from the text box, through template owner bindings.
- The `ScrollViewer` takes `Padding` from the text box as its `Margin`, so the text sits inside the border and the padding.
- The `ScrollViewer` takes no focus and is not a tab stop. Its scroll bars show the arrow cursor; the text shows the I-beam cursor of the text box.
- A press on a scroll bar scrolls. It does not place the caret or start a selection.
- A replacement `ComponentTemplate` must provide a `ScrollViewer` part named `PART_ContentHost`; otherwise applying the template fails with the part name and the expected type.

Default chrome is stored as framework defaults at the `Default` value source: `Padding` `4,2,4,2`, `BorderThickness` `1`, `BorderBrush` `Color(120, 130, 145)`, and a white `Background`. Any Aspect, local, or other higher-source value replaces them.

`Text` has only the `AffectsSemantics` option. It does not invalidate the measure or the render of the text box itself, as in WPF: the text view in the template measures and draws the text and invalidates itself. An edit therefore measures the text view and only the ancestors whose size it changes. A left-aligned text box in a `StackPanel` still grows in the same frame when the typed text becomes wider.

The caret is one device pixel wide (`1 / Root.Scale` layout units) and is drawn to the right of its position. As in WPF's `TextBoxView`, which keeps a caret-wide margin, the text view reserves that width on the right side only: its desired width is the text width plus the caret width, and with `TextWrapping.Wrap` the text wraps at the available width minus the caret width. An auto-width text box is therefore one device pixel wider than its text, and the caret after the last character of a line that fills or overflows the viewport stays inside the clip of the `ScrollViewer`.

### Lines, size and scrolling

`MinLines` and `MaxLines` set the height of the text box in lines of its font, plus the vertical `Padding` and the negative sides of `BorderThickness`. The default `BorderThickness` is a positive 1, painted outside the bounds, so it adds nothing; for example, `MinLines = 3` gives a height of 3 times the line height plus 4 (the default `Padding` of 2 on top and bottom). `MaxLines` limits the height that the template gets; `MinLines` is the least desired height. The default `MaxLines` is `int.MaxValue`, which is no limit. If `MinLines` is greater than `MaxLines` when the text box is measured, measure throws `InvalidOperationException`. For example, `MinLines = 4` with `MaxLines = 2` throws at the first `Measure`.

`TextWrapping.Wrap` breaks the text at word boundaries at the width of the text view. A word wider than the width is split. While `TextWrapping` is `Wrap`, the horizontal scroll bar of the template is `Disabled`, whatever `HorizontalScrollBarVisibility` says, so the text does not scroll horizontally. `NoWrap` breaks lines only at line break characters.

`TextWrapping.WrapWithOverflow` breaks at word boundaries at the same width as `Wrap` (the width of the text view minus the caret width), but a word that is wider than that width stays whole on one line instead of being split. Only `Wrap` disables the horizontal scroll bar, so with `WrapWithOverflow` the scroll bar follows `HorizontalScrollBarVisibility`, and a line with a too-long word scrolls horizontally to the caret. For example, in a `TextBox` with `TextWrapping = TextWrapping.WrapWithOverflow` and `Width = 100`, typing the 26 letters `abcdefghijklmnopqrstuvwxyz` gives `LineCount` equal to `1`, an extent wider than the viewport, and a horizontal offset greater than `0`. With `TextWrapping.Wrap` the same input gives `LineCount` greater than `1` and a horizontal offset of `0`.

`FontWeight`, `FontStyle`, and `FontStretch` are inherited from `Control` and select the face of `FontFamily` that the text view measures and draws with; changing one of them measures and draws the text again, like a change of `FontFamily` or `FontSize`. A `FontResourceId` font ignores them.

`HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` are passed to the `ScrollViewer` of the template. Both default to `Hidden`: the bar takes no space and is not drawn, and the text still scrolls on that axis. With `Auto` the bar shows when the text does not fit. With `Disabled` the text does not scroll on that axis. See `ScrollBarVisibility`.

After an edit or a caret move, the `ScrollViewer` scrolls the least amount that shows the caret, in the same frame. This does not raise `RequestBringIntoView`. The mouse wheel scrolls the text vertically. When the text cannot scroll, the wheel event stays unhandled, so an enclosing scroll viewer gets it.

`LineCount` is the number of drawn lines at the width of the last arrange. Before the first layout the text does not wrap. A line break at the end of the text adds an empty last line. `GetLineText(int)` returns the text of a drawn line with its line break characters, for example `"line 0\n"` for the first line of `"line 0\nline 1"` and `"line 1"` for the last. A line that wraps keeps its trailing spaces; for the text `"one two three"` in a text box with `TextWrapping = TextWrapping.Wrap` and `Width = 60`, line `0` is `"one "`. `GetLineText` and `ScrollToLine` throw `ArgumentOutOfRangeException` for an index below `0` or not below `LineCount`. `ScrollToLine(int)` scrolls vertically the least amount that shows the line, and leaves the horizontal scroll position alone. When the text box has not been laid out since the last change, the scroll is applied at its next arrange.

### Input

The default normalizer, `NormalizeTextInput`, removes control characters. With `AcceptsReturn`, it keeps the line breaks `\r`, `\n` and `\r\n`; with `AcceptsTab`, it keeps `\t`.

`MaxLength` is the most characters that typed and pasted text can bring `Text` to. `0`, the default, is no limit, and a negative value throws `ArgumentException`. The characters that the selection replaces are not counted. Example: `Text == "hello"`, `MaxLength = 5`, select all, type `abcdefg`: `Text == "abcde"`. A longer input is cut to the room that is left, without splitting a text element. `Text` set by code, `SelectedText` set by code, and `Undo`/`Redo` ignore `MaxLength`.

`CharacterCasing` changes the case of typed and pasted text: `Upper` and `Lower` use the invariant culture, because Cerneala has no input language. With `Upper`, typing `abC` and pasting `xY` gives `ABCXY`. Switching to `Lower` and typing `QrS` appends `qrs`; the earlier characters keep their case. `Text` set by code keeps its case.

When `IsReadOnly` is `true`, text can still be selected and copied, the caret moves, and `Text` can still be assigned programmatically. User insertion, deletion, Enter, Tab, cut, paste, undo, and redo are rejected, and so are the public `Cut`, `Paste`, `Undo` and `Redo`.

### Undo and redo

`IsUndoEnabled` defaults to `true`. Setting it to `false` clears the history, and nothing is recorded until it is `true` again. `UndoLimit` is the most undo units kept: `-1`, the default, is no limit, `0` keeps none, and `n` keeps the newest `n` units. A value below `-1` throws `ArgumentException`. Lowering `UndoLimit` drops the oldest units at once.

Typing is recorded in units. Consecutive typed insertions with no other edit or caret move between them are one unit: typing `a`, `b`, `c` as three text inputs gives `Text == "abc"`, and one Ctrl+Z gives `Text == ""`. Ctrl+Y or Ctrl+Shift+Z then gives `"abc"` again. A caret move, a deletion, a paste, a cut, or a `SelectedText` assignment ends the run. Assigning `Text` clears the history. `CanUndo` and `CanRedo` tell whether `Undo()` and `Redo()` would change the text. `Undo()` and `Redo()` return `false` when they change nothing.

### Selection and clipboard

`SelectedText` gets the selected text, and `string.Empty` for an empty selection. Setting it replaces the selection with the new text, selects the new text, and records one undo unit that later typing does not join. Undo restores the old text and the old selection. `null` is treated as an empty string. The setter is a programmatic change, like `Text`: it works when `IsReadOnly` is `true` and ignores `MaxLength` and `CharacterCasing`. Setting a value equal to the current selected text does nothing.

`Copy()`, `Cut()` and `Paste()` do what Ctrl+C, Ctrl+X and Ctrl+V do, through the clipboard of the `UIRoot` platform services; in a window opened through the SDL platform, that is the system clipboard. They return nothing and do nothing when there is no clipboard, when `Copy` or `Cut` has an empty selection, or when `Paste` finds no text. `Cut()` and `Paste()` also do nothing while `IsReadOnly` is `true`; `Copy()` still works. Pasted text goes through `NormalizeTextInput`, `CharacterCasing` and `MaxLength`. There is no context menu.

### Keyboard

| Key | Behavior |
| --- | --- |
| Left, Right | Move the caret by one text element. Shift extends the selection. |
| Ctrl+Left, Ctrl+Right | Move the caret by one word. Shift extends the selection. Example: in `hello world` with the caret at `0`, Ctrl+Right moves it to `6`. |
| Home, End | Move to the start or the end of the drawn line. Shift extends the selection. |
| Ctrl+Home, Ctrl+End | Move to the start or the end of the text. Shift extends the selection. |
| Up, Down, PageUp, PageDown | Move between drawn lines, keeping the horizontal position through shorter lines. PageUp and PageDown move by the number of whole lines in the viewport. Up on the first line moves to the start of the text, and Down on the last line to the end. Shift extends the selection. With a single drawn line, or with Ctrl or Alt down, these keys stay unhandled, so an enclosing `ComboBox` still gets them. |
| Backspace, Delete | Delete a character or the selection. |
| Ctrl+Backspace, Ctrl+Delete | With an empty selection, delete a word: Ctrl+Backspace deletes from the place Ctrl+Left moves to up to the caret, and Ctrl+Delete from the caret to the place Ctrl+Right moves to. With a selection, they delete only the selection, like Backspace and Delete. Shift does not change them. Each is one undo unit, and they do nothing while `IsReadOnly` is `true`. Example: with `Text == "hello world"` and the caret at `11`, Ctrl+Backspace gives `"hello "` with the caret at `6`; with the caret at `8`, it gives `"hello rld"`; with the caret at `0`, Ctrl+Delete gives `"world"`; Ctrl+Z then gives `"hello world"` again. |
| Enter | Types a line break when `AcceptsReturn` is `true`, `IsReadOnly` is `false`, and Ctrl and Alt are up. Otherwise it stays unhandled, so a default button or an enclosing handler gets it. |
| Tab, Shift+Tab | Type a tab when `AcceptsTab` is `true`, `IsReadOnly` is `false`, and Ctrl and Alt are up. Otherwise they stay unhandled and move the focus. Ctrl+Tab always moves the focus. |
| Ctrl+A | Selects all text. |
| Ctrl+C, Ctrl+X, Ctrl+V | Copy, cut and paste. |
| Ctrl+Z | Undo. |
| Ctrl+Y, Ctrl+Shift+Z | Redo. |

Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z stay unhandled when there is nothing to undo or redo.

### Pointer

A pointer press places the caret, and a drag extends the selection, at the character drawn under the pointer. The unrounded root pointer position is converted through the render transforms of the content host and every ancestor before it is compared with the caret stops of the text. For example, for a text box at the left edge of a panel at the root origin with `RenderTransform = new Transform(Matrix3x2.CreateScale(2, 2))` and `RenderTransformOrigin = new LayoutPoint(0, 0)`, a click at root x `60` gives the same caret as a click at root x `30` when the panel has no transform. A pointer position that cannot be converted, for example through a non-invertible transform, keeps the current caret.

The click count comes from `MouseButtonEventArgs.ClickCount` of the press (see `ClickTracker`):

- Double-click selects the word under the pointer and the whitespace after it. The word is the run of letters, digits and `_`, or the run of other symbols, or the run of whitespace, that contains the character drawn under the pointer, not the caret position nearest the pointer. In `hello world`, a double-click on the `w` selects `6..11`, and a double-click on the `h` selects `0..6`, which includes the space. A double-click right of the end of a line selects the last word of that line.
- Triple-click selects the text line under the pointer without its line break. In `one two\nthree`, a triple-click on the `o` selects `0..7`. A wrapped line is not split: the text line runs from one line break to the next.
- A drag after a double-click extends the selection by whole words, and a drag after a triple-click extends it by whole text lines. In `hello brave world`, a double-click on `hello` followed by a drag to `brave` gives `Selection.Anchor == 0` and `Selection.Active == 12`. A drag backward from a double-clicked word keeps the whole word selected: a double-click on `world` followed by a drag to the `o` of `hello` gives `Anchor == 17` and `Active == 0`.

Pointer selection captures the mouse until the left button is released or capture is lost.

An enabled text box owns a left click into it, as WPF's text editor handles `MouseLeftButtonDown` and `MouseLeftButtonUp`. It marks the left `MouseDown` handled, the input system does not set `IsPressed` of an enclosing `ButtonBase` for the press, and it marks handled the left `MouseUp` that ends the pointer selection it began. An enclosing `Button` therefore raises no `Click` and runs no `Command` for a click into the text box, and an enclosing `ListBoxItem` is not selected by it. A release over the text box after a press elsewhere is not handled by the text box.

Derived controls can override `NormalizeTextInput`, `OnTextChanged` and `OnSelectionChanged`. Event hooks must call the base implementation when the routed event should continue to be raised.

## Constructors

| Name | Description |
| --- | --- |
| `TextBox()` | Initializes an empty, focusable, one-line text box with the default template. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the concrete `TextBox.Text` UI property. |
| `IsReadOnlyProperty` | `UiProperty<bool>` | Identifies the read-only editing property. Defaults to `false`. |
| `AcceptsReturnProperty` | `UiProperty<bool>` | Identifies `AcceptsReturn`. Defaults to `false`. |
| `AcceptsTabProperty` | `UiProperty<bool>` | Identifies `AcceptsTab`. Defaults to `false`. |
| `TextWrappingProperty` | `UiProperty<TextWrapping>` | Identifies `TextWrapping`. Defaults to `NoWrap`. |
| `MinLinesProperty` | `UiProperty<int>` | Identifies `MinLines`. Defaults to `1`. |
| `MaxLinesProperty` | `UiProperty<int>` | Identifies `MaxLines`. Defaults to `int.MaxValue`. |
| `HorizontalScrollBarVisibilityProperty` | `UiProperty<ScrollBarVisibility>` | Identifies `HorizontalScrollBarVisibility`. Defaults to `Hidden`. |
| `VerticalScrollBarVisibilityProperty` | `UiProperty<ScrollBarVisibility>` | Identifies `VerticalScrollBarVisibility`. Defaults to `Hidden`. |
| `MaxLengthProperty` | `UiProperty<int>` | Identifies `MaxLength`. Defaults to `0`. |
| `CharacterCasingProperty` | `UiProperty<CharacterCasing>` | Identifies `CharacterCasing`. Defaults to `Normal`. |
| `IsUndoEnabledProperty` | `UiProperty<bool>` | Identifies `IsUndoEnabled`. Defaults to `true`. |
| `UndoLimitProperty` | `UiProperty<int>` | Identifies `UndoLimit`. Defaults to `-1`. |
| `CaretBrushProperty` | `UiProperty<Brush>` | Identifies the caret brush property. |
| `SelectionBackgroundProperty` | `UiProperty<Color>` | Identifies the selection background property. |
| `TextChangedEvent` | `RoutedEvent` | Identifies the bubbling text-change event. |
| `SelectionChangedEvent` | `RoutedEvent` | Identifies the bubbling selection-change event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets or sets the editable text. `null` is coerced to an empty string. |
| `IsReadOnly` | `bool` | Gets or sets whether user editing operations are rejected. |
| `AcceptsReturn` | `bool` | Gets or sets whether Enter types a line break. The default is `false`. |
| `AcceptsTab` | `bool` | Gets or sets whether Tab and Shift+Tab type a tab instead of moving the focus. The default is `false`. |
| `TextWrapping` | `TextWrapping` | Gets or sets whether lines wrap at the width of the text view: `NoWrap` (default), `Wrap`, or `WrapWithOverflow`, which keeps a too-long word whole on one line. |
| `MinLines` | `int` | Gets or sets the least height in lines. The default is `1`; a value below `1` throws `ArgumentException`. |
| `MaxLines` | `int` | Gets or sets the greatest height in lines. The default is `int.MaxValue`; a value below `1` throws `ArgumentException`. |
| `HorizontalScrollBarVisibility` | `ScrollBarVisibility` | Gets or sets the horizontal scroll bar policy of the template. The default is `Hidden`. |
| `VerticalScrollBarVisibility` | `ScrollBarVisibility` | Gets or sets the vertical scroll bar policy of the template. The default is `Hidden`. |
| `MaxLength` | `int` | Gets or sets the most characters that typed and pasted text can bring `Text` to. `0` is no limit. |
| `CharacterCasing` | `CharacterCasing` | Gets or sets the case change of typed and pasted text. The default is `Normal`. |
| `IsUndoEnabled` | `bool` | Gets or sets whether undo history is recorded. Setting `false` clears it. |
| `UndoLimit` | `int` | Gets or sets the most undo units kept. `-1` is no limit, `0` keeps none. |
| `CanUndo` | `bool` | Gets whether `Undo()` has a unit to restore. |
| `CanRedo` | `bool` | Gets whether `Redo()` has a unit to reapply. |
| `SelectedText` | `string` | Gets the selected text. Setting it replaces the selection and selects the new text, as one undo unit. |
| `LineCount` | `int` | Gets the number of drawn lines at the width of the last arrange. |
| `Selection` | `TextSelection` | Gets the current selection without exposing the editor. |
| `Caret` | `TextCaret` | Gets the current caret state. |
| `CaretBrush` | `Brush` | Gets or sets the brush used to paint the caret. |
| `SelectionBackground` | `Color` | Gets or sets the selection highlight color. |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the fallback text measurer. |
| `TextRenderer` | `TextRenderer` | Gets or sets the fallback text renderer. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the optional font resource identifier. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the optional resource provider. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ReceiveTextInput(string)` | `void` | Normalizes and inserts text at the current selection, as typed text: `NormalizeTextInput`, `CharacterCasing` and `MaxLength` apply. |
| `Select(int, int)` | `void` | Sets the selection anchor and active positions. |
| `MoveCaret(int, bool)` | `void` | Moves the caret and optionally extends the selection. |
| `Undo()` | `bool` | Restores the previous editor snapshot when available. Returns `false` when it changes nothing. |
| `Redo()` | `bool` | Reapplies the next editor snapshot when available. Returns `false` when it changes nothing. |
| `Copy()` | `void` | Copies the selected text to the clipboard. Works while `IsReadOnly`. |
| `Cut()` | `void` | Copies the selected text to the clipboard and deletes it. Does nothing while `IsReadOnly`. |
| `Paste()` | `void` | Inserts the clipboard text at the selection. Does nothing while `IsReadOnly`. |
| `GetLineText(int)` | `string` | Gets the text of a drawn line, with its line break characters. |
| `ScrollToLine(int)` | `void` | Scrolls vertically the least amount that shows a drawn line. |
| `UpdateRenderTime(TimeSpan)` | `bool` | Advances caret blink state. |

## Events

| Name | Description |
| --- | --- |
| `TextChanged` | Occurs when `Text` changes. |
| `SelectionChanged` | Occurs when the selection changes. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Text` | `TextProperty` | `string.Empty` | `UiPropertyOptions.AffectsSemantics`; `null` is coerced to `string.Empty`. |
| `IsReadOnly` | `IsReadOnlyProperty` | `false` | `UiPropertyOptions.AffectsInputVisual | UiPropertyOptions.AffectsSemantics` |
| `AcceptsReturn` | `AcceptsReturnProperty` | `false` | None. |
| `AcceptsTab` | `AcceptsTabProperty` | `false` | None. |
| `TextWrapping` | `TextWrappingProperty` | `TextWrapping.NoWrap` | `UiPropertyOptions.AffectsMeasure | UiPropertyOptions.AffectsRender` |
| `MinLines` | `MinLinesProperty` | `1` | `UiPropertyOptions.AffectsMeasure`; rejects values below `1`. |
| `MaxLines` | `MaxLinesProperty` | `int.MaxValue` | `UiPropertyOptions.AffectsMeasure`; rejects values below `1`. |
| `HorizontalScrollBarVisibility` | `HorizontalScrollBarVisibilityProperty` | `ScrollBarVisibility.Hidden` | `UiPropertyOptions.AffectsMeasure` |
| `VerticalScrollBarVisibility` | `VerticalScrollBarVisibilityProperty` | `ScrollBarVisibility.Hidden` | `UiPropertyOptions.AffectsMeasure` |
| `MaxLength` | `MaxLengthProperty` | `0` | None; rejects negative values. |
| `CharacterCasing` | `CharacterCasingProperty` | `CharacterCasing.Normal` | None. |
| `IsUndoEnabled` | `IsUndoEnabledProperty` | `true` | None. |
| `UndoLimit` | `UndoLimitProperty` | `-1` | None; rejects values below `-1`. |
| `CaretBrush` | `CaretBrushProperty` | Black `SolidColorBrush` | `UiPropertyOptions.AffectsRender`; rejects `null`. |
| `SelectionBackground` | `SelectionBackgroundProperty` | `Color(0, 120, 215)` | `UiPropertyOptions.AffectsRender` |

## Applies to

Project: `Cerneala`

## See also

- `PasswordBox`
- `CharacterCasing`
- `ScrollViewer`
- `ScrollBarVisibility`
- `TextWrapping`
- `ClickTracker`
- `TextSelection`
- `TextCaret`
