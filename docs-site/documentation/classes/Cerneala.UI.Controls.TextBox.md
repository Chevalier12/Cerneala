# TextBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBox.cs`

Represents a focusable editable text control that displays its text directly and inherits editing, caret, selection, clipboard, and rendering behavior from `TextBoxBase`.

```csharp
public class TextBox : TextBoxBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBoxBase` -> `TextBox`

## Examples

Create a text box, set its initial text, and append text through the editing pipeline:

```csharp
using Cerneala.UI.Controls;

TextBox textBox = new()
{
    Text = "Hello"
};

textBox.MoveCaret(textBox.Text.Length);
textBox.ReceiveTextInput("!");

string value = textBox.Text; // "Hello!"
```

Bind `Text` two ways through the UI property system:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

ObservableValue<string> source = new("query");
TextBox textBox = new();

using IDisposable binding = BindingOperations.BindTwoWay(
    textBox,
    TextBoxBase.TextProperty,
    source);
```

## Remarks

`TextBox` does not declare additional members or override `TextBoxBase` behavior. It is the concrete plain-text editing control for the retained UI layer. Use it when the stored `Text` value should also be the rendered and accessibility-exposed value.

The inherited constructor path makes the control focusable, includes it in tab navigation, sets default text-box chrome, and uses the I-beam cursor. The inherited defaults include `Padding = new Thickness(4, 2, 4, 2)`, `BorderThickness = new Thickness(1)`, `BorderBrush = new SolidColorBrush(new Color(120, 130, 145))`, `Background = new SolidColorBrush(Color.White)`, `CaretColor = Color.Black`, and `SelectionBackground = new Color(0, 120, 215)`.

Text input is normalized before insertion: `null`, empty input, and control characters do not insert text. Programmatic `Text` assignments also normalize `null` to `string.Empty`. Editing operations keep the inherited `TextEditor`, `TextSelection`, and `TextCaret` state synchronized with the `Text` UI property.

Keyboard handling supports backspace, delete, home, end, left, right, Shift-based selection extension, and Ctrl+A/C/X/V clipboard shortcuts when a platform clipboard is available. Pointer input moves the caret to the nearest text position and dragging selects ranges. The caret is kept visible by an internal horizontal text offset when text exceeds the arranged content width.

Rendering draws the inherited background and border, clips content to the text area, renders normal text, paints the selection background, redraws selected text in white inside the selection clip, and renders a blinking caret while the control is focused, enabled, visible for rendering, and has a non-transparent caret color.

Accessibility semantics use `TextBoxAutomationPeer` for `TextBoxBase` controls. A `TextBox` exposes `SemanticsRole.EditableText` and reports its current `Text` as `SemanticsProperty.Value`.

## Constructors

| Name | Description |
| --- | --- |
| `TextBox()` | Initializes a new plain text box through the inherited `TextBoxBase` constructor. |

## Fields

This class does not declare public fields.

### Important Inherited Fields

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | `TextBoxBase` | Identifies the `Text` UI property. Defaults to `string.Empty`, coerces `null` to empty text, and affects measure, render, and semantics. |
| `CaretColorProperty` | `UiProperty<Color>` | `TextBoxBase` | Identifies the `CaretColor` UI property. Defaults to `Color.Black` and affects render. |
| `SelectionBackgroundProperty` | `UiProperty<Color>` | `TextBoxBase` | Identifies the `SelectionBackground` UI property. Defaults to `new Color(0, 120, 215)` and affects render. |

## Properties

This class does not declare public properties.

### Important Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Editor` | `TextEditor` | `TextBoxBase` | Gets the editor that owns the text document, caret, selection, and undo/redo state. |
| `Text` | `string` | `TextBoxBase` | Gets or sets the editable and rendered text. Setting `null` stores `string.Empty`. |
| `Selection` | `TextSelection` | `TextBoxBase` | Gets the current text selection. |
| `Caret` | `TextCaret` | `TextBoxBase` | Gets the current caret state. |
| `CaretColor` | `Color` | `TextBoxBase` | Gets or sets the rendered caret color. |
| `SelectionBackground` | `Color` | `TextBoxBase` | Gets or sets the selection highlight color. |
| `TextMeasurer` | `TextMeasurer` | `TextBoxBase` | Gets or sets the text measurer used when no resource-backed font path is active. Setting `null` throws `ArgumentNullException`; changing it invalidates text metrics. |
| `TextRenderer` | `TextRenderer` | `TextBoxBase` | Gets or sets the text renderer used when no resource-backed font path is active. Setting `null` throws `ArgumentNullException`; changing it invalidates render. |
| `FontResourceId` | `ResourceId<FontResource>?` | `TextBoxBase` | Gets or sets the optional font resource used for text metrics and rendering. |
| `ResourceProvider` | `IResourceProvider?` | `TextBoxBase` | Gets or sets the optional resource provider used before falling back to the root resource provider. |

## Methods

This class does not declare public methods.

### Important Inherited Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `ReceiveTextInput(string text)` | `void` | `TextBoxBase` | Inserts normalized non-control text at the caret or replaces the current selection, then syncs `Text`, updates layout/render state, keeps the caret visible, and resets caret blink. |
| `Select(int anchor, int active)` | `void` | `TextBoxBase` | Updates the selection range, keeps the caret visible, and invalidates render. |
| `MoveCaret(int position, bool extendSelection = false)` | `void` | `TextBoxBase` | Moves the caret, optionally extending selection, keeps the caret visible, resets caret blink, and invalidates render. |
| `Undo()` | `bool` | `TextBoxBase` | Reverts the latest editor operation when available, syncs `Text`, and returns whether text changed. |
| `Redo()` | `bool` | `TextBoxBase` | Reapplies the latest undone editor operation when available, syncs `Text`, and returns whether text changed. |
| `UpdateRenderTime(TimeSpan frameTime)` | `bool` | `TextBoxBase` | Advances caret blink timing for focused, enabled, render-visible text boxes and returns whether render work was invalidated. |

## Events

This class does not declare public events.

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Text` | `TextProperty` | `string.Empty` | `UiPropertyOptions.AffectsMeasure`, `UiPropertyOptions.AffectsRender`, `UiPropertyOptions.AffectsSemantics`; coerces `null` to `string.Empty`. |
| `CaretColor` | `CaretColorProperty` | `Color.Black` | `UiPropertyOptions.AffectsRender`. |
| `SelectionBackground` | `SelectionBackgroundProperty` | `new Color(0, 120, 215)` | `UiPropertyOptions.AffectsRender`. |

## Applies to

`Cerneala.UI.Controls.TextBox` in the `Cerneala` project.

## See also

- `UI/Controls/TextBox.cs`
- `UI/Controls/TextBoxBase.cs`
- `UI/Accessibility/TextBoxAutomationPeer.cs`
- `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxClipboardShortcutTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxCaretBlinkTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxTwoWayBindingTests.cs`
