using System.Numerics;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class ColliderOwnershipTests
{
    [Theory]
    [InlineData(typeof(Sprite2D), typeof(Collider2D))]
    [InlineData(typeof(Tile), typeof(TileColliderDescriptor2D))]
    [InlineData(typeof(TileDefinition2D), typeof(TileColliderDescriptor2D))]
    [InlineData(typeof(Scene2DEntity), typeof(TileColliderDescriptor2D))]
    public void EveryPublicColliderOwnerExposesOneSingularSlot(Type ownerType, Type colliderType)
    {
        PropertyInfo? collider = ownerType.GetProperty("Collider");
        Assert.NotNull(collider);
        Assert.Equal(colliderType, collider.PropertyType);
        Assert.Null(ownerType.GetProperty("Colliders"));
    }

    [Theory]
    [InlineData("Box")]
    [InlineData("Circle")]
    [InlineData("Polygon")]
    [InlineData("Segment")]
    public void EveryColliderKindRejectsSceneAndGenericTreeOwnership(string shape)
    {
        Collider2D collider = CreateCollider(shape);
        Scene2D scene = new();
        Assert.Throws<InvalidOperationException>(() => scene.Children.Add(collider));
        Assert.Empty(scene.Children);
        Assert.Empty(scene.LogicalChildren);
        Assert.Null(collider.LogicalParent);

        foreach (UIElement owner in new UIElement[]
        {
            new UIElement(), scene, new RenderSurface2D(), new Border(),
            new BoxCollider2D(), new Sprite2D()
        })
        {
            Assert.Throws<InvalidOperationException>(() => owner.LogicalChildren.Add(collider));
            Assert.Throws<InvalidOperationException>(() => owner.VisualChildren.Add(collider));
            Assert.Empty(owner.LogicalChildren);
            Assert.Empty(owner.VisualChildren);
            Assert.Null(collider.LogicalParent);
            Assert.Null(collider.VisualParent);
        }
    }

    [Theory]
    [InlineData("Box")]
    [InlineData("Circle")]
    [InlineData("Polygon")]
    [InlineData("Segment")]
    public void SpriteOwnsEveryColliderKindThroughItsSingularSlot(string shape)
    {
        Sprite2D sprite = CreateSprite(8, 9);
        Collider2D collider = CreateCollider(shape);
        sprite.Collider = collider;
        Scene2D scene = new() { TranslateX = 20, Scale = 2 };
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene };

        Assert.Same(sprite, collider.LogicalParent);
        Assert.Same(surface, collider.Surface);
        Assert.Null(collider.VisualParent);
        Assert.Same(collider, Assert.Single(sprite.LogicalChildren));
        Assert.Empty(sprite.VisualChildren);
        Assert.True(collider.TryGetActiveSceneGeometry(out ColliderGeometry2D geometry));
        Assert.Equal(new Vector2(36, 18), Vector2.Transform(Vector2.Zero, geometry.ShapeToSceneTransform));
    }

    [Fact]
    public void SpriteDimensionsAndImagePresentationDoNotManageColliderGeometry()
    {
        Sprite2D sprite = CreateSprite(10, 20);
        BoxCollider2D collider = new() { Width = 32, Height = 32 };
        sprite.Collider = collider;
        Scene2D scene = new();
        scene.Children.Add(sprite);
        AssertBounds(collider, new DrawRect(10, 20, 32, 32));

        sprite.Width = 64;
        sprite.Height = 96;
        sprite.Origin = new DrawPoint(8, 8);
        sprite.Flip = RenderSurface2DSpriteFlip.Horizontal;
        AssertBounds(collider, new DrawRect(10, 20, 32, 32));
        Assert.Equal(32, collider.Width);
        Assert.Equal(32, collider.Height);

        sprite.X = 40;
        sprite.Y = 50;
        sprite.Rotation = MathF.PI / 2;
        AssertBounds(collider, new DrawRect(8, 50, 32, 32));
        collider.Width = 16;
        AssertBounds(collider, new DrawRect(8, 50, 32, 16));
        scene.Scale = 2;
        AssertBounds(collider, new DrawRect(16, 100, 64, 32));
    }

    [Fact]
    public void InvalidReplacementCannotDetachThePreviousSceneNode()
    {
        Scene2D scene = new();
        Sprite2D sprite = CreateSprite();
        scene.Children.Add(sprite);
        long before = scene.CollisionMutationVersion;
        Assert.Throws<InvalidOperationException>(() => scene.Children[0] = new BoxCollider2D());
        Assert.Same(sprite, Assert.Single(scene.Children));
        Assert.Same(sprite, Assert.Single(scene.LogicalChildren));
        Assert.Same(scene, sprite.LogicalParent);
        Assert.Equal(before, scene.CollisionMutationVersion);
    }

    [Fact]
    public void ColliderReplacementIsAtomicAndGenericRemovalCannotCorruptOwnership()
    {
        Sprite2D first = CreateSprite();
        Sprite2D second = CreateSprite();
        BoxCollider2D firstCollider = new();
        BoxCollider2D secondCollider = new();
        first.Collider = firstCollider;
        second.Collider = secondCollider;

        first.Collider = firstCollider;
        Assert.Throws<InvalidOperationException>(() => second.Collider = firstCollider);
        Assert.Throws<InvalidOperationException>(() => first.LogicalChildren.Remove(firstCollider));
        Assert.Same(firstCollider, first.Collider);
        Assert.Same(first, firstCollider.LogicalParent);
        Assert.Same(secondCollider, second.Collider);
        Assert.Same(second, secondCollider.LogicalParent);

        first.Collider = null;
        second.Collider = firstCollider;
        Assert.Null(first.Collider);
        Assert.Same(firstCollider, second.Collider);
        Assert.Same(second, firstCollider.LogicalParent);
        Assert.Null(secondCollider.LogicalParent);
    }

    [Fact]
    public void SpritePoseAndLifecycleKeepTheWorldCurrentWithoutDuplicateEntries()
    {
        Scene2D first = new();
        Scene2D second = new();
        Sprite2D sprite = CreateSprite(10, 0);
        BoxCollider2D collider = new() { Width = 4, Height = 4 };
        sprite.Collider = collider;
        first.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = first };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        Assert.Same(sprite, Assert.Single(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40)).Entity);

        long before = first.CollisionMutationVersion;
        sprite.X = 20;
        Assert.Equal(before + 1, first.CollisionMutationVersion);
        Assert.Equal(20, Assert.Single(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40)).Distance, 3);
        sprite.IsVisible = false;
        Assert.Empty(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));
        sprite.IsVisible = true;
        Assert.Single(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));

        first.Children.Remove(sprite);
        Assert.Null(collider.Root);
        Assert.Null(collider.Surface);
        Assert.Empty(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));
        second.Children.Add(sprite);
        Assert.Single(second.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));
        second.Children.Remove(sprite);
        first.Children.Add(sprite);
        Assert.Same(root, collider.Root);
        Assert.Same(surface, collider.Surface);
        Assert.Single(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));
        sprite.Collider = null;
        Assert.Null(collider.LogicalParent);
        Assert.Empty(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    public async Task FreePlacementTileStoresOneImmutableDescriptorAndKeepsCollisionIndependentOfDrawSize()
    {
        TileColliderDescriptor2D descriptor = new(TileColliderShape2D.Box, width: 32, height: 32);
        Tile tile = new(new ImageReference(new TestImage()), descriptor, 10, 20, 64, 96);
        Assert.Same(descriptor, tile.Collider);
        Assert.False(typeof(UIElement).IsAssignableFrom(typeof(Tile)));
        Assert.Null(typeof(Tile).GetProperty(nameof(Tile.Collider))!.SetMethod);

        TileMap2D map = new() { Source = TileMapTestSource.Create(new TileMap2DModel([tile])), TranslateX = 5 };
        Scene2D scene = new();
        scene.Children.Add(map);
        using SceneSimulationContext2D context = new(scene);
        using SceneCollisionRegion2D prepared = await scene.CollisionWorld.PrepareRegionAsync(new(0, 0, 100, 100));
        CollisionHit2D hit = Assert.Single(scene.CollisionWorld.Raycast(new Vector2(0, 30), Vector2.UnitX, 100));
        AssertBounds(hit.Collider, new DrawRect(15, 20, 32, 32));
        Assert.Empty(scene.CollisionWorld.Raycast(new Vector2(0, 80), Vector2.UnitX, 100));
        Assert.Throws<InvalidOperationException>(() => scene.Children.Add(hit.Collider));
        Assert.Throws<InvalidOperationException>(() => CreateSprite().Collider = hit.Collider);
        map.Source = TileMapTestSource.Create(new TileMap2DModel([new Tile(tile.Image, 10, 20, 64, 96)]));
        Assert.Empty(scene.CollisionWorld.Raycast(new Vector2(0, 30), Vector2.UnitX, 100));
    }

    private static Collider2D CreateCollider(string shape) => shape switch
    {
        "Box" => new BoxCollider2D { Width = 32, Height = 32 },
        "Circle" => new CircleCollider2D { Radius = 16 },
        "Polygon" => new PolygonCollider2D { Points = "0,0 32,0 0,32" },
        "Segment" => new SegmentCollider2D { EndX = 32 },
        _ => throw new ArgumentOutOfRangeException(nameof(shape))
    };

    private static Sprite2D CreateSprite(float x = 0, float y = 0) => new()
    {
        Image = new ImageReference(new TestImage()), X = x, Y = y, Width = 32, Height = 32
    };

    private static void AssertBounds(Collider2D collider, DrawRect expected)
    {
        Assert.True(collider.TryGetSceneGeometry(out ColliderGeometry2D geometry));
        Assert.Equal(expected.X, geometry.SceneBounds.X, 3);
        Assert.Equal(expected.Y, geometry.SceneBounds.Y, 3);
        Assert.Equal(expected.Width, geometry.SceneBounds.Width, 3);
        Assert.Equal(expected.Height, geometry.SceneBounds.Height, 3);
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 32;
        public int Height => 32;
    }
}
