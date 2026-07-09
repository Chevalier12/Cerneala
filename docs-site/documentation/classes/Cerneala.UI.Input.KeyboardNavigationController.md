# KeyboardNavigationController Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardNavigationController.cs`

Runs the retained input pipeline's default Tab and Shift+Tab focus navigation stage.

```csharp
internal sealed class KeyboardNavigationController
```

Inheritance:
`object` -> `KeyboardNavigationController`

## Examples

Keyboard navigation normally runs through `ElementInputBridge.Dispatch` after keyboard events and retained input bindings have been processed.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new(100, 100);
ElementInputBridge bridge = new();

InputFrame frame = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.FromDownKeys([InputKey.Tab]),
    []);

bridge.Dispatch(root, frame);
```

## Remarks

`KeyboardNavigationController` is an internal stage in `ElementInputBridge.Dispatch`. It receives the keyboard dispatch and retained input binding results, then decides whether the frame should invoke default keyboard navigation.

The controller only attempts navigation while `InputKey.Tab` is currently pressed. If a Tab key press result is present and already handled, default navigation is suppressed. If no Tab press result exists and an element is already focused, navigation is also skipped; this prevents a held Tab key from moving focus again on frames that do not contain a fresh press transition.

When Tab navigation is allowed, the controller treats `LeftShift` or `RightShift` as reverse navigation and delegates the focus movement to `KeyboardNavigation.MoveNext`. Candidate selection, `TabIndex` ordering, wrapping, focus validity, and the actual focus change are handled by `KeyboardNavigation` and `FocusManager`.

The route map supplied to `Process` is the same retained input route map used by the earlier keyboard dispatch stages. That keeps default Tab navigation aligned with the pre-input route order even if key handlers reorder visual children while processing the current frame.

## Constructors

| Name | Description |
| --- | --- |
| `KeyboardNavigationController()` | Initializes a controller with its own `KeyboardNavigation` helper. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(IReadOnlyList<KeyboardDispatchResult>, InputFrame, UIRoot, FocusManager, ElementInputRouteMap)` | `bool` | Applies default Tab or Shift+Tab navigation for the current input frame. Returns `true` when focus navigation moves focus; otherwise, returns `false`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Process` | `ArgumentNullException` | `results`, `inputFrame`, `root`, `focusManager`, or `routeMap` is `null`. |

## Applies to

Cerneala retained UI keyboard input and focus navigation.

## See also

- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Input.KeyboardNavigation`
- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.KeyboardDispatchResult`
- `Cerneala.UI.Input.ElementInputRouteMap`
