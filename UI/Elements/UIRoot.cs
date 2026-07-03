using Cerneala.UI.Diagnostics;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Styling;

namespace Cerneala.UI.Elements;

public sealed class UIRoot : UIElement, IElementHost, IInvalidationSink
{
    private readonly StyleApplicator styleApplicator;
    private ThemeProvider? themeProvider;
    private ThemeChangedSubscription? themeChangedSubscription;

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
        styleApplicator = new StyleApplicator();
        StyleProcessor = new StyleProcessor(styleApplicator, () => StyleSheet, () => themeProvider);
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

    public StyleSheet? StyleSheet { get; private set; }

    public ThemeProvider? ThemeProvider => themeProvider;

    public StyleProcessor StyleProcessor { get; }

    public void SetViewport(float width, float height, float scale)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        Scale = scale;
        IncrementTreeVersion();
    }

    public void SetStyleSheet(StyleSheet? styleSheet)
    {
        if (ReferenceEquals(StyleSheet, styleSheet))
        {
            return;
        }

        StyleSheet = styleSheet;
        Invalidate(InvalidationFlags.Style | InvalidationFlags.Subtree, "Style sheet changed");
    }

    public void SetThemeProvider(ThemeProvider? provider)
    {
        if (ReferenceEquals(themeProvider, provider))
        {
            return;
        }

        themeChangedSubscription?.Dispose();
        themeChangedSubscription = null;

        themeProvider = provider;
        if (themeProvider is not null)
        {
            themeChangedSubscription = new ThemeChangedSubscription(this, themeProvider);
        }

        Invalidate(InvalidationFlags.Style | InvalidationFlags.Subtree, "Theme provider changed");
    }

    private void InvalidateThemeChange()
    {
        Invalidate(InvalidationFlags.Style | InvalidationFlags.Subtree, "Theme changed");
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
            Style = StyleProcessor.Process,
            Measure = layoutProcessors.Measure,
            Arrange = layoutProcessors.Arrange,
            RenderCache = RenderQueueProcessor.Process
        };
    }

    private sealed class ThemeChangedSubscription : IDisposable
    {
        private readonly WeakReference<UIRoot> rootReference;
        private readonly ThemeProvider provider;
        private bool disposed;

        public ThemeChangedSubscription(UIRoot root, ThemeProvider provider)
        {
            rootReference = new WeakReference<UIRoot>(root);
            this.provider = provider;
            provider.ThemeChanged += OnThemeChanged;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            provider.ThemeChanged -= OnThemeChanged;
            disposed = true;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs args)
        {
            if (rootReference.TryGetTarget(out UIRoot? root))
            {
                root.InvalidateThemeChange();
                return;
            }

            Dispose();
        }
    }
}
