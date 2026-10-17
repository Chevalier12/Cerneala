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
