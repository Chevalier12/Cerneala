using Cerneala.UI.Input;
using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

public partial class UIElement
{
    public static readonly UiProperty<bool> IsMouseDirectlyOverProperty = IsPointerOverProperty;

    public static readonly RoutedEvent QueryCursorEvent = InputEvents.QueryCursorEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseLeftButtonDownEvent = InputEvents.PreviewMouseLeftButtonDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseLeftButtonDownEvent = InputEvents.MouseLeftButtonDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseLeftButtonUpEvent = InputEvents.PreviewMouseLeftButtonUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseLeftButtonUpEvent = InputEvents.MouseLeftButtonUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseRightButtonDownEvent = InputEvents.PreviewMouseRightButtonDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseRightButtonDownEvent = InputEvents.MouseRightButtonDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewMouseRightButtonUpEvent = InputEvents.PreviewMouseRightButtonUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent MouseRightButtonUpEvent = InputEvents.MouseRightButtonUpEvent.AddOwner(typeof(UIElement));

    public static readonly RoutedEvent PreviewStylusDownEvent = InputEvents.PreviewStylusDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusDownEvent = InputEvents.StylusDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusUpEvent = InputEvents.PreviewStylusUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusUpEvent = InputEvents.StylusUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusMoveEvent = InputEvents.PreviewStylusMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusMoveEvent = InputEvents.StylusMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusInAirMoveEvent = InputEvents.PreviewStylusInAirMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusInAirMoveEvent = InputEvents.StylusInAirMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusEnterEvent = InputEvents.StylusEnterEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusLeaveEvent = InputEvents.StylusLeaveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusInRangeEvent = InputEvents.PreviewStylusInRangeEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusInRangeEvent = InputEvents.StylusInRangeEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusOutOfRangeEvent = InputEvents.PreviewStylusOutOfRangeEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusOutOfRangeEvent = InputEvents.StylusOutOfRangeEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusSystemGestureEvent = InputEvents.PreviewStylusSystemGestureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusSystemGestureEvent = InputEvents.StylusSystemGestureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent GotStylusCaptureEvent = InputEvents.GotStylusCaptureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent LostStylusCaptureEvent = InputEvents.LostStylusCaptureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusButtonDownEvent = InputEvents.PreviewStylusButtonDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusButtonDownEvent = InputEvents.StylusButtonDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewStylusButtonUpEvent = InputEvents.PreviewStylusButtonUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent StylusButtonUpEvent = InputEvents.StylusButtonUpEvent.AddOwner(typeof(UIElement));

    public static readonly RoutedEvent PreviewTouchDownEvent = InputEvents.PreviewTouchDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent TouchDownEvent = InputEvents.TouchDownEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewTouchMoveEvent = InputEvents.PreviewTouchMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent TouchMoveEvent = InputEvents.TouchMoveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewTouchUpEvent = InputEvents.PreviewTouchUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent TouchUpEvent = InputEvents.TouchUpEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent TouchEnterEvent = InputEvents.TouchEnterEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent TouchLeaveEvent = InputEvents.TouchLeaveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent GotTouchCaptureEvent = InputEvents.GotTouchCaptureEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent LostTouchCaptureEvent = InputEvents.LostTouchCaptureEvent.AddOwner(typeof(UIElement));

    public static readonly RoutedEvent ManipulationStartingEvent = InputEvents.ManipulationStartingEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent ManipulationStartedEvent = InputEvents.ManipulationStartedEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent ManipulationDeltaEvent = InputEvents.ManipulationDeltaEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent ManipulationInertiaStartingEvent = InputEvents.ManipulationInertiaStartingEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent ManipulationBoundaryFeedbackEvent = InputEvents.ManipulationBoundaryFeedbackEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent ManipulationCompletedEvent = InputEvents.ManipulationCompletedEvent.AddOwner(typeof(UIElement));

    public static readonly RoutedEvent PreviewQueryContinueDragEvent = InputEvents.PreviewQueryContinueDragEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewGiveFeedbackEvent = InputEvents.PreviewGiveFeedbackEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewDragEnterEvent = InputEvents.PreviewDragEnterEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewDragOverEvent = InputEvents.PreviewDragOverEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewDragLeaveEvent = InputEvents.PreviewDragLeaveEvent.AddOwner(typeof(UIElement));
    public static readonly RoutedEvent PreviewDropEvent = InputEvents.PreviewDropEvent.AddOwner(typeof(UIElement));

    public event RoutedEventHandler QueryCursor { add => AddHandler(QueryCursorEvent, value); remove => RemoveHandler(QueryCursorEvent, value); }
    public event RoutedEventHandler PreviewMouseLeftButtonDown { add => AddHandler(PreviewMouseLeftButtonDownEvent, value); remove => RemoveHandler(PreviewMouseLeftButtonDownEvent, value); }
    public event RoutedEventHandler MouseLeftButtonDown { add => AddHandler(MouseLeftButtonDownEvent, value); remove => RemoveHandler(MouseLeftButtonDownEvent, value); }
    public event RoutedEventHandler PreviewMouseLeftButtonUp { add => AddHandler(PreviewMouseLeftButtonUpEvent, value); remove => RemoveHandler(PreviewMouseLeftButtonUpEvent, value); }
    public event RoutedEventHandler MouseLeftButtonUp { add => AddHandler(MouseLeftButtonUpEvent, value); remove => RemoveHandler(MouseLeftButtonUpEvent, value); }
    public event RoutedEventHandler PreviewMouseRightButtonDown { add => AddHandler(PreviewMouseRightButtonDownEvent, value); remove => RemoveHandler(PreviewMouseRightButtonDownEvent, value); }
    public event RoutedEventHandler MouseRightButtonDown { add => AddHandler(MouseRightButtonDownEvent, value); remove => RemoveHandler(MouseRightButtonDownEvent, value); }
    public event RoutedEventHandler PreviewMouseRightButtonUp { add => AddHandler(PreviewMouseRightButtonUpEvent, value); remove => RemoveHandler(PreviewMouseRightButtonUpEvent, value); }
    public event RoutedEventHandler MouseRightButtonUp { add => AddHandler(MouseRightButtonUpEvent, value); remove => RemoveHandler(MouseRightButtonUpEvent, value); }

    public event RoutedEventHandler PreviewStylusDown { add => AddHandler(PreviewStylusDownEvent, value); remove => RemoveHandler(PreviewStylusDownEvent, value); }
    public event RoutedEventHandler StylusDown { add => AddHandler(StylusDownEvent, value); remove => RemoveHandler(StylusDownEvent, value); }
    public event RoutedEventHandler PreviewStylusUp { add => AddHandler(PreviewStylusUpEvent, value); remove => RemoveHandler(PreviewStylusUpEvent, value); }
    public event RoutedEventHandler StylusUp { add => AddHandler(StylusUpEvent, value); remove => RemoveHandler(StylusUpEvent, value); }
    public event RoutedEventHandler PreviewStylusMove { add => AddHandler(PreviewStylusMoveEvent, value); remove => RemoveHandler(PreviewStylusMoveEvent, value); }
    public event RoutedEventHandler StylusMove { add => AddHandler(StylusMoveEvent, value); remove => RemoveHandler(StylusMoveEvent, value); }
    public event RoutedEventHandler PreviewStylusInAirMove { add => AddHandler(PreviewStylusInAirMoveEvent, value); remove => RemoveHandler(PreviewStylusInAirMoveEvent, value); }
    public event RoutedEventHandler StylusInAirMove { add => AddHandler(StylusInAirMoveEvent, value); remove => RemoveHandler(StylusInAirMoveEvent, value); }
    public event RoutedEventHandler StylusEnter { add => AddHandler(StylusEnterEvent, value); remove => RemoveHandler(StylusEnterEvent, value); }
    public event RoutedEventHandler StylusLeave { add => AddHandler(StylusLeaveEvent, value); remove => RemoveHandler(StylusLeaveEvent, value); }
    public event RoutedEventHandler PreviewStylusInRange { add => AddHandler(PreviewStylusInRangeEvent, value); remove => RemoveHandler(PreviewStylusInRangeEvent, value); }
    public event RoutedEventHandler StylusInRange { add => AddHandler(StylusInRangeEvent, value); remove => RemoveHandler(StylusInRangeEvent, value); }
    public event RoutedEventHandler PreviewStylusOutOfRange { add => AddHandler(PreviewStylusOutOfRangeEvent, value); remove => RemoveHandler(PreviewStylusOutOfRangeEvent, value); }
    public event RoutedEventHandler StylusOutOfRange { add => AddHandler(StylusOutOfRangeEvent, value); remove => RemoveHandler(StylusOutOfRangeEvent, value); }
    public event RoutedEventHandler PreviewStylusSystemGesture { add => AddHandler(PreviewStylusSystemGestureEvent, value); remove => RemoveHandler(PreviewStylusSystemGestureEvent, value); }
    public event RoutedEventHandler StylusSystemGesture { add => AddHandler(StylusSystemGestureEvent, value); remove => RemoveHandler(StylusSystemGestureEvent, value); }
    public event RoutedEventHandler GotStylusCapture { add => AddHandler(GotStylusCaptureEvent, value); remove => RemoveHandler(GotStylusCaptureEvent, value); }
    public event RoutedEventHandler LostStylusCapture { add => AddHandler(LostStylusCaptureEvent, value); remove => RemoveHandler(LostStylusCaptureEvent, value); }
    public event RoutedEventHandler PreviewStylusButtonDown { add => AddHandler(PreviewStylusButtonDownEvent, value); remove => RemoveHandler(PreviewStylusButtonDownEvent, value); }
    public event RoutedEventHandler StylusButtonDown { add => AddHandler(StylusButtonDownEvent, value); remove => RemoveHandler(StylusButtonDownEvent, value); }
    public event RoutedEventHandler PreviewStylusButtonUp { add => AddHandler(PreviewStylusButtonUpEvent, value); remove => RemoveHandler(PreviewStylusButtonUpEvent, value); }
    public event RoutedEventHandler StylusButtonUp { add => AddHandler(StylusButtonUpEvent, value); remove => RemoveHandler(StylusButtonUpEvent, value); }

    public event RoutedEventHandler PreviewTouchDown { add => AddHandler(PreviewTouchDownEvent, value); remove => RemoveHandler(PreviewTouchDownEvent, value); }
    public event RoutedEventHandler TouchDown { add => AddHandler(TouchDownEvent, value); remove => RemoveHandler(TouchDownEvent, value); }
    public event RoutedEventHandler PreviewTouchMove { add => AddHandler(PreviewTouchMoveEvent, value); remove => RemoveHandler(PreviewTouchMoveEvent, value); }
    public event RoutedEventHandler TouchMove { add => AddHandler(TouchMoveEvent, value); remove => RemoveHandler(TouchMoveEvent, value); }
    public event RoutedEventHandler PreviewTouchUp { add => AddHandler(PreviewTouchUpEvent, value); remove => RemoveHandler(PreviewTouchUpEvent, value); }
    public event RoutedEventHandler TouchUp { add => AddHandler(TouchUpEvent, value); remove => RemoveHandler(TouchUpEvent, value); }
    public event RoutedEventHandler TouchEnter { add => AddHandler(TouchEnterEvent, value); remove => RemoveHandler(TouchEnterEvent, value); }
    public event RoutedEventHandler TouchLeave { add => AddHandler(TouchLeaveEvent, value); remove => RemoveHandler(TouchLeaveEvent, value); }
    public event RoutedEventHandler GotTouchCapture { add => AddHandler(GotTouchCaptureEvent, value); remove => RemoveHandler(GotTouchCaptureEvent, value); }
    public event RoutedEventHandler LostTouchCapture { add => AddHandler(LostTouchCaptureEvent, value); remove => RemoveHandler(LostTouchCaptureEvent, value); }

    public event RoutedEventHandler ManipulationStarting { add => AddHandler(ManipulationStartingEvent, value); remove => RemoveHandler(ManipulationStartingEvent, value); }
    public event RoutedEventHandler ManipulationStarted { add => AddHandler(ManipulationStartedEvent, value); remove => RemoveHandler(ManipulationStartedEvent, value); }
    public event RoutedEventHandler ManipulationDelta { add => AddHandler(ManipulationDeltaEvent, value); remove => RemoveHandler(ManipulationDeltaEvent, value); }
    public event RoutedEventHandler ManipulationInertiaStarting { add => AddHandler(ManipulationInertiaStartingEvent, value); remove => RemoveHandler(ManipulationInertiaStartingEvent, value); }
    public event RoutedEventHandler ManipulationBoundaryFeedback { add => AddHandler(ManipulationBoundaryFeedbackEvent, value); remove => RemoveHandler(ManipulationBoundaryFeedbackEvent, value); }
    public event RoutedEventHandler ManipulationCompleted { add => AddHandler(ManipulationCompletedEvent, value); remove => RemoveHandler(ManipulationCompletedEvent, value); }

    public event RoutedEventHandler PreviewQueryContinueDrag { add => AddHandler(PreviewQueryContinueDragEvent, value); remove => RemoveHandler(PreviewQueryContinueDragEvent, value); }
    public event RoutedEventHandler PreviewGiveFeedback { add => AddHandler(PreviewGiveFeedbackEvent, value); remove => RemoveHandler(PreviewGiveFeedbackEvent, value); }
    public event RoutedEventHandler PreviewDragEnter { add => AddHandler(PreviewDragEnterEvent, value); remove => RemoveHandler(PreviewDragEnterEvent, value); }
    public event RoutedEventHandler PreviewDragOver { add => AddHandler(PreviewDragOverEvent, value); remove => RemoveHandler(PreviewDragOverEvent, value); }
    public event RoutedEventHandler PreviewDragLeave { add => AddHandler(PreviewDragLeaveEvent, value); remove => RemoveHandler(PreviewDragLeaveEvent, value); }
    public event RoutedEventHandler PreviewDrop { add => AddHandler(PreviewDropEvent, value); remove => RemoveHandler(PreviewDropEvent, value); }
}
