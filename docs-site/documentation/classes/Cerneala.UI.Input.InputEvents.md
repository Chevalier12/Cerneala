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
| `PreviewStylusDownEvent` | `PreviewStylusDown` | `Tunnel` | `StylusEventArgs` |
| `StylusDownEvent` | `StylusDown` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusUpEvent` | `PreviewStylusUp` | `Tunnel` | `StylusEventArgs` |
| `StylusUpEvent` | `StylusUp` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusMoveEvent` | `PreviewStylusMove` | `Tunnel` | `StylusEventArgs` |
| `StylusMoveEvent` | `StylusMove` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusInAirMoveEvent` | `PreviewStylusInAirMove` | `Tunnel` | `StylusEventArgs` |
| `StylusInAirMoveEvent` | `StylusInAirMove` | `Bubble` | `StylusEventArgs` |
| `StylusEnterEvent` | `StylusEnter` | `Direct` | `StylusEventArgs` |
| `StylusLeaveEvent` | `StylusLeave` | `Direct` | `StylusEventArgs` |
| `PreviewStylusInRangeEvent` | `PreviewStylusInRange` | `Tunnel` | `StylusEventArgs` |
| `StylusInRangeEvent` | `StylusInRange` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusOutOfRangeEvent` | `PreviewStylusOutOfRange` | `Tunnel` | `StylusEventArgs` |
| `StylusOutOfRangeEvent` | `StylusOutOfRange` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusSystemGestureEvent` | `PreviewStylusSystemGesture` | `Tunnel` | `StylusEventArgs` |
| `StylusSystemGestureEvent` | `StylusSystemGesture` | `Bubble` | `StylusEventArgs` |
| `GotStylusCaptureEvent` | `GotStylusCapture` | `Bubble` | `StylusEventArgs` |
| `LostStylusCaptureEvent` | `LostStylusCapture` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusButtonDownEvent` | `PreviewStylusButtonDown` | `Tunnel` | `StylusEventArgs` |
| `StylusButtonDownEvent` | `StylusButtonDown` | `Bubble` | `StylusEventArgs` |
| `PreviewStylusButtonUpEvent` | `PreviewStylusButtonUp` | `Tunnel` | `StylusEventArgs` |
| `StylusButtonUpEvent` | `StylusButtonUp` | `Bubble` | `StylusEventArgs` |

## Touch Fields

| Name | Routed event name | Routing strategy | Args type |
| --- | --- | --- | --- |
| `PreviewTouchDownEvent` | `PreviewTouchDown` | `Tunnel` | `TouchEventArgs` |
| `TouchDownEvent` | `TouchDown` | `Bubble` | `TouchEventArgs` |
| `PreviewTouchMoveEvent` | `PreviewTouchMove` | `Tunnel` | `TouchEventArgs` |
| `TouchMoveEvent` | `TouchMove` | `Bubble` | `TouchEventArgs` |
| `PreviewTouchUpEvent` | `PreviewTouchUp` | `Tunnel` | `TouchEventArgs` |
| `TouchUpEvent` | `TouchUp` | `Bubble` | `TouchEventArgs` |
| `TouchEnterEvent` | `TouchEnter` | `Direct` | `TouchEventArgs` |
| `TouchLeaveEvent` | `TouchLeave` | `Direct` | `TouchEventArgs` |
| `GotTouchCaptureEvent` | `GotTouchCapture` | `Bubble` | `TouchEventArgs` |
| `LostTouchCaptureEvent` | `LostTouchCapture` | `Bubble` | `TouchEventArgs` |

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
| `PreviewDragEnterEvent` | `PreviewDragEnter` | `Tunnel` | `DragEventArgs` |
| `DragEnterEvent` | `DragEnter` | `Bubble` | `DragEventArgs` |
| `PreviewDragOverEvent` | `PreviewDragOver` | `Tunnel` | `DragEventArgs` |
| `DragOverEvent` | `DragOver` | `Bubble` | `DragEventArgs` |
| `PreviewDragLeaveEvent` | `PreviewDragLeave` | `Tunnel` | `DragEventArgs` |
| `DragLeaveEvent` | `DragLeave` | `Bubble` | `DragEventArgs` |
| `PreviewDropEvent` | `PreviewDrop` | `Tunnel` | `DragEventArgs` |
| `DropEvent` | `Drop` | `Bubble` | `DragEventArgs` |

## Applies To

Cerneala retained UI input routing.

## See Also

- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutingStrategy`
- `Cerneala.UI.Input.RoutedEventArgs`
- `Cerneala.UI.Elements.UIElement`
