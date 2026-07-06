using Cerneala.UI.Diagnostics;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Styling;

namespace Cerneala.UI.Elements;

public sealed class UIRoot : UIElement, IElementHost, IInvalidationSink
{
    private readonly StyleApplicator styleApplicator;
    private ThemeProvider? themeProvider;
    private ThemeChangedSubscription? themeChangedSubscription;
    private IObservableResourceProvider? observableResourceProvider;
    private FrameStats? activeFrameStats;
    private readonly SemanticsProvider semanticsProvider = new();
    private SemanticsTree? cachedSemanticsTree;
    private int cachedSemanticsTreeVersion = -1;
    private bool semanticsDirty = true;

    public UIRoot(float viewportWidth = 0, float viewportHeight = 0, float scale = 1)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Scale = scale;
        ElementIds = new ElementIdProvider();
        Trace = new InvalidationTrace();
        LayoutQueue = new LayoutQueue(this);
        InheritedPropertyQueue = new InheritedPropertyQueue(this);
        CommandStateQueue = new CommandStateQueue(this);
        StyleQueue = new StyleQueue(this);
        RenderQueue = new RenderQueue(this);
        HitTestQueue = new HitTestQueue(this);
        InputCache = new ElementInputCache();
        LayoutManager = new LayoutManager(this);
        RenderCounters = new RenderCounters();
        RetainedRenderCache = new RetainedRenderCache();
        RenderQueueProcessor = new RenderQueueProcessor(RetainedRenderCache, RenderCounters);
        RetainedRenderer = new RetainedRenderer(RetainedRenderCache, new DrawCommandListBuilder(), RenderCounters);
        InheritedPropertyPropagator = new InheritedPropertyPropagator();
        ResourceDependencyTracker = new ResourceDependencyTracker();
        styleApplicator = new StyleApplicator();
        StyleProcessor = new StyleProcessor(styleApplicator, () => StyleSheet, () => themeProvider);
        Scheduler = new UiFrameScheduler(LayoutQueue, InheritedPropertyQueue, CommandStateQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
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

    public InheritedPropertyQueue InheritedPropertyQueue { get; }

    public CommandStateQueue CommandStateQueue { get; }

    public StyleQueue StyleQueue { get; }

    public RenderQueue RenderQueue { get; }

    public HitTestQueue HitTestQueue { get; }

    public ElementInputCache InputCache { get; }

    public LayoutManager LayoutManager { get; }

    public RenderCounters RenderCounters { get; }

    public RetainedRenderCache RetainedRenderCache { get; }

    public RenderQueueProcessor RenderQueueProcessor { get; }

    public RetainedRenderer RetainedRenderer { get; }

    public InheritedPropertyPropagator InheritedPropertyPropagator { get; }

    public IResourceProvider? ResourceProvider { get; private set; }

    public ResourceDependencyTracker ResourceDependencyTracker { get; }

    public IImageLoader? ImageLoader { get; private set; }

    public ImageResourceCache? ImageResourceCache { get; private set; }

    public UiFrameScheduler Scheduler { get; }

    public StyleSheet? StyleSheet { get; private set; }

    public ThemeProvider? ThemeProvider => themeProvider;

    public StyleProcessor StyleProcessor { get; }

    public void SetResourceProvider(IResourceProvider? provider)
    {
        if (ReferenceEquals(ResourceProvider, provider))
        {
            return;
        }

        if (observableResourceProvider is not null)
        {
            observableResourceProvider.ResourceChanged -= OnResourceChanged;
        }

        ResourceProvider = provider;
        observableResourceProvider = provider as IObservableResourceProvider;
        if (observableResourceProvider is not null)
        {
            observableResourceProvider.ResourceChanged += OnResourceChanged;
        }

        Invalidate(InvalidationFlags.Resource | InvalidationFlags.Subtree, "Root resource provider changed");
    }

    public void SetImageLoader(IImageLoader? loader)
    {
        if (ReferenceEquals(ImageLoader, loader))
        {
            return;
        }

        ImageResourceCache?.Clear();
        ImageLoader = loader;
        ImageResourceCache = loader is null ? null : new ImageResourceCache(loader);
        Invalidate(InvalidationFlags.Resource | InvalidationFlags.Render | InvalidationFlags.Subtree, "Root image loader changed");
    }

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

    private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
    {
        foreach (ResourceDependencyChange change in ResourceDependencyTracker.NotifyResourceChanged(args))
        {
            if ((change.Effects & (InvalidationFlags.Measure | InvalidationFlags.Arrange)) != InvalidationFlags.None)
            {
                change.Owner.IncrementLayoutVersion();
            }

            if (change.Effects.HasFlag(InvalidationFlags.Render))
            {
                change.Owner.IncrementRenderVersion();
            }

            change.Owner.Invalidate(new InvalidationRequest(
                change.Owner,
                InvalidationFlags.Resource,
                "Resource changed",
                resourceEffects: change.Effects,
                affectsIntrinsicSize: change.AffectsIntrinsicSize));
        }
    }

    internal void IncrementTreeVersion()
    {
        TreeVersion++;
        semanticsDirty = true;
        RetainedRenderCache.InvalidateRoot();
    }

    internal void ClearStyleScope(UIElement element)
    {
        styleApplicator.Clear(element);
    }

    public override void Invalidate(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Trace.RecordRequest(request);
        InvalidationFlags effective = DirtyPropagation.Default.GetEffectiveFlags(request);
        if (effective.HasFlag(InvalidationFlags.Semantics))
        {
            semanticsDirty = true;
        }

        if (effective.HasFlag(InvalidationFlags.Render))
        {
            RetainedRenderCache.InvalidateRoot();
        }

        if (effective.HasFlag(InvalidationFlags.HitTest))
        {
            InputCache.Invalidate(request.Reason);
        }

        DirtyPropagation.Default.Propagate(request, this, LayoutQueue, InheritedPropertyQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
    }

    public SemanticsTree GetSemanticsTree()
    {
        if (!semanticsDirty && cachedSemanticsTree is not null && cachedSemanticsTreeVersion == TreeVersion)
        {
            return cachedSemanticsTree;
        }

        cachedSemanticsTree = semanticsProvider.Build(this);
        cachedSemanticsTreeVersion = TreeVersion;
        semanticsDirty = false;
        return cachedSemanticsTree;
    }

    public FrameStats ProcessFrame(FramePhaseProcessors? processors = null, FrameBudget budget = default, FrameStats? stats = null)
    {
        FrameStats frameStats = stats ?? new FrameStats();
        activeFrameStats = frameStats;
        try
        {
            return Scheduler.ProcessFrame(processors ?? CreatePhaseProcessors(), budget, frameStats);
        }
        finally
        {
            activeFrameStats = null;
        }
    }

    internal void CountMeasureCall()
    {
        activeFrameStats?.CountMeasureCall();
    }

    internal void CountArrangeCall()
    {
        activeFrameStats?.CountArrangeCall();
    }

    private FramePhaseProcessors CreatePhaseProcessors()
    {
        FramePhaseProcessors layoutProcessors = LayoutManager.CreatePhaseProcessors();
        CommandRouter commandRouter = new();
        ElementInputRouteMap? commandStateRouteMap = null;
        return new FramePhaseProcessors
        {
            InheritedProperties = element => InheritedPropertyPropagator.PropagateFrom(element),
            CommandState = element =>
            {
                commandStateRouteMap ??= CreateCommandStateRouteMap();
                ProcessCommandState(element, commandRouter, commandStateRouteMap);
            },
            Style = StyleProcessor.Process,
            Measure = layoutProcessors.Measure,
            Arrange = layoutProcessors.Arrange,
            RenderCache = RenderQueueProcessor.Process,
            HitTest = _ => InputCache.EnsureCurrent(this)
        };
    }

    private ElementInputRouteMap CreateCommandStateRouteMap()
    {
        InputCache.EnsureCurrent(this);
        return new ElementInputRouteBuilder().BuildForCommandState(this);
    }

    private static void ProcessCommandState(UIElement element, CommandRouter router, ElementInputRouteMap routeMap)
    {
        if (element is not ICommandStateSource source)
        {
            return;
        }

        _ = source.RefreshCommandState(router, routeMap);
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
