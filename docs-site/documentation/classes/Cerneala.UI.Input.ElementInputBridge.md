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

`ElementInputBridge` is the top-level dispatcher that turns one `InputFrame` into retained UI input behavior. It refreshes the root input route map, hit-tests the current pointer position, applies pointer capture overrides, then routes pointer, keyboard, command activation, keyboard navigation, keyboard activation, and text input events.

Pointer dispatch updates hover state when the pointer moves, raises preview and bubbling mouse move, wheel, down, and up event pairs, tracks pressed visual state, tracks click counts, starts and completes pointer drags for `IPointerDragSource` ancestors, and executes `IInputCommandSource` commands after unhandled left-button clicks. While pointer capture is active, both routed pointer events and hover state target the captured element. When capture is released during dispatch, hover state is immediately reconciled with the element physically under the cursor.

When a modal dialog disables the window that hosts the bridge, for example `dialog.ShowDialogAsync()` on a window whose `Owner` is that window, the bridge cancels the pointer mode of the interrupted press, like the `WM_CANCELMODE` message that Win32 `EnableWindow` sends. It releases pointer capture, so the captured element gets `LostMouseCapture`: a `TextBox` ends its selection drag, and a `Thumb` ends its drag with `DragCompleted` and `Canceled = true`. It also clears the pressed, click, and repeat state of that press. When a `PreviewMouseDown` handler shows the dialog, the cancel runs at the end of that dispatch, after the press has taken the capture. After the dialog closes, moving the pointer with no button down does not extend a selection or move a thumb, and the next click on a `Button` raises its `Click`.

The bridge raises one `PreviewMouseWheel` and `MouseWheel` pair for each entry of `InputFrame.Pointer.WheelDeltas`, in list order, with that entry as `MouseWheelEventArgs.Delta`, after the frame's mouse move and before its button events. Like WPF, which raises one `MouseWheel` event per `WM_MOUSEWHEEL` message, three platform wheel notches that reach one frame raise three pairs with `Delta = -120`, not one pair with `Delta = -360`.

A press belongs to the nearest element in its visual route that is an `IInputCommandSource` or the field of a non-editable `ComboBox`, as in WPF the nearest `ButtonBase` handles `MouseLeftButtonDown`. A release completes the click only on that element; a release on a non-editable `ComboBox` field after a press on an enclosing command source still completes the command source's click. For example, with a non-editable `ComboBox` as the `Content` of a `Button`, a press on the middle of the field followed by a release on the `Button` outside the `ComboBox` has `ClickCount == 0`, so the `Button` raises no `Click` and runs no `Command`.

The `frameTime` overload treats its `TimeSpan` argument as the elapsed delta for the current input frame. The value must be non-negative. Time-dependent input behavior such as `RepeatButton` consumes that same delta once per host update. The compatibility overload without `frameTime` delegates with `TimeSpan.Zero`.

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
