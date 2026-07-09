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

`TextInputBridge` raises preview and bubbling text input routed events for each `TextInputSnapshotEvent`.

If there is no focused element, or if the focused element is no longer present in the supplied `ElementInputRouteMap`, `Dispatch` returns without raising events.

The method throws when `textInputEvents`, `focusManager`, or `routeMap` is `null`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Dispatch(IReadOnlyList<TextInputSnapshotEvent>, FocusManager, ElementInputRouteMap)` | `void` | Raises text input event pairs for the focused element in the route map. |

## Applies to

- `Cerneala.UI.Input.TextInputBridge`

## See also

- `Cerneala.UI.Input.TextCompositionEventArgs`
- `Cerneala.UI.Input.TextInputSnapshotEvent`
- `Cerneala.UI.Input.FocusManager`
- `Cerneala.UI.Input.InputEvents`
