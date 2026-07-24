# TextBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/TextBox.cs`

Represents an extensible single-line plain-text editing control.

```csharp
public class TextBox : Control, ITimeSensitiveRenderElement, IPointerDragSource
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBox`

Implements:
`ITimeSensitiveRenderElement`, `IPointerDragSource`

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

Pointer selection captures the mouse until the left button is released or capture is lost. Long text uses an internal horizontal viewport to keep the caret visible.

The control renders its background, border, text, selection and blinking caret directly. Default chrome is supplied through `AspectBase`, so aspects and local values can override it.

Derived controls can override `NormalizeTextInput`, `OnTextChanged` and `OnSelectionChanged`. Event hooks must call the base implementation when the routed event should continue to be raised.

## Constructors

| Name | Description |
| --- | --- |
| `TextBox()` | Initializes an empty, focusable single-line text box. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `TextProperty` | `UiProperty<string>` | Identifies the concrete `TextBox.Text` UI property. |
| `CaretColorProperty` | `UiProperty<Color>` | Identifies the caret color property. |
| `SelectionBackgroundProperty` | `UiProperty<Color>` | Identifies the selection background property. |
| `TextChangedEvent` | `RoutedEvent` | Identifies the bubbling text-change event. |
| `SelectionChangedEvent` | `RoutedEvent` | Identifies the bubbling selection-change event. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets or sets the editable text. `null` is coerced to an empty string. |
| `Selection` | `TextSelection` | Gets the current selection without exposing the editor. |
| `Caret` | `TextCaret` | Gets the current caret state. |
| `CaretColor` | `Color` | Gets or sets the caret color. |
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
