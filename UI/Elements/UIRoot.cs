namespace Cerneala.UI.Elements;

using Cerneala.UI.Diagnostics;
using Cerneala.UI.Invalidation;

public sealed class UIRoot : UIElement, IElementHost, IInvalidationSink
{
    public UIRoot(float viewportWidth = 0, float viewportHeight = 0, float scale = 1)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Scale = scale;
        ElementIds = new ElementIdProvider();
        Trace = new InvalidationTrace();
        LayoutQueue = new LayoutQueue(this);
        RenderQueue = new RenderQueue(this);
        HitTestQueue = new HitTestQueue(this);
        Scheduler = new UiFrameScheduler(LayoutQueue, RenderQueue, HitTestQueue, Trace);
        ElementLifecycle.AttachSubtree(this, this);
    }

    UIRoot IElementHost.Root => this;

    public float ViewportWidth { get; private set; }

    public float ViewportHeight { get; private set; }

    public float Scale { get; private set; }

    public int TreeVersion { get; private set; }

    public ElementIdProvider ElementIds { get; }

    public InvalidationTrace Trace { get; }

    public LayoutQueue LayoutQueue { get; }

    public RenderQueue RenderQueue { get; }

    public HitTestQueue HitTestQueue { get; }

    public UiFrameScheduler Scheduler { get; }

    public void SetViewport(float width, float height, float scale)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        Scale = scale;
        IncrementTreeVersion();
    }

    internal void IncrementTreeVersion()
    {
        TreeVersion++;
    }

    public override void Invalidate(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Trace.RecordRequest(request);
        DirtyPropagation.Default.Propagate(request, this, LayoutQueue, RenderQueue, HitTestQueue, Trace);
    }

    public FrameStats ProcessFrame(FramePhaseProcessors? processors = null, FrameBudget budget = default)
    {
        return Scheduler.ProcessFrame(processors, budget);
    }
}
