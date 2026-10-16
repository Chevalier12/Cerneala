using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls.Primitives;

public class Thumb : Control, IPointerDragSource
{
    public static readonly RoutedEvent DragStartedEvent = RoutedEventRegistry.Register(nameof(DragStarted), typeof(Thumb), RoutingStrategy.Bubble, typeof(DragStartedEventArgs));
    public static readonly RoutedEvent DragDeltaEvent = RoutedEventRegistry.Register(nameof(DragDelta), typeof(Thumb), RoutingStrategy.Bubble, typeof(DragDeltaEventArgs));
    public static readonly RoutedEvent DragCompletedEvent = RoutedEventRegistry.Register(nameof(DragCompleted), typeof(Thumb), RoutingStrategy.Bubble, typeof(DragCompletedEventArgs));
    private bool isDragging;
    private Vector2 lastPoint;
    private Vector2 startPoint;

    public Thumb()
    {
        SetFrameworkDefault(
            BackgroundProperty,
            new Cerneala.UI.Media.SolidColorBrush(new Color(180, 180, 180)));
        SetFrameworkDefault(
            BorderBrushProperty,
            new Cerneala.UI.Media.SolidColorBrush(new Color(80, 80, 80)));
        SetFrameworkDefault(BorderThicknessProperty, new Thickness(1));
        Handlers.AddHandler(InputEvents.MouseMoveEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseWheelEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseDownEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseUpEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseLeftButtonDownEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseLeftButtonUpEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseRightButtonDownEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.MouseRightButtonUpEvent, OnCapturedPointerInput);
        Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => CancelDrag());
    }

    public event EventHandler<DragStartedEventArgs> DragStarted { add => AddTypedHandler(DragStartedEvent, value); remove => RemoveTypedHandler(DragStartedEvent, value); }

    public event EventHandler<DragDeltaEventArgs> DragDelta { add => AddTypedHandler(DragDeltaEvent, value); remove => RemoveTypedHandler(DragDeltaEvent, value); }

    public event EventHandler<DragCompletedEventArgs> DragCompleted { add => AddTypedHandler(DragCompletedEvent, value); remove => RemoveTypedHandler(DragCompletedEvent, value); }

    public bool IsDragging => isDragging;

    public float LastHorizontalChange { get; private set; }

    public float LastVerticalChange { get; private set; }

    public float TotalHorizontalChange { get; private set; }

    public float TotalVerticalChange { get; private set; }

    public bool BeginDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(captureManager);
        ArgumentNullException.ThrowIfNull(routeMap);
        ArgumentNullException.ThrowIfNull(args);

        if (!IsEnabled || args.ChangedButton != InputMouseButton.Left)
        {
            return false;
        }

        isDragging = true;
        startPoint = lastPoint = GetDragPoint(args);
        LastHorizontalChange = 0;
        LastVerticalChange = 0;
        TotalHorizontalChange = 0;
        TotalVerticalChange = 0;
        captureManager.Capture(this, routeMap);
        RaiseEvent(new DragStartedEventArgs(DragStartedEvent, this, args.X, args.Y));
        args.Handled = true;
        return true;
    }

    public bool BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
    {
        return BeginDrag(captureManager, routeMap, args);
    }

    public bool UpdateDrag(MouseEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!isDragging)
        {
            return false;
        }

        Vector2 point = GetDragPoint(args);
        LastHorizontalChange = point.X - lastPoint.X;
        LastVerticalChange = point.Y - lastPoint.Y;
        TotalHorizontalChange = point.X - startPoint.X;
        TotalVerticalChange = point.Y - startPoint.Y;
        lastPoint = point;
        if (LastHorizontalChange != 0 || LastVerticalChange != 0)
        {
            RaiseEvent(new DragDeltaEventArgs(DragDeltaEvent, this, LastHorizontalChange, LastVerticalChange, TotalHorizontalChange, TotalVerticalChange));
        }

        args.Handled = true;
        return true;
    }

    public bool UpdatePointerDrag(MouseEventArgs args)
    {
        return UpdateDrag(args);
    }

    public bool CompleteDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(captureManager);
        ArgumentNullException.ThrowIfNull(routeMap);
        ArgumentNullException.ThrowIfNull(args);
        if (!isDragging || args.ChangedButton != InputMouseButton.Left)
        {
            return false;
        }

        UpdateDrag(args);
        isDragging = false;
        captureManager.Release(routeMap);
        RaiseEvent(new DragCompletedEventArgs(DragCompletedEvent, this, TotalHorizontalChange, TotalVerticalChange, false));
        args.Handled = true;
        return true;
    }

    public bool CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
    {
        return CompleteDrag(captureManager, routeMap, args);
    }

    public void CancelDrag()
    {
        if (!isDragging)
        {
            return;
        }

        isDragging = false;
        RaiseEvent(new DragCompletedEventArgs(DragCompletedEvent, this, TotalHorizontalChange, TotalVerticalChange, true));
    }

    protected override void OnDetached()
    {
        CancelDrag();
        base.OnDetached();
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(10, 10);
    }

    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Background is { } background && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderBrush is { } borderBrush && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, borderBrush, thickness);
        }
    }

    // Drag changes are measured in the layout space where the thumb is arranged: the root point goes
    // back through the render transforms of every ancestor, so moving the thumb by a change keeps the
    // drawn thumb under the pointer. A point that cannot be converted reports no movement.
    private Vector2 GetDragPoint(MouseEventArgs args)
    {
        Vector2 rootPoint = new(args.RootX, args.RootY);
        if (VisualParent is not { } parent)
        {
            return rootPoint;
        }

        if (!InputCoordinateConverter.TryRootToElement(parent, rootPoint, out Vector2 parentPoint))
        {
            return lastPoint;
        }

        return new Vector2(parentPoint.X + parent.ArrangedBounds.X, parentPoint.Y + parent.ArrangedBounds.Y);
    }

    private void OnCapturedPointerInput(UiElementId source, RoutedEventArgs args)
    {
        if (isDragging)
        {
            args.Handled = true;
        }
    }
}
