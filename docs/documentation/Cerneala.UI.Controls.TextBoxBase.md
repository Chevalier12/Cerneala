# TextBoxBase Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBoxBase.cs`

Provides the shared retained text-editing, caret, selection, clipboard, layout, and rendering behavior for text-entry controls.

```csharp
public abstract class TextBoxBase : Control, ITimeSensitiveRenderElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBoxBase`

Derived:
`TextBox`, `PasswordBox`

Implements:
`ITimeSensitiveRenderElement`

## Examples

Create a concrete text box and edit its text through the shared editing pipeline:

```csharp
using Cerneala.UI.Controls;

TextBox textBox = new()
{
    Text = "Ink"
};

textBox.MoveCaret(textBox.Text.Length);
textBox.ReceiveTextInput(" flows");

string value = textBox.Text; // "Ink flows"
```

Select text and replace it with normalized text input:

```csharp
using Cerneala.UI.Controls;

TextBox textBox = new()
{
    Text = "hello world"
};

textBox.Select(6, 11);
textBox.ReceiveTextInput("Cerneala");

string value = textBox.Text; // "hello Cerneala"
```

Bind the `Text` UI property two ways:

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

`TextBoxBase` is abstract. Use `TextBox` for plain editable text, or derive a specialized control when the stored `Text` and rendered text should differ. `PasswordBox` does this by overriding the protected `DisplayText` property and rendering a repeated mask character while keeping the inherited text editor state.

The constructor configures the control as focusable and tab-stop enabled, assigns default text-box chrome, and uses the I-beam cursor. The default visual values include `Padding = new Thickness(4, 2, 4, 2)`, `BorderThickness = new Thickness(1)`, `BorderColor = new DrawColor(120, 130, 145)`, `Background = DrawColor.White`, `CaretColor = DrawColor.Black`, and `SelectionBackground = new DrawColor(0, 120, 215)`.

The `Text` UI property defaults to `string.Empty`, coerces `null` to `string.Empty`, and affects measure, render, and semantics. Programmatic `Text` changes synchronize the internal `TextEditor`, clear undo/redo state, keep the caret visible, and invalidate text metrics.

`ReceiveTextInput` removes control characters before inserting text. Empty, `null`, or control-only input does not change the document. Keyboard handling supports backspace, delete, home, end, left, right, Shift-based selection extension, and Ctrl+A/C/X/V clipboard shortcuts when a platform clipboard is available from the root platform services.

Pointer input moves the caret to the closest text position on left mouse down and extends selection while dragging. When the caret moves outside the arranged content width, an internal horizontal offset scrolls the rendered text so the caret remains visible.

Rendering draws the background and border, clips the text area, renders `DisplayText`, paints the selection background, redraws selected text in white inside the selection clip, and renders a blinking caret while the control is keyboard-focused, enabled, render-visible, and `CaretColor` is not transparent.

When `FontResourceId` resolves through `ResourceProvider` or the root resource provider, text measurement and rendering use a resource-backed `FontResolver`. Otherwise the control uses the assigned `TextMeasurer` and `TextRenderer` instances.

Accessibility peers for `TextBoxBase` controls use `TextBoxAutomationPeer`, which reports `SemanticsRole.EditableText`. The peer exposes `Text` as the semantics value except for `PasswordBox`, where the value is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `TextBoxBase()` | Initializes shared text-editing state, default text-box visuals, focus behavior, cursor, and routed input handlers. This constructor is protected. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the `Text` UI property. Defaults to `string.Empty`, coerces `null` to empty text, and affects measure, render, and semantics. |
| `CaretColorProperty` | `UiProperty<DrawColor>` | Identifies the `CaretColor` UI property. Defaults to `DrawColor.Black` and affects render. |
| `SelectionBackgroundProperty` | `UiProperty<DrawColor>` | Identifies the `SelectionBackground` UI property. Defaults to `new DrawColor(0, 120, 215)` and affects render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Editor` | `TextEditor` | Gets the editor that owns the text document, caret, selection, and undo/redo stack. |
| `Text` | `string` | Gets or sets the editable text. Setting `null` stores `string.Empty`; setting a new value synchronizes the editor and clears undo/redo history. |
| `Selection` | `TextSelection` | Gets the current editor selection. |
| `Caret` | `TextCaret` | Gets the current caret state. |
| `CaretColor` | `DrawColor` | Gets or sets the rendered caret color. |
| `SelectionBackground` | `DrawColor` | Gets or sets the selection highlight color. |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the text measurer used when no resource-backed font path is active. Setting `null` throws `ArgumentNullException`; changing the instance invalidates text metrics. |
| `TextRenderer` | `TextRenderer` | Gets or sets the text renderer used when no resource-backed font path is active. Setting `null` throws `ArgumentNullException`; changing the instance invalidates render. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the optional font resource used for text metrics and rendering. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the optional resource provider used before falling back to the root resource provider. |

## Protected Properties

| Name | Type | Description |
| --- | --- | --- |
| `DisplayText` | `string` | Gets the text used for measurement, rendering, caret positioning, and hit testing. The base implementation returns `Text`; derived controls can override it. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ReceiveTextInput(string text)` | `void` | Inserts normalized, non-control text through the editor, synchronizes `Text`, keeps the caret visible, and resets caret blink. |
| `Select(int anchor, int active)` | `void` | Updates the selection range, keeps the caret visible, and invalidates rendering. |
| `MoveCaret(int position, bool extendSelection = false)` | `void` | Moves the caret, optionally extending selection, keeps the caret visible, resets caret blink, and invalidates rendering. |
| `Undo()` | `bool` | Reverts the latest editor operation when available, synchronizes `Text`, and returns whether text changed. |
| `Redo()` | `bool` | Reapplies the latest undone editor operation when available, synchronizes `Text`, and returns whether text changed. |
| `UpdateRenderTime(TimeSpan frameTime)` | `bool` | Advances caret blink timing. Returns `true` when the blink phase changes and render invalidation is requested. |

## Protected Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetTextCore(string text)` | `void` | Sets the stored text, coerces `null` to `string.Empty`, synchronizes the editor, clears undo/redo state, keeps the caret visible, and invalidates text metrics. |
| `MeasureCore(MeasureContext context)` | `LayoutSize` | Measures `DisplayText` with current text aspect and insets, ensuring the result is tall enough for caret metrics. |
| `ArrangeCore(ArrangeContext context)` | `LayoutRect` | Returns the final arrange rectangle. |
| `OnPropertyChanged(UiPropertyChangedEventArgs args)` | `void` | Synchronizes the editor when `Text` changes externally and resets caret blink when keyboard focus is gained. |
| `OnRender(RenderContext context)` | `void` | Renders chrome, text, selection, selected-text foreground, clipping, and caret. |

## Events

This class does not declare public events.

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Text` | `TextProperty` | `string.Empty` | `UiPropertyOptions.AffectsMeasure`, `UiPropertyOptions.AffectsRender`, `UiPropertyOptions.AffectsSemantics`; coerces `null` to `string.Empty`. |
| `CaretColor` | `CaretColorProperty` | `DrawColor.Black` | `UiPropertyOptions.AffectsRender`. |
| `SelectionBackground` | `SelectionBackgroundProperty` | `new DrawColor(0, 120, 215)` | `UiPropertyOptions.AffectsRender`. |

## Applies to

`Cerneala.UI.Controls.TextBoxBase` in the `Cerneala` project.

## See also

- `UI/Controls/TextBoxBase.cs`
- `UI/Controls/TextBox.cs`
- `UI/Controls/PasswordBox.cs`
- `UI/Accessibility/TextBoxAutomationPeer.cs`
- `tests/Cerneala.Tests/Controls/TextBoxTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxClipboardShortcutTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxCaretBlinkTests.cs`
- `tests/Cerneala.Tests/Controls/TextBoxTwoWayBindingTests.cs`
- `tests/Cerneala.Tests/Controls/PasswordBoxTests.cs`
