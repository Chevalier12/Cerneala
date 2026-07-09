# InputFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputFrame.cs`

Represents one retained UI input frame by comparing previous and current pointer and keyboard snapshots, and by carrying text input events for the frame.

```csharp
public sealed class InputFrame
```

Inheritance:
`Object` -> `InputFrame`

## Examples

Create an input frame from previous and current snapshots, then read per-frame transitions from the nested pointer and keyboard views.

```csharp
using Cerneala.UI.Input;

PointerSnapshot previousPointer = PointerSnapshot.Empty.WithWheelValue(120);
PointerSnapshot currentPointer = previousPointer
    .WithPosition(24, 48)
    .WithWheelValue(360)
    .WithButton(InputMouseButton.Left, isDown: true);

KeyboardSnapshot previousKeyboard = KeyboardSnapshot.Empty;
KeyboardSnapshot currentKeyboard = KeyboardSnapshot.FromDownKeys(new[] { InputKey.Enter });

InputFrame frame = new(
    previousPointer,
    currentPointer,
    previousKeyboard,
    currentKeyboard,
    new[] { new TextInputSnapshotEvent("a") });

bool leftPressedThisFrame = frame.Pointer.IsPressed(InputMouseButton.Left);
bool enterPressedThisFrame = frame.Keyboard.IsPressed(InputKey.Enter);
int wheelDelta = frame.Pointer.WheelDelta;
string text = frame.TextInputEvents[0].Text;
```

## Remarks

`InputFrame` is a snapshot-delta object for one update pass. It stores a `PointerFrame` built from the previous and current `PointerSnapshot`, a `KeyboardFrame` built from the previous and current `KeyboardSnapshot`, and a read-only copy of the provided text input events.

Pointer and keyboard transition methods compare the current snapshot against the previous snapshot. `IsDown` reports current state, `IsPressed` reports a transition from up to down, and `IsReleased` reports a transition from down to up.

`TextInputEvents` is copied from the constructor argument with `ToArray()` and exposed as a read-only list. Later changes to the caller's source list are not reflected by the frame. The event objects themselves are not cloned.

The nested `PointerFrame` and `KeyboardFrame` classes are created by `InputFrame`; their constructors are internal, so callers normally access them through the `Pointer` and `Keyboard` properties.

## Constructors

| Name | Description |
| --- | --- |
| `InputFrame(PointerSnapshot, PointerSnapshot, KeyboardSnapshot, KeyboardSnapshot, IReadOnlyList<TextInputSnapshotEvent>)` | Initializes an input frame from previous/current pointer snapshots, previous/current keyboard snapshots, and text input events. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Pointer` | `InputFrame.PointerFrame` | Gets the pointer state and pointer transitions for this frame. |
| `Keyboard` | `InputFrame.KeyboardFrame` | Gets the keyboard state and keyboard transitions for this frame. |
| `TextInputEvents` | `IReadOnlyList<TextInputSnapshotEvent>` | Gets the copied, read-only text input events associated with this frame. |

## Nested Types

| Name | Description |
| --- | --- |
| `InputFrame.PointerFrame` | Exposes current pointer coordinates, wheel state, wheel delta, and mouse button transitions for the frame. |
| `InputFrame.KeyboardFrame` | Exposes key state and key transitions for the frame. |

## PointerFrame Properties

| Name | Type | Description |
| --- | --- | --- |
| `X` | `float` | Gets the current pointer X coordinate. |
| `Y` | `float` | Gets the current pointer Y coordinate. |
| `WheelValue` | `int` | Gets the current pointer wheel value. |
| `WheelDelta` | `int` | Gets the difference between the current and previous pointer wheel values. |

## PointerFrame Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsDown(InputMouseButton)` | `bool` | Returns whether the mouse button is down in the current pointer snapshot. |
| `IsPressed(InputMouseButton)` | `bool` | Returns whether the mouse button is down in the current pointer snapshot and was not down in the previous snapshot. |
| `IsReleased(InputMouseButton)` | `bool` | Returns whether the mouse button is not down in the current pointer snapshot and was down in the previous snapshot. |

## KeyboardFrame Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsDown(InputKey)` | `bool` | Returns whether the key is down in the current keyboard snapshot. |
| `IsPressed(InputKey)` | `bool` | Returns whether the key is down in the current keyboard snapshot and was not down in the previous snapshot. |
| `IsReleased(InputKey)` | `bool` | Returns whether the key is not down in the current keyboard snapshot and was down in the previous snapshot. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `InputFrame(...)` | `ArgumentNullException` | `previousPointer`, `currentPointer`, `previousKeyboard`, `currentKeyboard`, or `textInputEvents` is `null`. |

## Applies to

Cerneala retained UI input frame processing.

## See also

- `Cerneala.UI.Input.PointerSnapshot`
- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Input.TextInputSnapshotEvent`
- `Cerneala.UI.Input.ElementInputBridge`
