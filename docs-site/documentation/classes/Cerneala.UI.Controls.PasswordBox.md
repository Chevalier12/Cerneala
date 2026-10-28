# PasswordBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/PasswordBox.cs`

Represents an extensible single-line password entry control with masked rendering.

```csharp
[TemplatePart("PART_ContentHost", typeof(ScrollViewer))]
public class PasswordBox : Control, ITimeSensitiveRenderElement, IPointerDragSource, ITextInputHost
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `PasswordBox`

Implements:
`ITimeSensitiveRenderElement`, `IPointerDragSource`, `ITextInputHost`

## Examples

```csharp
using Cerneala.UI.Controls;

PasswordBox passwordBox = new()
{
    Password = "secret",
    PasswordChar = '#'
};
```

## Remarks

`PasswordBox` exposes content only through `Password`. It does not expose `Text`, the internal editor, selection/caret state, or undo/redo operations.

Copy and cut shortcuts are consumed without placing password text on the clipboard. Paste remains available and passes through single-line input normalization. Password edits do not create undo or redo snapshots, so Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z do nothing: after typing `abc` and pressing Ctrl+Z, `Password` is still `"abc"`. With `Password = "secret"` and the clipboard holding `"keep"`, Ctrl+A, Ctrl+C and Ctrl+X leave the clipboard at `"keep"` and `Password` at `"secret"`.

`PasswordBox` has none of the multi-line and limit members of `TextBox`: it has no `AcceptsReturn`, `AcceptsTab`, `TextWrapping`, `MaxLength`, `CharacterCasing`, `MinLines`, `MaxLines`, scroll bar visibility properties or `LineCount`. It is always one line without wrapping. Enter and Tab stay unhandled, and the default normalizer removes line breaks and tabs.

### Template

`PasswordBox` shares the default template of `TextBox`: a `Border` that contains a `ScrollViewer` named `PART_ContentHost`, which contains an internal view that draws the mask characters, the selection and the caret. The `Border` takes `Background`, `BorderBrush` and `BorderThickness` from the password box, and the `ScrollViewer` takes `Padding` as its `Margin`. The password box sets both scroll bar policies of the `ScrollViewer` to `ScrollBarVisibility.Hidden`: long input scrolls horizontally to keep the caret visible, and no scroll bar shows. A replacement `ComponentTemplate` must provide a `ScrollViewer` part named `PART_ContentHost`.

Pointer press and drag place the internal caret and selection like `TextBox`: at the mask character drawn under the pointer, after converting the unrounded root pointer position through the render transforms of the content host and every ancestor. A pointer position that cannot be converted keeps the current caret. Shift+click extends the selection from its anchor to the click, as in `TextBox`: with `Password == "secret"` and the caret at the start, Shift+click right of the mask and then typing `X` gives `Password == "X"`.

Double-click and triple-click select by the mask, not by the words of the password. The mask is one run of one character, so a double-click or a triple-click selects the whole password and shows nothing of its words. For the same reason Ctrl+Left and Ctrl+Right move over the mask, which has no word structure, as in WPF's `PasswordBox`: Ctrl+Left goes to the start of the password and Ctrl+Right to its end, and Shift extends the selection. For example, with `Password == "ab cd"` and the caret at the end, Ctrl+Left and then typing `X` gives `"Xab cd"`; Ctrl+Right and then typing `Y` gives `"Xab cdY"`; Ctrl+Shift+Left from the end and then typing `X` replaces everything, so `Password == "X"`. The password is also one word for deletion: Ctrl+Backspace deletes from the start of the password to the caret and Ctrl+Delete from the caret to the end. For example, with `Password == "secret"` and the caret at the end, Ctrl+Backspace gives `Password == ""`; with the caret at the start, Ctrl+Delete gives `Password == ""`; with `Password == "ab cd"` and the caret before the last character, Ctrl+Backspace gives `"d"`. With a selection they delete only the selection, like Backspace and Delete.

The internal view reserves one device pixel (`1 / Root.Scale` layout units) right of the mask for the caret, as `TextBox` does, so an auto-width password box is one device pixel wider than its mask and the caret after the last character stays visible.

Like `TextBox`, an enabled password box owns a left click into it: it marks the left `MouseDown` and the left `MouseUp` that ends its pointer selection handled, and an enclosing `ButtonBase` is not pressed, raises no `Click`, and runs no `Command` for that click.

Rendering uses one `PasswordChar` for each Unicode text element in the stored password. Accessibility semantics report an editable-text role but never publish the password value.

`Password` is still a normal managed `string`. The control prevents accidental exposure through editing APIs, clipboard history, undo history and semantics; it does not provide secure-memory wiping.

Default chrome is stored as framework defaults at the `Default` value source: `Padding` `4,2,4,2`, `BorderThickness` `1`, `BorderBrush` `Color(120, 130, 145)`, and a white `Background`. Any Aspect, local, or other higher-source value replaces them. Derived controls can override `NormalizeTextInput` and `OnPasswordChanged` without receiving access to the internal editor.

## Constructors

| Name | Description |
| --- | --- |
| `PasswordBox()` | Initializes an empty, focusable password box. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `PasswordChangedEvent` | `RoutedEvent` | Identifies the bubbling password-change event. |
| `PasswordCharProperty` | `UiProperty<char>` | Identifies the mask-character property. |
| `CaretBrushProperty` | `UiProperty<Brush>` | Identifies the caret brush property. |
| `SelectionBackgroundProperty` | `UiProperty<Color>` | Identifies the selection background property. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Password` | `string` | Gets or sets the stored password. `null` becomes an empty string. |
| `PasswordChar` | `char` | Gets or sets the mask character. The default is `*`. |
| `CaretBrush` | `Brush` | Gets or sets the brush used to paint the caret. |
| `SelectionBackground` | `Color` | Gets or sets the internal selection highlight color. |
| `TextMeasurer` | `TextMeasurer` | Gets or sets the fallback text measurer. |
| `TextRenderer` | `TextRenderer` | Gets or sets the fallback text renderer. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the optional font resource identifier. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the optional resource provider. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `UpdateRenderTime(TimeSpan)` | `bool` | Advances the internal caret blink state. |

## Events

| Name | Description |
| --- | --- |
| `PasswordChanged` | Occurs once for each effective password change. |

## Applies to

Project: `Cerneala`

## See also

- `TextBox`
- `PasswordBoxAutomationPeer`
