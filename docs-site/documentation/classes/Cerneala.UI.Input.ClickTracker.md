# ClickTracker Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ClickTracker.cs`

Tracks a pressed `UIElement` and reports whether a later release targets the same element instance.

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
int clickCount = tracker.Release(target);

// clickCount == 1
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

`Release` returns `1` only when the stored pressed target is not `null` and is the same `UIElement` instance as the release target. Any other release returns `0`. Every `Release` clears the stored target by calling `Cancel`, so a click is consumed after one release attempt.

Calling `Cancel` clears the stored target and prevents a later release from reporting a click for the previous press. Calling `Press` again replaces the previous pressed target.

The class does not count repeated clicks over time; it reports either `1` for a matching press/release pair or `0` otherwise.

## Constructors

| Name | Description |
| --- | --- |
| `ClickTracker()` | Initializes a tracker with no pressed target. |

## Methods

| Name | Description |
| --- | --- |
| `Press(UIElement?)` | Stores the element that received the press. Passing `null` clears the effective click target for matching purposes. |
| `Release(UIElement?)` | Clears the stored target and returns `1` when the release target is the same non-null element instance as the stored press target; otherwise returns `0`. |
| `Cancel()` | Clears the stored pressed target without reporting a click. |

## Applies to

Cerneala retained UI pointer input.

## See also

- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Input.InputEvents`
