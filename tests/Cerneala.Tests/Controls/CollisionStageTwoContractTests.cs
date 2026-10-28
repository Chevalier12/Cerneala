using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cerneala.UI.Controls;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class CollisionStageTwoContractTests
{
    [Fact]
    [Trait("CollisionStage", "2")]
    public void BroadphaseResultsMatchExhaustiveOracleOnSeededSmallWorlds()
    {
        const int seed = 0x2D_2026;
        Random random = new(seed);
        Scene2D scene = new();
        List<Collider2D> colliders = [];
        for (int index = 0; index < 72; index++)
        {
            Collider2D collider = CreateRandomCollider(random, index);
            colliders.Add(collider);
            scene.Children.Add(new Sprite2D { Collider = collider });
        }

        CollisionWorld2D world = scene.CollisionWorld;
        foreach (Collider2D source in colliders)
        {
            Collider2D[] expected = colliders
                .Where(target => !ReferenceEquals(source, target) && world.Intersects(source, target))
                .ToArray();
            Collider2D[] actual = world.Overlap(source)
                .Select(static hit => hit.Collider)
                .ToArray();

            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void ExactGeometryRejectsAabbFalsePositiveAndPreservesAffineEllipse()
    {
        CircleCollider2D ellipse = new()
        {
            Radius = 10,
            ScaleX = 3,
            ScaleY = 0.5f,
            SkewX = 0.25f
        };
        CircleCollider2D corner = new()
        {
            Radius = 1,
            TranslateX = 27,
            TranslateY = 4
        };
        CircleCollider2D inside = new()
        {
            Radius = 1,
            TranslateX = 20,
            TranslateY = 1
        };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = ellipse });
        scene.Children.Add(new Sprite2D { Collider = corner });
        scene.Children.Add(new Sprite2D { Collider = inside });

        Assert.False(scene.CollisionWorld.Intersects(ellipse, corner));
        Assert.True(scene.CollisionWorld.Intersects(ellipse, inside));
        Assert.Equal([inside], scene.CollisionWorld.Overlap(ellipse).Select(static hit => hit.Collider));
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void ContinuousMovementCannotTunnelAndDoesNotMutateTheSource()
    {
        CircleCollider2D actor = new()
        {
            Radius = 2,
            TranslateX = -100,
            ScaleX = 1.5f,
            ScaleY = 0.75f
        };
        BoxCollider2D wall = new() { Width = 1, Height = 20, TranslateY = -10 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = actor });
        scene.Children.Add(new Sprite2D { Collider = wall });

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(200, 0));

        Assert.NotNull(result.Collision);
        Assert.Same(wall, result.Collision.Collider);
        Assert.InRange(result.Collision.Fraction, 0.48f, 0.51f);
        Assert.Equal(-100, actor.TranslateX);
        Assert.Equal(new Vector2(200, 0), result.RequestedDisplacement);
        Assert.Equal(result.RequestedDisplacement, result.Travel + result.Remainder);
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void SingularAffineCircleRemainsExactAndRayQueryable()
    {
        CircleCollider2D segment = new() { Radius = 10, ScaleX = 0 };
        CircleCollider2D touching = new() { Radius = 1, TranslateY = 8 };
        CircleCollider2D separated = new() { Radius = 1, TranslateX = 2, TranslateY = 8 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = segment });
        scene.Children.Add(new Sprite2D { Collider = touching });
        scene.Children.Add(new Sprite2D { Collider = separated });

        Assert.True(scene.CollisionWorld.Intersects(segment, touching));
        Assert.False(scene.CollisionWorld.Intersects(segment, separated));
        CollisionHit2D hit = Assert.Single(
            scene.CollisionWorld.Raycast(new Vector2(-5, 0), Vector2.UnitX, 10));
        Assert.Same(segment, hit.Collider);
        Assert.InRange(hit.Fraction, 0.49f, 0.51f);
    }

    [Theory]
    [InlineData(1_000_000f)]
    [InlineData(1e12f)]
    [InlineData(float.MaxValue)]
    public void RaycastWithAHugeFiniteMaximumDistanceReturnsHitsInsteadOfThrowing(float maxDistance)
    {
        // A 10x10 box at (100, 0) lies on the ray from (0, 5) along +X, so every row has exactly one hit.
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = 100, Collider = Box() });
        CollisionHit2D[]? hits = null;

        Exception? failure = Record.Exception(() =>
            hits = scene.CollisionWorld.Raycast(new Vector2(0, 5), Vector2.UnitX, maxDistance));

        string firstFrame = failure?.StackTrace?
            .Split('\n')
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("at Cerneala.", StringComparison.Ordinal)) ?? "none";
        Assert.True(
            failure is null && hits is { Length: 1 },
            $"maxDistance={maxDistance}: " +
            (failure is null
                ? $"{hits?.Length} hit(s)"
                : $"threw {failure.GetType().Name}: {failure.Message} First Cerneala frame: {firstFrame}") + ".");
        Assert.InRange(hits![0].Distance, 99f, 101f);
    }

    [Theory]
    [InlineData(1_000_000f)]
    [InlineData(1e12f)]
    [InlineData(float.MaxValue)]
    public void MoveAndCollideWithAHugeFiniteDisplacementReturnsTheCollisionInsteadOfThrowing(float distance)
    {
        // A 10x10 actor at (0, 0) moves along +X through a 10x10 wall at (100, 0), so it stops after 90 units.
        BoxCollider2D actor = new() { Width = 10, Height = 10 };
        BoxCollider2D wall = new() { Width = 10, Height = 10 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = actor });
        scene.Children.Add(new Sprite2D { X = 100, Collider = wall });
        MoveCollisionResult2D? result = null;

        Exception? failure = Record.Exception(() =>
            result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(distance, 0)));

        Assert.True(
            failure is null && ReferenceEquals(result?.Collision?.Collider, wall),
            $"distance={distance}: " + (failure is null
                ? $"collision={result?.Collision?.Collider?.GetType().Name ?? "null"}"
                : $"threw {failure.GetType().Name}: {failure.Message}"));
        Assert.InRange(result!.Collision!.Distance, 89.9f, 90.1f);
        Assert.InRange(result.Travel.X, 89.9f, 90.1f);
    }

    [Theory]
    [InlineData(-1f, 0f)]
    [InlineData(0f, 1f)]
    [InlineData(0f, -1f)]
    [InlineData(1f, 1f)]
    [InlineData(-1f, -1f)]
    public void MoveAndCollideWithAHugeDisplacementInAnyDirectionStopsAtTheWall(float directionX, float directionY)
    {
        // The wall's near side is 90 units from the actor on each moving axis, so the actor stops after 90 units per axis.
        BoxCollider2D actor = new() { Width = 10, Height = 10 };
        BoxCollider2D wall = new() { Width = 10, Height = 10 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = actor });
        scene.Children.Add(new Sprite2D { X = 100 * directionX, Y = 100 * directionY, Collider = wall });

        foreach (float distance in new[] { 1e12f, float.MaxValue })
        {
            MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(
                actor, new Vector2(directionX * distance, directionY * distance));

            Assert.Same(wall, result.Collision?.Collider);
            Assert.True(float.IsFinite(result.Collision!.Distance), $"Distance={result.Collision.Distance}.");
            Assert.InRange(result.Travel.X * directionX, 89.9f * MathF.Abs(directionX), 90.1f * MathF.Abs(directionX));
            Assert.InRange(result.Travel.Y * directionY, 89.9f * MathF.Abs(directionY), 90.1f * MathF.Abs(directionY));
        }
    }

    [Fact]
    public void MoveAndCollideAcrossMoreThanOneDrawRectOfSceneStopsAtAFarWall()
    {
        // The actor and the wall are 3.8e9 scene units apart, wider than one DrawRect (2e9), so even the
        // part of the move inside the scene's coordinate range needs more than one query rectangle.
        BoxCollider2D actor = new() { Width = 1024, Height = 1024 };
        BoxCollider2D wall = new() { Width = 1024, Height = 1024 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = -1_900_000_000f, Collider = actor });
        scene.Children.Add(new Sprite2D { X = 1_900_000_000f, Collider = wall });

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(1e12f, 0));

        Assert.Same(wall, result.Collision?.Collider);
        Assert.InRange(result.Collision!.Distance, 3.7999e9f, 3.8001e9f);
    }

    [Fact]
    public void MoveAndCollideHitsAWallAtTheEdgeOfTheSceneCoordinateRange()
    {
        // The wall's right side is the largest scene coordinate (2e9). The actor moves 1:12.5 diagonally and touches
        // the wall after 48 % of the move, when its right side is already past 2e9.
        BoxCollider2D actor = new() { Width = 4096, Height = 4096 };
        BoxCollider2D wall = new() { Width = 4096, Height = 4096 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = 1_999_990_016f, Y = -102_400f, Collider = actor });
        scene.Children.Add(new Sprite2D { X = 1_999_995_904f, Collider = wall });

        MoveCollisionResult2D result = scene.CollisionWorld.MoveAndCollide(actor, new Vector2(16_384f, 204_800f));

        Assert.Same(wall, result.Collision?.Collider);
        Assert.InRange(result.Collision!.Fraction, 0.4799f, 0.4801f);
        Assert.InRange(result.Travel.Y, 98_303f, 98_305f);
    }

    [Fact]
    public void RaycastAcrossMoreThanOneDrawRectOfSceneFindsBoxesAtBothEnds()
    {
        // The boxes are 3.8e9 scene units apart, wider than one DrawRect (2e9), so even the
        // part of the ray inside the scene's coordinate range needs more than one query rectangle.
        BoxCollider2D near = new() { Width = 1024, Height = 1024 };
        BoxCollider2D far = new() { Width = 1024, Height = 1024 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = -1_900_000_000f, Collider = near });
        scene.Children.Add(new Sprite2D { X = 1_900_000_000f, Collider = far });

        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(
            new Vector2(-1_950_000_000f, 512), Vector2.UnitX, 1e12f);

        Assert.Equal([near, far], hits.Select(static hit => hit.Collider));
    }

    [Fact]
    public void DiagonalRaycastFromOutsideTheSceneCoordinateRangeFindsABoxInside()
    {
        BoxCollider2D box = new() { Width = 1024, Height = 1024 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = box });

        CollisionHit2D hit = Assert.Single(scene.CollisionWorld.Raycast(
            new Vector2(-3_000_000_000f, -3_000_000_000f), Vector2.One, 1e12f));

        Assert.Same(box, hit.Collider);
        Assert.InRange(hit.Distance, 4.24e9f, 4.25e9f);
    }

    [Theory]
    [InlineData(0f, 1e12f, 1f)]
    [InlineData(0f, float.MaxValue, 1f)]
    [InlineData(-1e20f, 1e21f, 512f)]
    [InlineData(-1e20f, float.MaxValue, 512f)]
    public void RaycastWithAHugeRayReturnsAFiniteHitOnACircle(float originX, float maxDistance, float pointTolerance)
    {
        // A circle of radius 5 centered at (100, 0) lies on the ray along +X, so the ray enters it at x = 95.
        // The point tolerance is the float spacing of the DrawRect coordinate range the ray enters from.
        CircleCollider2D circle = new() { Radius = 5 };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = 100, Collider = circle });

        CollisionHit2D hit = Assert.Single(scene.CollisionWorld.Raycast(
            new Vector2(originX, 0), Vector2.UnitX, maxDistance));

        Assert.True(
            float.IsFinite(hit.Distance) && float.IsFinite(hit.Fraction) &&
            float.IsFinite(hit.Point.X) && float.IsFinite(hit.Point.Y),
            $"Distance={hit.Distance}, Fraction={hit.Fraction}, Point={hit.Point}.");
        Assert.Same(circle, hit.Collider);
        Assert.InRange(hit.Point.X, 95 - pointTolerance, 95 + pointTolerance);
        float expectedDistance = 95 - originX;
        Assert.InRange(hit.Distance, expectedDistance - pointTolerance, expectedDistance + pointTolerance);
        Assert.InRange(hit.Fraction * maxDistance, hit.Distance * 0.9999f, hit.Distance * 1.0001f);
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void RaycastEdgeContactToleranceIsOneEMinusFiveSceneUnitsOnAUnitLengthRay()
    {
        // Control: with maxDistance 1 the fraction tolerance equals the documented 1e-5 scene units.
        Scene2D tooFar = new();
        tooFar.Children.Add(new Sprite2D { X = 1 + 1e-4f, Collider = Box() });
        Scene2D withinEpsilon = new();
        withinEpsilon.Children.Add(new Sprite2D { X = 1 + 1e-6f, Collider = Box() });

        Assert.Empty(tooFar.CollisionWorld.Raycast(new Vector2(0, 5), Vector2.UnitX, 1));
        Assert.Single(withinEpsilon.CollisionWorld.Raycast(new Vector2(0, 5), Vector2.UnitX, 1));
    }

    [Fact]
    public void RaycastDoesNotHitAFaceBeyondTheRayEndOnALongRay()
    {
        // The box face is at x = 10000.05, 0.05 units beyond the end of the ray.
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = 10000.05f, Collider = Box() });

        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(new Vector2(0, 5), Vector2.UnitX, 10000);

        Assert.True(hits.Length == 0, "Unexpected hit: " + Describe(hits));
    }

    [Fact]
    public void RaycastDoesNotHitABoxTheLongDiagonalLineMissesByAboutTwoAndAHalfUnits()
    {
        // The line y = x passes the box x = 14..24, y = 0..10 above its top-left corner (14, 10).
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = 14, Y = 0, Collider = Box() });

        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(Vector2.Zero, Vector2.Normalize(new Vector2(1, 1)), 1_000_000);

        Assert.True(hits.Length == 0, "Unexpected hit: " + Describe(hits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RaycastDoesNotHitACircleOrSegmentBeyondTheRayEndOnALongRay(bool segment)
    {
        // The left edge of the circle of radius 5000, or the vertical segment, is at x = 10000.05,
        // 0.05 units beyond the end of the ray.
        Scene2D scene = new();
        scene.Children.Add(segment
            ? new Sprite2D { X = 10000.05f, Y = -5, Collider = new SegmentCollider2D { EndY = 10, EndX = 0 } }
            : new Sprite2D { X = 15000.05f, Collider = new CircleCollider2D { Radius = 5000 } });

        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(Vector2.Zero, Vector2.UnitX, 10000);

        Assert.True(hits.Length == 0, "Unexpected hit: " + Describe(hits));
    }

    [Fact]
    public void RaycastDoesNotHitALongSegmentThatEndsBeforeTheRay()
    {
        // The segment runs from (0, 0) to (10000, 0); the vertical ray at x = 10000.05 passes 0.05 units beyond its end.
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = new SegmentCollider2D { EndX = 10000, EndY = 0 } });

        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(new Vector2(10000.05f, -5), Vector2.UnitY, 10);

        Assert.True(hits.Length == 0, "Unexpected hit: " + Describe(hits));
    }

    [Fact]
    public void RaycastDoesNotHitACircleThatAShortRayPassesTwoUnitsAway()
    {
        // The ray from (0, 3) is 0.001 long; the circle of radius 1 at (0.0005, 0) is 2 units below it.
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { X = 0.0005f, Collider = new CircleCollider2D { Radius = 1 } });

        CollisionHit2D[] hits = scene.CollisionWorld.Raycast(new Vector2(0, 3), Vector2.UnitX, 0.001f);

        Assert.True(hits.Length == 0, "Unexpected hit: " + Describe(hits));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverlapOrderDoesNotDependOnWhetherTheWorldWasQueriedBeforeAnInsert(bool showInsteadOfInsert)
    {
        // Sprites a (X = 0), p (X = 2) and b (X = 5), each with a 10x10 box. b is inserted first in the tree,
        // or is first in the tree from the start but hidden and then shown.
        string initializedOrder = BuildAndQuery(queryBeforeChange: true, showInsteadOfInsert);
        string lazyOrder = BuildAndQuery(queryBeforeChange: false, showInsteadOfInsert);

        Assert.True(
            initializedOrder == lazyOrder,
            $"Overlap order with the world initialized before the change: [{initializedOrder}]; without: [{lazyOrder}].");
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void LayerMaskTriggerAndQueryFiltersRunBeforeExactTests()
    {
        BoxCollider2D source = Box(layer: 1, mask: 2);
        BoxCollider2D accepted = Box(layer: 2, mask: 1);
        BoxCollider2D rejected = Box(layer: 4, mask: uint.MaxValue);
        BoxCollider2D trigger = Box(layer: 2, mask: 1);
        trigger.IsTrigger = true;
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D { Collider = source });
        scene.Children.Add(new Sprite2D { Collider = accepted });
        scene.Children.Add(new Sprite2D { Collider = rejected });
        scene.Children.Add(new Sprite2D { Collider = trigger });
        CollisionWorld2D world = scene.CollisionWorld;
        CollisionWorld2DDiagnosticsSnapshot before = world.GetDiagnosticsSnapshot();

        CollisionHit2D[] hits = world.Overlap(
            source,
            new CollisionQuery2D(includeTriggers: false));
        CollisionWorld2DDiagnosticsSnapshot after = world.GetDiagnosticsSnapshot();

        Assert.Equal([accepted], hits.Select(static hit => hit.Collider));
        Assert.Equal(1, after.ExactTestCount - before.ExactTestCount);
        Assert.Empty(world.Overlap(source, new CollisionQuery2D(collisionMask: 0)));
        Assert.Empty(world.Overlap(source, new CollisionQuery2D(exclude: accepted, includeTriggers: false)));
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void RootOwnsOneWorldAndMutationsUpdateOnlyAffectedEntries()
    {
        Scene2D root = new();
        Scene2D village = new();
        BoxCollider2D house = Box();
        CircleCollider2D actor = new() { Radius = 3, TranslateX = 50 };
        village.Children.Add(new Sprite2D { Collider = house });
        root.Children.Add(village);
        root.Children.Add(new Sprite2D { Collider = actor });

        Assert.Same(root.CollisionWorld, village.CollisionWorld);
        CollisionWorld2DDiagnosticsSnapshot initial = root.CollisionWorld.GetDiagnosticsSnapshot();
        actor.TranslateX = 1;
        CollisionWorld2DDiagnosticsSnapshot moved = root.CollisionWorld.GetDiagnosticsSnapshot();

        Assert.Equal(initial.RebuildCount, moved.RebuildCount);
        Assert.Equal(initial.IncrementalUpdateCount + 1, moved.IncrementalUpdateCount);
        Assert.Equal(initial.UpdatedEntryCount + 1, moved.UpdatedEntryCount);
        Assert.Equal(2, moved.EntryCount);

        root.Children.Remove(village);
        Assert.Equal(1, root.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        Assert.Equal(1, village.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        root.Children.Insert(0, village);
        Assert.Equal(2, root.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void DetachClearsIndexReferencesAndDoesNotRetainNodeOrDataContext()
    {
        (Scene2D scene, WeakReference node, WeakReference dataContext) = CreateDetachedReferences();

        ForceCollection();

        Assert.Equal(0, scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        Assert.False(node.IsAlive);
        Assert.False(dataContext.IsAlive);
    }

    [Fact]
    [Trait("CollisionStage", "2")]
    public void PublicQueryAndResultObjectsAreImmutable()
    {
        Type[] immutableTypes =
        [
            typeof(CollisionQuery2D),
            typeof(CollisionHit2D),
            typeof(MoveCollisionResult2D),
            typeof(CollisionWorld2DDiagnosticsSnapshot)
        ];

        foreach (Type type in immutableTypes)
        {
            PropertyInfo[] writable = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(static property => property.SetMethod is not null)
                .ToArray();
            Assert.Empty(writable);
        }

        CollisionQuery2D defaults = default;
        Assert.Equal(uint.MaxValue, defaults.CollisionLayer);
        Assert.Equal(uint.MaxValue, defaults.CollisionMask);
        Assert.True(defaults.IncludeTriggers);
        Assert.Null(defaults.Exclude);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Scene2D Scene, WeakReference Node, WeakReference DataContext) CreateDetachedReferences()
    {
        Scene2D scene = new();
        BoxCollider2D collider = Box();
        object dataContext = new();
        collider.DataContext = dataContext;
        Sprite2D sprite = new() { Collider = collider };
        scene.Children.Add(sprite);
        Assert.Equal(1, scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        WeakReference node = new(collider);
        WeakReference context = new(dataContext);
        scene.Children.Remove(sprite);
        return (scene, node, context);
    }

    private static string Describe(CollisionHit2D[] hits) =>
        string.Join("; ", hits.Select(static hit => $"Point={hit.Point}, Distance={hit.Distance}, Fraction={hit.Fraction}"));

    private static string BuildAndQuery(bool queryBeforeChange, bool showInsteadOfInsert)
    {
        Sprite2D a = new() { X = 0, Collider = Box() };
        Sprite2D p = new() { X = 2, Collider = Box() };
        Sprite2D b = new() { X = 5, Collider = Box(), IsVisible = !showInsteadOfInsert };
        Scene2D scene = new();
        if (showInsteadOfInsert) { scene.Children.Add(b); }
        scene.Children.Add(a);
        scene.Children.Add(p);
        if (queryBeforeChange) { scene.CollisionWorld.Overlap(p.Collider!); }

        if (showInsteadOfInsert) { b.IsVisible = true; }
        else { scene.Children.Insert(0, b); }

        return string.Join(",", scene.CollisionWorld.Overlap(p.Collider!)
            .Select(hit => ReferenceEquals(hit.Collider, a.Collider) ? "a" : ReferenceEquals(hit.Collider, b.Collider) ? "b" : "?"));
    }

    private static void ForceCollection()
    {
        for (int iteration = 0; iteration < 3; iteration++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static Collider2D CreateRandomCollider(Random random, int index)
    {
        Collider2D collider = (index % 3) switch
        {
            0 => new BoxCollider2D
            {
                Width = 1 + (float)random.NextDouble() * 12,
                Height = 1 + (float)random.NextDouble() * 12
            },
            1 => new CircleCollider2D
            {
                Radius = 0.5f + (float)random.NextDouble() * 6
            },
            _ => new PolygonCollider2D { Points = "0,0 8,1 6,7 1,6" }
        };
        collider.TranslateX = random.Next(-100, 101);
        collider.TranslateY = random.Next(-100, 101);
        collider.Rotation = (float)(random.NextDouble() * Math.PI);
        collider.ScaleX = 0.5f + (float)random.NextDouble() * 1.5f;
        collider.ScaleY = 0.5f + (float)random.NextDouble() * 1.5f;
        collider.CollisionLayer = 1u << random.Next(0, 3);
        collider.CollisionMask = (uint)random.Next(1, 8);
        collider.IsTrigger = index % 11 == 0;
        return collider;
    }

    private static BoxCollider2D Box(uint layer = 1, uint mask = uint.MaxValue) => new()
    {
        Width = 10,
        Height = 10,
        CollisionLayer = layer,
        CollisionMask = mask
    };
}
