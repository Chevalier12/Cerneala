using Cerneala.UI.Detective;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Aspect;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Platform;
using Cerneala.UI.Relay;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;

namespace Cerneala.UI.Elements;

public sealed partial class UIRoot : UIElement, IElementHost, IInvalidationSink
{
    private ThemeProvider? themeProvider;
    private ThemeChangedSubscription? themeChangedSubscription;
    private ResourceChangedSubscription? resourceChangedSubscription;
    private FrameStats? activeFrameStats;
    private readonly Action<UIElement> beginInheritedDescendantPropagation;
    private readonly SemanticsProvider semanticsProvider = new();
    private SemanticsTree? cachedSemanticsTree;
    private int cachedSemanticsTreeVersion = -1;
    private bool semanticsDirty = true;
    private SemanticsTree? cachedServoSemanticsTree;
    private int cachedServoSemanticsTreeVersion = -1;
    private bool servoSemanticsDirty = true;
    private readonly HashSet<UIElement> commandStateRefreshesAwaitingInputRoute = new(ReferenceEqualityComparer.Instance);
    private int commandStateInputRouteVersion;

    public UIRoot(
        float viewportWidth = 0,
        float viewportHeight = 0,
        float scale = 1,
        IMotionClock? motionClock = null,
        ReducedMotionPolicy? reducedMotion = null,
        UiRelayOptions? relayOptions = null)
        : this(
            viewportWidth,
            viewportHeight,
            scale,
            motionClock,
            reducedMotion,
            relayOptions,
            InvalidationTrace.Disabled)
    {
    }

    public UIRoot(
        InvalidationTrace invalidationTrace,
        float viewportWidth = 0,
        float viewportHeight = 0,
        float scale = 1,
        IMotionClock? motionClock = null,
        ReducedMotionPolicy? reducedMotion = null,
        UiRelayOptions? relayOptions = null)
        : this(
            viewportWidth,
            viewportHeight,
            scale,
            motionClock,
            reducedMotion,
            relayOptions,
            invalidationTrace ?? throw new ArgumentNullException(nameof(invalidationTrace)))
    {
    }

    private UIRoot(
        float viewportWidth,
        float viewportHeight,
        float scale,
        IMotionClock? motionClock,
        ReducedMotionPolicy? reducedMotion,
        UiRelayOptions? relayOptions,
        InvalidationTrace invalidationTrace)
    {
        Relay = new UiRelay(relayOptions);
        Detective = new Cerneala.UI.Detective.Detective(this, invalidationTrace);
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        Scale = scale;
        Trace = invalidationTrace;
        QueueOrderIndex = new ElementQueueOrderIndex(this);
        LayoutQueue = new LayoutQueue(this);
        InheritedPropertyQueue = new InheritedPropertyQueue(this);
        CommandStateQueue = new CommandStateQueue(this);
        AspectQueue = new AspectQueue(this);
        RenderQueue = new RenderQueue(this);
        HitTestQueue = new HitTestQueue(this);
        InputCache = new ElementInputCache();
        LayoutManager = new LayoutManager(this);
        OverlayManager = new OverlayManager(this);
        RenderCounters = new RenderCounters();
        RetainedRenderCache = new RetainedRenderCache();
        RenderQueueProcessor = new RenderQueueProcessor(RetainedRenderCache, RenderCounters);
        RetainedRenderer = new RetainedRenderer(RetainedRenderCache, new DrawCommandListBuilder(), RenderCounters);
        InheritedPropertyPropagator = new InheritedPropertyPropagator();
        ResourceDependencyTracker = new ResourceDependencyTracker();
        AspectRegistry = new AspectRegistry(Relay, InvalidateAspectRegistryChange);
        AspectRegistry.RegisterFrameworkDefault(DefaultAspectPackage.Create(), notify: false);
        AspectProcessor = new AspectProcessor(this);
        Scheduler = new UiFrameScheduler(LayoutQueue, InheritedPropertyQueue, CommandStateQueue, AspectQueue, RenderQueue, HitTestQueue, Trace);
        beginInheritedDescendantPropagation = Scheduler.BeginInheritedDescendantPropagation;
        Motion = new MotionSystem(this, motionClock ?? new SystemMotionClock(), reducedMotion ?? ReducedMotionPolicy.Default);
        PropertyMutations = new RootPropertyMutationObserver(this);
        IsLayoutBoundary = true;
        ElementLifecycle.AttachSubtree(this, this);

        // A new root is clean: it queues no frame work. A clean element's local render cache must be valid,
        // because composition reads it. An element added to a tree gets its first render work from the
        // collection that adds it; the root attaches itself, so it builds its own local cache here, through the
        // same processor as the frame. Otherwise Commit on an empty root throws, also after a frame (#955).
        RenderQueueProcessor.Process(this);
    }

    UIRoot IElementHost.Root => this;

    public float ViewportWidth { get; private set; }

    public float ViewportHeight { get; private set; }

    public new float Scale { get; private set; }

    public int TreeVersion { get; private set; }

    internal ElementQueueOrderIndex QueueOrderIndex { get; }

    internal int ViewportVersion { get; private set; }

    internal InvalidationTrace Trace { get; }

    public Cerneala.UI.Detective.Detective Detective { get; }

    public LayoutQueue LayoutQueue { get; }

    public InheritedPropertyQueue InheritedPropertyQueue { get; }

    public CommandStateQueue CommandStateQueue { get; }

    public AspectQueue AspectQueue { get; }

    public RenderQueue RenderQueue { get; }

    public HitTestQueue HitTestQueue { get; }

    public ElementInputCache InputCache { get; }

    public LayoutManager LayoutManager { get; }

    internal OverlayManager OverlayManager { get; }

    internal FocusManager? ActiveFocusManager { get; set; }

    internal RenderCounters RenderCounters { get; }

    public RetainedRenderCache RetainedRenderCache { get; }

    public RenderQueueProcessor RenderQueueProcessor { get; }

    public RetainedRenderer RetainedRenderer { get; }

    public InheritedPropertyPropagator InheritedPropertyPropagator { get; }

    public IResourceProvider? ResourceProvider { get; private set; }

    // Raised on the root's thread after SetResourceProvider replaced ResourceProvider, so attachments that subscribed to
    // the previous provider move to the new one and re-resolve (#838).
    internal event Action? ResourceProviderChanged;

    public ResourceDependencyTracker ResourceDependencyTracker { get; }

    public IPlatformServices PlatformServices { get; private set; } = Cerneala.UI.Platform.PlatformServices.Empty;

    public IImageLoader? ImageLoader { get; private set; }

    public ImageResourceCache? ImageResourceCache { get; private set; }

    private long imageResolutionEpoch = ImageResourceResolutionEpoch.Next();

    // See ImageResolutionStamp; advanced from any thread that changes an input.
    internal long ImageResolutionEpoch => Volatile.Read(ref imageResolutionEpoch);

    internal void AdvanceImageResolutionEpoch() =>
        Volatile.Write(ref imageResolutionEpoch, ImageResourceResolutionEpoch.Next());

    public UiFrameScheduler Scheduler { get; }

    public UiRelay Relay { get; }

    public MotionSystem Motion { get; }

    internal UiPropertyMutationObserver PropertyMutations { get; }

    internal PrismCacheInvalidationQueue PrismCacheInvalidations { get; } =
        new();

    public ThemeProvider? ThemeProvider => themeProvider;

    public AspectRegistry AspectRegistry { get; }

    public AspectProcessor AspectProcessor { get; }

    public void SetResourceProvider(IResourceProvider? provider)
    {
        Relay.VerifyAccess();
        if (ReferenceEquals(ResourceProvider, provider))
        {
            return;
        }

        resourceChangedSubscription?.Dispose();
        resourceChangedSubscription = null;

        ResourceProvider = provider;
        AdvanceImageResolutionEpoch();
        if (provider is IObservableResourceProvider observableProvider)
        {
            resourceChangedSubscription = new ResourceChangedSubscription(this, observableProvider);
        }

        Invalidate(InvalidationFlags.Resource | InvalidationFlags.Aspect | InvalidationFlags.Subtree, "Root resource provider changed");
        ResourceProviderChanged?.Invoke();
    }

    public void SetPlatformServices(IPlatformServices? services)
    {
        Relay.VerifyAccess();
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
        Relay.VerifyAccess();
        if (ReferenceEquals(ImageLoader, loader) && ReferenceEquals(ImageResourceCache, cache))
        {
            return;
        }

        if (!ReferenceEquals(ImageResourceCache, cache))
        {
            ReleaseDrawingResources();
        }

        ImageLoader = loader;
        ImageResourceCache = cache;
        AdvanceImageResolutionEpoch();
        Invalidate(InvalidationFlags.Resource | InvalidationFlags.Render | InvalidationFlags.Subtree, "Root image loader changed");
    }

    internal void ReleaseDrawingResources()
    {
        // A device cache can be shared by several windows. Release this root's
        // acquisitions, never the shared cache's identity mappings.
        foreach (UIElement element in ElementTreeWalker.PreOrderRenderability(this))
        {
            if (element is IRenderSurfaceResourceOwner surface) { surface.ReleaseDrawingResources(); }
            element.ReleaseImageResources();
            RetainedRenderCache.ReleaseElement(element);
        }
        RetainedRenderCache.ReleaseRootResources();
    }

    public void SetViewport(float width, float height, float scale)
    {
        Relay.VerifyAccess();
        if (ViewportWidth != width || ViewportHeight != height || Scale != scale)
        {
            ViewportVersion++;
        }

        ViewportWidth = width;
        ViewportHeight = height;
        Scale = scale;
        IncrementTreeVersion();
        OverlayManager.OnViewportChanged();
    }

    public void SetThemeProvider(ThemeProvider? provider)
    {
        Relay.VerifyAccess();
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

    private void ApplyResourceChange(ResourceChangedEventArgs args)
    {
        if (args.OldValue is AspectPackage || args.NewValue is AspectPackage)
        {
            Invalidate(InvalidationFlags.Aspect | InvalidationFlags.Subtree, "Application Aspect package changed");
        }

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
        servoSemanticsDirty = true;
        RetainedRenderCache.InvalidateRoot();
        OnCommandStateInputRouteChanged();
    }

    // A command source whose routed command could not be evaluated because the source was outside the command-state
    // input route (CommandSourceState.Refresh) is refreshed again after the next change that can rebuild that route:
    // the tree version (structure, overlay projection) or an input-route invalidation (Visibility, IsVisible,
    // virtualization, presence) (#901). A source that is still outside the route defers again.
    internal void DeferCommandStateRefreshUntilInputRouteChanges(UIElement source)
    {
        commandStateRefreshesAwaitingInputRoute.Add(source);
    }

    private void OnCommandStateInputRouteChanged()
    {
        commandStateInputRouteVersion++;
        if (commandStateRefreshesAwaitingInputRoute.Count == 0)
        {
            return;
        }

        UIElement[] sources = [.. commandStateRefreshesAwaitingInputRoute];
        commandStateRefreshesAwaitingInputRoute.Clear();
        foreach (UIElement source in sources)
        {
            source.QueueCommandStateRefresh();
        }
    }

    internal void RemovePendingWork(UIElement element)
    {
        LayoutQueue.RemoveMeasure(element);
        LayoutQueue.RemoveArrange(element);
        InheritedPropertyQueue.Remove(element);
        CommandStateQueue.Remove(element);
        commandStateRefreshesAwaitingInputRoute.Remove(element);
        AspectQueue.Remove(element);
        RenderQueue.Remove(element);
        HitTestQueue.Remove(element);
    }

    public override void Invalidate(InvalidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Relay.VerifyAccess();
        Trace.RecordRequest(request);
        InvalidationFlags effective = DirtyPropagation.Default.GetEffectiveFlags(request);
        if (effective.HasFlag(InvalidationFlags.Semantics))
        {
            semanticsDirty = true;
            servoSemanticsDirty = true;
        }

        if (effective.HasFlag(InvalidationFlags.Render))
        {
            RetainedRenderCache.InvalidateRoot();
        }

        if (effective.HasFlag(InvalidationFlags.HitTest) &&
            RequiresInputRouteRebuild(request))
        {
            InputCache.Invalidate(request.Reason);
            OnCommandStateInputRouteChanged();
        }

        DirtyPropagation.Default.Propagate(request, this, LayoutQueue, InheritedPropertyQueue, AspectQueue, RenderQueue, HitTestQueue, Trace);
    }

    private static bool RequiresInputRouteRebuild(InvalidationRequest request) =>
        ReferenceEquals(request.SourceProperty, UIElement.IsEnabledProperty) ||
        ReferenceEquals(request.SourceProperty, UIElement.IsVisibleProperty) ||
        ReferenceEquals(request.SourceProperty, UIElement.VisibilityProperty) ||
        ReferenceEquals(request.SourceProperty, Cerneala.UI.Layout.Panels.Panel.ZIndexProperty) ||
        string.Equals(request.Reason, "Presence state changed", StringComparison.Ordinal) ||
        string.Equals(request.Reason, "Virtualization realization changed", StringComparison.Ordinal) ||
        string.Equals(request.Reason, "Input handler added", StringComparison.Ordinal) ||
        string.Equals(request.Reason, "Input handler removed", StringComparison.Ordinal);

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

    internal int ServoSemanticsBuildCount { get; private set; }

    // The Servo projection also includes scene input subtrees, whose presence depends on
    // layout and scene presentation readiness. Those change only through frame work and
    // raise no semantics invalidation, so ProcessFrameCore also discards this tree.
    internal SemanticsTree GetServoSemanticsTree()
    {
        Relay.VerifyAccess();
        if (!servoSemanticsDirty && cachedServoSemanticsTree is not null && cachedServoSemanticsTreeVersion == TreeVersion)
        {
            return cachedServoSemanticsTree;
        }

        cachedServoSemanticsTree = semanticsProvider.Build(this, SemanticsProjection.Servo);
        cachedServoSemanticsTreeVersion = TreeVersion;
        servoSemanticsDirty = false;
        ServoSemanticsBuildCount++;
        return cachedServoSemanticsTree;
    }

    public FrameStats ProcessFrame(
        FramePhaseProcessors? processors = null,
        FrameBudget budget = default,
        FrameStats? stats = null,
        MotionFrameReason motionReason = MotionFrameReason.Scheduled)
    {
        FrameStats frameStats = stats ?? new FrameStats();
        using UpdateScope updateScope = BeginUpdate(frameStats);
        if (!Scheduler.HasWork && !Motion.HasActiveMotion && frameStats.HasWork)
        {
            return frameStats;
        }

        return ProcessFrameCore(processors, budget, frameStats, motionReason);
    }

    internal UpdateScope BeginUpdate(FrameStats stats)
    {
        ArgumentNullException.ThrowIfNull(stats);
        Relay.VerifyAccess();
        UiRelaySynchronizationContext.Scope contextScope = Relay.EnterSynchronizationContext();
        try
        {
            UiRelayDrainResult relayResult = Relay.Drain(out AggregateException? postException);
            stats.CountRelay(relayResult);
            if (postException is not null)
            {
                throw postException;
            }

            return new UpdateScope(contextScope);
        }
        catch
        {
            contextScope.Dispose();
            throw;
        }
    }

    internal FrameStats ProcessFrameCore(
        FramePhaseProcessors? processors,
        FrameBudget budget,
        FrameStats frameStats,
        MotionFrameReason motionReason)
    {
        activeFrameStats = frameStats;
        try
        {
            MotionFrameCoordinator? motion = Scheduler.HasWork || Motion.HasActiveMotion ? Motion.Frames : null;
            return Scheduler.ProcessFrame(processors ?? CreatePhaseProcessors(), budget, frameStats, motion, motionReason);
        }
        finally
        {
            activeFrameStats = null;
            servoSemanticsDirty = true;
        }
    }

    internal readonly struct UpdateScope : IDisposable
    {
        private readonly UiRelaySynchronizationContext.Scope contextScope;

        internal UpdateScope(UiRelaySynchronizationContext.Scope contextScope)
        {
            this.contextScope = contextScope;
        }

        public void Dispose()
        {
            contextScope.Dispose();
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

    // Runs the frame's Inherited and Aspect work for a subtree that layout creates under parent, so the subtree is
    // complete before it is measured in the same frame: the panel and rows of an ItemsPresenter, and a template that a
    // control builds in its measure (#714). The frame's own phases ran before the measure, so they do not reach it.
    // Adding the subtree queued parent for that work, which this removes from the queue. So the propagation starts
    // at parent, whose visual child is the subtree, and also writes the subtree root's own inherited values.
    // Like the frame's Aspect phase, only elements with queued Aspect work are processed, for example the rows
    // that a refresh added; a row whose Aspect inputs did not change keeps its result.
    // An apply can change the tree, for example a property-change callback or a package behavior factory that detaches
    // its element, so the walk does not enumerate the live child lists. Like a live pre-order walk, it reads an
    // element's children after that element's apply, so the subtree that the apply creates, for example from a content
    // template, is processed before it is measured. An element that an earlier apply detached is skipped, and an
    // element's work is taken off the queue before its apply, like in the frame's Aspect phase, so the work that a
    // reattach during the apply queues stays queued for the next pass.
    internal void ProcessInheritedAndAspectForSubtree(UIElement parent, UIElement subtree)
    {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(subtree);
        if (!ReferenceEquals(subtree.Root, this))
        {
            return;
        }

        InheritedPropertyPropagator.PropagateFrom(parent);
        Stack<UIElement> pending = new();
        pending.Push(subtree);
        while (pending.Count > 0)
        {
            UIElement current = pending.Pop();
            if (!ReferenceEquals(current.Root, this))
            {
                continue;
            }

            ProcessQueuedAspect(current);
            IReadOnlyList<UIElement> children = current.VisualChildren;
            for (int index = children.Count - 1; index >= 0; index--)
            {
                pending.Push(children[index]);
            }
        }

        InheritedPropertyPropagator.PropagateFrom(parent);
        InheritedPropertyQueue.Remove(parent);
        parent.DirtyState.Clear(InvalidationFlags.Inherited);
        foreach (UIElement current in ElementTreeWalker.PreOrder(subtree, ElementChildRole.Visual))
        {
            InheritedPropertyQueue.Remove(current);
            current.DirtyState.Clear(InvalidationFlags.Inherited);
        }
    }

    private void ProcessQueuedAspect(UIElement element)
    {
        if (!element.DirtyState.Has(InvalidationFlags.Aspect) && !AspectQueue.Contains(element))
        {
            return;
        }

        AspectQueue.Remove(element);
        element.DirtyState.Clear(InvalidationFlags.Aspect);
        try
        {
            AspectProcessor.Process(element);
        }
        catch
        {
            element.DirtyState.Mark(InvalidationFlags.Aspect);
            AspectQueue.Enqueue(element);
            throw;
        }
    }

    private FramePhaseProcessors CreatePhaseProcessors()
    {
        FramePhaseProcessors layoutProcessors = LayoutManager.CreatePhaseProcessors();
        CommandRouter commandRouter = new();
        ElementInputRouteMap? commandStateRouteMap = null;
        int commandStateRouteMapVersion = 0;
        return new FramePhaseProcessors
        {
            InheritedProperties = element => InheritedPropertyPropagator.PropagateFrom(element, beginInheritedDescendantPropagation),
            CommandState = element =>
            {
                if (commandStateRouteMap is null)
                {
                    commandStateRouteMap = CreateCommandStateRouteMap();
                    commandStateRouteMapVersion = commandStateInputRouteVersion;
                }

                ProcessCommandState(element, commandRouter, commandStateRouteMap, commandStateRouteMapVersion);
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

    private void ProcessCommandState(UIElement element, CommandRouter router, ElementInputRouteMap routeMap, int routeMapVersion)
    {
        if (element is not ICommandStateSource source)
        {
            return;
        }

        // This refresh replaces an earlier deferral; it defers again if the source is still outside the route.
        commandStateRefreshesAwaitingInputRoute.Remove(element);
        _ = source.RefreshCommandState(router, routeMap);

        // The route changed after this frame's map was built, for example in a handler of an earlier refresh, so the
        // wake-up for that change has already run. A source that deferred against the old map refreshes in the next
        // frame, against a new map.
        if (routeMapVersion != commandStateInputRouteVersion && commandStateRefreshesAwaitingInputRoute.Remove(element))
        {
            element.QueueCommandStateRefresh();
        }
    }

}
