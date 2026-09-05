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
            scene.Children.Add(collider);
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
        scene.Children.Add(ellipse);
        scene.Children.Add(corner);
        scene.Children.Add(inside);

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
        scene.Children.Add(actor);
        scene.Children.Add(wall);

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
        scene.Children.Add(segment);
        scene.Children.Add(touching);
        scene.Children.Add(separated);

        Assert.True(scene.CollisionWorld.Intersects(segment, touching));
        Assert.False(scene.CollisionWorld.Intersects(segment, separated));
        CollisionHit2D hit = Assert.Single(
            scene.CollisionWorld.Raycast(new Vector2(-5, 0), Vector2.UnitX, 10));
        Assert.Same(segment, hit.Collider);
        Assert.InRange(hit.Fraction, 0.49f, 0.51f);
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
        scene.Children.Add(source);
        scene.Children.Add(accepted);
        scene.Children.Add(rejected);
        scene.Children.Add(trigger);
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
        village.Children.Add(house);
        root.Children.Add(village);
        root.Children.Add(actor);

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
        scene.Children.Add(collider);
        Assert.Equal(1, scene.CollisionWorld.GetDiagnosticsSnapshot().EntryCount);
        WeakReference node = new(collider);
        WeakReference context = new(dataContext);
        scene.Children.Remove(collider);
        return (scene, node, context);
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
