# TextInputBridge Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/TextInputBridge.cs`

Dispatches text input snapshot events to the currently focused retained UI element.

```csharp
public sealed class TextInputBridge
```

Inheritance:
`Object` -> `TextInputBridge`

## Examples

Dispatch text input events after keyboard processing has established focus for the current frame.

```csharp
using Cerneala.UI.Input;

TextInputBridge bridge = new();

bridge.Dispatch(inputFrame.TextInputEvents, focusManager, routeMap);
```

## Remarks

`TextInputBridge` raises a `PreviewTextInput`/`TextInput` pair for each `TextInputSnapshotEvent`, in list order.

Each event goes to the element that has keyboard focus at the moment that event is raised, the same target rule that key events use. A handler of one text event can move focus or detach the focused element, and the next text event of the same call sees that change. For example, a handler on box `a` that calls `focusManager.Focus(b, routeMap)` after it receives `"x"` makes the next event `"y"` go to `b`.

An event is skipped, without raising anything, when the focused element is `null` or can no longer hold keyboard focus: it is detached, not focusable, disabled, does not take part in input, or is not in the supplied `ElementInputRouteMap`. The remaining events are still resolved one by one.

The method throws when `textInputEvents`, `focusManager`, or `routeMap` is `null`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispatch(IReadOnlyList<TextInputSnapshotEvent>, FocusManager, ElementInputRouteMap)` | `void` | Raises a text input event pair for each event on the element focused when that event is raised. |

## Applies to

- `Cerneala.UI.Input.TextInputBridge`

## See also

- `Cerneala.UI.Input.TextCompositionEventArgs`
- `Cerneala.UI.Input.TextInputSnapshotEvent`
- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.InputEvents`
