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
            Span<DrawRect> queryBounds = stackalloc DrawRect[MaxRayQueryBoundsCount];
            int queryBoundsCount = CreateRayQueryBounds(origin, end, queryBounds, out double start, out double stop);
            for (int index = 0; index < queryBoundsCount; index++)
            {
                EnsureCollisionCoverage(queryBounds[index], null, query);
            }

            // The narrow phase tests only the part of the ray that the query rectangles cover, so its float math
            // stays within the DrawRect coordinate range for a huge maxDistance or an origin outside that range.
            bool clipped = start > 0 || stop < 1;
            double skippedDistance = (double)maxDistance * start;
            Vector2 testedOrigin = clipped
                ? new Vector2(
                    (float)(origin.X + (((double)end.X - origin.X) * start)),
                    (float)(origin.Y + (((double)end.Y - origin.Y) * start)))
                : origin;
            float testedDistance = clipped ? RoundUpToFloat((double)maxDistance * (stop - start)) : maxDistance;
            HashSet<int>? tested = queryBoundsCount > 1 ? [] : null;
            List<(CollisionHit2D Hit, long Ordinal)> hits = [];
            for (int index = 0; index < queryBoundsCount; index++)
            {
                broadphase.Query(queryBounds[index], candidates);
                broadphaseCandidateCount += candidates.Count;
                foreach (int id in candidates)
                {
                    CollisionEntry2D? target = GetEntry(id);
                    if (target is null ||
                        tested?.Add(id) == false ||
                        !PassesQueryFilter(target, query))
                    {
                        continue;
                    }

                    exactTestCount++;
                    if (CollisionNarrowPhase2D.Raycast(
                        target.Geometry,
                        testedOrigin,
                        direction,
                        testedDistance,
                        out NarrowPhaseContact2D contact))
                    {
                        if (clipped)
                        {
                            // Distance and Fraction measure the full requested ray from origin. The tested
                            // distance was rounded up, so a hit at the ray end is kept within the ray.
                            double distance = Math.Min(skippedDistance + contact.Distance, maxDistance);
                            contact = contact with
                            {
                                Distance = (float)distance,
                                Fraction = (float)(distance / maxDistance)
                            };
                        }

                        hits.Add((CreateHit(target, contact), target.Ordinal));
                    }
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

            Span<DrawRect> queryBounds = stackalloc DrawRect[MaxMoveQueryBoundsCount];
            int queryBoundsCount = CreateMoveQueryBounds(
                source.Geometry.SceneBounds,
                displacement,
                queryBounds,
                out double stop);
            for (int index = 0; index < queryBoundsCount; index++)
            {
                EnsureCollisionCoverage(queryBounds[index], collider, query);
            }

            // The narrow phase casts only the part of the move during which the collider's bounds touch the DrawRect
            // coordinate range, so its float math stays within that range for a huge displacement.
            bool clipped = stop < 1;
            Vector2 testedDisplacement = clipped
                ? new Vector2((float)(displacement.X * stop), (float)(displacement.Y * stop))
                : displacement;
            double displacementLength = Math.Sqrt(
                ((double)displacement.X * displacement.X) + ((double)displacement.Y * displacement.Y));
            HashSet<int>? tested = queryBoundsCount > 1 ? [] : null;
            List<(CollisionHit2D Hit, long Ordinal)> blocking = [];
            List<(CollisionHit2D Hit, long Ordinal)> triggers = [];
            for (int index = 0; index < queryBoundsCount; index++)
            {
                broadphase.Query(queryBounds[index], candidates);
                broadphaseCandidateCount += candidates.Count;
                foreach (int id in candidates)
                {
                    CollisionEntry2D? target = GetEntry(id);
                    if (target is null ||
                        tested?.Add(id) == false ||
                        ReferenceEquals(target.Collider, collider) ||
                        !PassesQueryFilter(target, query) ||
                        !PassesPairFilter(collider, target.Collider))
                    {
                        continue;
                    }

                    exactTestCount++;
                    if (!CollisionNarrowPhase2D.ShapeCast(
                        source.Geometry,
                        testedDisplacement,
                        target.Geometry,
                        out NarrowPhaseContact2D contact,
                        includeNonblockingInitialContact: collider.IsTrigger || target.Collider.IsTrigger))
                    {
                        continue;
                    }

                    if (clipped)
                    {
                        // Distance and Fraction measure the full requested displacement.
                        double fraction = contact.Fraction * stop;
                        contact = contact with
                        {
                            Distance = (float)(displacementLength * fraction),
                            Fraction = (float)fraction
                        };
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

    // Every collider's scene bounds and every spatial entry's collision bounds is a DrawRect, so everything a ray
    // can hit lies inside the coordinates a DrawRect holds. A longer ray queries only its part inside that range,
    // cut into rectangles at most half as wide and tall as the largest DrawRect: at most four, because that range
    // is twice as wide as the largest DrawRect.
    private const float MaxSceneCoordinate = DrawArgument.MaxPixelSize;
    private const float MaxRayQueryExtent = DrawArgument.MaxPixelSize / 2;
    private const int MaxRayQueryBoundsCount = 4;

    // The broadphase rectangles that cover the ray from origin to end inside the DrawRect coordinate range.
    // [start, stop] is that part of the ray as fractions of it, the segment the narrow phase tests.
    private static int CreateRayQueryBounds(
        Vector2 origin,
        Vector2 end,
        Span<DrawRect> bounds,
        out double start,
        out double stop)
    {
        start = 0;
        stop = 1;
        if (IsSceneCoordinate(origin) && IsSceneCoordinate(end) &&
            MathF.Abs(end.X - origin.X) <= MaxRayQueryExtent &&
            MathF.Abs(end.Y - origin.Y) <= MaxRayQueryExtent)
        {
            bounds[0] = CreateBounds(origin, end);
            return 1;
        }

        // An end that overflowed moves away from the coordinate range on that axis, so nothing lies on the ray.
        if (!float.IsFinite(end.X) || !float.IsFinite(end.Y))
        {
            return 0;
        }

        double deltaX = (double)end.X - origin.X;
        double deltaY = (double)end.Y - origin.Y;
        if (!ClipRayAxis(origin.X, deltaX, ref start, ref stop) ||
            !ClipRayAxis(origin.Y, deltaY, ref start, ref stop))
        {
            return 0;
        }

        double extent = Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)) * (stop - start);
        int count = Math.Clamp((int)Math.Ceiling(extent / MaxRayQueryExtent), 1, MaxRayQueryBoundsCount);
        for (int index = 0; index < count; index++)
        {
            double first = start + ((stop - start) * index / count);
            double second = index == count - 1 ? stop : start + ((stop - start) * (index + 1) / count);
            double firstX = origin.X + (deltaX * first);
            double firstY = origin.Y + (deltaY * first);
            double secondX = origin.X + (deltaX * second);
            double secondY = origin.Y + (deltaY * second);
            float left = RoundDownToSceneCoordinate(Math.Min(firstX, secondX));
            float top = RoundDownToSceneCoordinate(Math.Min(firstY, secondY));
            bounds[index] = new DrawRect(
                left,
                top,
                GetQueryExtent(left, RoundUpToSceneCoordinate(Math.Max(firstX, secondX))),
                GetQueryExtent(top, RoundUpToSceneCoordinate(Math.Max(firstY, secondY))));
        }

        return count;
    }

    // A move's swept bounds inside the DrawRect coordinate range are cut at 0 on each axis that crosses it, so each
    // piece is at most as wide and tall as the largest DrawRect: at most four.
    private const int MaxMoveQueryBoundsCount = 4;

    // The broadphase rectangles that cover the move of bounds by displacement inside the DrawRect coordinate range.
    // stop is the fraction of the move after which the bounds lie outside that range, so the move hits nothing later.
    private static int CreateMoveQueryBounds(
        DrawRect bounds,
        Vector2 displacement,
        Span<DrawRect> queryBounds,
        out double stop)
    {
        stop = 1;
        float left = MathF.Min(bounds.X, bounds.X + displacement.X);
        float top = MathF.Min(bounds.Y, bounds.Y + displacement.Y);
        float width = MathF.Max(bounds.Right, bounds.Right + displacement.X) - left;
        float height = MathF.Max(bounds.Bottom, bounds.Bottom + displacement.Y) - top;
        if (IsSceneCoordinate(new Vector2(left, top)) &&
            DrawArgument.IsNonNegativeValidPixelSize(width) &&
            DrawArgument.IsNonNegativeValidPixelSize(height) &&
            IsSceneCoordinate(new Vector2(left + width, top + height)))
        {
            queryBounds[0] = new DrawRect(left, top, width, height);
            return 1;
        }

        ClipMoveAxis(bounds.X, bounds.Right, displacement.X, ref stop);
        ClipMoveAxis(bounds.Y, bounds.Bottom, displacement.Y, ref stop);
        Span<float> columns = stackalloc float[3];
        Span<float> rows = stackalloc float[3];
        int columnCount = SplitMoveAxis(bounds.X, bounds.Right, displacement.X, stop, columns);
        int rowCount = SplitMoveAxis(bounds.Y, bounds.Bottom, displacement.Y, stop, rows);
        int count = 0;
        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                queryBounds[count++] = new DrawRect(
                    columns[column],
                    rows[row],
                    GetQueryExtent(columns[column], columns[column + 1]),
                    GetQueryExtent(rows[row], rows[row + 1]));
            }
        }

        return count;
    }

    // Narrows stop to the fraction of the move after which the bounds [low, high] lie outside the coordinate range
    // on one axis. The bounds start inside that range, so stop stays at least 0.
    private static void ClipMoveAxis(float low, float high, float delta, ref double stop)
    {
        if (delta > 0)
        {
            stop = Math.Min(stop, (MaxSceneCoordinate - (double)low) / delta);
        }
        else if (delta < 0)
        {
            stop = Math.Min(stop, (-MaxSceneCoordinate - (double)high) / delta);
        }
    }

    // The edges of the pieces that cover the bounds [low, high] moved by delta up to stop, inside the coordinate
    // range: one piece, or two that meet at 0, so that no piece is wider than the largest DrawRect.
    private static int SplitMoveAxis(float low, float high, float delta, double stop, Span<float> edges)
    {
        double moved = delta * stop;
        float first = RoundDownToSceneCoordinate(Math.Min(low, low + moved));
        float last = RoundUpToSceneCoordinate(Math.Max(high, high + moved));
        edges[0] = first;
        if (first < 0 && last > 0)
        {
            edges[1] = 0;
            edges[2] = last;
            return 2;
        }

        edges[1] = last;
        return 1;
    }

    private static bool IsSceneCoordinate(Vector2 point) =>
        MathF.Abs(point.X) <= MaxSceneCoordinate && MathF.Abs(point.Y) <= MaxSceneCoordinate;

    // Narrows the ray parameter range [start, stop] to the part whose coordinate on one axis is a scene coordinate.
    private static bool ClipRayAxis(double origin, double delta, ref double start, ref double stop)
    {
        if (delta == 0)
        {
            return Math.Abs(origin) <= MaxSceneCoordinate;
        }

        double first = (-MaxSceneCoordinate - origin) / delta;
        double second = (MaxSceneCoordinate - origin) / delta;
        start = Math.Max(start, Math.Min(first, second));
        stop = Math.Min(stop, Math.Max(first, second));
        return start <= stop;
    }

    private static float RoundDownToSceneCoordinate(double value)
    {
        float rounded = (float)value;
        return Math.Clamp(rounded > value ? MathF.BitDecrement(rounded) : rounded, -MaxSceneCoordinate, MaxSceneCoordinate);
    }

    private static float RoundUpToSceneCoordinate(double value)
    {
        float rounded = (float)value;
        return Math.Clamp(rounded < value ? MathF.BitIncrement(rounded) : rounded, -MaxSceneCoordinate, MaxSceneCoordinate);
    }

    private static float RoundUpToFloat(double value)
    {
        float rounded = (float)value;
        return rounded < value ? MathF.BitIncrement(rounded) : rounded;
    }

    // The float extent from start that reaches end without passing the largest scene coordinate.
    private static float GetQueryExtent(float start, float end)
    {
        float extent = end - start;
        while (start + extent < end)
        {
            extent = MathF.BitIncrement(extent);
        }

        while (start + extent > MaxSceneCoordinate)
        {
            extent = MathF.BitDecrement(extent);
        }

        return extent;
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
