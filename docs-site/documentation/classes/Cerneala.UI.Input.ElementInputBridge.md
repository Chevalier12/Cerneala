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

The bridge raises one `PreviewMouseWheel` and `MouseWheel` pair for each entry of `InputFrame.Pointer.WheelDeltas`, in list order, with that entry as `MouseWheelEventArgs.Delta`, after the frame's mouse move and before its button events. Like WPF, which raises one `MouseWheel` event per `WM_MOUSEWHEEL` message, three platform wheel notches that reach one frame raise three pairs with `Delta = -120`, not one pair with `Delta = -360`.

Handlers of an earlier pointer event of a frame can remove, add, or move elements, so each later pointer event of the same frame is raised on the element under the pointer at that moment, as WPF raises each mouse message on the element under the mouse when it processes that message. Before each wheel pair, before each button press, before each button release, and before the mouse move when the hover change raised `MouseEnter` or `MouseLeave`, the bridge brings the root's route map up to date, hit-tests the pointer position again, and applies pointer capture again. For example, when a `MouseWheel` handler removes the element under the pointer and the frame holds two more wheel notches, those two notches are raised on the element that is now under the pointer, not on the removed element; a button press in the same frame after a `MouseMove` handler that removed the element goes to the new element too. The events that belong to one platform event keep one target: `MouseDown` and `MouseLeftButtonDown` of one press go to the same element, and the drag update of a mouse move goes to the drag source of that move's target.

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
