# InputFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputFrame.cs`

Represents one retained UI input frame by comparing previous and current pointer and keyboard snapshots, and by carrying text input events, key auto-repeat events, and wheel events for the frame.

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

Create a frame in which a held Backspace key auto-repeated twice since the previous frame.

```csharp
using Cerneala.UI.Input;

KeyboardSnapshot held = KeyboardSnapshot.FromDownKeys(new[] { InputKey.Back });

InputFrame frame = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    held,
    held,
    Array.Empty<TextInputSnapshotEvent>(),
    new[] { InputKey.Back, InputKey.Back });

bool pressedThisFrame = frame.Keyboard.IsPressed(InputKey.Back); // false: it was already down
int repeats = frame.Keyboard.GetRepeatCount(InputKey.Back);      // 2
```

Create a frame in which three wheel notches down arrived since the previous frame.

```csharp
using Cerneala.UI.Input;

PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(10, 10);

InputFrame frame = new(
    previous,
    previous.WithWheelValue(-360),
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.Empty,
    Array.Empty<TextInputSnapshotEvent>(),
    Array.Empty<InputKey>(),
    new[] { -120, -120, -120 });

int total = frame.Pointer.WheelDelta;                  // -360
IReadOnlyList<int> events = frame.Pointer.WheelDeltas; // [-120, -120, -120]
```

`ElementInputBridge.Dispatch` raises three `MouseWheel` events for this frame, each with `Delta = -120`, so a `ScrollViewer` under the pointer scrolls 3 x 48 = 144 units.

## Remarks

`InputFrame` is a snapshot-delta object for one update pass. It stores a `PointerFrame` built from the previous and current `PointerSnapshot`, a `KeyboardFrame` built from the previous and current `KeyboardSnapshot`, and a read-only copy of the provided text input events.

Pointer and keyboard transition methods compare the current snapshot against the previous snapshot. `IsDown` reports current state, `IsPressed` reports a transition from up to down, and `IsReleased` reports a transition from down to up.

`TextInputEvents` is copied from the constructor argument with `ToArray()` and exposed as a read-only list. Later changes to the caller's source list are not reflected by the frame. The event objects themselves are not cloned.

Key auto-repeat events are not visible in snapshots, because a held key is down in both the previous and the current snapshot. The six-parameter constructor takes them as `keyRepeatEvents`: one `InputKey` entry for each operating-system auto-repeat received since the previous frame. The frame copies the list and drops `InputKey.None` and `InputKey.Unknown` entries. `Keyboard.GetRepeatCount` reads the copied entries. The five-parameter constructor creates a frame without repeat events. The focus manager raises one repeated `KeyDown` pair per entry, with `KeyEventArgs.IsRepeat` set to `true`, but only for a key that is down in the current snapshot or released in this frame.

Wheel events are not visible in snapshots either: the snapshots only hold the accumulated `WheelValue`, so three notches of `-120` and one notch of `-360` give the same `WheelDelta`. The seven-parameter constructor takes `wheelDeltas`: one entry for each platform wheel event received since the previous frame, in arrival order. The frame copies the list, drops `0` entries, and exposes the rest as `Pointer.WheelDeltas`. The entries must add up to the wheel change between the pointer snapshots (`currentPointer.WheelValue - previousPointer.WheelValue`); otherwise the constructor throws `ArgumentException`. The five- and six-parameter constructors create a frame whose `Pointer.WheelDeltas` holds the snapshots' wheel change as one entry, or no entry when the wheel value did not change. `ElementInputBridge` raises one wheel event pair per entry.

The nested `PointerFrame` and `KeyboardFrame` classes are created by `InputFrame`; their constructors are internal, so callers normally access them through the `Pointer` and `Keyboard` properties.

## Constructors

| Name | Description |
| --- | --- |
| `InputFrame(PointerSnapshot, PointerSnapshot, KeyboardSnapshot, KeyboardSnapshot, IReadOnlyList<TextInputSnapshotEvent>)` | Initializes an input frame from previous/current pointer snapshots, previous/current keyboard snapshots, and text input events, without key repeat events. |
| `InputFrame(PointerSnapshot, PointerSnapshot, KeyboardSnapshot, KeyboardSnapshot, IReadOnlyList<TextInputSnapshotEvent>, IReadOnlyList<InputKey>)` | Initializes an input frame like the five-parameter constructor and also records one key auto-repeat event per `InputKey` entry. |
| `InputFrame(PointerSnapshot, PointerSnapshot, KeyboardSnapshot, KeyboardSnapshot, IReadOnlyList<TextInputSnapshotEvent>, IReadOnlyList<InputKey>, IReadOnlyList<int>)` | Initializes an input frame like the six-parameter constructor and also records one wheel event per non-zero `int` entry of `wheelDeltas`, in order. |

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
| `WheelDeltas` | `IReadOnlyList<int>` | Gets the delta of each wheel event of the frame, in arrival order; the entries add up to `WheelDelta`. |

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
| `GetRepeatCount(InputKey)` | `int` | Returns how many key auto-repeat events the frame carries for the key. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `InputFrame(...)` | `ArgumentNullException` | `previousPointer`, `currentPointer`, `previousKeyboard`, `currentKeyboard`, `textInputEvents`, `keyRepeatEvents`, or `wheelDeltas` is `null`. |
| `InputFrame(..., IReadOnlyList<int>)` | `ArgumentException` | The entries of `wheelDeltas` do not add up to `currentPointer.WheelValue - previousPointer.WheelValue`. |

## Applies to

Cerneala retained UI input frame processing.

## See also

- `Cerneala.UI.Input.PointerSnapshot`
- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Input.TextInputSnapshotEvent`
- `Cerneala.UI.Input.ElementInputBridge`
