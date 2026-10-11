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

The SDL input source produces every other member: letters, digits, `F1` to `F12`, the arrow, editing, and navigation keys, and the left and right Shift, Ctrl, and Alt keys.

Some keys have built-in behavior:

| Keys | Behavior |
| --- | --- |
| `Tab`, with Shift for backward | Keyboard focus navigation. |
| `Enter`, `Space` | Activation of buttons and other activatable command sources. |
| `Back`, `Delete`, `Left`, `Right`, `Home`, `End` | Text editing in `TextBox` and `PasswordBox`. |
| `A`, `C`, `X`, `V` with Ctrl and without Alt | Select all, copy, cut, and paste in text input. |
| `LeftShift`/`RightShift`, `LeftCtrl`/`RightCtrl`, `LeftAlt`/`RightAlt` | Set `KeyEventArgs.IsShiftDown`, `IsControlDown`, and `IsAltDown`, and match `KeyModifiers` in `KeyGesture`. |
| `Up`, `Down`, `Escape`, `Enter`, `Tab`, `F4` | `ComboBox` and `Menu` keyboard navigation. |

The numeric values follow declaration order: `Unknown` is `0`, `None` is `1`, `A` is `27`, and `F12` is `70`.

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

## Applies to

`KeyboardSnapshot`, `InputFrame.KeyboardFrame`, `KeyEventArgs`, `KeyGesture`, `KeyBinding`, and Servo key input.

## See also

- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Input.KeyEventArgs`
- `Cerneala.UI.Input.KeyGesture`
- `Cerneala.UI.Input.KeyModifiers`
- `Cerneala.UI.Input.KeyBinding`
