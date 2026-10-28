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
first.KeyDown += (_, _) =>
{
    receivers.Add("first");
    focusManager.Focus(second, routeMap);
};
second.KeyDown += (_, _) => receivers.Add("second");

routeMap = new ElementInputRouteBuilder().Build(root);
focusManager.Focus(first, routeMap);

KeyboardSnapshot held = KeyboardSnapshot.FromDownKeys([InputKey.Down]);
focusManager.DispatchKeyboard(
    new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, held, held, [], [InputKey.Down, InputKey.Down]),
    routeMap);

// receivers is ["first", "second"].
```

Handlers are copied into the route map when `ElementInputRouteBuilder.Build` runs, so the example adds them before building it.

The following example focuses an element that a `KeyDown` handler creates during dispatch. The frame presses Back and Tab. Back goes to `first`, whose handler adds `created` to the tree and focuses it. `created` is not in `frameMap`, so the Tab press is routed over the root's current route map and goes to `created`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement first = new() { Focusable = true };
root.VisualChildren.Add(first);

FocusManager focusManager = new();
List<string> receivers = [];
first.KeyDown += (_, args) =>
{
    receivers.Add($"first {args.Key}");
    UIElement created = new() { Focusable = true };
    created.KeyDown += (_, e) => receivers.Add($"created {e.Key}");
    root.VisualChildren.Add(created);
    focusManager.Focus(created, root.InputCache.EnsureCurrent(root));
};

ElementInputRouteMap frameMap = root.InputCache.EnsureCurrent(root);
focusManager.Focus(first, frameMap);
focusManager.DispatchKeyboard(
    new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys([InputKey.Back, InputKey.Tab]), []),
    frameMap);

// receivers is ["first Back", "created Tab"].
```

## Remarks

`FocusManager` stores the current keyboard-focused `UIElement` in `FocusedElement`. Calling `Focus` with the same element returns `false` and does not raise focus events. Calling `Focus(null, routeMap)` clears focus when an element is currently focused.

For non-null targets, focus is accepted only when `FocusPolicy.CanFocus` accepts the element. The target must be attached, focusable, enabled, participate in input visibility, and exist in the supplied `ElementInputRouteMap` and in the current route map of its `UIRoot`. So `Focus(element, frameMap)` returns `false` for an element whose ancestor a handler collapsed or hid after `frameMap` was built, although `frameMap` still contains the element.

When focus changes, the manager updates `IsKeyboardFocused` on the old and new focused elements and updates `IsKeyboardFocusWithin` along the input route from the focused element to the root. Ancestors that are not in the input route map, for example disabled ones, are not marked. Shared ancestors remain marked as containing keyboard focus when focus moves within the same branch.

Focus events are raised after state is updated. Preview keyboard focus events are raised before bubbling keyboard focus events. If a preview lost or got keyboard focus event is handled, the matching `LostKeyboardFocus` or `GotKeyboardFocus` event is suppressed. The non-keyboard `LostFocus` and `GotFocus` events are still raised when the old or new element has a route-map id.

`DispatchKeyboard` sends the key events of an `InputFrame` to the focused element: for each key, the press, then one `PreviewKeyDown`/`KeyDown` pair with `IsRepeat == true` for each auto-repeat in `InputFrame.KeyboardFrame.GetRepeatCount`, then the release. It raises preview and bubbling key events through the routed event system, skips `InputKey.None` and `InputKey.Unknown`, and includes current Ctrl, Shift, and Alt modifier state in `KeyEventArgs`. If the focused element can no longer receive focus when dispatch starts, dispatch clears focus and does not route the keyboard input.

Each key event goes to the element that has keyboard focus at the moment the event is raised, not to the element that had focus when dispatch started. A handler that moves focus during one key event therefore changes the target of the next key event of the same frame, including the next repeat of the same key. When a handler clears focus or makes the focused element unable to receive focus, for example by detaching, disabling, or hiding it, the remaining key events of the frame are not raised.

A key event of a focused element that belongs to a `UIRoot` is routed over the current route map of that root (`UIRoot.InputCache.EnsureCurrent`), resolved when the event is raised; the supplied route map is used only for an element without a `UIRoot`. As in WPF, where every raise builds its route from the handlers registered at that moment, each key event of a frame sees the tree and the handlers as the handlers of the earlier key events left them:

- When a handler of the same frame attaches an element after the supplied map was built and focuses it, the remaining key events of the frame, including a release, reach that element.
- When a handler of the same frame collapses or hides an ancestor of the focused element, the element can no longer receive focus and the remaining key events of the frame are not raised.
- When a handler of one key event removes a handler with `RemoveHandler`, the removed handler does not run on the next key event of the same frame; a handler added with `AddHandler` runs on it. The event that is being raised keeps the route it already built.

The same rule decides, when dispatch starts, whether the focused element can still receive focus, and `TextInput` of the same frame is routed the same way. For example, a field gets two auto-repeats of Down in one frame, and its `KeyDown` handler removes handler `h` from the field's parent during the first repeat. `h` runs once, on the first repeat.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement panel = new();
UIElement field = new() { Focusable = true };
panel.VisualChildren.Add(field);
root.VisualChildren.Add(panel);

int hCalls = 0;
RoutedEventHandler h = (_, _) => hCalls++;
panel.Handlers.AddHandler(InputEvents.KeyDownEvent, h);
field.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, _) => panel.Handlers.RemoveHandler(InputEvents.KeyDownEvent, h));

ElementInputRouteMap frameMap = root.InputCache.EnsureCurrent(root);
FocusManager focusManager = new();
focusManager.Focus(field, frameMap);
KeyboardSnapshot held = KeyboardSnapshot.FromDownKeys([InputKey.Down]);
focusManager.DispatchKeyboard(
    new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, held, held, [], [InputKey.Down, InputKey.Down]),
    frameMap);

// hCalls is 1.
```

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
