# InputEvents Class

## Definition
Namespace: `Cerneala.UI.Input`  
Assembly/Project: `Cerneala`  
Source: `UI/Input/InputEvents.cs`

Defines the shared routed input event identifiers used by the Cerneala UI input system.

```csharp
public static class InputEvents
```

Inheritance: `object` -> `InputEvents`

## Examples

Register a handler for a routed keyboard event and inspect the event catalog:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

var element = new UIElement();

element.Handlers.AddHandler(InputEvents.KeyDownEvent, (_, args) =>
{
    var keyArgs = (KeyEventArgs)args;

    if (keyArgs.Key == InputKey.Enter)
    {
        args.Handled = true;
    }
});

foreach (RoutedEvent routedEvent in InputEvents.All)
{
    Console.WriteLine($"{routedEvent.Name}: {routedEvent.RoutingStrategy}");
}
```

## Remarks

`InputEvents` is a static catalog of `RoutedEvent` instances for mouse, keyboard, stylus, touch, manipulation, and drag/drop input. Each event is registered with `InputEvents` as the owner type, a WPF-style event name, a `RoutingStrategy`, and the routed event argument type used when the event is raised.

Preview events generally use `RoutingStrategy.Tunnel`, matching the preview-before-bubble pattern. Non-preview counterparts generally use `RoutingStrategy.Bubble`. Some mouse button and pointer boundary events are `RoutingStrategy.Direct`, so handlers are invoked only for the direct target route used by the input system.

The `All` property exposes the complete catalog in declaration order. It is useful for diagnostics, tests, and runtime surfaces that need to enumerate known input events instead of hard-coding each identifier again.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `All` | `IReadOnlyList<RoutedEvent>` | Contains every routed event defined by `InputEvents`, in declaration order. |

## Mouse Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `PreviewMouseDownEvent` | `PreviewMouseDown` | `Tunnel` | `MouseButtonEventArgs` |
| `MouseDownEvent` | `MouseDown` | `Bubble` | `MouseButtonEventArgs` |
| `PreviewMouseUpEvent` | `PreviewMouseUp` | `Tunnel` | `MouseButtonEventArgs` |
| `MouseUpEvent` | `MouseUp` | `Bubble` | `MouseButtonEventArgs` |
| `PreviewMouseMoveEvent` | `PreviewMouseMove` | `Tunnel` | `MouseEventArgs` |
| `MouseMoveEvent` | `MouseMove` | `Bubble` | `MouseEventArgs` |
| `PreviewMouseWheelEvent` | `PreviewMouseWheel` | `Tunnel` | `MouseWheelEventArgs` |
| `MouseWheelEvent` | `MouseWheel` | `Bubble` | `MouseWheelEventArgs` |
| `MouseEnterEvent` | `MouseEnter` | `Direct` | `MouseEventArgs` |
| `MouseLeaveEvent` | `MouseLeave` | `Direct` | `MouseEventArgs` |
| `GotMouseCaptureEvent` | `GotMouseCapture` | `Bubble` | `MouseEventArgs` |
| `LostMouseCaptureEvent` | `LostMouseCapture` | `Bubble` | `MouseEventArgs` |
| `QueryCursorEvent` | `QueryCursor` | `Bubble` | `MouseEventArgs` |
| `PreviewMouseLeftButtonDownEvent` | `PreviewMouseLeftButtonDown` | `Direct` | `MouseButtonEventArgs` |
| `MouseLeftButtonDownEvent` | `MouseLeftButtonDown` | `Direct` | `MouseButtonEventArgs` |
| `PreviewMouseLeftButtonUpEvent` | `PreviewMouseLeftButtonUp` | `Direct` | `MouseButtonEventArgs` |
| `MouseLeftButtonUpEvent` | `MouseLeftButtonUp` | `Direct` | `MouseButtonEventArgs` |
| `PreviewMouseRightButtonDownEvent` | `PreviewMouseRightButtonDown` | `Direct` | `MouseButtonEventArgs` |
| `MouseRightButtonDownEvent` | `MouseRightButtonDown` | `Direct` | `MouseButtonEventArgs` |
| `PreviewMouseRightButtonUpEvent` | `PreviewMouseRightButtonUp` | `Direct` | `MouseButtonEventArgs` |
| `MouseRightButtonUpEvent` | `MouseRightButtonUp` | `Direct` | `MouseButtonEventArgs` |
| `PreviewMouseDoubleClickEvent` | `PreviewMouseDoubleClick` | `Direct` | `MouseButtonEventArgs` |
| `MouseDoubleClickEvent` | `MouseDoubleClick` | `Direct` | `MouseButtonEventArgs` |

## Keyboard And Text Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `PreviewKeyDownEvent` | `PreviewKeyDown` | `Tunnel` | `KeyEventArgs` |
| `KeyDownEvent` | `KeyDown` | `Bubble` | `KeyEventArgs` |
| `PreviewKeyUpEvent` | `PreviewKeyUp` | `Tunnel` | `KeyEventArgs` |
| `KeyUpEvent` | `KeyUp` | `Bubble` | `KeyEventArgs` |
| `PreviewGotKeyboardFocusEvent` | `PreviewGotKeyboardFocus` | `Tunnel` | `KeyboardFocusChangedEventArgs` |
| `GotKeyboardFocusEvent` | `GotKeyboardFocus` | `Bubble` | `KeyboardFocusChangedEventArgs` |
| `PreviewLostKeyboardFocusEvent` | `PreviewLostKeyboardFocus` | `Tunnel` | `KeyboardFocusChangedEventArgs` |
| `LostKeyboardFocusEvent` | `LostKeyboardFocus` | `Bubble` | `KeyboardFocusChangedEventArgs` |
| `GotFocusEvent` | `GotFocus` | `Bubble` | `RoutedEventArgs` |
| `LostFocusEvent` | `LostFocus` | `Bubble` | `RoutedEventArgs` |
| `PreviewTextInputEvent` | `PreviewTextInput` | `Tunnel` | `TextCompositionEventArgs` |
| `TextInputEvent` | `TextInput` | `Bubble` | `TextCompositionEventArgs` |

## Stylus Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `PreviewStylusDownEvent` | `PreviewStylusDown` | `Tunnel` | `RoutedEventArgs` |
| `StylusDownEvent` | `StylusDown` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusUpEvent` | `PreviewStylusUp` | `Tunnel` | `RoutedEventArgs` |
| `StylusUpEvent` | `StylusUp` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusMoveEvent` | `PreviewStylusMove` | `Tunnel` | `RoutedEventArgs` |
| `StylusMoveEvent` | `StylusMove` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusInAirMoveEvent` | `PreviewStylusInAirMove` | `Tunnel` | `RoutedEventArgs` |
| `StylusInAirMoveEvent` | `StylusInAirMove` | `Bubble` | `RoutedEventArgs` |
| `StylusEnterEvent` | `StylusEnter` | `Direct` | `RoutedEventArgs` |
| `StylusLeaveEvent` | `StylusLeave` | `Direct` | `RoutedEventArgs` |
| `PreviewStylusInRangeEvent` | `PreviewStylusInRange` | `Tunnel` | `RoutedEventArgs` |
| `StylusInRangeEvent` | `StylusInRange` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusOutOfRangeEvent` | `PreviewStylusOutOfRange` | `Tunnel` | `RoutedEventArgs` |
| `StylusOutOfRangeEvent` | `StylusOutOfRange` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusSystemGestureEvent` | `PreviewStylusSystemGesture` | `Tunnel` | `RoutedEventArgs` |
| `StylusSystemGestureEvent` | `StylusSystemGesture` | `Bubble` | `RoutedEventArgs` |
| `GotStylusCaptureEvent` | `GotStylusCapture` | `Bubble` | `RoutedEventArgs` |
| `LostStylusCaptureEvent` | `LostStylusCapture` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusButtonDownEvent` | `PreviewStylusButtonDown` | `Tunnel` | `RoutedEventArgs` |
| `StylusButtonDownEvent` | `StylusButtonDown` | `Bubble` | `RoutedEventArgs` |
| `PreviewStylusButtonUpEvent` | `PreviewStylusButtonUp` | `Tunnel` | `RoutedEventArgs` |
| `StylusButtonUpEvent` | `StylusButtonUp` | `Bubble` | `RoutedEventArgs` |

## Touch Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `PreviewTouchDownEvent` | `PreviewTouchDown` | `Tunnel` | `RoutedEventArgs` |
| `TouchDownEvent` | `TouchDown` | `Bubble` | `RoutedEventArgs` |
| `PreviewTouchMoveEvent` | `PreviewTouchMove` | `Tunnel` | `RoutedEventArgs` |
| `TouchMoveEvent` | `TouchMove` | `Bubble` | `RoutedEventArgs` |
| `PreviewTouchUpEvent` | `PreviewTouchUp` | `Tunnel` | `RoutedEventArgs` |
| `TouchUpEvent` | `TouchUp` | `Bubble` | `RoutedEventArgs` |
| `TouchEnterEvent` | `TouchEnter` | `Direct` | `RoutedEventArgs` |
| `TouchLeaveEvent` | `TouchLeave` | `Direct` | `RoutedEventArgs` |
| `GotTouchCaptureEvent` | `GotTouchCapture` | `Bubble` | `RoutedEventArgs` |
| `LostTouchCaptureEvent` | `LostTouchCapture` | `Bubble` | `RoutedEventArgs` |

## Manipulation Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `ManipulationStartingEvent` | `ManipulationStarting` | `Bubble` | `RoutedEventArgs` |
| `ManipulationStartedEvent` | `ManipulationStarted` | `Bubble` | `RoutedEventArgs` |
| `ManipulationDeltaEvent` | `ManipulationDelta` | `Bubble` | `RoutedEventArgs` |
| `ManipulationInertiaStartingEvent` | `ManipulationInertiaStarting` | `Bubble` | `RoutedEventArgs` |
| `ManipulationBoundaryFeedbackEvent` | `ManipulationBoundaryFeedback` | `Bubble` | `RoutedEventArgs` |
| `ManipulationCompletedEvent` | `ManipulationCompleted` | `Bubble` | `RoutedEventArgs` |

## Drag And Drop Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `PreviewQueryContinueDragEvent` | `PreviewQueryContinueDrag` | `Tunnel` | `RoutedEventArgs` |
| `QueryContinueDragEvent` | `QueryContinueDrag` | `Bubble` | `RoutedEventArgs` |
| `PreviewGiveFeedbackEvent` | `PreviewGiveFeedback` | `Tunnel` | `RoutedEventArgs` |
| `GiveFeedbackEvent` | `GiveFeedback` | `Bubble` | `RoutedEventArgs` |
| `PreviewDragEnterEvent` | `PreviewDragEnter` | `Tunnel` | `RoutedEventArgs` |
| `DragEnterEvent` | `DragEnter` | `Bubble` | `RoutedEventArgs` |
| `PreviewDragOverEvent` | `PreviewDragOver` | `Tunnel` | `RoutedEventArgs` |
| `DragOverEvent` | `DragOver` | `Bubble` | `RoutedEventArgs` |
| `PreviewDragLeaveEvent` | `PreviewDragLeave` | `Tunnel` | `RoutedEventArgs` |
| `DragLeaveEvent` | `DragLeave` | `Bubble` | `RoutedEventArgs` |
| `PreviewDropEvent` | `PreviewDrop` | `Tunnel` | `RoutedEventArgs` |
| `DropEvent` | `Drop` | `Bubble` | `RoutedEventArgs` |

## Applies To

Cerneala retained UI input routing.

## See Also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutingStrategy`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Elements.UIElement`
