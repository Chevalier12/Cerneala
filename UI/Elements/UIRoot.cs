using Cerneala.UI.Diagnostics;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

namespace Cerneala.UI.Elements;

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
        StyleQueue = new StyleQueue(this);
        RenderQueue = new RenderQueue(this);
        HitTestQueue = new HitTestQueue(this);
        LayoutManager = new LayoutManager(this);
        RenderCounters = new RenderCounters();
        RetainedRenderCache = new RetainedRenderCache();
        RenderQueueProcessor = new RenderQueueProcessor(RetainedRenderCache, RenderCounters);
        RetainedRenderer = new RetainedRenderer(RetainedRenderCache, new DrawCommandListBuilder(), RenderCounters);
        Scheduler = new UiFrameScheduler(LayoutQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
        IsLayoutBoundary = true;
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

    public StyleQueue StyleQueue { get; }

    public RenderQueue RenderQueue { get; }

    public HitTestQueue HitTestQueue { get; }

    public LayoutManager LayoutManager { get; }

    public RenderCounters RenderCounters { get; }

    public RetainedRenderCache RetainedRenderCache { get; }

    public RenderQueueProcessor RenderQueueProcessor { get; }

    public RetainedRenderer RetainedRenderer { get; }

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
        RetainedRenderCache.InvalidateRoot();
    }

    public override void Invalidate(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Trace.RecordRequest(request);
        if (DirtyPropagation.Default.GetEffectiveFlags(request).HasFlag(InvalidationFlags.Render))
        {
            RetainedRenderCache.InvalidateRoot();
        }

        DirtyPropagation.Default.Propagate(request, this, LayoutQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
    }

    public FrameStats ProcessFrame(FramePhaseProcessors? processors = null, FrameBudget budget = default)
    {
        return Scheduler.ProcessFrame(processors ?? CreatePhaseProcessors(), budget);
    }

    private FramePhaseProcessors CreatePhaseProcessors()
    {
        FramePhaseProcessors layoutProcessors = LayoutManager.CreatePhaseProcessors();
        return new FramePhaseProcessors
        {
            Measure = layoutProcessors.Measure,
            Arrange = layoutProcessors.Arrange,
            RenderCache = RenderQueueProcessor.Process
        };
    }
}
