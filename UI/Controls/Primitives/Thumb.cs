using Cerneala.Drawing;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls.Primitives;

public class Thumb : Control, IPointerDragSource
{
    private bool isDragging;
    private int lastX;
    private int lastY;
    private int startX;
    private int startY;

    public Thumb()
    {
        Background = new DrawColor(180, 180, 180);
        BorderColor = new DrawColor(80, 80, 80);
        BorderThickness = new Thickness(1);
        Handlers.AddHandler(InputEvents.LostMouseCaptureEvent, (_, _) => CancelDrag());
    }

    public event EventHandler<DragStartedEventArgs>? DragStarted;

    public event EventHandler<DragDeltaEventArgs>? DragDelta;

    public event EventHandler<DragCompletedEventArgs>? DragCompleted;

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
        startX = lastX = args.X;
        startY = lastY = args.Y;
        LastHorizontalChange = 0;
        LastVerticalChange = 0;
        TotalHorizontalChange = 0;
        TotalVerticalChange = 0;
        captureManager.Capture(this, routeMap);
        DragStarted?.Invoke(this, new DragStartedEventArgs(args.X, args.Y));
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

        LastHorizontalChange = args.X - lastX;
        LastVerticalChange = args.Y - lastY;
        TotalHorizontalChange = args.X - startX;
        TotalVerticalChange = args.Y - startY;
        lastX = args.X;
        lastY = args.Y;
        if (LastHorizontalChange != 0 || LastVerticalChange != 0)
        {
            DragDelta?.Invoke(this, new DragDeltaEventArgs(LastHorizontalChange, LastVerticalChange, TotalHorizontalChange, TotalVerticalChange));
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
        DragCompleted?.Invoke(this, new DragCompletedEventArgs(TotalHorizontalChange, TotalVerticalChange, false));
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
        DragCompleted?.Invoke(this, new DragCompletedEventArgs(TotalHorizontalChange, TotalVerticalChange, true));
    }

    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(10, 10);
    }

    protected override void OnRender(RenderContext context)
    {
        DrawRect rect = Border.ToDrawRect(context.Bounds);
        if (Background.A != 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.FillRectangle(rect, Background);
        }

        float thickness = MathF.Max(MathF.Max(BorderThickness.Left, BorderThickness.Top), MathF.Max(BorderThickness.Right, BorderThickness.Bottom));
        if (BorderColor.A != 0 && thickness > 0 && rect.Width > 0 && rect.Height > 0)
        {
            context.DrawingContext.DrawRectangle(rect, BorderColor, thickness);
        }
    }
}

public sealed class DragStartedEventArgs : EventArgs
{
    public DragStartedEventArgs(float x, float y)
    {
        X = x;
        Y = y;
    }

    public float X { get; }

    public float Y { get; }
}

public sealed class DragDeltaEventArgs : EventArgs
{
    public DragDeltaEventArgs(float horizontalChange, float verticalChange, float totalHorizontalChange, float totalVerticalChange)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        TotalHorizontalChange = totalHorizontalChange;
        TotalVerticalChange = totalVerticalChange;
    }

    public float HorizontalChange { get; }

    public float VerticalChange { get; }

    public float TotalHorizontalChange { get; }

    public float TotalVerticalChange { get; }
}

public sealed class DragCompletedEventArgs : EventArgs
{
    public DragCompletedEventArgs(float horizontalChange, float verticalChange, bool canceled)
    {
        HorizontalChange = horizontalChange;
        VerticalChange = verticalChange;
        Canceled = canceled;
    }

    public float HorizontalChange { get; }

    public float VerticalChange { get; }

    public bool Canceled { get; }
}
