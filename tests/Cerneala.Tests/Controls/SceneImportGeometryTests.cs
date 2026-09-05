using System.Numerics;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneImportGeometryTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [Trait("SceneImportStage", "1")]
    public void RectangularTileFlipPreservesUvCornersAndCollisionBeforeAndAfterPromotion(int flags)
    {
        DrawSize size = new(20, 10);
        TileColliderDescriptor2D descriptor = new(TileColliderShape2D.Box, width: 4, height: 2, offsetX: 2, offsetY: 1);
        TileSet2D tileset = new("Atlas", new ResourceId<ImageResource>("Atlas"),
            [new TileDefinition2D(1, new DrawRect(0, 0, 20, 10), colliders: [descriptor])]);
        TileMap2D map = new() { Model = new(size, [tileset],
            [new TileLayer2DModel("Tiles", [new TileChunk2D(default, 1, 1, [new TileCell2D(1, (TileFlip2D)flags)])])]) };
        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new() { Scene = scene };
        surface.Resources.SetResource(new ResourceId<ImageResource>("Atlas"), new ImageResource("atlas"));
        UIRoot root = new();
        root.SetImageLoader(new ImageLoader());
        root.VisualChildren.Add(surface);

        DrawCommandList commands = Record(surface);
        DrawSprite2D sprite = Assert.Single(Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawSpriteBatch)).SpriteBatch!.Sprites);
        AssertCorners(sprite.Destination, sprite.Options, flags);
        Vector2 interior = Flip(new Vector2(0.2f, 0.2f), flags) * new Vector2(20, 10);
        Vector2 exterior = Flip(new Vector2(0.8f, 0.8f), flags) * new Vector2(20, 10);
        Assert.Single(OverlapCircle(scene, interior, 0.1f));
        Assert.Empty(OverlapCircle(scene, exterior, 0.1f));

        TileInstance2D promoted = map.Promote(new TileCellKey2D("Tiles", 0, 0));
        promoted.Flip = (TileFlip2D)flags;
        DrawCommand draw = Assert.Single(Record(surface).Where(command => command.Kind == DrawCommandKind.DrawImage));
        AssertCorners(draw.Rect, new DrawImageOptions(draw.ImageSource, rotation: draw.ImageRotation, origin: draw.ImageOrigin, flip: draw.ImageFlip), flags);
        Assert.Single(OverlapCircle(scene, interior, 0.1f));
        Assert.True(map.Demote(new TileCellKey2D("Tiles", 0, 0)));
        Assert.Single(Record(surface).Where(command => command.Kind == DrawCommandKind.DrawSpriteBatch));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("SceneImportStage", "1")]
    public void SegmentRaycastHonorsEndpointsCollinearityAndTransforms(bool rotated)
    {
        Collider2D segment = Segment();
        segment.GetType().GetProperty("EndX")!.SetValue(segment, 10f);
        if (rotated) { segment.Rotation = MathF.PI / 2; }
        Scene2D scene = new();
        scene.Children.Add(segment);
        Matrix3x2 transform = rotated ? Matrix3x2.CreateRotation(MathF.PI / 2) : Matrix3x2.Identity;
        Vector2 Point(float x, float y) => Vector2.Transform(new Vector2(x, y), transform);
        Vector2 Direction(float x, float y) => Vector2.TransformNormal(new Vector2(x, y), transform);
        Assert.Empty(scene.CollisionWorld.Raycast(Point(11, -5), Direction(0, 1), 10));
        CollisionHit2D hit = Assert.Single(scene.CollisionWorld.Raycast(Point(10, -5), Direction(0, 1), 10));
        Assert.InRange(hit.Distance, 4.999f, 5.001f);
        Assert.Single(scene.CollisionWorld.Raycast(Point(-5, 0), Direction(1, 0), 10));
        Assert.Empty(OverlapCircle(scene, Point(12, 0), 1));
        Assert.Single(OverlapCircle(scene, Point(10.5f, 0), 1));
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void SegmentsDoNotIntersectWhenTheyAreCollinearButDisjoint()
    {
        Collider2D first = Segment();
        Collider2D second = Segment();
        second.TranslateX = 2;
        Assert.True(first.TryGetSceneGeometry(out ColliderGeometry2D a));
        Assert.True(second.TryGetSceneGeometry(out ColliderGeometry2D b));
        Assert.False(CollisionNarrowPhase2D.Intersects(a, b));
        second.TranslateX = 0.5f;
        Assert.True(second.TryGetSceneGeometry(out b));
        Assert.True(CollisionNarrowPhase2D.Intersects(a, b));
    }

    [Fact]
    [Trait("SceneImportStage", "1")]
    public void TileColliderDescriptorRetainsAffineEllipseWithoutApproximation()
    {
        ConstructorInfo constructor = Assert.Single(typeof(TileColliderDescriptor2D).GetConstructors()
            .Where(candidate => candidate.GetParameters().Any(parameter => parameter.Name == "localTransform")));
        object?[] arguments = constructor.GetParameters().Select(parameter => parameter.DefaultValue).ToArray();
        arguments[0] = TileColliderShape2D.Circle;
        arguments[Array.FindIndex(constructor.GetParameters(), parameter => parameter.Name == "localTransform")] = Matrix3x2.CreateScale(10, 2) * Matrix3x2.CreateTranslation(12, 3);
        TileColliderDescriptor2D descriptor = (TileColliderDescriptor2D)constructor.Invoke(arguments);
        TileStaticCollider2D collider = new(descriptor, default, new DrawSize(32, 16), TileFlip2D.None);
        Scene2D scene = new();
        scene.Children.Add(collider);
        CollisionHit2D horizontal = Assert.Single(scene.CollisionWorld.Raycast(new Vector2(0, 3), Vector2.UnitX, 32));
        CollisionHit2D vertical = Assert.Single(scene.CollisionWorld.Raycast(new Vector2(12, 0), Vector2.UnitY, 16));
        Assert.InRange(horizontal.Distance, 1.999f, 2.001f);
        Assert.InRange(vertical.Distance, 0.999f, 1.001f);
        Assert.Empty(OverlapCircle(scene, new Vector2(21, 4.8f), 0.01f));
    }

    private static CollisionHit2D[] OverlapCircle(Scene2D scene, Vector2 center, float radius)
    {
        CircleCollider2D probe = new() { Radius = radius, TranslateX = center.X, TranslateY = center.Y };
        scene.Children.Add(probe);
        try { return scene.CollisionWorld.Overlap(probe); }
        finally { scene.Children.Remove(probe); }
    }

    private static Collider2D Segment()
    {
        Type? type = typeof(Scene2D).Assembly.GetType("Cerneala.UI.Controls.SegmentCollider2D");
        Assert.True(type is not null, "RED: SegmentCollider2D is absent.");
        return (Collider2D)Activator.CreateInstance(type)!;
    }

    private static void AssertCorners(DrawRect destination, DrawImageOptions options, int flags)
    {
        Image image = new();
        DrawPoint[] corners = DrawImageGeometry.GetDestinationCorners(image, destination, options);
        DrawPoint[] texture = DrawImageGeometry.GetTextureCoordinates(image, options);
        for (int index = 0; index < corners.Length; index++)
        {
            Vector2 expected = Flip(new Vector2(texture[index].X, texture[index].Y), flags) * new Vector2(20, 10);
            Assert.InRange(Vector2.Distance(new Vector2(corners[index].X, corners[index].Y), expected), 0, 0.0001f);
        }
    }

    private static Vector2 Flip(Vector2 point, int flags)
    {
        if ((flags & 4) != 0) { point = new Vector2(point.Y, point.X); }
        if ((flags & 1) != 0) { point.X = 1 - point.X; }
        if ((flags & 2) != 0) { point.Y = 1 - point.Y; }
        return point;
    }

    private static DrawCommandList Record(RenderSurface2D surface)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 40, 40));
        return commands;
    }

    private sealed class Image : IDrawImage
    {
        public int Width => 20;
        public int Height => 10;
    }

    private sealed class ImageLoader : IImageLoader
    {
        public IDrawImage Load(string path) => new Image();
    }
}
