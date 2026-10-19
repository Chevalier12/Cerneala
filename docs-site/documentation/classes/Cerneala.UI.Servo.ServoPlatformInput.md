# ServoPlatformInput Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoPlatformInput.cs`

Describes one raw input event that `Servo.SendPlatformInputAsync` pushes through a Window platform's own event queue.

```csharp
public sealed class ServoPlatformInput
```

## Examples

```csharp
// Hold Backspace with two auto-repeat events, release it, then type text.
await servo.SendPlatformInputAsync(
    ServoPlatformInput.KeyDown(InputKey.Back),
    ServoPlatformInput.KeyDown(InputKey.Back, repeat: true),
    ServoPlatformInput.KeyDown(InputKey.Back, repeat: true),
    ServoPlatformInput.KeyUp(InputKey.Back),
    ServoPlatformInput.Text("abc"));

// Press keypad 5, which types "5", while Backspace is held, as a keyboard does.
await servo.SendPlatformInputAsync(
    ServoPlatformInput.KeyDown(InputKey.Back),
    ServoPlatformInput.KeyDown(InputKey.NumPad5),
    ServoPlatformInput.Text("5"),
    ServoPlatformInput.KeyUp(InputKey.NumPad5),
    ServoPlatformInput.KeyDown(InputKey.Back, repeat: true),
    ServoPlatformInput.KeyUp(InputKey.Back));

// Click at client DIP (120, 40), then scroll down two notches at the same point.
var point = new ServoPoint(120, 40);
await servo.SendPlatformInputAsync(
    ServoPlatformInput.PointerMove(point),
    ServoPlatformInput.PointerDown(point, InputMouseButton.Left),
    ServoPlatformInput.PointerUp(point, InputMouseButton.Left),
    ServoPlatformInput.Wheel(point, -240));
```

## Remarks

Instances are immutable and created only through the factory methods. They have no public constructor.

Each event becomes one platform event, in the order given. The platform input source then builds frames from them exactly as it does for a physical device. That is the difference from `Servo.PressKeyAsync` and the other Servo actions, which build `InputFrame` values themselves and never touch the platform. See "Input paths" on the `Servo` page.

`KeyDown(key, repeat: true)` is an auto-repeat event: the key is already down, and the app sees a `KeyDown` with `IsRepeat` set to `true`. `Text` is committed text input, for example the `"A"` that follows `KeyDown(InputKey.A)` with Shift held. It is dropped by the platform while text input is inactive.

Every `InputKey` member except `None` and `Unknown` can be sent, including the keypad keys (`NumPad0` to `NumPad9`, `Multiply`, `Add`, `Subtract`, `Decimal`, `Divide`) and the punctuation keys (`OemPeriod`, `OemComma`, `OemMinus`, `OemPlus` and the other `Oem` keys). The SDL platform sends each key as its SDL scancode, for example `NumPad5` as `SDL_SCANCODE_KP_5` (93) and `OemPeriod` as `SDL_SCANCODE_PERIOD` (55). `Enter` is sent as the main Enter key; keypad Enter also arrives as `Enter`. A key event carries no text: send the text the key types with `Text`, as in the example above.

Every key is sent with Num Lock on (`SDL_KMOD_NUM` set), except `Clear`, which is sent as keypad 5 (`SDL_SCANCODE_KP_5`) with Num Lock off, the only way a keyboard produces it. To send another keypad key with Num Lock off, send the key it arrives as, because the app sees the same `InputKey`: keypad 4 with Num Lock off is `KeyDown(InputKey.Left)`, keypad `.` with Num Lock off is `KeyDown(InputKey.Delete)`. These are sent as the arrow and editing keys (`InputKey.Left` as `SDL_SCANCODE_LEFT`, 80), not as keypad scancodes. See `InputKey` for the complete Num Lock table.

Pointer positions are client DIPs. A wheel delta uses the unit of `Servo.ScrollAsync`: 120 per notch, and the sign is the direction. SDL reports whole notches, so the delta of an SDL window must be a non-zero multiple of 120; another value makes `SendPlatformInputAsync` throw `ArgumentException` before any event is pushed.

Factories throw `ArgumentOutOfRangeException` for `InputKey.None`, `InputKey.Unknown` or an undefined key, for `InputMouseButton.None` or an undefined button, and for a zero wheel delta. `Text` throws `ArgumentException` for null or empty text.

## Methods

| Name | Description |
| --- | --- |
| `KeyDown(InputKey, bool)` | Creates a key-down event; `repeat: true` marks it as an auto-repeat. |
| `KeyUp(InputKey)` | Creates a key-up event. |
| `Text(string)` | Creates a committed text input event. |
| `PointerMove(ServoPoint)` | Creates a pointer move to a client DIP position. |
| `PointerDown(ServoPoint, InputMouseButton)` | Creates a pointer button press at a client DIP position. |
| `PointerUp(ServoPoint, InputMouseButton)` | Creates a pointer button release at a client DIP position. |
| `Wheel(ServoPoint, int)` | Creates a wheel event at a client DIP position; the delta is in the unit of `ScrollAsync`. |

## Applies to

`Servo.SendPlatformInputAsync` on a `Window` whose platform can inject input events (the SDL platform).

## See also

- `Servo`
- `ServoPoint`
