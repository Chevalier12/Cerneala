# TouchEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/TouchInputBridge.cs`

Provides routed event data for touch input raised by `TouchInputBridge`.

```csharp
public sealed class TouchEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `TouchEventArgs`

## Examples
```csharp
target.Handlers.AddHandler(InputEvents.TouchDownEvent, (_, args) =>
{
    TouchEventArgs touch = (TouchEventArgs)args;

    int id = touch.TouchId;
    float x = touch.X;
    float y = touch.Y;
    TouchInputAction action = touch.Action;
});
```

## Remarks
`TouchEventArgs` is created by `TouchInputBridge` when dispatching `TouchInputPoint` values from a `TouchInputFrame`. The bridge raises paired preview and bubble events for `Down`, `Move`, and `Up` actions:

| Touch action | Preview event | Bubble event |
| --- | --- | --- |
| `TouchInputAction.Down` | `InputEvents.PreviewTouchDownEvent` | `InputEvents.TouchDownEvent` |
| `TouchInputAction.Move` | `InputEvents.PreviewTouchMoveEvent` | `InputEvents.TouchMoveEvent` |
| `TouchInputAction.Up` | `InputEvents.PreviewTouchUpEvent` | `InputEvents.TouchUpEvent` |

When a touch is captured with `TouchInputBridge.Capture`, subsequent touch points for the same `TouchId` are routed to the captured element while that element remains in the current route map. Capture change notifications also use `TouchEventArgs`: `GotTouchCaptureEvent` and `LostTouchCaptureEvent` are raised with the touch id, `X` and `Y` set to `0`, and `Action` set to `TouchInputAction.Move`.

`OriginalSource` is supplied to the base `RoutedEventArgs` constructor by the bridge. For touch dispatch, it is the target element id selected by hit testing or capture routing.

## Constructors
| Name | Description |
| --- | --- |
| `TouchEventArgs(RoutedEvent routedEvent, object originalSource, int touchId, float x, float y, TouchInputAction action)` | Initializes a touch routed event payload and stores the touch id, coordinates, and action. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `TouchId` | `int` | Identifies the touch point that produced the event. |
| `X` | `float` | The x coordinate from the dispatched touch point. |
| `Y` | `float` | The y coordinate from the dispatched touch point. |
| `Action` | `TouchInputAction` | The touch action represented by the event. |

## Inherited Properties
| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | The routed event being raised. |
| `OriginalSource` | `object` | The original source passed by the input bridge. |
| `Source` | `object` | The current routed event source; initialized to `OriginalSource` by `RoutedEventArgs`. |
| `Handled` | `bool` | Indicates whether the routed event has been handled. |

## Applies to
`Cerneala` retained UI input routing.

## See also
- `TouchInputBridge`
- `TouchInputFrame`
- `TouchInputPoint`
- `TouchInputAction`
- `InputEvents`
- `RoutedEventArgs`
