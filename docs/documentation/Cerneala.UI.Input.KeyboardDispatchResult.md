# KeyboardDispatchResult Record

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardDispatchResult.cs`

Represents one keyboard key transition dispatched to the focused element during an input frame.

```csharp
internal sealed record KeyboardDispatchResult(
    UIElement Target,
    UiElementId TargetId,
    InputKey Key,
    KeyboardDispatchKind Kind,
    bool Handled);
```

Inheritance:
`object` -> `KeyboardDispatchResult`

## Examples
`FocusManager` creates dispatch results after raising the preview and bubble keyboard routed events for each pressed or released key.

```csharp
bool handled = RaiseKeyPair(
    routeMap,
    focusedId,
    key,
    inputFrame.Keyboard,
    InputEvents.PreviewKeyDownEvent,
    InputEvents.KeyDownEvent);

KeyboardDispatchResult result = new(
    FocusedElement,
    focusedId,
    key,
    KeyboardDispatchKind.Pressed,
    handled);
```

## Remarks
`KeyboardDispatchResult` is an internal handoff object in the retained input pipeline. `FocusManager.DispatchKeyboardWithResults` returns one result for each non-`None`, non-`Unknown` `InputKey` that is pressed or released in the current `InputFrame`.

The `Handled` value records whether either the preview or bubble routed keyboard event was handled. Later pipeline stages use that value to avoid default processing for consumed input. `RetainedInputBindingProcessor` skips key binding execution for handled results, `KeyboardNavigationController` ignores handled Tab presses, and `KeyboardActivationController` ignores handled Enter and Space activation.

`ElementInputBridge.Dispatch` processes the returned results after pointer dispatch and before text input dispatch. It first lets retained input bindings consume eligible pressed-key results, then passes the remaining results to keyboard navigation and default keyboard activation.

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Target` | `UIElement` | Gets the focused element that received the routed keyboard event pair. |
| `TargetId` | `UiElementId` | Gets the route-map identifier for `Target` at the time the result was created. |
| `Key` | `InputKey` | Gets the key that was pressed or released. |
| `Kind` | `KeyboardDispatchKind` | Gets whether the result represents a key press or key release. |
| `Handled` | `bool` | Gets whether the preview or bubble keyboard event was marked handled. |

## Related Keyboard Dispatch Kinds
| Name | Description |
| --- | --- |
| `KeyboardDispatchKind.Pressed` | Indicates that `Key` became pressed in the current frame. |
| `KeyboardDispatchKind.Released` | Indicates that `Key` was released in the current frame. |

## Applies to
`Cerneala` retained UI input dispatch.

## See also
- `FocusManager`
- `ElementInputBridge`
- `InputFrame`
- `InputEvents`
- `KeyEventArgs`
