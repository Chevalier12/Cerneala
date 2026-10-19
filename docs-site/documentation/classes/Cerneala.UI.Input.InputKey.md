# InputKey Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputKey.cs`

Identifies a keyboard key in Cerneala input frames, key events, and key gestures.

```csharp
public enum InputKey
```

## Examples

Build a keyboard snapshot and react to a key in a key handler:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

KeyboardSnapshot keyboard = KeyboardSnapshot.FromDownKeys([InputKey.None, InputKey.Escape]);
Console.WriteLine(keyboard.IsDown(InputKey.Escape)); // True
Console.WriteLine(keyboard.IsDown(InputKey.None));   // False, None is dropped

UIElement panel = new() { Focusable = true };
panel.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
{
    if (args is KeyEventArgs { Key: InputKey.Escape })
    {
        args.Handled = true;
    }
});
```

## Remarks

`Unknown` and `None` are not real keys:

- `KeyboardSnapshot.FromDownKeys` drops them, so `IsDown` returns `false` for them.
- The SDL input source maps an unmapped scancode to `Unknown` and then ignores it.
- `KeyGesture` throws `ArgumentException` when created with either one.
- Keyboard dispatch skips them.
- Servo rejects them with `ArgumentOutOfRangeException`, together with any undefined value.

For every other key, the focus manager raises `PreviewKeyDown` and `KeyDown` on the focused element when the key goes down in a frame, and `PreviewKeyUp` and `KeyUp` when it goes up.

The SDL input source produces every other member: letters, digits, `F1` to `F12`, the arrow, editing, and navigation keys, the left and right Shift, Ctrl, and Alt keys, the numeric keypad keys, and the punctuation keys. The keypad and punctuation keys have the names of WPF `System.Windows.Input.Key`. Keypad Enter is `Enter`, as in WPF.

A key raises its key events and, separately, the text it types. Example: with Num Lock on, keypad 5 in a focused `TextBox` raises `KeyDown` with `NumPad5`, then `TextInput` with the text `"5"`, then `KeyUp` with `NumPad5`; the `.` key raises `KeyDown` with `OemPeriod`, then `TextInput` with `"."`.

```csharp
TextBox box = new();
box.KeyDown += (_, e) => Console.WriteLine(((KeyEventArgs)e).Key);
// Keypad 5 pressed in the focused box prints "NumPad5", and the box text becomes "5".
```

The SDL input source maps SDL scancodes, which name a key by its position on a US keyboard, not by the character it types in the current layout. Example: the key right of `L` is `OemSemicolon` also on a German keyboard, where it types `ö`.

Some keys have built-in behavior:

| Keys | Behavior |
| --- | --- |
| `Tab`, with Shift for backward | Keyboard focus navigation. |
| `Enter`, `Space` | Activation of buttons and other activatable command sources. |
| `Back`, `Delete`, `Left`, `Right`, `Home`, `End` | Text editing in `TextBox` and `PasswordBox`. |
| `A`, `C`, `X`, `V` with Ctrl and without Alt | Select all, copy, cut, and paste in text input. |
| `LeftShift`/`RightShift`, `LeftCtrl`/`RightCtrl`, `LeftAlt`/`RightAlt` | Set `KeyEventArgs.IsShiftDown`, `IsControlDown`, and `IsAltDown`, and match `KeyModifiers` in `KeyGesture`. |
| `Up`, `Down`, `Escape`, `Enter`, `Tab`, `F4` | `ComboBox` and `Menu` keyboard navigation. |

The numeric values follow declaration order: `Unknown` is `0`, `None` is `1`, `A` is `27`, `F12` is `70`, and `OemBackslash` is `97`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Unknown` | `0` | A key that has no mapping. Ignored by input dispatch. |
| `None` | `1` | No key. Ignored by input dispatch. |
| `Back` | `2` | Backspace. |
| `Tab` | `3` | Tab. |
| `Enter` | `4` | Enter. |
| `Escape` | `5` | Escape. |
| `Space` | `6` | Space bar. |
| `PageUp` | `7` | Page Up. |
| `PageDown` | `8` | Page Down. |
| `End` | `9` | End. |
| `Home` | `10` | Home. |
| `Left` | `11` | Left arrow. |
| `Up` | `12` | Up arrow. |
| `Right` | `13` | Right arrow. |
| `Down` | `14` | Down arrow. |
| `Insert` | `15` | Insert. |
| `Delete` | `16` | Delete. |
| `D0` to `D9` | `17` to `26` | Digit keys `0` to `9` on the main keyboard row. |
| `A` to `Z` | `27` to `52` | Letter keys. |
| `LeftShift` | `53` | Left Shift. |
| `RightShift` | `54` | Right Shift. |
| `LeftCtrl` | `55` | Left Ctrl. |
| `RightCtrl` | `56` | Right Ctrl. |
| `LeftAlt` | `57` | Left Alt. |
| `RightAlt` | `58` | Right Alt. |
| `F1` to `F12` | `59` to `70` | Function keys. |
| `NumPad0` to `NumPad9` | `71` to `80` | Digit keys `0` to `9` on the numeric keypad. |
| `Multiply` | `81` | Keypad `*`. |
| `Add` | `82` | Keypad `+`. |
| `Subtract` | `83` | Keypad `-`. |
| `Decimal` | `84` | Keypad `.`. |
| `Divide` | `85` | Keypad `/`. |
| `OemSemicolon` | `86` | The `;` and `:` key. |
| `OemPlus` | `87` | The `=` and `+` key. |
| `OemComma` | `88` | The `,` and `<` key. |
| `OemMinus` | `89` | The `-` and `_` key. |
| `OemPeriod` | `90` | The `.` and `>` key. |
| `OemQuestion` | `91` | The `/` and `?` key. |
| `OemTilde` | `92` | The `` ` `` and `~` key. |
| `OemOpenBrackets` | `93` | The `[` and `{` key. |
| `OemPipe` | `94` | The `\` and `\|` key above Enter. |
| `OemCloseBrackets` | `95` | The `]` and `}` key. |
| `OemQuotes` | `96` | The `'` and `"` key. |
| `OemBackslash` | `97` | The extra key next to left Shift on ISO keyboards. |

## Applies to

`KeyboardSnapshot`, `InputFrame.KeyboardFrame`, `KeyEventArgs`, `KeyGesture`, `KeyBinding`, and Servo key input.

## See also

- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Input.KeyEventArgs`
- `Cerneala.UI.Input.KeyGesture`
- `Cerneala.UI.Input.KeyModifiers`
- `Cerneala.UI.Input.KeyBinding`
