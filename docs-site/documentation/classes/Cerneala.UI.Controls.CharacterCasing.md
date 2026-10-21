# CharacterCasing Enum

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/CharacterCasing.cs`

Specifies how a `TextBox` changes the case of the text the user types or pastes.

```csharp
public enum CharacterCasing
```

## Examples

```csharp
using Cerneala.UI.Controls;

TextBox textBox = new() { CharacterCasing = CharacterCasing.Upper };

// The user types "abC" and then pastes "xY":
// Text == "ABCXY"

textBox.CharacterCasing = CharacterCasing.Lower;

// The user types "QrS":
// Text == "ABCXYqrs": the earlier characters keep their case.

textBox.Text = "MiXeD";
// Text == "MiXeD": text set by code keeps its case.
```

## Remarks

`TextBox.CharacterCasing` defaults to `Normal`. The setting changes the case of typed text (`TextBox.ReceiveTextInput` and the platform text input) and of pasted text (Ctrl+V and `TextBox.Paste`) before `MaxLength` cuts it. It does not change text that code assigns to `Text` or `SelectedText`, text that is already in the box, or the result of undo and redo.

`Upper` and `Lower` use the invariant culture (`ToUpperInvariant` and `ToLowerInvariant`). WPF uses the culture of the current input language; Cerneala has no input language.

`PasswordBox` has no `CharacterCasing`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Normal` | `0` | Keeps the case of the text. |
| `Lower` | `1` | Converts typed and pasted text to lower case. |
| `Upper` | `2` | Converts typed and pasted text to upper case. |

## Applies to

`TextBox.CharacterCasing`.

## See also

- `Cerneala.UI.Controls.TextBox`
