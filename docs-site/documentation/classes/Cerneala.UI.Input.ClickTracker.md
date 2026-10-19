# ClickTracker Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ClickTracker.cs`

Tracks a pressed `UIElement` and reports the consecutive click count when a later release targets the same element instance.

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

`ClickTracker` is a small input helper used by `ElementInputBridge` during pointer button dispatch. It stores the element supplied to `Press` and compares it by reference with the element supplied to `Release`.

`Release` clears the stored pressed target after every release attempt. A matching press/release pair returns `1` for a new target, or increments the count when the release matches the same target as the previous click. A release for a different target returns `0` and resets the previous-click state.

Calling `Cancel` clears the pressed target and the accumulated click count. Calling `Press` again replaces the previous pressed target. The class does not apply a time-based double-click window; consecutive matching pairs are counted until a different target, a failed release, or `Cancel` resets the state.

The comparison uses `ReferenceEquals`, so two different `UIElement` instances are not treated as the same click target.

The public `Press` and `Release` track the left mouse button. `ElementInputBridge` keeps one press per mouse button, as each WPF mouse button has its own down and up messages: a right-button press and release while the left button is held do not complete or clear the left press. For example, a left press on a `Button`, a right press and release on the same `Button`, then a left release over it give the left release `ClickCount == 1`, so the `Button` raises `Click` once.

As WPF's `MouseDevice` starts `ClickCount` at 1 when the pressed button differs from the previously pressed button, a press of another button ends the consecutive-click sequence. For example, a left click, a second left click, a right click, and a third left click on the same element give the left releases `ClickCount` values `1`, `2`, and `1`. A count continues only when the same button clicks the same element again.

## Constructors

| Name | Description |
| --- | --- |
| `ClickTracker()` | Initializes a tracker with no pressed target. |

## Methods

| Name | Description |
| --- | --- |
| `Press(UIElement?)` | Stores the element that received the left-button press. Passing `null` clears the effective click target for matching purposes. |
| `Release(UIElement?)` | Clears the stored left-button press and returns the current consecutive click count for a matching target; returns `0` and resets the count for a non-matching release. |
| `Cancel()` | Clears the stored pressed targets of every button and resets the consecutive click count. |

## Applies to

Cerneala retained UI pointer input.

## See also

- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.InputEvents`
