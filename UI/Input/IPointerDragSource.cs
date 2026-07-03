namespace Cerneala.UI.Input;

public interface IPointerDragSource
{
    bool BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args);

    bool UpdatePointerDrag(MouseEventArgs args);

    bool CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args);
}
