# InputStateEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputStateEventArgs.cs`

Provides the routed data of `PreviewInputHeld` and `InputHeld`: which keys and mouse buttons are down in the current frame, where the pointer is, and how much time has passed since the previous frame of the hold.

```csharp
public sealed class InputStateEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `InputStateEventArgs`

## Examples

Move a `Sprite2D` at 120 pixels per second with the A, D, W and S keys. The handler is on the `Window`, so it runs while nothing is focused and while a descendant of the window is focused. `Elapsed` is zero on the first frame of a hold, so that frame moves nothing.

```csharp
const float speed = 120f;

window.InputHeld += (_, args) =>
{
    Vector2 direction = new(
        (args.IsDown(InputKey.D) ? 1f : 0f) - (args.IsDown(InputKey.A) ? 1f : 0f),
        (args.IsDown(InputKey.S) ? 1f : 0f) - (args.IsDown(InputKey.W) ? 1f : 0f));
    if (direction == Vector2.Zero)
    {
        return;
    }

    direction = Vector2.Normalize(direction);
    float seconds = (float)args.Elapsed.TotalSeconds;
    sprite.X += direction.X * speed * seconds;
    sprite.Y += direction.Y * speed * seconds;
};
```

Read the left mouse button and the pointer position while it is held.

```csharp
element.InputHeld += (_, args) =>
{
    if (args.IsDown(InputMouseButton.Left))
    {
        Vector2 position = args.PointerPosition;
    }
};
```

Create the event data directly when raising or testing input behavior.

```csharp
KeyboardSnapshot keyboard = KeyboardSnapshot.FromDownKeys([InputKey.D]);

InputStateEventArgs args = new(
    InputEvents.InputHeldEvent,
    new object(),
    keyboard,
    PointerSnapshot.Empty,
    TimeSpan.FromMilliseconds(16));
```

## Remarks

`InputStateEventArgs` is used by the routed events `InputEvents.PreviewInputHeldEvent` (`Tunnel`) and `InputEvents.InputHeldEvent` (`Bubble`). On `UIElement` they are the CLR events `PreviewInputHeld` and `InputHeld`, declared like `PreviewKeyDown` and `KeyDown`. `PreviewInputHeld` tunnels from the root to the target first, then `InputHeld` bubbles from the target to the root. A handler that sets `Handled` on `PreviewInputHeld` stops the `InputHeld` handlers that were not registered with `handledEventsToo`, as for the key events.

The pair is raised once per rendered frame while at least one key or mouse button is down. It is raised after the rest of the input of that frame, so `KeyDown`, `MouseDown` and text input of the frame that pressed the key come first. Nothing is raised while no key and no mouse button is down: pointer movement and the mouse wheel alone do not count, and the frame that releases the last key or button raises no `InputHeld`.

The target is the focused element. When no element can take keyboard input, the target is the first visual child of the root that is registered in the input route map, which is the hosted `Window`, so a `Window` handler works with nothing focused. When the root has no such child, the target is the root. `OriginalSource` is the target.

`Elapsed` is `TimeSpan.Zero` on the first frame of a hold. On the frames after it, `Elapsed` is the frame time that the host gave to input dispatch. A frame with no key and no button down ends the hold, so the next press starts a new hold whose first frame again has a zero `Elapsed`.

A window renders a frame only when something asks for one, so a held key alone would not produce frames. The window keeps rendering frames while input is held only when some element of the input route tree has a `PreviewInputHeld` or `InputHeld` handler. Without such a handler a held key schedules no frames. The host looks for these handlers after each input dispatch. Servo `WaitForIdleAsync` does not report idle while this condition holds.

A key held while the SDL3 window loses focus is dropped without a release, so `InputHeld` stops being raised for it. A key or button held while the window is disabled is dropped the same way.

`IsDown` reads the snapshot taken when the frame was dispatched. Changing keyboard state later does not update an existing `InputStateEventArgs` object. `PointerPosition` is the pointer position in root coordinates.

The constructor requires non-null `routedEvent` and `originalSource` through the `RoutedEventArgs` base constructor, and throws `ArgumentNullException` for a null `keyboard` or `pointer`.

## Constructors

| Name | Description |
| --- | --- |
| `InputStateEventArgs(RoutedEvent routedEvent, object originalSource, KeyboardSnapshot keyboard, PointerSnapshot pointer, TimeSpan elapsed)` | Initializes the event data from the keyboard and pointer snapshots of a frame and the time since the previous frame of the hold. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Elapsed` | `TimeSpan` | Gets the time since the previous frame of the hold. It is `TimeSpan.Zero` on the first frame of a hold. |
| `PointerPosition` | `Vector2` | Gets the pointer position of the frame, in root coordinates. |

## Methods

| Name | Description |
| --- | --- |
| `IsDown(InputKey key)` | Returns whether the key is down in the frame. |
| `IsDown(InputMouseButton button)` | Returns whether the mouse button is down in the frame. `InputMouseButton.None` is never down. |

## Inherited Properties

| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event represented by this event data. |
| `OriginalSource` | `object` | Gets the original source object supplied when the event data was created. |
| `Source` | `object` | Gets or sets the current routed source. It is initialized to `OriginalSource`. |
| `Handled` | `bool` | Gets or sets whether the routed event has been handled. |

## Related Routed Events

| Event Field | Routing Strategy | Description |
| --- | --- | --- |
| `InputEvents.PreviewInputHeldEvent` | `Tunnel` | Preview event raised each frame while a key or mouse button is down. |
| `InputEvents.InputHeldEvent` | `Bubble` | Bubble event raised after `PreviewInputHeld` unless it was handled. |

## Applies to

`Cerneala` retained UI input routing.

## See also

- `InputEvents`
- `KeyEventArgs`
- `KeyboardSnapshot`
- `PointerSnapshot`
- `InputKey`
- `InputMouseButton`
- `RoutedEventArgs`
- `UIElement`
