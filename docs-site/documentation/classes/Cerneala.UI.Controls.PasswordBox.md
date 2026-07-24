# PasswordBox Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/PasswordBox.cs`

Represents an extensible single-line password entry control with masked rendering.

```csharp
public class PasswordBox : Control, ITimeSensitiveRenderElement, IPointerDragSource
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `PasswordBox`

Implements:
`ITimeSensitiveRenderElement`, `IPointerDragSource`

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

Copy and cut shortcuts are consumed without placing password text on the clipboard. Paste remains available and passes through single-line input normalization. Password edits do not create undo or redo snapshots.

Rendering uses one `PasswordChar` for each Unicode text element in the stored password. Accessibility semantics report an editable-text role but never publish the password value.

`Password` is still a normal managed `string`. The control prevents accidental exposure through editing APIs, clipboard history, undo history and semantics; it does not provide secure-memory wiping.

Default chrome is supplied through `AspectBase`. Derived controls can override `NormalizeTextInput` and `OnPasswordChanged` without receiving access to the internal editor.

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
