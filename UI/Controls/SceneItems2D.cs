using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed class SceneItems2D : SceneNode2D, ISceneSpatialParticipant2D
{
    public static readonly UiProperty<ISceneSpatialSource2D<object>?> ItemsSourceProperty =
        UiProperty<ISceneSpatialSource2D<object>?>.Register(
            nameof(ItemsSource), typeof(SceneItems2D),
            new UiPropertyMetadata<ISceneSpatialSource2D<object>?>(null, UiPropertyOptions.AffectsRender));

    private readonly List<RealizedItem> realized = [];
    private readonly Dictionary<string, RealizedItem> byId = new(StringComparer.Ordinal);
    private ContentTemplateRegistry templateRegistry = new();
    private ISceneSpatialSource2D<object>? observedSource;
    private SceneSpatialResidency2D<object>? residency;
    private IReadOnlyList<SceneSpatialEntry2D>? appliedCatalog;
    private IReadOnlyList<SceneSpatialEntry2D>? requestedCatalog;
    private IReadOnlyList<SceneSpatialEntry2D>? boundsCatalog;
    private SceneBounds2D catalogBounds;
    private SceneBounds2D requestedBounds;
    private bool requestedPresentationVisible;
    private IReadOnlyList<SceneBounds2D>? requestedCollisionInterest;
    private Request? pending;
    private long templateVersion;
    private long requestVersion;

    public SceneItems2D() => Templates = new TemplateCollection(RebuildTemplates, VerifyOwnerAccess);

    public ISceneSpatialSource2D<object>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Collection<ContentTemplate> Templates { get; }
    public int RealizedItemCount => realized.Count;
    public Task Preparation { get; private set; } = Task.CompletedTask;
    public Exception? PreparationError { get; private set; }

    SceneNode2D ISceneSpatialParticipant2D.Node => this;
    IReadOnlyList<SceneSpatialEntry2D>? ISceneSpatialParticipant2D.SimulationCatalog => ItemsSource?.Entries;
    void ISceneSpatialParticipant2D.UpdateSpatialInterest(SceneBounds2D visibleBounds, IReadOnlyList<SceneBounds2D> collisionInterest,
        DrawRect? surfaceBounds) => UpdateSpatialInterest(visibleBounds, collisionInterest, surfaceBounds);
    string? ISceneSpatialParticipant2D.GetUnpreparedCollisionEntry(DrawRect sceneBounds) => GetUnpreparedCollisionEntry(sceneBounds);

    public bool TryGetRealizedNode(string id, [NotNullWhen(true)] out SceneNode2D? node)
    {
        ArgumentNullException.ThrowIfNull(id);
        VerifyOwnerAccess();
        node = byId.TryGetValue(id, out RealizedItem? item) ? item.Node : null;
        return node is not null;
    }

    // Explicit retry, not an unbounded automatic I/O retry loop.
    public void Refresh()
    {
        VerifyOwnerAccess();
        requestedCatalog = null;
        SimulationContext?.RefreshSpatialItems();
    }

    internal SceneItems2DUpdateCounters UpdateCounters { get; } = new();

    protected override void OnAttached()
    {
        base.OnAttached();
        SimulationContext?.RefreshSpatialItems();
    }

    internal override void OnSimulationContextChanged(SceneSimulationContext2D? previous)
    {
        StopSource();
        StartSource();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (!ReferenceEquals(args.Property, ItemsSourceProperty)) { return; }
        StopSource();
        StartSource();
        Refresh();
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        if (ReferenceEquals(Surface, surface)) { return; }
        base.AttachSurface(surface);
        foreach (RealizedItem item in realized) { item.Node.AttachSurface(surface); }
        SimulationContext?.RefreshSpatialItems();
    }

    internal void UpdateSpatialInterest(SceneBounds2D visibleBounds, IReadOnlyList<SceneBounds2D> collisionInterest,
        DrawRect? surfaceBounds = null)
    {
        if (SimulationContext is not { IsDisposed: false } context || residency is null || observedSource is null) { return; }
        context.Relay.VerifyAccess();
        // Presentation uses the shared Prism input region; simulation and
        // collision interest remain independent of its camera footprint.
        if (!context.IsHeadless) { visibleBounds = SceneSpatialInterest2D.ResolveInputBounds(this, visibleBounds, includeSelf: false,
            surfaceBounds: surfaceBounds); }
        bool presentationVisible = !context.IsHeadless && IsPresentationVisible();
        IReadOnlyList<SceneSpatialEntry2D> catalog = observedSource.Entries;
        if (ReferenceEquals(requestedCatalog, catalog) && requestedBounds == visibleBounds &&
            requestedPresentationVisible == presentationVisible &&
            ReferenceEquals(requestedCollisionInterest, collisionInterest)) { return; }
        requestedCatalog = catalog;
        requestedBounds = visibleBounds;
        requestedPresentationVisible = presentationVisible;
        requestedCollisionInterest = collisionInterest;
        PreparationError = null;
        ISceneSpatialSource2D<object> source = observedSource;
        if (!ReferenceEquals(appliedCatalog, catalog)) { RetireObsoleteCatalogEntries(catalog); }
        RetireUnusedPresentation(visibleBounds, presentationVisible);
        // Release callbacks may publish another catalog or replace the source.
        if (!ReferenceEquals(observedSource, source) || !ReferenceEquals(source.Entries, catalog) ||
            !ReferenceEquals(SimulationContext, context) || context.IsDisposed || residency is null) { return; }

        Request? previous = pending;
        Request request = new(context, templateVersion, ++requestVersion);
        pending = request;
        // Acquire replacement interests before cancelling the previous request.
        ValueTask<SceneSpatialRegion2D<object>> acquisition;
        try
        {
            List<SceneBounds2D> localInterests = new(collisionInterest.Count);
            var transform = SceneGeometry2D.GetLocalToSceneTransform(this);
            foreach (SceneBounds2D interest in collisionInterest)
            {
                localInterests.Add(interest.Kind == SceneBoundsKind.Known &&
                    SceneGeometry2D.TryTransformBoundsToLocal(interest.Bounds, transform, out DrawRect local)
                        ? SceneBounds2D.Known(local) : interest.Kind == SceneBoundsKind.Empty ? interest : SceneBounds2D.Unknown);
            }
            acquisition = residency.AcquireSceneAsync(visibleBounds, localInterests, request.Token);
        }
        catch (Exception failure)
        {
            acquisition = ValueTask.FromException<SceneSpatialRegion2D<object>>(failure);
        }
        previous?.Cancel();
        Task preparation = CompleteRequestAsync(request, acquisition);
        // A synchronous application callback can replace the source and start a
        // newer preparation before this call returns.
        if (request.Version == requestVersion) { Preparation = preparation; }
        _ = ObserveAsync(preparation);
    }

    private async Task CompleteRequestAsync(Request request, ValueTask<SceneSpatialRegion2D<object>> acquisition)
    {
        SceneSpatialRegion2D<object>? acquired = null;
        CancellationToken token = request.Token;
        try
        {
            acquired = await acquisition.ConfigureAwait(false);
            if (request.Context.Relay.CheckAccess()) { Publish(); }
            else { await request.Context.Relay.InvokeAsync(Publish, token).ConfigureAwait(false); }

            void Publish()
            {
                token.ThrowIfCancellationRequested();
                if (!ReferenceEquals(pending, request) || !ReferenceEquals(SimulationContext, request.Context) || request.Context.IsDisposed) { return; }
                if (!acquired.IsCurrent) { requestedCatalog = null; return; }
                Commit(acquired, request);
            }
        }
        catch (Exception failure)
        {
            if (!token.IsCancellationRequested)
            {
                if (request.Context.Relay.CheckAccess()) { Report(); }
                else { await request.Context.Relay.InvokeAsync(Report, token).ConfigureAwait(false); }
                void Report()
                {
                    if (!ReferenceEquals(pending, request) || !ReferenceEquals(SimulationContext, request.Context) || request.Context.IsDisposed) { return; }
                    PreparationError = failure;
                    Surface?.InvalidateFrame();
                }
            }
            throw;
        }
        finally
        {
            try { acquired?.Dispose(); }
            // Retirement must finish even when a disposed headless owner will
            // never drain its Relay again. The request does not mutate the tree.
            finally { request.Complete(); }
        }
    }

    private static async Task ObserveAsync(Task task)
    {
        // The application can inspect/await Preparation; automatic preparation
        // must not leave unobserved fire-and-forget task failures.
        try { await task.ConfigureAwait(false); }
        catch { }
    }

    private void Commit(SceneSpatialRegion2D<object> prepared, Request request)
    {
        long version = request.TemplateVersion;
        List<RealizedItem> next = new(prepared.Entries.Count);
        List<RealizedItem> created = [];
        List<RealizedItem> removed = [];
        HashSet<SceneNode2D> nodes = new(ReferenceEqualityComparer.Instance);
        try
        {
            foreach (SceneSpatialEntry2D entry in prepared.Entries)
            {
                RealizedItem item;
                if (byId.TryGetValue(entry.Id, out RealizedItem? existing) &&
                    existing.Entry.Version == entry.Version && existing.TemplateVersion == version)
                {
                    item = existing;
                }
                else
                {
                    SceneNode2D node = CreateNode(prepared.GetValue(entry.Id));
                    if (!IsCurrent()) { return; }
                    ContentControl.ValidateCanOwnChild(this, node);
                    item = new(entry, node, version, prepared.RetainValue(entry.Id));
                    created.Add(item);
                }
                if (!nodes.Add(item.Node))
                {
                    throw new InvalidOperationException("Distinct spatial identities must create distinct scene nodes.");
                }
                next.Add(item);
            }

            if (!IsCurrent()) { return; }

            Scene2D? scene = SceneGeometry2D.FindRootScene(this);
            for (int index = realized.Count - 1; index >= 0; index--)
            {
                if (!nodes.Contains(realized[index].Node)) { removed.Add(RemoveAt(index)); }
            }
            for (int index = 0; index < next.Count; index++)
            {
                RealizedItem item = next[index];
                item.Entry = prepared.Entries[index];
                int currentIndex = realized.IndexOf(item);
                if (currentIndex < 0)
                {
                    LogicalChildren.Insert(index, item.Node);
                    realized.Insert(index, item);
                    item.Node.AttachSurface(Surface);
                    if (item.Node is Scene2D nested) { nested.ResetOwnedCollisionWorlds(); }
                    if (item.Node.IsAttached) { UpdateCounters.CountAttached(); }
                }
                else if (currentIndex != index)
                {
                    LogicalChildren.Move(currentIndex, index);
                    realized.RemoveAt(currentIndex);
                    realized.Insert(index, item);
                    UpdateCounters.CountMoved();
                }
            }
            byId.Clear();
            foreach (RealizedItem item in realized) { byId.Add(item.Entry.Id, item); }
            scene?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
            Surface?.InvalidateFrame();
        }
        finally
        {
            // New template acquisitions not adopted by the tree must be retired
            // even when a later factory fails or reenters source publication.
            ReleasePayloads(removed.Concat(created.Where(item => !realized.Contains(item))));
        }

        bool IsCurrent() => ReferenceEquals(pending, request) &&
            ReferenceEquals(SimulationContext, request.Context) && !request.Context.IsDisposed &&
            prepared.IsCurrent && templateVersion == version;
    }

    private void RetireObsoleteCatalogEntries(IReadOnlyList<SceneSpatialEntry2D> catalog)
    {
        Dictionary<string, SceneSpatialEntry2D> current = catalog.ToDictionary(static entry => entry.Id, StringComparer.Ordinal);
        List<RealizedItem> removed = [];
        try
        {
            for (int index = realized.Count - 1; index >= 0; index--)
            {
                RealizedItem item = realized[index];
                if (current.TryGetValue(item.Entry.Id, out SceneSpatialEntry2D? entry) && entry.Version == item.Entry.Version)
                {
                    item.Entry = entry;
                }
                else { removed.Add(RemoveAt(index)); }
            }
            appliedCatalog = catalog;
            if (removed.Count == 0) { return; }
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
            Surface?.InvalidateFrame();
        }
        finally { ReleasePayloads(removed); }
    }

    internal override void Record(Scene2DRecordContext context)
    {
        bool visible = UIElementVisibility.ParticipatesInRendering(this);
        SceneBounds2D input = SceneSpatialInterest2D.ResolveInputBounds(this,
            context.GetConservativeVisibleLocalBounds(), includeSelf: false, surfaceBounds: context.Frame.Bounds);
        for (int index = 0; index < realized.Count; index++)
        {
            RealizedItem item = realized[index];
            if (visible && ScenePresentationContext2D.Intersects(input, item.Entry.Bounds))
            {
                item.Node.Record(context);
            }
            else { item.Node.ReleaseRenderCaches(); }
        }
    }

    internal override void CheckPresentation(ScenePresentationContext2D context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0 || ItemsSource is not { } source) { return; }
        SceneBounds2D visible = context.GetVisibleBounds(this);
        IReadOnlyList<SceneSpatialEntry2D> catalog = source.Entries;
        if (!ReferenceEquals(appliedCatalog, catalog))
        {
            // Do not present old positions/deletions while a worker publication
            // is waiting for the UI relay, even if the new catalog is empty.
            foreach (RealizedItem item in realized)
            {
                if (ScenePresentationContext2D.Intersects(visible, item.Entry.Bounds)) { context.Require(); break; }
            }
        }
        foreach (SceneSpatialEntry2D entry in catalog)
        {
            if (!ScenePresentationContext2D.Intersects(visible, entry.Bounds)) { continue; }
            if (!ReferenceEquals(source, observedSource) || !byId.TryGetValue(entry.Id, out RealizedItem? item) ||
                item.Entry.Version != entry.Version || item.TemplateVersion != templateVersion || !item.Node.IsAttached)
            {
                context.Require(ReferenceEquals(source, observedSource) && ReferenceEquals(requestedCatalog, catalog)
                    ? PreparationError : null);
            }
            else { item.Node.CheckPresentation(context); }
        }
    }

    internal string? GetUnpreparedCollisionEntry(DrawRect sceneBounds)
    {
        ISceneSpatialSource2D<object>? source = ItemsSource;
        if (source is null || !UIElementVisibility.IsEffectivelyVisible(this)) { return null; }
        bool invertible = SceneGeometry2D.TryTransformBoundsToLocal(sceneBounds,
            SceneGeometry2D.GetLocalToSceneTransform(this), out DrawRect localBounds);
        IReadOnlyList<SceneSpatialEntry2D> catalog = source.Entries;
        // A worker can publish before its UI notification is drained. Reject
        // affected stale geometry without loading or mutating the tree here.
        if (!ReferenceEquals(appliedCatalog, catalog))
        {
            foreach (RealizedItem item in realized)
            {
                if (Intersects(item.Entry)) { return item.Entry.Id; }
            }
        }
        foreach (SceneSpatialEntry2D entry in catalog)
        {
            if (!Intersects(entry)) { continue; }
            if (!ReferenceEquals(source, observedSource) || !byId.TryGetValue(entry.Id, out RealizedItem? item) ||
                item.Entry.Version != entry.Version || item.TemplateVersion != templateVersion ||
                SimulationContext is null || !ReferenceEquals(item.Node.SimulationContext, SimulationContext))
            {
                return entry.Id;
            }
        }
        return null;

        bool Intersects(SceneSpatialEntry2D entry) => entry.CollisionBounds is DrawRect collision &&
            (!invertible || localBounds.X <= collision.Right && localBounds.Right >= collision.X &&
                localBounds.Y <= collision.Bottom && localBounds.Bottom >= collision.Y);
    }

    internal IReadOnlyList<SceneNode2D> GetInputCandidates(DrawPoint point, IReadOnlySet<SceneNode2D>? colliderPaths)
    {
        List<SceneNode2D> candidates = [];
        foreach (RealizedItem item in realized)
        {
            if (!item.Node.ParticipatesInInputRoute) { continue; }
            DrawRect bounds = item.Entry.Bounds;
            // Metadata is a visual broadphase, not an exact hit shape. Actual
            // collider hits retain their route even outside the visual envelope.
            if (colliderPaths?.Contains(item.Node) == true ||
                point.X >= bounds.X && point.X <= bounds.Right && point.Y >= bounds.Y && point.Y <= bounds.Bottom)
            {
                candidates.Add(item.Node);
            }
        }
        return candidates;
    }

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        IReadOnlyList<SceneSpatialEntry2D>? catalog = ItemsSource?.Entries;
        if (ReferenceEquals(boundsCatalog, catalog)) { return catalogBounds; }
        SceneBounds2D bounds = SceneBounds2D.Empty;
        if (catalog is not null)
        {
            for (int index = 0; index < catalog.Count; index++)
            {
                bounds = SceneGeometry2D.Union(bounds, SceneBounds2D.Known(catalog[index].Bounds));
            }
        }
        // Logical geometry (Prism capture coordinates and painter ordering) must
        // not change when a payload is acquired or retired for camera residency.
        boundsCatalog = catalog;
        return catalogBounds = bounds;
    }

    private bool IsPresentationVisible()
    {
        for (UIElement? node = this; node is SceneNode2D; node = node.LogicalParent)
        {
            if (!UIElementVisibility.ParticipatesInRendering(node) || node.Opacity <= 0) { return false; }
        }
        return true;
    }

    private void RetireUnusedPresentation(SceneBounds2D visibleBounds, bool visible)
    {
        // Residency retirement belongs to interest updates, not successful
        // recording. An unrelated pending source can withhold the whole scene.
        List<Exception>? failures = null;
        foreach (RealizedItem item in realized.ToArray())
        {
            if (visible && ScenePresentationContext2D.Intersects(visibleBounds, item.Entry.Bounds)) { continue; }
            try { item.Node.ReleaseRenderCaches(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private SceneNode2D CreateNode(object item)
    {
        // Index intentionally stays -1. Spatial identity is not a list position.
        if (templateRegistry.TryResolve(new ContentTemplateMatchContext(item, owner: this), out ContentTemplate template))
        {
            SceneNode2D created = template.Create(new ContentTemplateContext(item, owner: this)) as SceneNode2D
                ?? throw new InvalidOperationException($"Content template '{template.Name}' must create a {nameof(SceneNode2D)}.");
            UpdateCounters.CountCreated();
            return created;
        }
        return item as SceneNode2D ?? throw new InvalidOperationException(
            $"No content template matches item type '{item.GetType().FullName}' in {nameof(SceneItems2D)}.");
    }

    private void RebuildTemplates()
    {
        templateRegistry = new ContentTemplateRegistry();
        foreach (ContentTemplate template in Templates) { templateRegistry.Register(template); }
        templateVersion++;
        Refresh();
    }

    private void StartSource()
    {
        if (SimulationContext is not { IsDisposed: false } || observedSource is not null || ItemsSource is null) { return; }
        observedSource = ItemsSource;
        residency = new(observedSource);
        observedSource.Changed += OnSourceChanged;
    }

    private void OnSourceChanged(object? sender, EventArgs args)
    {
        SceneSimulationContext2D? context = SimulationContext;
        if (context is null || context.IsDisposed) { return; }
        if (context.Relay.CheckAccess())
        {
            if (ReferenceEquals(sender, observedSource) && ReferenceEquals(SimulationContext, context) && !context.IsDisposed) { Refresh(); }
            return;
        }
        if (sender is not ISceneSpatialSource2D<object> source) { return; }
        // Preserve Post's owner-visible failure contract without a queued
        // callback retaining a retired scene, source or independent context.
        WeakReference<SceneItems2D> target = new(this);
        WeakReference<SceneSimulationContext2D> lifetime = new(context);
        WeakReference<ISceneSpatialSource2D<object>> publisher = new(source);
        context.Relay.Post(() =>
        {
            if (target.TryGetTarget(out SceneItems2D? items) &&
                lifetime.TryGetTarget(out SceneSimulationContext2D? owner) && !owner.IsDisposed &&
                ReferenceEquals(items.SimulationContext, owner) && publisher.TryGetTarget(out var current) &&
                ReferenceEquals(items.observedSource, current)) { items.Refresh(); }
        });
    }

    private void StopSource()
    {
        if (observedSource is not null) { observedSource.Changed -= OnSourceChanged; }
        observedSource = null;
        appliedCatalog = null;
        requestedCatalog = null;
        boundsCatalog = null;
        catalogBounds = SceneBounds2D.Empty;
        Request? previousRequest = pending;
        pending = null;
        SceneSpatialResidency2D<object>? previous = residency;
        residency = null;
        Scene2D? scene = SceneGeometry2D.FindRootScene(this);
        // Freeze the outgoing ownership before invoking application cleanup.
        // Reentrant publication must not cause us to retire a newer source.
        RealizedItem[] removed = realized.ToArray();
        List<Exception>? failures = null;
        try { previousRequest?.Cancel(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        for (int index = removed.Length - 1; index >= 0; index--)
        {
            int current = realized.IndexOf(removed[index]);
            if (current < 0) { continue; }
            try { RemoveAt(current); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        try { scene?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { ReleasePayloads(removed); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { previous?.Dispose(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        Surface?.InvalidateFrame();
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private RealizedItem RemoveAt(int index)
    {
        RealizedItem item = realized[index];
        SceneNode2D node = item.Node;
        realized.RemoveAt(index);
        byId.Remove(item.Entry.Id);
        List<Exception>? failures = null;
        try { node.ReleaseRenderCaches(); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { node.AttachSurface(null); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { LogicalChildren.Remove(node); }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        try { if (node is Scene2D nested) { nested.ResetOwnedCollisionWorlds(); } }
        catch (Exception failure) { (failures ??= []).Add(failure); }
        UpdateCounters.CountRemoved();
        if (failures is not null)
        {
            // The caller cannot adopt a return value when cleanup throws.
            // Retire its lease here and publish the completed tree removal.
            try { item.Payload.Dispose(); }
            catch (Exception failure) { failures.Add(failure); }
            try { SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure); }
            catch (Exception failure) { failures.Add(failure); }
            throw new AggregateException(failures);
        }
        return item;
    }

    private static void ReleasePayloads(IEnumerable<RealizedItem> items)
    {
        List<Exception>? failures = null;
        foreach (RealizedItem item in items)
        {
            try { item.Payload.Dispose(); }
            catch (Exception failure) { (failures ??= []).Add(failure); }
        }
        if (failures is not null) { throw new AggregateException(failures); }
    }

    private sealed class RealizedItem(SceneSpatialEntry2D entry, SceneNode2D node, long templateVersion, SceneSpatialLease2D<object> payload)
    {
        internal SceneSpatialEntry2D Entry = entry;
        internal readonly SceneNode2D Node = node;
        internal readonly long TemplateVersion = templateVersion;
        internal readonly SceneSpatialLease2D<object> Payload = payload;
    }

    private sealed class Request(SceneSimulationContext2D context, long templateVersion, long version)
        : ScenePreparationRequest2D(context, version)
    {
        internal readonly long TemplateVersion = templateVersion;
    }

    private sealed class TemplateCollection(Action changed, Action verifyAccess) : Collection<ContentTemplate>
    {
        protected override void InsertItem(int index, ContentTemplate item)
        {
            verifyAccess();
            ArgumentNullException.ThrowIfNull(item);
            base.InsertItem(index, item);
            changed();
        }
        protected override void SetItem(int index, ContentTemplate item)
        {
            verifyAccess();
            ArgumentNullException.ThrowIfNull(item);
            base.SetItem(index, item);
            changed();
        }
        protected override void RemoveItem(int index) { verifyAccess(); base.RemoveItem(index); changed(); }
        protected override void ClearItems() { verifyAccess(); base.ClearItems(); changed(); }
    }
}

internal sealed class SceneItems2DUpdateCounters
{
    internal int CreatedNodes { get; private set; }
    internal int AttachedNodes { get; private set; }
    internal int MovedNodes { get; private set; }
    internal int RemovedNodes { get; private set; }
    internal SceneItems2DUpdateSnapshot Snapshot() => new(CreatedNodes, AttachedNodes, MovedNodes, RemovedNodes);
    internal void CountCreated() => CreatedNodes++;
    internal void CountAttached() => AttachedNodes++;
    internal void CountMoved() => MovedNodes++;
    internal void CountRemoved() => RemovedNodes++;
}

internal readonly record struct SceneItems2DUpdateSnapshot(int CreatedNodes, int AttachedNodes, int MovedNodes, int RemovedNodes);
