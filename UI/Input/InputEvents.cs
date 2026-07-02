namespace Cerneala.UI.Input;

public static class InputEvents
{
    public static readonly RoutedEvent PreviewMouseDownEvent = Register("PreviewMouseDown", RoutingStrategy.Tunnel, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseDownEvent = Register("MouseDown", RoutingStrategy.Bubble, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent PreviewMouseUpEvent = Register("PreviewMouseUp", RoutingStrategy.Tunnel, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseUpEvent = Register("MouseUp", RoutingStrategy.Bubble, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent PreviewMouseMoveEvent = Register("PreviewMouseMove", RoutingStrategy.Tunnel, typeof(MouseEventArgs));
    public static readonly RoutedEvent MouseMoveEvent = Register("MouseMove", RoutingStrategy.Bubble, typeof(MouseEventArgs));
    public static readonly RoutedEvent PreviewMouseWheelEvent = Register("PreviewMouseWheel", RoutingStrategy.Tunnel, typeof(MouseWheelEventArgs));
    public static readonly RoutedEvent MouseWheelEvent = Register("MouseWheel", RoutingStrategy.Bubble, typeof(MouseWheelEventArgs));
    public static readonly RoutedEvent MouseEnterEvent = Register("MouseEnter", RoutingStrategy.Direct, typeof(MouseEventArgs));
    public static readonly RoutedEvent MouseLeaveEvent = Register("MouseLeave", RoutingStrategy.Direct, typeof(MouseEventArgs));
    public static readonly RoutedEvent GotMouseCaptureEvent = Register("GotMouseCapture", RoutingStrategy.Bubble, typeof(MouseEventArgs));
    public static readonly RoutedEvent LostMouseCaptureEvent = Register("LostMouseCapture", RoutingStrategy.Bubble, typeof(MouseEventArgs));
    public static readonly RoutedEvent QueryCursorEvent = Register("QueryCursor", RoutingStrategy.Bubble, typeof(MouseEventArgs));
    public static readonly RoutedEvent PreviewMouseLeftButtonDownEvent = Register("PreviewMouseLeftButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseLeftButtonDownEvent = Register("MouseLeftButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent PreviewMouseLeftButtonUpEvent = Register("PreviewMouseLeftButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseLeftButtonUpEvent = Register("MouseLeftButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent PreviewMouseRightButtonDownEvent = Register("PreviewMouseRightButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseRightButtonDownEvent = Register("MouseRightButtonDown", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent PreviewMouseRightButtonUpEvent = Register("PreviewMouseRightButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseRightButtonUpEvent = Register("MouseRightButtonUp", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent PreviewMouseDoubleClickEvent = Register("PreviewMouseDoubleClick", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));
    public static readonly RoutedEvent MouseDoubleClickEvent = Register("MouseDoubleClick", RoutingStrategy.Direct, typeof(MouseButtonEventArgs));

    public static readonly RoutedEvent PreviewKeyDownEvent = Register("PreviewKeyDown", RoutingStrategy.Tunnel, typeof(KeyEventArgs));
    public static readonly RoutedEvent KeyDownEvent = Register("KeyDown", RoutingStrategy.Bubble, typeof(KeyEventArgs));
    public static readonly RoutedEvent PreviewKeyUpEvent = Register("PreviewKeyUp", RoutingStrategy.Tunnel, typeof(KeyEventArgs));
    public static readonly RoutedEvent KeyUpEvent = Register("KeyUp", RoutingStrategy.Bubble, typeof(KeyEventArgs));
    public static readonly RoutedEvent PreviewGotKeyboardFocusEvent = Register("PreviewGotKeyboardFocus", RoutingStrategy.Tunnel, typeof(KeyboardFocusChangedEventArgs));
    public static readonly RoutedEvent GotKeyboardFocusEvent = Register("GotKeyboardFocus", RoutingStrategy.Bubble, typeof(KeyboardFocusChangedEventArgs));
    public static readonly RoutedEvent PreviewLostKeyboardFocusEvent = Register("PreviewLostKeyboardFocus", RoutingStrategy.Tunnel, typeof(KeyboardFocusChangedEventArgs));
    public static readonly RoutedEvent LostKeyboardFocusEvent = Register("LostKeyboardFocus", RoutingStrategy.Bubble, typeof(KeyboardFocusChangedEventArgs));
    public static readonly RoutedEvent GotFocusEvent = Register("GotFocus", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent LostFocusEvent = Register("LostFocus", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewTextInputEvent = Register("PreviewTextInput", RoutingStrategy.Tunnel, typeof(TextCompositionEventArgs));
    public static readonly RoutedEvent TextInputEvent = Register("TextInput", RoutingStrategy.Bubble, typeof(TextCompositionEventArgs));

    public static readonly RoutedEvent PreviewStylusDownEvent = Register("PreviewStylusDown", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusDownEvent = Register("StylusDown", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusUpEvent = Register("PreviewStylusUp", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusUpEvent = Register("StylusUp", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusMoveEvent = Register("PreviewStylusMove", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusMoveEvent = Register("StylusMove", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusInAirMoveEvent = Register("PreviewStylusInAirMove", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusInAirMoveEvent = Register("StylusInAirMove", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusEnterEvent = Register("StylusEnter", RoutingStrategy.Direct, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusLeaveEvent = Register("StylusLeave", RoutingStrategy.Direct, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusInRangeEvent = Register("PreviewStylusInRange", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusInRangeEvent = Register("StylusInRange", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusOutOfRangeEvent = Register("PreviewStylusOutOfRange", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusOutOfRangeEvent = Register("StylusOutOfRange", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusSystemGestureEvent = Register("PreviewStylusSystemGesture", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusSystemGestureEvent = Register("StylusSystemGesture", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent GotStylusCaptureEvent = Register("GotStylusCapture", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent LostStylusCaptureEvent = Register("LostStylusCapture", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusButtonDownEvent = Register("PreviewStylusButtonDown", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusButtonDownEvent = Register("StylusButtonDown", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewStylusButtonUpEvent = Register("PreviewStylusButtonUp", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent StylusButtonUpEvent = Register("StylusButtonUp", RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public static readonly RoutedEvent PreviewTouchDownEvent = Register("PreviewTouchDown", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent TouchDownEvent = Register("TouchDown", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewTouchMoveEvent = Register("PreviewTouchMove", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent TouchMoveEvent = Register("TouchMove", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewTouchUpEvent = Register("PreviewTouchUp", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent TouchUpEvent = Register("TouchUp", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent TouchEnterEvent = Register("TouchEnter", RoutingStrategy.Direct, typeof(RoutedEventArgs));
    public static readonly RoutedEvent TouchLeaveEvent = Register("TouchLeave", RoutingStrategy.Direct, typeof(RoutedEventArgs));
    public static readonly RoutedEvent GotTouchCaptureEvent = Register("GotTouchCapture", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent LostTouchCaptureEvent = Register("LostTouchCapture", RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public static readonly RoutedEvent ManipulationStartingEvent = Register("ManipulationStarting", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ManipulationStartedEvent = Register("ManipulationStarted", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ManipulationDeltaEvent = Register("ManipulationDelta", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ManipulationInertiaStartingEvent = Register("ManipulationInertiaStarting", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ManipulationBoundaryFeedbackEvent = Register("ManipulationBoundaryFeedback", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent ManipulationCompletedEvent = Register("ManipulationCompleted", RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public static readonly RoutedEvent PreviewQueryContinueDragEvent = Register("PreviewQueryContinueDrag", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent QueryContinueDragEvent = Register("QueryContinueDrag", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewGiveFeedbackEvent = Register("PreviewGiveFeedback", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent GiveFeedbackEvent = Register("GiveFeedback", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewDragEnterEvent = Register("PreviewDragEnter", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent DragEnterEvent = Register("DragEnter", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewDragOverEvent = Register("PreviewDragOver", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent DragOverEvent = Register("DragOver", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewDragLeaveEvent = Register("PreviewDragLeave", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent DragLeaveEvent = Register("DragLeave", RoutingStrategy.Bubble, typeof(RoutedEventArgs));
    public static readonly RoutedEvent PreviewDropEvent = Register("PreviewDrop", RoutingStrategy.Tunnel, typeof(RoutedEventArgs));
    public static readonly RoutedEvent DropEvent = Register("Drop", RoutingStrategy.Bubble, typeof(RoutedEventArgs));

    public static IReadOnlyList<RoutedEvent> All { get; } =
    [
        PreviewMouseDownEvent,
        MouseDownEvent,
        PreviewMouseUpEvent,
        MouseUpEvent,
        PreviewMouseMoveEvent,
        MouseMoveEvent,
        PreviewMouseWheelEvent,
        MouseWheelEvent,
        MouseEnterEvent,
        MouseLeaveEvent,
        GotMouseCaptureEvent,
        LostMouseCaptureEvent,
        QueryCursorEvent,
        PreviewMouseLeftButtonDownEvent,
        MouseLeftButtonDownEvent,
        PreviewMouseLeftButtonUpEvent,
        MouseLeftButtonUpEvent,
        PreviewMouseRightButtonDownEvent,
        MouseRightButtonDownEvent,
        PreviewMouseRightButtonUpEvent,
        MouseRightButtonUpEvent,
        PreviewMouseDoubleClickEvent,
        MouseDoubleClickEvent,
        PreviewKeyDownEvent,
        KeyDownEvent,
        PreviewKeyUpEvent,
        KeyUpEvent,
        PreviewGotKeyboardFocusEvent,
        GotKeyboardFocusEvent,
        PreviewLostKeyboardFocusEvent,
        LostKeyboardFocusEvent,
        GotFocusEvent,
        LostFocusEvent,
        PreviewTextInputEvent,
        TextInputEvent,
        PreviewStylusDownEvent,
        StylusDownEvent,
        PreviewStylusUpEvent,
        StylusUpEvent,
        PreviewStylusMoveEvent,
        StylusMoveEvent,
        PreviewStylusInAirMoveEvent,
        StylusInAirMoveEvent,
        StylusEnterEvent,
        StylusLeaveEvent,
        PreviewStylusInRangeEvent,
        StylusInRangeEvent,
        PreviewStylusOutOfRangeEvent,
        StylusOutOfRangeEvent,
        PreviewStylusSystemGestureEvent,
        StylusSystemGestureEvent,
        GotStylusCaptureEvent,
        LostStylusCaptureEvent,
        PreviewStylusButtonDownEvent,
        StylusButtonDownEvent,
        PreviewStylusButtonUpEvent,
        StylusButtonUpEvent,
        PreviewTouchDownEvent,
        TouchDownEvent,
        PreviewTouchMoveEvent,
        TouchMoveEvent,
        PreviewTouchUpEvent,
        TouchUpEvent,
        TouchEnterEvent,
        TouchLeaveEvent,
        GotTouchCaptureEvent,
        LostTouchCaptureEvent,
        ManipulationStartingEvent,
        ManipulationStartedEvent,
        ManipulationDeltaEvent,
        ManipulationInertiaStartingEvent,
        ManipulationBoundaryFeedbackEvent,
        ManipulationCompletedEvent,
        PreviewQueryContinueDragEvent,
        QueryContinueDragEvent,
        PreviewGiveFeedbackEvent,
        GiveFeedbackEvent,
        PreviewDragEnterEvent,
        DragEnterEvent,
        PreviewDragOverEvent,
        DragOverEvent,
        PreviewDragLeaveEvent,
        DragLeaveEvent,
        PreviewDropEvent,
        DropEvent
    ];

    private static RoutedEvent Register(string name, RoutingStrategy routingStrategy, Type argsType)
    {
        return RoutedEventRegistry.Register(name, typeof(InputEvents), routingStrategy, argsType);
    }
}
