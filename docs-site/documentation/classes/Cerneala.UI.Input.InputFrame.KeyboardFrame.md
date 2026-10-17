# InputFrame.KeyboardFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputFrame.cs`

Represents keyboard state changes between the previous and current keyboard snapshots in an `InputFrame`.

```csharp
public sealed class InputFrame.KeyboardFrame
```

Inheritance:
`Object` -> `InputFrame.KeyboardFrame`

## Examples

Use the keyboard frame to detect key transitions during input dispatch.

```csharp
using Cerneala.UI.Input;

InputFrame frame = CaptureFrame();

if (frame.Keyboard.IsPressed(InputKey.Enter))
{
    SubmitFocusedControl();
}
```

## Remarks

`InputFrame.KeyboardFrame` compares a previous `KeyboardSnapshot` with a current `KeyboardSnapshot`. It exposes current key state and transition helpers for pressed and released keys.

`GetRepeatCount` reports operating-system key auto-repeats that happened since the previous frame. A held key is down in both snapshots, so `IsPressed` is `false` for it; the repeats are carried separately as the `keyRepeatEvents` argument of `InputFrame`. Example: Backspace was pressed in an earlier frame, is still held, and the operating system auto-repeated it twice before this frame. Then `IsDown(InputKey.Back)` is `true`, `IsPressed(InputKey.Back)` is `false`, and `GetRepeatCount(InputKey.Back)` is `2`. The focus manager raises two `KeyDown` pairs with `KeyEventArgs.IsRepeat` set to `true` for that frame.

The constructor is internal. Instances are created by `InputFrame`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsDown(InputKey)` | `bool` | Returns whether the key is down in the current snapshot. |
| `IsPressed(InputKey)` | `bool` | Returns whether the key is down in the current snapshot and was up in the previous snapshot. |
| `IsReleased(InputKey)` | `bool` | Returns whether the key is up in the current snapshot and was down in the previous snapshot. |
| `GetRepeatCount(InputKey)` | `int` | Returns how many key auto-repeat events the frame carries for the key. Returns `0` when the frame has none for it. |

## Applies to

- `Cerneala.UI.Input.InputFrame.KeyboardFrame`

## See also

- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Input.InputKey`
- `Cerneala.UI.Input.KeyEventArgs`
