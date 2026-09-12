using System.Collections.ObjectModel;
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
            new BoxCollider2D(), new Sprite2D(), new TileInstance2D()
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
    public void SpriteOwnsEveryColliderKindThroughItsTypedCollection(string shape)
    {
        Sprite2D sprite = CreateSprite(8, 9);
        Collider2D collider = CreateCollider(shape);
        GetColliders(sprite).Add(collider);
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
        GetColliders(sprite).Add(collider);
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
    public void OwnedColliderCollectionRejectsDuplicateAndGenericRemovalWithoutCorruptingOwnership()
    {
        Sprite2D first = CreateSprite();
        Sprite2D second = CreateSprite();
        BoxCollider2D firstCollider = new();
        BoxCollider2D secondCollider = new();
        Collection<Collider2D> firstColliders = GetColliders(first);
        Collection<Collider2D> secondColliders = GetColliders(second);
        firstColliders.Add(firstCollider);
        secondColliders.Add(secondCollider);

        Assert.Throws<InvalidOperationException>(() => firstColliders.Add(firstCollider));
        Assert.Throws<InvalidOperationException>(() => secondColliders[0] = firstCollider);
        Assert.Throws<InvalidOperationException>(() => first.LogicalChildren.Remove(firstCollider));
        Assert.Same(firstCollider, Assert.Single(firstColliders));
        Assert.Same(first, firstCollider.LogicalParent);
        Assert.Same(secondCollider, Assert.Single(secondColliders));
        Assert.Same(second, secondCollider.LogicalParent);

        firstColliders.Remove(firstCollider);
        secondColliders.Add(firstCollider);
        Assert.Empty(firstColliders);
        Assert.Equal(2, secondColliders.Count);
        Assert.Same(second, firstCollider.LogicalParent);
    }

    [Fact]
    public void SpritePoseAndLifecycleKeepTheWorldCurrentWithoutDuplicateEntries()
    {
        Scene2D first = new();
        Scene2D second = new();
        Sprite2D sprite = CreateSprite(10, 0);
        BoxCollider2D collider = new() { Width = 4, Height = 4 };
        GetColliders(sprite).Add(collider);
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
        GetColliders(sprite).Clear();
        Assert.Null(collider.LogicalParent);
        Assert.Empty(first.CollisionWorld.Raycast(new Vector2(0, 2), Vector2.UnitX, 40));
        root.VisualChildren.Remove(surface);
    }

    [Fact]
    public void FreePlacementTileCopiesItsDescriptorsAndKeepsCollisionIndependentOfDrawSize()
    {
        TileColliderDescriptor2D descriptor = new(TileColliderShape2D.Box, width: 32, height: 32);
        TileColliderDescriptor2D[] input = [descriptor];
        Tile tile = CreateTile(input, 10, 20, 64, 96);
        input[0] = new TileColliderDescriptor2D(TileColliderShape2D.Circle);
        IReadOnlyList<TileColliderDescriptor2D> stored = GetTileColliders(tile);
        Assert.Same(descriptor, Assert.Single(stored));
        Assert.False(typeof(UIElement).IsAssignableFrom(typeof(Tile)));
        Assert.Throws<NotSupportedException>(() => ((IList<TileColliderDescriptor2D>)stored).Clear());

        TileMap2D map = new() { Model = new TileMap2DModel([tile]), TranslateX = 5 };
        Scene2D scene = new();
        scene.Children.Add(map);
        CollisionHit2D hit = Assert.Single(scene.CollisionWorld.Raycast(new Vector2(0, 30), Vector2.UnitX, 100));
        AssertBounds(hit.Collider, new DrawRect(15, 20, 32, 32));
        Assert.Empty(scene.CollisionWorld.Raycast(new Vector2(0, 80), Vector2.UnitX, 100));
        Assert.Throws<InvalidOperationException>(() => scene.Children.Add(hit.Collider));
        Assert.Throws<InvalidOperationException>(() => GetColliders(CreateSprite()).Add(hit.Collider));
        map.Model = new TileMap2DModel([new Tile(tile.Image, 10, 20, 64, 96)]);
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

    private static Collection<Collider2D> GetColliders(Sprite2D sprite)
    {
        PropertyInfo? property = typeof(Sprite2D).GetProperty("Colliders");
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<Collection<Collider2D>>(property.GetValue(sprite));
    }

    private static Tile CreateTile(IEnumerable<TileColliderDescriptor2D> colliders, float x, float y, float width, float height)
    {
        ConstructorInfo? constructor = typeof(Tile).GetConstructor(
            [typeof(ImageReference), typeof(IEnumerable<TileColliderDescriptor2D>), typeof(float), typeof(float), typeof(float), typeof(float)]);
        Assert.NotNull(constructor);
        return (Tile)constructor.Invoke([new ImageReference(new TestImage()), colliders, x, y, width, height]);
    }

    private static IReadOnlyList<TileColliderDescriptor2D> GetTileColliders(Tile tile)
    {
        PropertyInfo? property = typeof(Tile).GetProperty("Colliders");
        Assert.NotNull(property);
        return Assert.IsAssignableFrom<IReadOnlyList<TileColliderDescriptor2D>>(property.GetValue(tile));
    }

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
