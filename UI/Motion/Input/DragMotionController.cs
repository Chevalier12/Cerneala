using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Input;

public sealed class DragMotionController : IDisposable
{
    private readonly UIElement element;
    private readonly VelocityTracker velocity = new();
    private float originX;
    private float originY;
    private float startX;
    private float startY;
    private IDisposable? xSubscription;
    private IDisposable? ySubscription;
    private MotionHandle? xSettleHandle;
    private MotionHandle? ySettleHandle;
    private bool disposed;

    internal DragMotionController(UIElement element)
    {
        this.element = element ?? throw new ArgumentNullException(nameof(element));
        MotionSystem motion = element.Root?.Motion ?? throw new InvalidOperationException("Element must be attached before drag motion can be created.");
        DragX = motion.Graph.CreateValue(element.TranslateX);
        DragY = motion.Graph.CreateValue(element.TranslateY);
        xSubscription = DragX.Subscribe(change => element.SetValue(UIElement.TranslateXProperty, change.NewValue, UiPropertyValueSource.Animation));
        ySubscription = DragY.Subscribe(change => element.SetValue(UIElement.TranslateYProperty, change.NewValue, UiPropertyValueSource.Animation));
    }

    public PointerMotionState State { get; private set; }

    public MotionValue<float> DragX { get; }

    public MotionValue<float> DragY { get; }

    public float VelocityX => velocity.VelocityX;

    public float VelocityY => velocity.VelocityY;

    public void Begin(float x, float y, TimeSpan time)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        State = PointerMotionState.Dragging;
        startX = DragX.Current;
        startY = DragY.Current;
        originX = x - DragX.Current;
        originY = y - DragY.Current;
        velocity.Reset(x, y, time);
    }

    public void Move(float x, float y, TimeSpan time)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        velocity.Add(x, y, time);
        DragX.JumpTo(x - originX);
        DragY.JumpTo(y - originY);
    }

    public void End(MotionSpec<float> settleSpec)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(settleSpec);
        State = PointerMotionState.Settling;
        xSettleHandle = DragX.AnimateTo(DragX.Current + (velocity.VelocityX * 0.1f), settleSpec);
        ySettleHandle = DragY.AnimateTo(DragY.Current + (velocity.VelocityY * 0.1f), settleSpec);
    }

    public void PointerCaptureLost(MotionSpec<float> settleSpec)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(settleSpec);
        if (State != PointerMotionState.Dragging)
        {
            return;
        }

        State = PointerMotionState.Settling;
        xSettleHandle = DragX.AnimateTo(startX, settleSpec);
        ySettleHandle = DragY.AnimateTo(startY, settleSpec);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        xSettleHandle?.Dispose();
        ySettleHandle?.Dispose();
        xSettleHandle = null;
        ySettleHandle = null;
        xSubscription?.Dispose();
        ySubscription?.Dispose();
        xSubscription = null;
        ySubscription = null;
        State = PointerMotionState.Idle;
    }
}
