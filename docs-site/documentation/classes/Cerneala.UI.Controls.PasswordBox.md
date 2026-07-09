# PasswordBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/PasswordBox.cs`

Represents a text-editing control that stores the password text while rendering a repeated masking character.

```csharp
public class PasswordBox : TextBoxBase
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `TextBoxBase` -> `PasswordBox`

## Examples

Create a password box with the default mask character:

```csharp
using Cerneala.UI.Controls;

PasswordBox passwordBox = new()
{
    Password = "secret"
};

string password = passwordBox.Password;
```

Change the displayed mask character:

```csharp
using Cerneala.UI.Controls;

PasswordBox passwordBox = new()
{
    Password = "secret",
    PasswordChar = '#'
};
```

## Remarks

`PasswordBox` derives from `TextBoxBase` and reuses its retained text editing, caret, selection, keyboard input, pointer selection, undo/redo, and fallback rendering behavior. The stored value is exposed through `Password`, which forwards to the inherited `Text` property.

Rendering uses the protected `DisplayText` override instead of the stored text. `DisplayText` returns `PasswordChar` repeated once for each text element in `Text`, using `StringInfo.ParseCombiningCharacters` to count text elements. For example, the password `"secret"` renders as `"******"` with the default `PasswordChar`.

`PasswordChar` is a UI property with default value `'*'`. Changing it affects measure and render, because the displayed mask text can have different metrics.

`Password` is not stored as a secure string. The password remains available as a normal `string` through `Password`, inherited `Text`, and the inherited text editor state. `PasswordBox` also inherits `TextBoxBase` clipboard and selection behavior; it does not override copy, cut, paste, undo, redo, or text-input handling.

Setting `Password` to `null` stores `string.Empty`.

## Constructors

| Name | Description |
| --- | --- |
| `PasswordBox()` | Initializes a new password box. The inherited `TextBoxBase` constructor makes it focusable, a tab stop, assigns default text-box chrome, and uses the I-beam cursor. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `PasswordCharProperty` | `UiProperty<char>` | Identifies the `PasswordChar` UI property. Defaults to `'*'`; affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PasswordChar` | `char` | Gets or sets the character repeated for each text element when the password is rendered. |
| `Password` | `string` | Gets or sets the stored password text by forwarding to inherited `Text`; setting `null` stores `string.Empty`. |

## Important Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Text` | `string` | `TextBoxBase` | Gets or sets the underlying editable text used by `Password`. Values are coerced to `string.Empty` when `null`. |
| `Editor` | `TextEditor` | `TextBoxBase` | Gets the text editor that owns document, caret, selection, and undo/redo state. |
| `Selection` | `TextSelection` | `TextBoxBase` | Gets the current text selection. |
| `Caret` | `TextCaret` | `TextBoxBase` | Gets the current caret state. |
| `CaretColor` | `DrawColor` | `TextBoxBase` | Gets or sets the rendered caret color. Defaults to `DrawColor.Black`. |
| `SelectionBackground` | `DrawColor` | `TextBoxBase` | Gets or sets the selection highlight color. Defaults to `new DrawColor(0, 120, 215)`. |
| `TextMeasurer` | `TextMeasurer` | `TextBoxBase` | Gets or sets the text measurer used for layout. Rejects `null`; changing it invalidates text metrics. |
| `TextRenderer` | `TextRenderer` | `TextBoxBase` | Gets or sets the text renderer used for fallback text rendering. Rejects `null`; changing it invalidates render. |
| `FontResourceId` | `ResourceId<FontResource>?` | `TextBoxBase` | Gets or sets an optional font resource used when resolving text metrics and rendering. |
| `ResourceProvider` | `IResourceProvider?` | `TextBoxBase` | Gets or sets an optional resource provider used before falling back to the root resource provider. |

## Important Inherited Methods

| Name | Return Type | Declared by | Description |
| --- | --- | --- | --- |
| `ReceiveTextInput(string text)` | `void` | `TextBoxBase` | Inserts normalized non-control text into the password and updates layout/render state. |
| `Select(int anchor, int active)` | `void` | `TextBoxBase` | Updates the current selection and invalidates rendering. |
| `MoveCaret(int position, bool extendSelection = false)` | `void` | `TextBoxBase` | Moves the caret, optionally extending the selection, then resets caret blink and invalidates rendering. |
| `Undo()` | `bool` | `TextBoxBase` | Reverts the latest editor operation when available and syncs the stored text. |
| `Redo()` | `bool` | `TextBoxBase` | Reapplies the latest undone editor operation when available and syncs the stored text. |
| `UpdateRenderTime(TimeSpan frameTime)` | `bool` | `TextBoxBase` | Advances caret blink timing for focused, enabled, render-visible text boxes. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `PasswordChar` | `PasswordCharProperty` | `'*'` | `UiPropertyOptions.AffectsMeasure`, `UiPropertyOptions.AffectsRender` |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/PasswordBox.cs`
- `UI/Controls/TextBoxBase.cs`
- `tests/Cerneala.Tests/Controls/PasswordBoxTests.cs`
