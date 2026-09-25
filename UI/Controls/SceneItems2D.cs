using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Runtime.ExceptionServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Controls;

public sealed class SceneItems2D : SceneNode2D, ISceneSpatialParticipant2D
{
    public static readonly UiProperty<IEnumerable?> ItemsSourceProperty =
        UiProperty<IEnumerable?>.Register(nameof(ItemsSource), typeof(SceneItems2D),
            new UiPropertyMetadata<IEnumerable?>(null, UiPropertyOptions.AffectsRender));

    private readonly int constructionThreadId = Environment.CurrentManagedThreadId;
    private readonly List<Occurrence> realized = [];
    // Latest complete source enumeration, which can be newer than the realized tree after preflight fails.
    private List<object?> snapshot = [];
    private IEnumerable? snapshotSource;
    private bool sourceSnapshotCurrent = true;
    private INotifyCollectionChanged? observedCollection;
    private ContentTemplateRegistry templateRegistry = new();
    private UiRelay? rememberedRelay;
    private Exception? collisionReadinessError;
    private long requestVersion;
    private bool structuralMutation;
    private bool deferredReset;
    private bool deferredReenumerate;
    private bool deferredContextChange;
    private bool terminalFault;

    public SceneItems2D() => Templates = new TemplateCollection(RebuildTemplates, VerifyCollectionAccess);

    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public Collection<ContentTemplate> Templates { get; }
    public int RealizedItemCount => LogicalChildren.Count;

    internal Exception? CollisionReadinessError => collisionReadinessError;
    internal SceneItems2DUpdateCounters UpdateCounters { get; } = new();
    internal override UiRelay? OwnerRelay => SimulationContext?.Relay ?? rememberedRelay ?? base.OwnerRelay;

    SceneNode2D ISceneSpatialParticipant2D.Node => this;
    Task ISceneSpatialParticipant2D.Preparation => Task.CompletedTask;
    void ISceneSpatialParticipant2D.UpdateSpatialInterest(SceneBounds2D visibleBounds,
        IReadOnlyList<SceneBounds2D> collisionInterest, DrawRect? surfaceBounds) =>
        UpdateSpatialInterest(visibleBounds, surfaceBounds);
    string? ISceneSpatialParticipant2D.GetUnpreparedCollisionEntry(DrawRect sceneBounds) => null;

    // Explicit synchronous re-enumeration and identity reset, not an I/O retry.
    public void Refresh()
    {
        VerifyCollectionAccess();
        RequestSnapshot(reset: true, reenumerate: true);
    }

    internal override void ValidatePropertyMutation(UiProperty property, object? value)
    {
        base.ValidatePropertyMutation(property, value);
        if (ReferenceEquals(property, ItemsSourceProperty)) { VerifyCollectionAccess(); }
    }

    internal override void OnSimulationContextChanged(SceneSimulationContext2D? previous)
    {
        rememberedRelay ??= SimulationContext?.Relay ?? previous?.Relay;
        rememberedRelay?.VerifyAccess();
        requestVersion++;
        collisionReadinessError = new InvalidOperationException("The scene-items context is changing.");
        if (structuralMutation)
        {
            deferredContextChange = true;
            return;
        }
        ApplyContextChange();
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, ItemsSourceProperty))
        {
            RequestSnapshot(reset: true, reenumerate: true);
        }
    }

    internal override void AttachSurface(RenderSurface2D? surface)
    {
        if (ReferenceEquals(Surface, surface)) { return; }
        base.AttachSurface(surface);
        foreach (SceneNode2D node in LogicalChildren.OfType<SceneNode2D>()) { node.AttachSurface(surface); }
    }

    private void VerifyCollectionAccess()
    {
        if (OwnerRelay is UiRelay relay) { relay.VerifyAccess(); }
        else if (Environment.CurrentManagedThreadId != constructionThreadId)
        {
            throw new InvalidOperationException("Scene items must be changed on their owning thread.");
        }
    }

    private void SyncSubscription(IEnumerable? source)
    {
        INotifyCollectionChanged? next = SimulationContext is { IsDisposed: false }
            ? source as INotifyCollectionChanged : null;
        if (ReferenceEquals(observedCollection, next)) { return; }
        Unobserve();
        if (next is null) { return; }
        observedCollection = next;
        try { next.CollectionChanged += OnCollectionChanged; }
        catch
        {
            observedCollection = null;
            throw;
        }
    }

    private void Unobserve()
    {
        INotifyCollectionChanged? previous = observedCollection;
        observedCollection = null;
        if (previous is not null) { previous.CollectionChanged -= OnCollectionChanged; }
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        VerifyCollectionAccess();
        if (terminalFault) { throw TerminalException(); }
        if (ReferenceEquals(sender, observedCollection) && ReferenceEquals(sender, ItemsSource))
        {
            RequestSnapshot(reset: false, reenumerate: true, args);
        }
    }

    private void RequestSnapshot(bool reset, bool reenumerate, NotifyCollectionChangedEventArgs? change = null)
    {
        if (terminalFault) { throw TerminalException(); }
        IEnumerable? source = ItemsSource;
        if (!reenumerate && !structuralMutation &&
            (!sourceSnapshotCurrent || !ReferenceEquals(snapshotSource, source)))
        {
            // A template edit cannot publish the old tree as a source whose enumeration never completed.
            return;
        }
        bool hadCommittedSnapshot = collisionReadinessError is null;
        long version = ++requestVersion;
        collisionReadinessError = new InvalidOperationException("The requested scene-items snapshot has not committed.");
        if (structuralMutation)
        {
            deferredReset = true;
            deferredReenumerate |= reenumerate;
            return;
        }

        List<SceneNode2D> candidates = [];
        bool candidatesRetired = false;
        try
        {
            SyncSubscription(source);
            List<object?> requested;
            if (reenumerate)
            {
                sourceSnapshotCurrent = false;
                requested = Enumerate(source);
                if (!IsCurrent()) { return; }
                snapshot = requested;
                snapshotSource = source;
                sourceSnapshotCurrent = true;
            }
            else { requested = snapshot; }
            if (!IsCurrent()) { return; }
            if (SimulationContext is not { IsDisposed: false })
            {
                snapshot = requested;
                snapshotSource = source;
                collisionReadinessError = null;
                return;
            }

            List<Occurrence> next = !reset && change is not null &&
                ReferenceEquals(snapshotSource, source) && hadCommittedSnapshot &&
                TryApplyDelta(change, requested, out List<Occurrence>? projected)
                    ? projected!
                    : requested.Select(static value => new Occurrence(value)).ToList();

            HashSet<SceneNode2D> nodes = new(ReferenceEqualityComparer.Instance);
            foreach (Occurrence occurrence in next)
            {
                if (occurrence.Node is null)
                {
                    SceneNode2D node = CreateNode(occurrence.Value);
                    candidates.Add(node);
                    if (!nodes.Add(node))
                    {
                        throw new InvalidOperationException("A scene node cannot represent two collection occurrences.");
                    }
                    ContentControl.ValidateCanOwnChild(this, node);
                    LogicalChildren.ValidateInsertion(LogicalChildren.Count, node);
                    node.DataContext = occurrence.Value;
                    occurrence.Node = node;
                }
                else if (!nodes.Add(occurrence.Node))
                {
                    throw new InvalidOperationException("A scene node cannot represent two collection occurrences.");
                }
                if (!IsCurrent())
                {
                    Exception? cleanup = CleanupUnadopted(candidates);
                    candidatesRetired = true;
                    if (cleanup is not null) { ExceptionDispatchInfo.Capture(cleanup).Throw(); }
                    return;
                }
            }
            if (!IsCurrent())
            {
                Exception? cleanup = CleanupUnadopted(candidates);
                candidatesRetired = true;
                if (cleanup is not null) { ExceptionDispatchInfo.Capture(cleanup).Throw(); }
                return;
            }
            Commit(next, requested, source, candidates, version);
        }
        catch (Exception failure)
        {
            if (terminalFault) { throw; }
            Exception reported = candidatesRetired ? failure : CleanupUnadopted(candidates, failure)!;
            if (IsCurrent()) { collisionReadinessError = reported; }
            ExceptionDispatchInfo.Capture(reported).Throw();
            throw;
        }

        bool IsCurrent() => version == requestVersion && ReferenceEquals(source, ItemsSource);
    }

    private static List<object?> Enumerate(IEnumerable? source)
    {
        List<object?> values = [];
        if (source is not null)
        {
            foreach (object? item in source) { values.Add(item); }
        }
        return values;
    }

    private bool TryApplyDelta(NotifyCollectionChangedEventArgs change, List<object?> requested,
        out List<Occurrence>? projected)
    {
        projected = new(realized);
        int oldCount = projected.Count;
        int oldIndex = change.OldStartingIndex;
        int newIndex = change.NewStartingIndex;
        int oldItems = change.OldItems?.Count ?? 0;
        int newItems = change.NewItems?.Count ?? 0;
        switch (change.Action)
        {
            case NotifyCollectionChangedAction.Add when newItems > 0 &&
                newIndex >= 0 && newIndex <= oldCount && requested.Count == oldCount + newItems:
                projected.InsertRange(newIndex, requested.Skip(newIndex).Take(newItems).Select(static item => new Occurrence(item)));
                break;
            case NotifyCollectionChangedAction.Remove when oldItems > 0 &&
                oldIndex >= 0 && oldIndex + oldItems <= oldCount && requested.Count == oldCount - oldItems:
                projected.RemoveRange(oldIndex, oldItems);
                break;
            case NotifyCollectionChangedAction.Replace when oldItems > 0 && newItems > 0 &&
                oldIndex >= 0 && oldIndex + oldItems <= oldCount && newIndex == oldIndex &&
                requested.Count == oldCount - oldItems + newItems:
                projected.RemoveRange(oldIndex, oldItems);
                projected.InsertRange(newIndex, requested.Skip(newIndex).Take(newItems).Select(static item => new Occurrence(item)));
                break;
            case NotifyCollectionChangedAction.Move when oldItems > 0 && oldItems == newItems &&
                oldIndex >= 0 && oldIndex + oldItems <= oldCount && newIndex >= 0 &&
                newIndex <= oldCount - oldItems && requested.Count == oldCount:
                List<Occurrence> moved = projected.GetRange(oldIndex, oldItems);
                projected.RemoveRange(oldIndex, oldItems);
                projected.InsertRange(newIndex, moved);
                break;
            default:
                projected = null;
                return false;
        }

        for (int index = 0; index < requested.Count; index++)
        {
            object? old = projected[index].Value;
            object? current = requested[index];
            if (!ReferenceEquals(old, current) &&
                !(old is not null && old.GetType().IsValueType && Equals(old, current)))
            {
                projected = null;
                return false;
            }
        }
        return true;
    }

    private SceneNode2D CreateNode(object? item)
    {
        if (templateRegistry.TryResolve(new ContentTemplateMatchContext(item, owner: this), out ContentTemplate template))
        {
            SceneNode2D node = template.Create(new ContentTemplateContext(item, owner: this)) as SceneNode2D
                ?? throw new InvalidOperationException($"Content template '{template.Name}' must create a {nameof(SceneNode2D)}.");
            UpdateCounters.CountCreated();
            return node;
        }
        if (item is SceneNode2D direct) { return direct; }
        throw new InvalidOperationException($"No content template matches this collection item in {nameof(SceneItems2D)}.");
    }

    private void Commit(List<Occurrence> next, List<object?> requested, IEnumerable? source,
        List<SceneNode2D> candidates, long version)
    {
        bool changed = false;
        bool superseded = false;
        structuralMutation = true;
        try
        {
            HashSet<Occurrence> retained = new(next, ReferenceEqualityComparer.Instance);
            for (int index = realized.Count - 1; index >= 0; index--)
            {
                Occurrence old = realized[index];
                if (retained.Contains(old)) { continue; }
                RemoveNode(old.Node!);
                UpdateCounters.CountRemoved();
                changed = true;
                if (!IsCurrent()) { superseded = true; break; }
            }
            for (int index = 0; !superseded && index < next.Count; index++)
            {
                SceneNode2D node = next[index].Node!;
                int current = IndexOfNode(node);
                if (current < 0)
                {
                    LogicalChildren.Insert(index, node);
                    node.AttachSurface(Surface);
                    if (node is Scene2D nested) { nested.ResetOwnedCollisionWorlds(); }
                    if (node.IsAttached) { UpdateCounters.CountAttached(); }
                    changed = true;
                }
                else if (current != index)
                {
                    LogicalChildren.Move(current, index);
                    UpdateCounters.CountMoved();
                    changed = true;
                }
                if (!IsCurrent()) { superseded = true; }
            }
            if (superseded)
            {
                ReconcileRealized(realized.Concat(next));
            }
            else
            {
                realized.Clear();
                realized.AddRange(next);
                snapshot = requested;
                snapshotSource = source;
                collisionReadinessError = null;
            }
            if (changed)
            {
                SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
                Surface?.InvalidateFrame();
            }
        }
        catch (Exception failure)
        {
            Exception reported = EnterTerminalFault(failure, candidates);
            ExceptionDispatchInfo.Capture(reported).Throw();
            throw;
        }
        finally { structuralMutation = false; }

        if (superseded && CleanupUnadopted(candidates) is Exception cleanupFailure)
        {
            Exception reported = EnterTerminalFault(cleanupFailure, []);
            ExceptionDispatchInfo.Capture(reported).Throw();
        }

        ApplyDeferredChanges();

        bool IsCurrent() => version == requestVersion && ReferenceEquals(source, ItemsSource);
    }

    private void ReconcileRealized(IEnumerable<Occurrence> knownOccurrences)
    {
        Dictionary<SceneNode2D, Occurrence> known = new(ReferenceEqualityComparer.Instance);
        foreach (Occurrence occurrence in knownOccurrences)
        {
            if (occurrence.Node is not null) { known[occurrence.Node] = occurrence; }
        }
        realized.Clear();
        foreach (SceneNode2D member in LogicalChildren.OfType<SceneNode2D>())
        {
            if (known.TryGetValue(member, out Occurrence? occurrence)) { realized.Add(occurrence); }
        }
    }

    private void ApplyContextChange()
    {
        Unobserve();
        RetireRealizations();
        if (terminalFault) { return; }
        if (deferredContextChange)
        {
            ApplyDeferredChanges();
            return;
        }
        bool active = SimulationContext is { IsDisposed: false };
        bool reenumerate = deferredReenumerate || active &&
            (!sourceSnapshotCurrent || !ReferenceEquals(snapshotSource, ItemsSource) ||
             ItemsSource is INotifyCollectionChanged);
        deferredReset = false;
        deferredReenumerate = false;
        if (active)
        {
            RequestSnapshot(reset: true, reenumerate: reenumerate);
        }
        else if (reenumerate)
        {
            RequestSnapshot(reset: true, reenumerate: true);
        }
        else if (sourceSnapshotCurrent)
        {
            collisionReadinessError = null;
        }
    }

    private void ApplyDeferredChanges()
    {
        if (terminalFault) { return; }
        if (deferredContextChange)
        {
            deferredContextChange = false;
            ApplyContextChange();
        }
        else if (deferredReset)
        {
            bool reenumerate = deferredReenumerate;
            deferredReset = false;
            deferredReenumerate = false;
            RequestSnapshot(reset: true, reenumerate: reenumerate);
        }
    }

    private void RemoveNode(SceneNode2D node)
    {
        node.ReleaseRenderCaches();
        node.AttachSurface(null);
        if (!LogicalChildren.Remove(node))
        {
            throw new InvalidOperationException("A realized scene node was removed outside its collection owner.");
        }
        if (node is Scene2D nested) { nested.ResetOwnedCollisionWorlds(); }
    }

    private int IndexOfNode(SceneNode2D node)
    {
        for (int index = 0; index < LogicalChildren.Count; index++)
        {
            if (ReferenceEquals(LogicalChildren[index], node)) { return index; }
        }
        return -1;
    }

    private Exception EnterTerminalFault(Exception primary, IEnumerable<SceneNode2D> candidates)
    {
        terminalFault = true;
        collisionReadinessError = primary;
        deferredReset = false;
        deferredReenumerate = false;
        requestVersion++;
        List<Exception> failures = [primary];
        try { Unobserve(); }
        catch (Exception cleanup) { failures.Add(cleanup); }
        HashSet<SceneNode2D> members = new(LogicalChildren.OfType<SceneNode2D>(),
            ReferenceEqualityComparer.Instance);
        Dictionary<SceneNode2D, Occurrence> known = new(ReferenceEqualityComparer.Instance);
        foreach (Occurrence occurrence in realized)
        {
            if (occurrence.Node is not null) { known[occurrence.Node] = occurrence; }
        }
        foreach (SceneNode2D node in candidates)
        {
            if (!known.ContainsKey(node)) { known[node] = new Occurrence(node) { Node = node }; }
        }
        foreach (SceneNode2D node in known.Keys)
        {
            if (members.Contains(node)) { continue; }
            try { node.ReleaseRenderCaches(); }
            catch (Exception cleanup) { failures.Add(cleanup); }
            try { node.AttachSurface(null); }
            catch (Exception cleanup) { failures.Add(cleanup); }
        }
        realized.Clear();
        foreach (SceneNode2D member in LogicalChildren.OfType<SceneNode2D>())
        {
            if (known.TryGetValue(member, out Occurrence? occurrence)) { realized.Add(occurrence); }
        }
        Exception result = failures.Count == 1 ? primary : new AggregateException(failures);
        collisionReadinessError = result;
        return result;
    }

    private static Exception? CleanupUnadopted(IEnumerable<SceneNode2D> candidates, Exception? primary = null)
    {
        List<Exception> failures = primary is null ? [] : [primary];
        HashSet<SceneNode2D> visited = new(ReferenceEqualityComparer.Instance);
        foreach (SceneNode2D node in candidates)
        {
            if (!visited.Add(node) || node.LogicalParent is not null) { continue; }
            try { node.ReleaseRenderCaches(); }
            catch (Exception cleanup) { failures.Add(cleanup); }
        }
        return failures.Count switch
        {
            0 => null,
            1 => failures[0],
            _ => new AggregateException(failures)
        };
    }

    private void RetireRealizations()
    {
        if (realized.Count == 0) { return; }
        structuralMutation = true;
        try
        {
            for (int index = realized.Count - 1; index >= 0; index--)
            {
                RemoveNode(realized[index].Node!);
                UpdateCounters.CountRemoved();
            }
            realized.Clear();
            SceneGeometry2D.FindRootScene(this)?.NotifyCollisionMutation(this, SceneCollisionMutationKind.Structure);
            Surface?.InvalidateFrame();
        }
        catch (Exception failure)
        {
            Exception reported = EnterTerminalFault(failure, []);
            ExceptionDispatchInfo.Capture(reported).Throw();
            throw;
        }
        finally { structuralMutation = false; }
    }

    private Exception TerminalException() =>
        new InvalidOperationException("This scene-items control has a terminal structural fault and must be replaced.",
            collisionReadinessError);

    private void RebuildTemplates()
    {
        if (terminalFault) { throw TerminalException(); }
        ContentTemplateRegistry replacement = new();
        foreach (ContentTemplate template in Templates) { replacement.Register(template); }
        templateRegistry = replacement;
        RequestSnapshot(reset: true, reenumerate: false);
    }

    internal override void Record(Scene2DRecordContext context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0)
        {
            ReleaseRenderCaches();
            return;
        }
        SceneBounds2D visible = SceneSpatialInterest2D.ResolveInputBounds(this,
            context.GetConservativeVisibleLocalBounds(), includeSelf: false, surfaceBounds: context.Frame.Bounds);
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>())
        {
            if (Intersects(visible, child)) { child.Record(context); }
            else { child.ReleaseRenderCaches(); }
        }
    }

    internal override void CheckPresentation(ScenePresentationContext2D context)
    {
        if (!UIElementVisibility.ParticipatesInRendering(this) || Opacity <= 0) { return; }
        CheckPrismPresentation(context);
        SceneBounds2D visible = context.GetVisibleBounds(this);
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>())
        {
            if (Intersects(visible, child)) { child.CheckPresentation(context); }
            else { child.ReleaseRenderCaches(); }
        }
    }

    internal void UpdateSpatialInterest(SceneBounds2D visibleBounds, DrawRect? surfaceBounds = null)
    {
        if (SimulationContext is not { IsDisposed: false } context) { return; }
        context.Relay.VerifyAccess();
        if (!context.IsHeadless)
        {
            visibleBounds = SceneSpatialInterest2D.ResolveInputBounds(this, visibleBounds,
                includeSelf: false, surfaceBounds: surfaceBounds);
        }
        bool visible = !context.IsHeadless && UIElementVisibility.ParticipatesInRendering(this) && Opacity > 0;
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>())
        {
            if (!visible || !Intersects(visibleBounds, child)) { child.ReleaseRenderCaches(); }
        }
    }

    internal IReadOnlyList<SceneNode2D> GetInputCandidates(DrawPoint point, IReadOnlySet<SceneNode2D>? colliderPaths) =>
        LogicalChildren.OfType<SceneNode2D>()
            .Where(static node => node.ParticipatesInInputRoute)
            .ToArray();

    internal override SceneBounds2D GetVisibleLocalBounds()
    {
        SceneBounds2D bounds = SceneBounds2D.Empty;
        foreach (SceneNode2D child in LogicalChildren.OfType<SceneNode2D>())
        {
            bounds = SceneGeometry2D.Union(bounds,
                SceneGeometry2D.TransformBounds(child.GetLocalBounds(), child.GetLocalTransform()));
            if (bounds.Kind == SceneBoundsKind.Unknown) { break; }
        }
        return bounds;
    }

    private static bool Intersects(SceneBounds2D visible, SceneNode2D child)
    {
        SceneBounds2D bounds = SceneGeometry2D.TransformBounds(child.GetLocalBounds(), child.GetLocalTransform());
        return visible.Kind != SceneBoundsKind.Empty && bounds.Kind != SceneBoundsKind.Empty &&
            (bounds.Kind == SceneBoundsKind.Unknown ||
             ScenePresentationContext2D.Intersects(visible, bounds.Bounds));
    }

    private sealed class Occurrence(object? value)
    {
        internal readonly object? Value = value;
        internal SceneNode2D? Node;
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
