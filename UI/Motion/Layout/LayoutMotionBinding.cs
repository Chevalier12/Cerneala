using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;

namespace Cerneala.UI.Motion.Layout;

public sealed class LayoutMotionBinding : IDisposable
{
    private readonly UIElement element;
    private readonly MotionValue<Transform> correction;
    private readonly IDisposable subscription;
    private MotionHandle? activeHandle;
    private bool disposed;

    internal LayoutMotionBinding(MotionSystem motion, UIElement element)
    {
        ArgumentNullException.ThrowIfNull(motion);
        this.element = element ?? throw new ArgumentNullException(nameof(element));
        correction = motion.Graph.CreateValue(Transform.Identity);
        subscription = correction.Subscribe(change => element.SetLayoutCorrectionTransform(change.NewValue));
    }

    public Transform CurrentCorrection => correction.Current;

    public bool IsActive => correction.IsAnimating;

    internal void StartCorrection(Transform inverse, MotionSpec<Transform> spec)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(inverse);
        ArgumentNullException.ThrowIfNull(spec);

        activeHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        correction.JumpTo(inverse);
        element.SetLayoutCorrectionTransform(inverse);
        activeHandle = correction.AnimateTo(Transform.Identity, spec);
    }

    internal void ClearCorrection()
    {
        if (disposed)
        {
            return;
        }

        activeHandle?.Cancel(MotionCancelBehavior.KeepCurrent);
        activeHandle = null;
        correction.JumpTo(Transform.Identity);
        element.SetLayoutCorrectionTransform(Transform.Identity);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        activeHandle?.Dispose();
        subscription.Dispose();
        element.SetLayoutCorrectionTransform(Transform.Identity);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
