# ClickTracker Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ClickTracker.cs`

Tracks a pressed `UIElement` per mouse button and counts consecutive clicks: a press continues a click sequence only when it comes soon enough, close enough, with the same button, on the same element instance.

```csharp
public sealed class ClickTracker
```

Inheritance:
`Object` -> `ClickTracker`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement target = new();
ClickTracker tracker = new();

tracker.Press(target);
int firstClickCount = tracker.Release(target);

tracker.Press(target);
int secondClickCount = tracker.Release(target);

// firstClickCount == 1; secondClickCount == 2
```

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIElement target = new();
ClickTracker tracker = new();

tracker.Press(target);
tracker.Cancel();

int clickCount = tracker.Release(target);

// clickCount == 0
```

## Remarks

`ClickTracker` is a small input helper used by `ElementInputBridge` during pointer button dispatch. It stores the element of each press and compares it by reference with the element of the release.

As WPF's `MouseDevice`, the click count is decided on the press. A press has click count `1`, unless it continues the sequence of the previous press. It continues the sequence, and its count is the previous count plus `1`, when all of these hold:

- It is of the same button as the previous press.
- It is on the same element instance as the previous press (`ReferenceEquals`; two different `UIElement` instances are never the same target, and a `null` target never continues a sequence).
- It comes at most `500` ms after the previous press, the Windows default double-click time. A press exactly `500` ms later still continues; `600` ms later starts again at `1`.
- It is at most `4` pixels away from the previous press on each axis, a double-click size of 4 pixels. A press at x `14`, y `6` after a press at x `10`, y `10` continues; a press at x `20`, y `10` starts again at `1`.

`ElementInputBridge` gives each press its position and its time. The time is a clock that grows by the `frameTime` of each `Dispatch(UIRoot, InputFrame, TimeSpan)` call. `Dispatch(UIRoot, InputFrame)` uses `TimeSpan.Zero`, so with it presses never time out. In a running window the clock also grows on every runtime pump that renders nothing, so idle time counts: after a click, 2 s with nothing to render, and a second click at the same point, the second press has `ClickCount == 1`, not `2`. Presses that Servo builds (`Servo.ClickAsync`) do not use the time rule or the distance rule: they state whether they start or continue a sequence, so a plain `ClickAsync` always counts `1` and `ClickAsync(target, clickCount: 2)` counts `1`, `2` however much time passes. Raw platform input keeps the `500` ms and `4` pixel rule. The bridge puts the count of the press on the `MouseButtonEventArgs` of `PreviewMouseDown`, `MouseDown`, and the button-specific down events, and on the press that starts an `IPointerDragSource` drag. With two quick left clicks on the same element, the first press has `ClickCount == 1` and the second has `ClickCount == 2`; a third quick press has `3`.

`Release` clears the stored pressed target of that button after every release attempt. A release over the element that was pressed with the same button completes a click and returns the click count of the device, which is the count that the most recent press of any button set. A release over a different element returns `0` and ends the click sequence.

Calling `Cancel` clears the pressed targets and ends the click sequence. Calling `Press` again replaces the previous pressed target of that button.

The public `Press(UIElement?)` and `Release(UIElement?)` track the left mouse button. `Press(UIElement?)` uses position (0, 0) and time zero, so only the button and the target decide whether it continues a sequence. `ElementInputBridge` keeps one press per mouse button, as each WPF mouse button has its own down and up messages: a right-button press and release while the left button is held do not complete or clear the left press. For example, a left press on a `Button`, a right press and release on the same `Button`, then a left release over it give the left release `ClickCount == 1`, so the `Button` raises `Click` once.

As WPF's `MouseDevice` keeps one count for the device, a press of another button ends the consecutive-click sequence, and a release reports the count of the latest press of any button. For example, the left releases of this sequence on the same element, with no time between the presses, report `[1, 2, 1, 1, 1]`:

1. Left press and release: `1`.
2. Left press and release: `2`.
3. Right press and release: no left release.
4. Left press and release: `1`, because the previous press was a right press.
5. Left press, then a right press and release while the left button is held, then the left release: `1`. The left press itself had count `2`, but the right press set the count of the device to `1`, and the left release reports that count.
6. Left press and release: `1`, because the previous press was a right press.

## Constructors

| Name | Description |
| --- | --- |
| `ClickTracker()` | Initializes a tracker with no pressed target. |

## Methods

| Name | Description |
| --- | --- |
| `Press(UIElement?)` | Stores the element that received the left-button press, at position (0, 0) and time zero. Passing `null` clears the effective click target for matching purposes. |
| `Release(UIElement?)` | Clears the stored left-button press and returns the click count of the device for a matching target; returns `0` and ends the click sequence for a non-matching release. |
| `Cancel()` | Clears the stored pressed targets of every button and ends the click sequence. |

## Applies to

Cerneala retained UI pointer input.

## See also

- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.InputEvents`
