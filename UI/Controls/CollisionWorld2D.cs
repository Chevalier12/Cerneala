using System.Diagnostics;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls;

public sealed partial class CollisionWorld2D
{
    private readonly Scene2D owner;
    private readonly SparseCollisionGrid2D broadphase = new();
    private readonly Dictionary<Collider2D, CollisionEntry2D> entriesByCollider =
        new(ReferenceEqualityComparer.Instance);
    private readonly List<CollisionEntry2D?> entriesById = [];
    private readonly List<int> candidates = [];
    private bool initialized;
    private long observedVersion = -1;
    private int nextId;
    private long nextOrdinal;
    private long broadphaseCandidateCount;
    private long exactTestCount;
    private long rebuildCount;
    private long incrementalUpdateCount;
    private long updatedEntryCount;
    private long queryCount;
    private long lastQueryTicks;
    private long totalQueryTicks;

    internal CollisionWorld2D(Scene2D owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal long Version => owner.CollisionMutationVersion;

    public bool Intersects(Collider2D first, Collider2D second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        owner.VerifyOwnerAccess();
        long started = Stopwatch.GetTimestamp();
        try
        {
            EnsureCurrent();
            return entriesByCollider.TryGetValue(first, out CollisionEntry2D? firstEntry) &&
                entriesByCollider.TryGetValue(second, out CollisionEntry2D? secondEntry) &&
                IntersectsEntries(firstEntry, secondEntry);
        }
        finally
        {
            FinishQuery(started);
        }
    }

    // The rule that Intersects and contact events share: the pair rules, then the exact shape test.
    private bool IntersectsEntries(CollisionEntry2D first, CollisionEntry2D second)
    {
        if (!PassesPairRules(first, second))
        {
            return false;
        }

        exactTestCount++;
        return CollisionNarrowPhase2D.Intersects(first.Geometry, second.Geometry);
    }

    public CollisionHit2D[] Overlap(
        Collider2D collider,
        CollisionQuery2D query = default)
    {
        ArgumentNullException.ThrowIfNull(collider);
        owner.VerifyOwnerAccess();
        long started = Stopwatch.GetTimestamp();
        try
        {
            EnsureCurrent();
            if (!entriesByCollider.TryGetValue(collider, out CollisionEntry2D? source))
            {
                return [];
            }

            EnsureCollisionCoverage(source.Geometry.SceneBounds, collider, query);
            broadphase.Query(source.Geometry.SceneBounds, candidates);
            broadphaseCandidateCount += candidates.Count;
            List<(CollisionHit2D Hit, long Ordinal)> hits = [];
            foreach (int id in candidates)
            {
                CollisionEntry2D? target = GetEntry(id);
                if (target is null ||
                    ReferenceEquals(target.Collider, collider) ||
                    !PassesQueryFilter(target, query) ||
                    !PassesPairRules(source, target))
                {
                    continue;
                }

                exactTestCount++;
                if (!CollisionNarrowPhase2D.TryContact(
                    source.Geometry,
                    target.Geometry,
                    out NarrowPhaseContact2D contact))
                {
                    continue;
                }

                hits.Add((CreateHit(target, contact), target.Ordinal));
            }

            hits.Sort(static (left, right) => left.Ordinal.CompareTo(right.Ordinal));
            return hits.Select(static item => item.Hit).ToArray();
        }
        finally
        {
            FinishQuery(started);
        }
    }

    public CollisionHit2D[] Raycast(
        Vector2 origin,
        Vector2 direction,
        float maxDistance,
        CollisionQuery2D query = default)
    {
        owner.VerifyOwnerAccess();
        ValidateFinite(origin, nameof(origin));
        ValidateFinite(direction, nameof(direction));
        if (direction.LengthSquared() <= CollisionNarrowPhase2D.Epsilon * CollisionNarrowPhase2D.Epsilon)
        {
            throw new ArgumentOutOfRangeException(nameof(direction), direction, "Ray direction must be non-zero.");
        }

        if (!float.IsFinite(maxDistance) || maxDistance < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDistance), maxDistance, "Ray distance must be finite and non-negative.");
        }

        direction = Vector2.Normalize(direction);
        long started = Stopwatch.GetTimestamp();
        try
        {
            EnsureCurrent();
            Vector2 end = origin + (direction * maxDistance);
            DrawRect bounds = CreateBounds(origin, end);
            EnsureCollisionCoverage(bounds, null, query);
            broadphase.Query(bounds, candidates);
            broadphaseCandidateCount += candidates.Count;
            List<(CollisionHit2D Hit, long Ordinal)> hits = [];
            foreach (int id in candidates)
            {
                CollisionEntry2D? target = GetEntry(id);
                if (target is null || !PassesQueryFilter(target, query))
                {
                    continue;
                }

                exactTestCount++;
                if (CollisionNarrowPhase2D.Raycast(
                    target.Geometry,
                    origin,
                    direction,
                    maxDistance,
                    out NarrowPhaseContact2D contact))
                {
                    hits.Add((CreateHit(target, contact), target.Ordinal));
                }
            }

            hits.Sort(static (left, right) => CompareOrderedHits(left, right));
            return hits.Select(static item => item.Hit).ToArray();
        }
        finally
        {
            FinishQuery(started);
        }
    }

    public MoveCollisionResult2D MoveAndCollide(
        Collider2D collider,
        Vector2 displacement,
        CollisionQuery2D query = default)
    {
        ArgumentNullException.ThrowIfNull(collider);
        owner.VerifyOwnerAccess();
        ValidateFinite(displacement, nameof(displacement));
        if (collider.IsVisionSensor)
        {
            // Whatever its Enabled state, and even when its owner is not in this world.
            throw new InvalidOperationException("A Vision cone is a sensor; it cannot be moved with MoveAndCollide.");
        }

        long started = Stopwatch.GetTimestamp();
        try
        {
            EnsureCurrent();
            if (!entriesByCollider.TryGetValue(collider, out CollisionEntry2D? source))
            {
                return new MoveCollisionResult2D(displacement, displacement, null, []);
            }

            DrawRect sweptBounds = CollisionNarrowPhase2D.GetSweptBounds(
                source.Geometry.SceneBounds,
                displacement);
            EnsureCollisionCoverage(sweptBounds, collider, query);
            broadphase.Query(sweptBounds, candidates);
            broadphaseCandidateCount += candidates.Count;
            List<(CollisionHit2D Hit, long Ordinal)> blocking = [];
            List<(CollisionHit2D Hit, long Ordinal)> triggers = [];
            foreach (int id in candidates)
            {
                CollisionEntry2D? target = GetEntry(id);
                if (target is null ||
                    ReferenceEquals(target.Collider, collider) ||
                    !PassesQueryFilter(target, query) ||
                    !PassesPairFilter(collider, target.Collider))
                {
                    continue;
                }

                exactTestCount++;
                if (!CollisionNarrowPhase2D.ShapeCast(
                    source.Geometry,
                    displacement,
                    target.Geometry,
                    out NarrowPhaseContact2D contact,
                    includeNonblockingInitialContact: collider.IsTrigger || target.Collider.IsTrigger))
                {
                    continue;
                }

                CollisionHit2D hit = CreateHit(target, contact);
                if (collider.IsTrigger || target.Collider.IsTrigger)
                {
                    triggers.Add((hit, target.Ordinal));
                }
                else
                {
                    blocking.Add((hit, target.Ordinal));
                }
            }

            blocking.Sort(static (left, right) => CompareOrderedHits(left, right));
            triggers.Sort(static (left, right) => CompareOrderedHits(left, right));
            CollisionHit2D? collision = blocking.Count == 0 ? null : blocking[0].Hit;
            float travelFraction = collision?.Fraction ?? 1;
            return new MoveCollisionResult2D(
                displacement,
                displacement * travelFraction,
                collision,
                triggers.Select(static item => item.Hit).ToArray());
        }
        finally
        {
            FinishQuery(started);
        }
    }

    public CollisionWorld2DDiagnosticsSnapshot GetDiagnosticsSnapshot()
    {
        EnsureCurrent();
        return new CollisionWorld2DDiagnosticsSnapshot(
            entriesByCollider.Count,
            broadphase.CellCount,
            broadphaseCandidateCount,
            exactTestCount,
            rebuildCount,
            incrementalUpdateCount,
            updatedEntryCount,
            queryCount,
            lastQueryTicks,
            totalQueryTicks,
            broadphase.EstimatedRetainedBytes);
    }

    internal void CollectPointHits(
        Vector2 scenePoint,
        ISet<Collider2D> results)
    {
        ArgumentNullException.ThrowIfNull(results);
        owner.VerifyOwnerAccess();
        ValidateFinite(scenePoint, nameof(scenePoint));
        long started = Stopwatch.GetTimestamp();
        try
        {
            EnsureCurrent();
            results.Clear();
            broadphase.Query(
                new DrawRect(scenePoint.X, scenePoint.Y, 0, 0),
                candidates);
            broadphaseCandidateCount += candidates.Count;
            foreach (int id in candidates)
            {
                CollisionEntry2D? target = GetEntry(id);
                if (target is null || target.IsSensor)
                {
                    // A Vision is never picked.
                    continue;
                }

                exactTestCount++;
                if (SceneGeometry2D.ContainsPoint(target.Geometry, scenePoint))
                {
                    results.Add(target.Collider);
                }
            }
        }
        finally
        {
            FinishQuery(started);
        }
    }

    // Read-only viewport query. Debug observations do not count as gameplay
    // queries and never run contact resolution or update entries.
    internal void CollectDebugGeometry(DrawRect bounds, List<ColliderGeometry2D> results)
    {
        EnsureCurrent();
        results.Clear();
        broadphase.Query(bounds, candidates);
        candidates.Sort();
        foreach (int id in candidates)
        {
            CollisionEntry2D? entry = GetEntry(id);
            if (entry is null) { continue; }
            DrawRect box = entry.Geometry.SceneBounds;
            if (box.X <= bounds.Right && box.Right >= bounds.X &&
                box.Y <= bounds.Bottom && box.Bottom >= bounds.Y)
            {
                results.Add(entry.Geometry);
            }
        }
    }

    internal void ApplyMutation(
        SceneNode2D node,
        SceneCollisionMutationKind kind,
        long version)
    {
        if (!initialized)
        {
            observedVersion = version;
            return;
        }

        int changed = kind == SceneCollisionMutationKind.Structure
            ? ReconcileAll()
            : ReconcileSubtree(node);
        observedVersion = version;
        incrementalUpdateCount++;
        updatedEntryCount += changed;
    }

    internal void Reset()
    {
        ForgetContactEntries();
        InvalidateSpatialRegions();
        broadphase.Clear();
        entriesByCollider.Clear();
        entriesById.Clear();
        candidates.Clear();
        initialized = false;
        observedVersion = -1;
        nextId = 0;
        nextOrdinal = 0;
    }

    private void EnsureCurrent()
    {
        owner.VerifyOwnerAccess();
        if (!initialized || observedVersion != Version)
        {
            Rebuild();
        }
    }

    private void Rebuild()
    {
        broadphase.Clear();
        entriesByCollider.Clear();
        entriesById.Clear();
        candidates.Clear();
        nextId = 0;
        nextOrdinal = 0;
        BeginContactRebuild();
        try
        {
            foreach (Collider2D collider in EnumerateColliders(owner))
            {
                AddOrUpdate(collider);
            }
        }
        finally
        {
            EndContactRebuild();
        }

        initialized = true;
        observedVersion = Version;
        rebuildCount++;
    }

    private int ReconcileAll()
    {
        HashSet<Collider2D> present = new(ReferenceEqualityComparer.Instance);
        int changed = 0;
        foreach (Collider2D collider in EnumerateColliders(owner))
        {
            present.Add(collider);
            changed += AddOrUpdate(collider) ? 1 : 0;
        }

        Collider2D[] removed = entriesByCollider.Keys
            .Where(collider => !present.Contains(collider))
            .ToArray();
        foreach (Collider2D collider in removed)
        {
            Remove(collider);
            changed++;
        }

        return changed;
    }

    private int ReconcileSubtree(SceneNode2D node)
    {
        if (node is Collider2D collider)
        {
            return AddOrUpdate(collider) ? 1 : 0;
        }

        int changed = 0;
        foreach (Collider2D descendant in EnumerateColliders(node))
        {
            changed += AddOrUpdate(descendant) ? 1 : 0;
        }

        return changed;
    }

    private bool AddOrUpdate(Collider2D collider)
    {
        bool belongsToOwner = ReferenceEquals(SceneGeometry2D.FindRootScene(collider), owner);
        if (!belongsToOwner ||
            !collider.TryGetActiveSceneGeometry(out ColliderGeometry2D geometry))
        {
            return Remove(collider);
        }

        if (entriesByCollider.TryGetValue(collider, out CollisionEntry2D? existing))
        {
            if (existing.Geometry == geometry &&
                existing.CollisionLayer == collider.CollisionLayer &&
                existing.CollisionMask == collider.CollisionMask &&
                existing.IsTrigger == collider.IsTrigger &&
                ReferenceEquals(existing.SensorOwner, collider.VisionOwner))
            {
                return false;
            }

            CollisionEntry2D updated = existing with
            {
                Geometry = geometry,
                CollisionLayer = collider.CollisionLayer,
                CollisionMask = collider.CollisionMask,
                IsTrigger = collider.IsTrigger,
                SensorOwner = collider.VisionOwner
            };
            entriesByCollider[collider] = updated;
            entriesById[existing.Id] = updated;
            broadphase.AddOrUpdate(existing.Id, geometry.SceneBounds);
            MarkContactDirty(collider);
            return true;
        }

        if (nextOrdinal == long.MaxValue)
        {
            throw new InvalidOperationException("Collision attachment ordinal space was exhausted.");
        }

        int id = nextId++;
        CollisionEntry2D entry = new(
            id,
            ++nextOrdinal,
            collider,
            geometry,
            collider.CollisionLayer,
            collider.CollisionMask,
            collider.IsTrigger,
            collider.VisionOwner);
        entriesByCollider.Add(collider, entry);
        while (entriesById.Count <= id)
        {
            entriesById.Add(null);
        }

        entriesById[id] = entry;
        broadphase.AddOrUpdate(id, geometry.SceneBounds);
        OnContactEntryAdded(entry);
        return true;
    }

    private bool Remove(Collider2D collider)
    {
        if (!entriesByCollider.Remove(collider, out CollisionEntry2D? entry))
        {
            return false;
        }

        broadphase.Remove(entry.Id);
        entriesById[entry.Id] = null;
        OnContactEntryRemoved(collider);
        return true;
    }

    private CollisionEntry2D? GetEntry(int id) =>
        (uint)id < (uint)entriesById.Count ? entriesById[id] : null;

    private static IEnumerable<Collider2D> EnumerateColliders(SceneNode2D node)
    {
        foreach (SceneNode2D child in node.LogicalChildren.OfType<SceneNode2D>())
        {
            if (child is Collider2D collider)
            {
                yield return collider;
            }
            else
            {
                foreach (Collider2D descendant in EnumerateColliders(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private static bool PassesPairFilter(Collider2D first, Collider2D second) =>
        first.CollisionLayer != 0 &&
        first.CollisionMask != 0 &&
        second.CollisionLayer != 0 &&
        second.CollisionMask != 0 &&
        (first.CollisionMask & second.CollisionLayer) != 0 &&
        (second.CollisionMask & first.CollisionLayer) != 0;

    // The pair rules of Intersects, Overlap and contact events: the sensor rule, then the layer and mask filter.
    private static bool PassesPairRules(CollisionEntry2D first, CollisionEntry2D second) =>
        PassesSensorRule(first, second) &&
        PassesPairFilter(first.Collider, second.Collider);

    // A Vision is a sensor. Two sensors never touch, and a sensor never touches a collider inside the
    // subtree of its owner: the sprite's own body for a Sprite2D, every collider of the group for a Scene2D.
    private static bool PassesSensorRule(CollisionEntry2D first, CollisionEntry2D second)
    {
        if (first.SensorOwner is null && second.SensorOwner is null)
        {
            return true;
        }

        if (first.SensorOwner is not null && second.SensorOwner is not null)
        {
            return false;
        }

        (CollisionEntry2D sensor, CollisionEntry2D other) = first.SensorOwner is not null
            ? (first, second)
            : (second, first);
        return !IsInsideSubtree(other.Collider, sensor.SensorOwner!);
    }

    private static bool IsInsideSubtree(Collider2D collider, SceneNode2D subtreeRoot)
    {
        for (UIElement? current = collider.LogicalParent; current is not null; current = current.LogicalParent)
        {
            if (ReferenceEquals(current, subtreeRoot))
            {
                return true;
            }
        }

        return false;
    }

    // A Vision is never a target of a query: it does not block, and rays and other colliders' overlaps do not see it.
    private static bool PassesQueryFilter(CollisionEntry2D target, CollisionQuery2D query) =>
        !target.IsSensor &&
        !ReferenceEquals(target.Collider, query.Exclude) &&
        (query.IncludeTriggers || !target.Collider.IsTrigger) &&
        query.CollisionLayer != 0 &&
        query.CollisionMask != 0 &&
        (query.CollisionMask & target.Collider.CollisionLayer) != 0 &&
        (target.Collider.CollisionMask & query.CollisionLayer) != 0;

    private static CollisionHit2D CreateHit(
        CollisionEntry2D entry,
        NarrowPhaseContact2D contact) =>
        new(
            entry.Collider,
            FindEntity(entry.Collider),
            contact.Point,
            contact.Normal,
            contact.Distance,
            contact.Fraction,
            entry.Collider.IsTrigger);

    private static SceneNode2D FindEntity(Collider2D collider)
    {
        for (UIElement? current = collider.LogicalParent;
            current is not null;
            current = current.LogicalParent)
        {
            if (current is SceneNode2D node && current is not Collider2D)
            {
                return node;
            }
        }

        return collider;
    }

    private static int CompareOrderedHits(
        (CollisionHit2D Hit, long Ordinal) left,
        (CollisionHit2D Hit, long Ordinal) right)
    {
        int fraction = left.Hit.Fraction.CompareTo(right.Hit.Fraction);
        if (fraction != 0)
        {
            return fraction;
        }

        int distance = left.Hit.Distance.CompareTo(right.Hit.Distance);
        return distance != 0 ? distance : left.Ordinal.CompareTo(right.Ordinal);
    }

    private void FinishQuery(long started)
    {
        long elapsed = Stopwatch.GetTimestamp() - started;
        lastQueryTicks = elapsed;
        totalQueryTicks += elapsed;
        queryCount++;
    }

    private static DrawRect CreateBounds(Vector2 first, Vector2 second)
    {
        float left = MathF.Min(first.X, second.X);
        float top = MathF.Min(first.Y, second.Y);
        return new DrawRect(
            left,
            top,
            MathF.Max(first.X, second.X) - left,
            MathF.Max(first.Y, second.Y) - top);
    }

    private static void ValidateFinite(Vector2 value, string parameterName)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Coordinates must be finite.");
        }
    }

    private sealed record CollisionEntry2D(
        int Id,
        long Ordinal,
        Collider2D Collider,
        ColliderGeometry2D Geometry,
        uint CollisionLayer,
        uint CollisionMask,
        bool IsTrigger,
        SceneNode2D? SensorOwner)
    {
        // A Vision cone is a sensor; SensorOwner is the owner of its Vision slot.
        internal bool IsSensor => SensorOwner is not null;
    }
}
