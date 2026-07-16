using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Input;

public sealed class GestureMotionController : IDisposable
{
    private readonly UIElement element;
    private MotionHandle? scaleHandle;
    private bool disposed;

    internal GestureMotionController(UIElement element)
    {
        this.element = element ?? throw new ArgumentNullException(nameof(element));
    }

    public PointerMotionState State { get; private set; }

    public void PointerPressed(MotionSpec<float> spec)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(spec);
        State = PointerMotionState.Pressed;
        scaleHandle = element.Motion().Scale.To(0.97f, spec);
    }

    public void PointerReleased(MotionSpec<float> spec)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(spec);
        State = PointerMotionState.Idle;
        scaleHandle = element.Motion().Scale.To(1, spec);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        scaleHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        scaleHandle?.Dispose();
        scaleHandle = null;
        State = PointerMotionState.Idle;
    }
}
