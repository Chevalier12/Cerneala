# FocusManager Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/FocusManager.cs`

Manages the keyboard focus target for Cerneala UI elements and dispatches keyboard input to the focused element.

```csharp
public sealed class FocusManager
```

Inheritance:
`object` -> `FocusManager`

## Examples

The following example gives keyboard focus to an attached, focusable element.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement editor = new() { Focusable = true };
root.VisualChildren.Add(editor);

ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
FocusManager focusManager = new();

bool changed = focusManager.Focus(editor, routeMap);

if (changed)
{
    UIElement? focused = focusManager.FocusedElement;
}
```

The following example moves focus to the next element on every `KeyDown`. One frame holds Down with 2 auto-repeats. The first repeat goes to `first` and moves focus to `second`; the second repeat goes to `second`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement first = new() { Focusable = true };
UIElement second = new() { Focusable = true };
root.VisualChildren.Add(first);
root.VisualChildren.Add(second);

FocusManager focusManager = new();
ElementInputRouteMap routeMap = null!;
List<string> receivers = [];
first.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) =>
{
    receivers.Add("first");
    focusManager.Focus(second, routeMap);
});
second.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) => receivers.Add("second"));

routeMap = new ElementInputRouteBuilder().Build(root);
focusManager.Focus(first, routeMap);

KeyboardSnapshot held = KeyboardSnapshot.FromDownKeys([InputKey.Down]);
focusManager.DispatchKeyboard(
    new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, held, held, [], [InputKey.Down, InputKey.Down]),
    routeMap);

// receivers is ["first", "second"].
```

Handlers are copied into the route map when `ElementInputRouteBuilder.Build` runs, so the example adds them before building it.

## Remarks

`FocusManager` stores the current keyboard-focused `UIElement` in `FocusedElement`. Calling `Focus` with the same element returns `false` and does not raise focus events. Calling `Focus(null, routeMap)` clears focus when an element is currently focused.

For non-null targets, focus is accepted only when `FocusPolicy.CanFocus` accepts the element. The target must be attached, focusable, enabled, participate in input visibility, and exist in the supplied `ElementInputRouteMap`.

When focus changes, the manager updates `IsKeyboardFocused` on the old and new focused elements and updates `IsKeyboardFocusWithin` along the input route from the focused element to the root. Ancestors that are not in the input route map, for example disabled ones, are not marked. Shared ancestors remain marked as containing keyboard focus when focus moves within the same branch.

Focus events are raised after state is updated. Preview keyboard focus events are raised before bubbling keyboard focus events. If a preview lost or got keyboard focus event is handled, the matching `LostKeyboardFocus` or `GotKeyboardFocus` event is suppressed. The non-keyboard `LostFocus` and `GotFocus` events are still raised when the old or new element has a route-map id.

`DispatchKeyboard` sends the key events of an `InputFrame` to the focused element: for each key, the press, then one `PreviewKeyDown`/`KeyDown` pair with `IsRepeat == true` for each auto-repeat in `InputFrame.KeyboardFrame.GetRepeatCount`, then the release. It raises preview and bubbling key events through the routed event system, skips `InputKey.None` and `InputKey.Unknown`, and includes current Ctrl, Shift, and Alt modifier state in `KeyEventArgs`. If the focused element can no longer receive focus under the supplied route map when dispatch starts, dispatch clears focus and does not route the keyboard input.

Each key event goes to the element that has keyboard focus at the moment the event is raised, not to the element that had focus when dispatch started. A handler that moves focus during one key event therefore changes the target of the next key event of the same frame, including the next repeat of the same key. When a handler clears focus or makes the focused element unable to receive focus, for example by detaching it, the remaining key events of the frame are not raised.

## Constructors

| Name | Description |
| --- | --- |
| `FocusManager()` | Initializes a focus manager with no focused element. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FocusedElement` | `UIElement?` | Gets the element that currently has keyboard focus, or `null` when no element is focused. The setter is private. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Focus(UIElement? element, ElementInputRouteMap routeMap)` | `bool` | Moves keyboard focus to `element`, clears focus when `element` is `null`, updates focus state, and raises focus events. Returns `true` only when focus changes. Throws `ArgumentNullException` when `routeMap` is `null`. |
| `DispatchKeyboard(InputFrame inputFrame, ElementInputRouteMap routeMap)` | `void` | Dispatches pressed, repeated, and released keyboard input from `inputFrame`; each key event goes to the element focused when that event is raised. Throws `ArgumentNullException` when `inputFrame` or `routeMap` is `null`. |

## Applies to

Cerneala UI input routing and keyboard focus management.

## See also

- `Cerneala.UI.Input.FocusPolicy`
- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Input.ElementInputRouteBuilder`
- `Cerneala.UI.Input.InputEvents`
