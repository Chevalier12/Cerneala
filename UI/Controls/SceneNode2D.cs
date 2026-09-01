using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.UI.Controls;

public abstract class SceneNode2D : UIElement
{
    internal RenderSurface2D? Surface { get; private set; }

    internal virtual void AttachSurface(RenderSurface2D? surface)
    {
        Surface = surface;
    }

    internal abstract void Record(RenderSurface2DFrame frame);

    protected override void OnAttached()
    {
        base.OnAttached();
        ProcessPendingAspect();
    }

    public override void Invalidate(InvalidationRequest request)
    {
        base.Invalidate(request);
        ProcessPendingAspect();
        Surface?.InvalidateFrame();
    }

    private void ProcessPendingAspect()
    {
        if (Root is not UIRoot root ||
            !DirtyState.Has(InvalidationFlags.Aspect))
        {
            return;
        }

        root.AspectProcessor.Process(this);
        root.AspectQueue.Remove(this);
        DirtyState.Clear(InvalidationFlags.Aspect);
    }
}
