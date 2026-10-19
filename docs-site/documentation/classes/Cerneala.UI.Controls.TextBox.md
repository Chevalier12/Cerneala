# TextBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBox.cs`

Represents an extensible single-line plain-text editing control.

```csharp
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

Input is single-line. The default normalizer removes control characters. Keyboard handling supports navigation, selection extension, backspace, delete, undo/redo through the public methods, and Ctrl+A/C/X/V when a platform clipboard is available.

When `IsReadOnly` is `true`, text can still be selected and copied, and `Text` can still be assigned programmatically. User insertion, deletion, cut, paste, undo, and redo are rejected.

Pointer selection captures the mouse until the left button is released or capture is lost. Long text uses an internal horizontal viewport to keep the caret visible.

An enabled text box owns a left click into it, as WPF's text editor handles `MouseLeftButtonDown` and `MouseLeftButtonUp`. It marks the left `MouseDown` handled, the input system does not set `IsPressed` of an enclosing `ButtonBase` for the press, and it marks handled the left `MouseUp` that ends the pointer selection it began. An enclosing `Button` therefore raises no `Click` and runs no `Command` for a click into the text box, and an enclosing `ListBoxItem` is not selected by it. A release over the text box after a press elsewhere is not handled by the text box.

A pointer press places the caret, and a drag extends the selection, at the character drawn under the pointer. The unrounded root pointer position is converted through the render transforms of the text box and every ancestor, like `MouseEventArgs.GetPosition(textBox)`, before it is compared with the caret stops of the text. For example, for a text box at the left edge of a panel at the root origin with `RenderTransform = new Transform(Matrix3x2.CreateScale(2, 2))` and `RenderTransformOrigin = new LayoutPoint(0, 0)`, a click at root x `60` gives the same caret as a click at root x `30` when the panel has no transform. A pointer position that cannot be converted, for example through a non-invertible transform, keeps the current caret.

The control renders its background, border, text, selection and blinking caret directly. Default chrome is stored as framework defaults at the `Default` value source: `Padding` `4,2,4,2`, `BorderThickness` `1`, `BorderBrush` `Color(120, 130, 145)`, and a white `Background`. Any Aspect, local, or other higher-source value replaces them.

Derived controls can override `NormalizeTextInput`, `OnTextChanged` and `OnSelectionChanged`. Event hooks must call the base implementation when the routed event should continue to be raised.

## Constructors

| Name | Description |
| --- | --- |
| `TextBox()` | Initializes an empty, focusable single-line text box. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the concrete `TextBox.Text` UI property. |
| `IsReadOnlyProperty` | `UiProperty<bool>` | Identifies the read-only editing property. Defaults to `false`. |
| `CaretBrushProperty` | `UiProperty<Brush>` | Identifies the caret brush property. |
| `SelectionBackgroundProperty` | `UiProperty<Color>` | Identifies the selection background property. |
| `TextChangedEvent` | `RoutedEvent` | Identifies the bubbling text-change event. |
| `SelectionChangedEvent` | `RoutedEvent` | Identifies the bubbling selection-change event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets or sets the editable text. `null` is coerced to an empty string. |
| `IsReadOnly` | `bool` | Gets or sets whether user editing operations are rejected. |
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
| `ReceiveTextInput(string)` | `void` | Normalizes and inserts text at the current selection. |
| `Select(int, int)` | `void` | Sets the selection anchor and active positions. |
| `MoveCaret(int, bool)` | `void` | Moves the caret and optionally extends the selection. |
| `Undo()` | `bool` | Restores the previous editor snapshot when available. |
| `Redo()` | `bool` | Reapplies the next editor snapshot when available. |
| `UpdateRenderTime(TimeSpan)` | `bool` | Advances caret blink state. |

## Events

| Name | Description |
| --- | --- |
| `TextChanged` | Occurs when `Text` changes. |
| `SelectionChanged` | Occurs when the selection changes. |

## Applies to

Project: `Cerneala`

## See also

- `PasswordBox`
- `TextSelection`
- `TextCaret`
