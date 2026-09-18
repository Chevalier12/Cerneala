using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed partial class CollisionWorld2D
{
    private readonly List<SceneCollisionRegion2D> spatialRegions = [];
    private readonly List<SpatialInterestSnapshot> spatialInterestSnapshots = [];
    private IReadOnlyList<SceneBounds2D> spatialCollisionInterest = Array.Empty<SceneBounds2D>();
    private long spatialInterestVersion;
    private long observedSpatialInterestVersion = -1;

    public async ValueTask<SceneCollisionRegion2D> PrepareRegionAsync(
        DrawRect bounds, CancellationToken cancellationToken = default)
    {
        SceneSpatialEntry2D.ValidateBounds(bounds, nameof(bounds));
        cancellationToken.ThrowIfCancellationRequested();
        SceneSimulationContext2D context = owner.SimulationContext
            ?? throw new InvalidOperationException("Preparing collision regions requires a simulation context or an attached render surface.");
        context.Relay.VerifyAccess();
        ObjectDisposedException.ThrowIf(context.IsDisposed, context);
        SceneCollisionRegion2D region = new(this, context, bounds);
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, region.CancellationToken);
        CancellationToken token = cancellation.Token;
        spatialRegions.Add(region);
        spatialInterestVersion++;
        try
        {
            context.RefreshSpatialItems();
            while (true)
            {
                Task[] waiting = context.Relay.CheckAccess() ? ReadState()
                    : await context.Relay.InvokeAsync(ReadState, token).ConfigureAwait(false);
                if (waiting.Length == 0) { return region; }
                await Task.WhenAll(waiting).WaitAsync(token).ConfigureAwait(false);
            }
        }
        catch
        {
            region.Dispose();
            throw;
        }

        Task[] ReadState()
        {
            token.ThrowIfCancellationRequested();
            if (!ReferenceEquals(owner.SimulationContext, context) || context.IsDisposed)
            {
                throw new InvalidOperationException("The scene left its simulation context while preparing a collision region.");
            }
            string? missing = FindUnpreparedCollisionEntry(bounds);
            if (missing is null) { return []; }
            DrawRect collisionBounds = IncludeContactFringe(bounds);
            Task[] waiting = EnumerateSpatialItems(owner)
                .Where(items => items.GetUnpreparedCollisionEntry(collisionBounds) is not null)
                .Select(items => items.GetCollisionPreparation(collisionBounds))
                .Where(static task => !task.IsCompletedSuccessfully).Distinct().ToArray();
            if (waiting.Length == 0) { throw new SceneCollisionRegionNotReadyException(bounds, missing); }
            return waiting;
        }
    }

    internal bool IsSpatialRegionReady(DrawRect bounds, SceneSimulationContext2D context)
    {
        if (!ReferenceEquals(owner.SimulationContext, context) || context.IsDisposed) { return false; }
        context.Relay.VerifyAccess();
        return FindUnpreparedCollisionEntry(bounds) is null;
    }

    private void EnsureCollisionCoverage(DrawRect bounds, Collider2D? collider, CollisionQuery2D query)
    {
        if (query.CollisionLayer == 0 || query.CollisionMask == 0 || collider?.CollisionMask == 0) { return; }
        string? missing = FindUnpreparedCollisionEntry(bounds);
        if (missing is not null) { throw new SceneCollisionRegionNotReadyException(bounds, missing); }
    }

    private string? FindUnpreparedCollisionEntry(DrawRect bounds)
    {
        DrawRect expanded = IncludeContactFringe(bounds);
        foreach (ISceneSpatialParticipant2D items in EnumerateSpatialItems(owner))
        {
            string? missing = items.GetUnpreparedCollisionEntry(expanded);
            if (missing is not null) { return missing; }
        }
        return null;
    }

    private static DrawRect IncludeContactFringe(DrawRect bounds)
    {
        // Selection and readiness use the same scene-unit contact tolerance as
        // narrow phase, including when source transforms change local units.
        const float epsilon = CollisionNarrowPhase2D.Epsilon;
        return new(bounds.X - epsilon, bounds.Y - epsilon,
            bounds.Width + epsilon * 2, bounds.Height + epsilon * 2);
    }

    internal IReadOnlyList<SceneBounds2D> GetSpatialCollisionInterest()
    {
        IReadOnlyList<ISceneSpatialParticipant2D> items = owner.SimulationContext?.SpatialItems ?? Array.Empty<ISceneSpatialParticipant2D>();
        bool changed = observedSpatialInterestVersion != spatialInterestVersion || spatialInterestSnapshots.Count != items.Count;
        for (int index = 0; !changed && index < items.Count; index++)
        {
            ISceneSpatialParticipant2D item = items[index];
            SpatialInterestSnapshot before = spatialInterestSnapshots[index];
            changed = !ReferenceEquals(before.Items, item) || !ReferenceEquals(before.Catalog, item.SimulationCatalog) ||
                before.Transform != SceneGeometry2D.GetLocalToSceneTransform(item.Node) ||
                before.Visible != UIElementVisibility.IsEffectivelyVisible(item.Node);
        }
        if (!changed) { return spatialCollisionInterest; }

        List<SceneBounds2D> interests = new(spatialRegions.Count);
        foreach (SceneCollisionRegion2D region in spatialRegions)
        {
            if (!region.IsDisposed) { interests.Add(SceneBounds2D.Known(IncludeContactFringe(region.Bounds))); }
        }
        spatialInterestSnapshots.Clear();
        foreach (ISceneSpatialParticipant2D item in items)
        {
            IReadOnlyList<SceneSpatialEntry2D>? catalog = item.SimulationCatalog;
            Matrix3x2 transform = SceneGeometry2D.GetLocalToSceneTransform(item.Node);
            bool visible = UIElementVisibility.IsEffectivelyVisible(item.Node);
            spatialInterestSnapshots.Add(new(item, catalog, transform, visible));
            if (catalog is null || !visible) { continue; }
            foreach (SceneSpatialEntry2D entry in catalog)
            {
                if (entry.IsSimulated && entry.CollisionBounds is DrawRect bounds)
                {
                    SceneBounds2D transformed = SceneGeometry2D.TransformBounds(SceneBounds2D.Known(bounds), transform);
                    interests.Add(transformed.Kind == SceneBoundsKind.Known
                        ? SceneBounds2D.Known(IncludeContactFringe(transformed.Bounds)) : transformed);
                }
            }
        }
        observedSpatialInterestVersion = spatialInterestVersion;
        spatialCollisionInterest = interests.ToArray();
        return spatialCollisionInterest;
    }

    internal void ReleaseSpatialRegion(SceneCollisionRegion2D region, SceneSimulationContext2D context)
    {
        if (context.Relay.CheckAccess()) { ReleaseSpatialRegionCore(region, context); return; }
        WeakReference<CollisionWorld2D> target = new(this);
        WeakReference<SceneCollisionRegion2D> interest = new(region);
        WeakReference<SceneSimulationContext2D> lifetime = new(context);
        context.Relay.Post(() =>
        {
            if (target.TryGetTarget(out CollisionWorld2D? world) &&
                interest.TryGetTarget(out SceneCollisionRegion2D? released) &&
                lifetime.TryGetTarget(out SceneSimulationContext2D? owner) && !owner.IsDisposed)
            {
                world.ReleaseSpatialRegionCore(released, owner);
            }
        });
    }

    private void ReleaseSpatialRegionCore(SceneCollisionRegion2D region, SceneSimulationContext2D context)
    {
        if (!spatialRegions.Remove(region)) { return; }
        spatialInterestVersion++;
        if (ReferenceEquals(owner.SimulationContext, context)) { context.RefreshSpatialItems(); }
    }

    internal void InvalidateSpatialRegions()
    {
        SceneCollisionRegion2D[] previous = spatialRegions.ToArray();
        spatialRegions.Clear();
        spatialInterestSnapshots.Clear();
        spatialCollisionInterest = Array.Empty<SceneBounds2D>();
        spatialInterestVersion++;
        foreach (SceneCollisionRegion2D region in previous) { region.Invalidate(); }
    }

    private static IEnumerable<ISceneSpatialParticipant2D> EnumerateSpatialItems(SceneNode2D node)
    {
        if (node is ISceneSpatialParticipant2D items) { yield return items; }
        foreach (SceneNode2D child in node.LogicalChildren.OfType<SceneNode2D>())
        {
            foreach (ISceneSpatialParticipant2D descendant in EnumerateSpatialItems(child)) { yield return descendant; }
        }
    }

    private readonly record struct SpatialInterestSnapshot(
        ISceneSpatialParticipant2D Items, IReadOnlyList<SceneSpatialEntry2D>? Catalog, Matrix3x2 Transform, bool Visible);
}

public sealed class SceneCollisionRegion2D : IDisposable
{
    private CollisionWorld2D? owner;
    private SceneSimulationContext2D? context;
    private readonly CancellationTokenSource cancellation = new();

    internal SceneCollisionRegion2D(CollisionWorld2D owner, SceneSimulationContext2D context, DrawRect bounds)
    {
        this.owner = owner;
        this.context = context;
        Bounds = bounds;
    }

    public DrawRect Bounds { get; }
    public bool IsReady => Volatile.Read(ref context) is { } current &&
        (Volatile.Read(ref owner)?.IsSpatialRegionReady(Bounds, current) ?? false);
    internal bool IsDisposed => Volatile.Read(ref owner) is null;
    internal CancellationToken CancellationToken => cancellation.Token;

    public void Dispose()
    {
        CollisionWorld2D? previous = Interlocked.Exchange(ref owner, null);
        if (previous is null) { return; }
        SceneSimulationContext2D previousContext = Interlocked.Exchange(ref context, null)!;
        try { cancellation.Cancel(); }
        finally
        {
            cancellation.Dispose();
            previous.ReleaseSpatialRegion(this, previousContext);
        }
    }

    internal void Invalidate()
    {
        if (Interlocked.Exchange(ref owner, null) is null) { return; }
        Interlocked.Exchange(ref context, null);
        try { cancellation.Cancel(); }
        finally { cancellation.Dispose(); }
    }
}

public sealed class SceneCollisionRegionNotReadyException : InvalidOperationException
{
    public SceneCollisionRegionNotReadyException(DrawRect bounds, string entryId)
        : base($"Collision region {bounds} is not prepared; spatial entry '{entryId}' is unavailable.")
    {
        SceneSpatialEntry2D.ValidateBounds(bounds, nameof(bounds));
        ArgumentException.ThrowIfNullOrWhiteSpace(entryId);
        Bounds = bounds;
        EntryId = entryId;
    }

    public DrawRect Bounds { get; }
    public string EntryId { get; }
}
