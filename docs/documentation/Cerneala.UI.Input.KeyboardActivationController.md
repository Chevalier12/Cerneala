# KeyboardActivationController Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardActivationController.cs`

Converts unhandled keyboard dispatch results into default command activation for focused input elements.

```csharp
internal sealed class KeyboardActivationController
```

Inheritance:
`object` -> `KeyboardActivationController`

## Examples

Keyboard activation normally runs as part of the retained input bridge after keyboard events and input bindings have been processed.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(100, 100);
ElementInputBridge bridge = new();

InputFrame frame = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.FromDownKeys([InputKey.Enter]),
    []);

bridge.Dispatch(root, frame);
```

## Remarks

`KeyboardActivationController` is an internal stage in `ElementInputBridge.Dispatch`. It receives `KeyboardDispatchResult` values after `FocusManager.DispatchKeyboardWithResults`, retained input bindings, and keyboard navigation have had a chance to handle the same keyboard transition.

The controller only reacts to `InputKey.Enter` and `InputKey.Space`. Pressing `Enter` executes the nearest valid `IInputCommandSource` ancestor of the dispatch target when the key event was not already handled.

`Space` behaves like button activation. On press, the nearest valid `IInputPressable` ancestor has `IsPressed` set to `true` and is remembered. On release, the pressed state is cleared and the command executes only when the release target resolves to the same valid `IInputCommandSource`. This prevents a press that started on one command source from activating a different source on release.

Activation targets must still be valid in the supplied `ElementInputRouteMap`. A valid target is attached, enabled, visible for input through `UIElementVisibility.ParticipatesInInput`, and present in the route map. If focus is cleared or the remembered space-pressed element becomes invalid, the pressed state is cleared without executing a command.

Handled key events suppress default activation. This lets preview or bubbling key handlers, retained input bindings, and keyboard navigation consume keyboard input before the default command behavior runs.

## Constructors

| Name | Description |
| --- | --- |
| `KeyboardActivationController()` | Initializes a controller with no remembered space-pressed element. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(IReadOnlyList<KeyboardDispatchResult> results, FocusManager focusManager, CommandRouter commandRouter, ElementInputRouteMap routeMap)` | `void` | Applies default `Enter` and `Space` activation for unhandled keyboard dispatch results, using `commandRouter` to execute command sources found in `routeMap`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Process` | `ArgumentNullException` | `results`, `focusManager`, `commandRouter`, or `routeMap` is `null`. |

## Applies to

Cerneala retained UI keyboard input, command routing, and pressable control activation.

## See also

- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.KeyboardDispatchResult`
- `Cerneala.UI.Input.IInputCommandSource`
- `Cerneala.UI.Input.IInputPressable`
