# ElementInputBridge Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/ElementInputBridge.cs`

Coordinates per-frame retained UI input dispatch for pointer, keyboard, command, focus, drag, hover, pressed, and text input state.

```csharp
public sealed class ElementInputBridge
```

Inheritance:
`Object` -> `ElementInputBridge`

## Examples

Use `ElementInputBridge` once per retained UI host to dispatch each collected `InputFrame` into a `UIRoot`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

ElementInputBridge inputBridge = new();

void ProcessInput(UIRoot root, InputFrame inputFrame)
{
    inputBridge.Dispatch(root, inputFrame, TimeSpan.FromMilliseconds(16));
}
```

Custom services can be injected when a host needs to observe or share state with other infrastructure.

```csharp
using Cerneala.UI.Input;

FocusManager focusManager = new();
PointerCaptureManager captureManager = new();

ElementInputBridge inputBridge = new(
    pointerCaptureManager: captureManager,
    focusManager: focusManager);
```

## Remarks

`ElementInputBridge` is the top-level dispatcher that turns one `InputFrame` into retained UI input behavior. It refreshes the root input route map, hit-tests the current pointer position, applies pointer capture overrides, then routes pointer, keyboard, command activation, keyboard navigation, keyboard activation, and text input events. When the capture override releases a captured element that the route map no longer contains, its `LostMouseCapture` handlers, for example a `Thumb` that raises `DragCompleted`, can change the tree; the bridge then refreshes the route map and hit-tests again, so the pointer events of that frame go to the tree as those handlers left it.

Pointer dispatch updates hover state when the pointer moves, raises preview and bubbling mouse move, wheel, down, and up event pairs, tracks pressed visual state, tracks click counts, starts and completes pointer drags for `IPointerDragSource` ancestors, and executes `IInputCommandSource` commands after unhandled left-button clicks. While pointer capture is active, both routed pointer events and hover state target the captured element. When capture is released during dispatch, hover state is immediately reconciled with the element physically under the cursor.

Each mouse button keeps its own press: its click target, the `PressOriginalSource` of its release, and its click count. Only the left button sets pressed visual state, moves focus, starts drags and repeats, and runs commands. A right click while the left button is held therefore does not cancel the left press. For example, a left press on a `Button`, a right press and release on it, and a left release over it raise `Click` once and run the `Command` once; the same sequence on the middle of a non-editable `ComboBox` field opens its drop-down. A press of another button ends the consecutive-click sequence, as described on the `ClickTracker` page. The click count is decided on the press, so `MouseDown` already carries `ClickCount == 2` for the second press of a double click. The bridge dates each press with a clock that grows by the `frameTime` of every `Dispatch` call, so a press continues a sequence only within `500` ms and `4` pixels on each axis of the previous press; `Dispatch(UIRoot, InputFrame)` passes `TimeSpan.Zero`, so with it presses never time out. In a running window the clock also grows on every runtime pump that renders nothing, so idle time counts: a click, 2 s with nothing to render, and a click at the same point give click counts `1` and `1`, not `1` and `2`. Presses that Servo builds do not follow the time and distance rule: a plain `Servo.ClickAsync` always counts `1`, and `ClickAsync(target, clickCount: 2)` counts `1`, `2` regardless of time. Raw platform input keeps the `500` ms and `4` pixel rule.

When a modal dialog disables the window that hosts the bridge, for example `dialog.ShowDialogAsync()` on a window whose `Owner` is that window, the bridge cancels the pointer mode of the interrupted press, like the `WM_CANCELMODE` message that Win32 `EnableWindow` sends. It releases pointer capture, so the captured element gets `LostMouseCapture`: a `TextBox` ends its selection drag, and a `Thumb` ends its drag with `DragCompleted` and `Canceled = true`. It also clears the pressed, click, and repeat state of that press. When a `PreviewMouseDown` handler shows the dialog, the cancel runs at the end of that dispatch, after the press has taken the capture. After the dialog closes, moving the pointer with no button down does not extend a selection or move a thumb, and the next click on a `Button` raises its `Click`.

The bridge raises one `PreviewMouseWheel` and `MouseWheel` pair for each entry of `InputFrame.Pointer.WheelDeltas`, in list order, with that entry as `MouseWheelEventArgs.Delta`, after the frame's mouse move and before its button events. Like WPF, which raises one `MouseWheel` event per `WM_MOUSEWHEEL` message, three platform wheel notches that reach one frame raise three pairs with `Delta = -120`, not one pair with `Delta = -360`.

A press belongs to the nearest element in its visual route that is an `IInputCommandSource` or the field of a non-editable `ComboBox`, as in WPF the nearest `ButtonBase` handles `MouseLeftButtonDown`. A release completes the click only on that element; a release on a non-editable `ComboBox` field after a press on an enclosing command source still completes the command source's click. For example, with a non-editable `ComboBox` as the `Content` of a `Button`, a press on the middle of the field followed by a release on the `Button` outside the `ComboBox` has `ClickCount == 0`, so the `Button` raises no `Click` and runs no `Command`.

A left press moves keyboard focus to the nearest focusable element in its visual route, but the walk stops at an enabled element that takes the press for itself: a `Thumb`, a `ColorSpectrum`, a `TextBox`, a `PasswordBox`, a non-editable `ComboBox`, or an item container of a selector such as a `ListBoxItem`. Such an element takes focus when it is focusable, as a `ListBoxItem` or a `TabItem` is by default; when it is not, as a `Thumb` or a `ComboBoxItem`, focus stays where it was. A control can still focus itself from a handler of the press, as a `Slider` does on a press on its `Thumb` (see the third case below). This follows WPF, where a `Thumb` marks `MouseLeftButtonDown` handled, so an enclosing `ButtonBase` does not focus itself. For example, with `Thumb thumb = new() { Width = 40, Height = 20 };` as the `Content` of `Button button = new() { Width = 200, Height = 80 };`, a left press on the thumb while another `Button` has keyboard focus leaves `button.IsKeyboardFocused` at `false`, and the other `Button` keeps focus; a left press on `button` outside the thumb sets `button.IsKeyboardFocused` to `true`.

The focus moves after the press has raised `PreviewMouseDown`, `MouseDown`, `PreviewMouseLeftButtonDown`, and `MouseLeftButtonDown`, and before a drag begins, as a WPF control focuses itself when `MouseLeftButtonDown` reaches its class handler. Handlers of these four events therefore still see the focus where it was before the press. The focus does not move in three cases:

- A handler marks `PreviewMouseDown` or `PreviewMouseLeftButtonDown` of the press handled. The focus stays where it was, also on a `TextBox`.
- An element in a `ButtonBase`'s content marks the bubbling `MouseDown` or `MouseLeftButtonDown` handled before the event reaches the button. As described on the `ButtonBase` page, the button is then not pressed, and it does not take focus either. When the handler is on the button itself or on an ancestor, the button still takes focus. A handled bubbling event does not stop any other focus target: a `TextBox` that marks its own `MouseDown` handled still takes focus, and a `Slider` still focuses itself from its own `MouseDown` handler after a press beside the thumb that its track's `RepeatButton` took.
- A handler of one of the four events moves the focus itself without marking the event handled. The focus stays where that handler put it. For example, when a light-dismiss click outside an open `Menu` closes the menu, the menu returns focus to its root item, and the `Button` that was clicked does not take focus.

For example, with `first` focused and `second.PreviewMouseDown += (_, e) => e.Handled = true;`, a left press and release on `second` leave `second.IsKeyboardFocused` at `false` and `first.IsKeyboardFocused` at `true`. Without that handler, the same press sets `second.IsKeyboardFocused` to `true`.

Handlers of an earlier pointer event of a frame can remove, add, or move elements, so each later pointer event of the same frame is raised on the element under the pointer at that moment, as WPF raises each mouse message on the element under the mouse when it processes that message. Before each wheel pair, before each button press, before each button release, and before the mouse move when the hover change raised `MouseEnter` or `MouseLeave`, the bridge brings the root's route map up to date, hit-tests the pointer position again, and applies pointer capture again. For example, when a `MouseWheel` handler removes the element under the pointer and the frame holds two more wheel notches, those two notches are raised on the element that is now under the pointer, not on the removed element; a button press in the same frame after a `MouseMove` handler that removed the element goes to the new element too. The events that belong to one platform event keep one target: `MouseDown` and `MouseLeftButtonDown` of one press go to the same element, and the drag update of a mouse move goes to the drag source of that move's target.

The `frameTime` overload treats its `TimeSpan` argument as the elapsed delta for the current input frame. The value must be non-negative. Time-dependent input behavior such as `RepeatButton` consumes that same delta once per host update. A left press that a `RepeatButton` took starts its repeat unless `PreviewMouseDown` or `PreviewMouseLeftButtonDown` was handled, or an element in the button's content marked `MouseDown` or `MouseLeftButtonDown` handled before the event reached the button; a handler on the button or an ancestor that marks the press handled after the button does not stop it, as in WPF (see `RepeatButton`). The compatibility overload without `frameTime` delegates with `TimeSpan.Zero`.

Keyboard dispatch is delegated to `FocusManager`, `RetainedInputBindingProcessor`, `KeyboardNavigationController`, and `KeyboardActivationController`. Text input events are delegated to `TextInputBridge`.

The constructor creates default collaborator instances when no service is supplied. The exposed service properties return the same collaborator instances used by `Dispatch`, which lets callers inspect or coordinate focus, command routing, hover, pressed, and pointer capture state.

## Constructors

| Name | Description |
| --- | --- |
| `ElementInputBridge(HitTestService?, PointerCaptureManager?, HoverTracker?, PressedStateTracker?, ClickTracker?, CommandRouter?, FocusManager?, TextInputBridge?)` | Initializes a bridge with optional collaborator services. Missing collaborators are replaced with default instances. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CommandRouter` | `CommandRouter` | Gets the command router used for retained command queries and execution. |
| `FocusManager` | `FocusManager` | Gets the focus manager used for keyboard dispatch and focus changes. |
| `HoverTracker` | `HoverTracker` | Gets the hover tracker updated during pointer movement. |
| `PointerCaptureManager` | `PointerCaptureManager` | Gets the pointer capture manager used to override pointer event targets. |
| `PressedStateTracker` | `PressedStateTracker` | Gets the pressed-state tracker updated during pointer button transitions. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispatch(UIRoot, InputFrame)` | `void` | Dispatches one input frame with a neutral `TimeSpan.Zero` delta. Throws if `root` or `inputFrame` is `null`. |
| `Dispatch(UIRoot, InputFrame, TimeSpan frameTime)` | `void` | Dispatches one input frame using `frameTime` as the elapsed delta. Throws for null arguments or a negative delta. |

## Applies to

- `Cerneala.UI.Input.ElementInputBridge`

## See also

- `Cerneala.UI.Input.ElementInputRouteMap`
- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.PointerCaptureManager`
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.TextInputBridge`
- `Cerneala.UI.Controls.Primitives.RepeatButton`
