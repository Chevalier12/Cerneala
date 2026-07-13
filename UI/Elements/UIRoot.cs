using Cerneala.UI.Diagnostics;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Aspect;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Elements;

public sealed class UIRoot : UIElement, IElementHost, IInvalidationSink
{
    private ThemeProvider? themeProvider;
    private ThemeChangedSubscription? themeChangedSubscription;
    private IObservableResourceProvider? observableResourceProvider;
    private FrameStats? activeFrameStats;
    private readonly SemanticsProvider semanticsProvider = new();
    private SemanticsTree? cachedSemanticsTree;
    private int cachedSemanticsTreeVersion = -1;
    private bool semanticsDirty = true;

    public UIRoot(
        float viewportWidth = 0,
        float viewportHeight = 0,
        float scale = 1,
        IMotionClock? motionClock = null,
        ReducedMotionPolicy? reducedMotion = null)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Scale = scale;
        ElementIds = new ElementIdProvider();
        Trace = new InvalidationTrace();
        LayoutQueue = new LayoutQueue(this);
        InheritedPropertyQueue = new InheritedPropertyQueue(this);
        CommandStateQueue = new CommandStateQueue(this);
        AspectQueue = new AspectQueue(this);
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
        AspectRegistry = new AspectRegistry(InvalidateAspectRegistryChange);
        AspectRegistry.Register(DefaultAspectPackage.Create(), notify: false);
        AspectProcessor = new AspectProcessor(this);
        Scheduler = new UiFrameScheduler(LayoutQueue, InheritedPropertyQueue, CommandStateQueue, AspectQueue, RenderQueue, HitTestQueue, Trace);
        Motion = new MotionSystem(this, motionClock ?? new SystemMotionClock(), reducedMotion ?? ReducedMotionPolicy.Default);
        IsLayoutBoundary = true;
        ElementLifecycle.AttachSubtree(this, this);
    }

    UIRoot IElementHost.Root => this;

    public float ViewportWidth { get; private set; }

    public float ViewportHeight { get; private set; }

    public new float Scale { get; private set; }

    public int TreeVersion { get; private set; }

    internal int ViewportVersion { get; private set; }

    public ElementIdProvider ElementIds { get; }

    public InvalidationTrace Trace { get; }

    public LayoutQueue LayoutQueue { get; }

    public InheritedPropertyQueue InheritedPropertyQueue { get; }

    public CommandStateQueue CommandStateQueue { get; }

    public AspectQueue AspectQueue { get; }

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

    public IPlatformServices PlatformServices { get; private set; } = Cerneala.UI.Platform.PlatformServices.Empty;

    public IImageLoader? ImageLoader { get; private set; }

    public ImageResourceCache? ImageResourceCache { get; private set; }

    public UiFrameScheduler Scheduler { get; }

    public MotionSystem Motion { get; }

    public ThemeProvider? ThemeProvider => themeProvider;

    public AspectRegistry AspectRegistry { get; }

    public AspectProcessor AspectProcessor { get; }

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

    public void SetPlatformServices(IPlatformServices? services)
    {
        PlatformServices = services ?? Cerneala.UI.Platform.PlatformServices.Empty;
        if (PlatformServices.ReducedMotion is not null)
        {
            Motion.ReducedMotion.SetMode(PlatformServices.ReducedMotion.Mode);
        }
    }

    public void SetImageLoader(IImageLoader? loader)
    {
        SetImageResourceCache(loader, loader is null ? null : new ImageResourceCache(loader));
    }

    public void SetImageResourceCache(IImageLoader? loader, ImageResourceCache? cache)
    {
        if (ReferenceEquals(ImageLoader, loader) && ReferenceEquals(ImageResourceCache, cache))
        {
            return;
        }

        if (!ReferenceEquals(ImageResourceCache, cache))
        {
            ImageResourceCache?.Clear();
        }

        ImageLoader = loader;
        ImageResourceCache = cache;
        Invalidate(InvalidationFlags.Resource | InvalidationFlags.Render | InvalidationFlags.Subtree, "Root image loader changed");
    }

    public void SetViewport(float width, float height, float scale)
    {
        if (ViewportWidth != width || ViewportHeight != height || Scale != scale)
        {
            ViewportVersion++;
        }

        ViewportWidth = width;
        ViewportHeight = height;
        Scale = scale;
        IncrementTreeVersion();
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

        Invalidate(InvalidationFlags.Aspect | InvalidationFlags.Subtree, "Theme provider changed");
    }

    private void InvalidateThemeChange()
    {
        Invalidate(InvalidationFlags.Aspect | InvalidationFlags.Subtree, "Theme changed");
    }

    private void InvalidateAspectRegistryChange()
    {
        Invalidate(InvalidationFlags.Aspect | InvalidationFlags.Subtree, "Aspect registry changed");
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

        DirtyPropagation.Default.Propagate(request, this, LayoutQueue, InheritedPropertyQueue, AspectQueue, RenderQueue, HitTestQueue, Trace);
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

    public FrameStats ProcessFrame(
        FramePhaseProcessors? processors = null,
        FrameBudget budget = default,
        FrameStats? stats = null,
        MotionFrameReason motionReason = MotionFrameReason.Scheduled)
    {
        FrameStats frameStats = stats ?? new FrameStats();
        activeFrameStats = frameStats;
        try
        {
            MotionFrameCoordinator? motion = Scheduler.HasWork || Motion.HasActiveMotion ? Motion.Frames : null;
            return Scheduler.ProcessFrame(processors ?? CreatePhaseProcessors(), budget, frameStats, motion, motionReason);
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
            Aspect = AspectProcessor.Process,
            Measure = layoutProcessors.Measure,
            IncrementalMeasure = layoutProcessors.IncrementalMeasure,
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
