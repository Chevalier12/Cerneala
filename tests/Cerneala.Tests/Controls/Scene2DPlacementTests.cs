using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class Scene2DPlacementTests
{
    [Fact]
    public void GroupPlacementRecordsOneTranslationTransform()
    {
        Scene2D root = new();
        Scene2D house = new() { X = 10, Y = 20 };
        house.Children.Add(new Sprite2D { Image = new(new TestImage()), X = 2, Y = 3, Width = 4, Height = 5 });
        root.Children.Add(house);
        RenderSurface2D surface = new() { Scene = root, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        UIRoot uiRoot = new();
        ElementLifecycle.AttachSubtree(uiRoot, surface);
        try
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 100, 100));

            Matrix3x2 transform = Assert.Single(
                commands.Where(command => command.Kind == DrawCommandKind.PushTransform)
                    .Select(command => command.Transform));
            Assert.Equal(Matrix3x2.CreateTranslation(10, 20), transform);
        }
        finally
        {
            ElementLifecycle.DetachSubtree(uiRoot, surface);
        }
    }

    [Fact]
    public void PlacementIsAppliedAfterTheWholeLocalTransform()
    {
        Scene2D group = new()
        {
            X = 100,
            Y = 50,
            Rotation = MathF.PI / 2,
            TransformOrigin = new DrawPoint(4, 0)
        };

        Matrix3x2 actual = group.GetLocalTransform();

        Matrix3x2 expected = SceneGeometry2D.CreateLocalTransform(group, group.TransformOrigin) *
            Matrix3x2.CreateTranslation(100, 50);
        AssertMatrix(expected, actual);
        AssertVector(new Vector2(104, 50), Vector2.Transform(new Vector2(4, 0), actual));
    }

    [Fact]
    public void PlacementAddsToTranslate()
    {
        Scene2D group = new() { X = 100, TranslateX = 5, Y = 7, TranslateY = 3 };

        AssertVector(new Vector2(105, 10), Vector2.Transform(Vector2.Zero, group.GetLocalTransform()));
    }

    [Fact]
    public void PlacementChangesInvalidateTheCachedLocalTransform()
    {
        Scene2D group = new() { X = 1, Y = 2 };
        AssertVector(new Vector2(1, 2), Vector2.Transform(Vector2.Zero, group.GetLocalTransform()));

        group.X = 11;
        group.Y = 12;

        AssertVector(new Vector2(11, 12), Vector2.Transform(Vector2.Zero, group.GetLocalTransform()));
    }

    [Fact]
    public void HitTestingFollowsPlacement()
    {
        UIRoot root = CreateRoot(out Scene2D scene);
        Scene2D house = new() { X = 30, Y = 20 };
        Sprite2D sprite = new() { Collider = new BoxCollider2D { Width = 40, Height = 30 } };
        house.Children.Add(sprite);
        scene.Children.Add(house);

        Assert.Same(sprite, new HitTestService().HitTest(root, 35, 25)?.Element);

        house.X = 200;

        Assert.NotSame(sprite, new HitTestService().HitTest(root, 35, 25)?.Element);
    }

    [Fact]
    public void ColliderGeometryAndMutationFollowPlacement()
    {
        Scene2D root = new();
        Scene2D group = new() { X = 10 };
        BoxCollider2D collider = new() { Width = 3, Height = 4 };
        group.Children.Add(new Sprite2D { Collider = collider });
        root.Children.Add(group);
        List<SceneCollisionMutation2D> mutations = [];
        root.CollisionMutation += mutations.Add;

        Assert.True(collider.TryGetSceneGeometry(out ColliderGeometry2D geometry));
        Assert.Equal(new DrawRect(10, 0, 3, 4), geometry.SceneBounds);

        group.X = 8;

        SceneCollisionMutation2D mutation = Assert.Single(mutations);
        Assert.Equal(SceneCollisionMutationKind.Geometry, mutation.Kind);
        Assert.Same(group, mutation.Node);
        Assert.True(collider.TryGetSceneGeometry(out geometry));
        Assert.Equal(new DrawRect(8, 0, 3, 4), geometry.SceneBounds);

        group.Y = 5;

        Assert.Equal(2, mutations.Count);
        Assert.Equal(SceneCollisionMutationKind.Geometry, mutations[1].Kind);
    }

    [Fact]
    public void ChangingPlacementInvalidatesTheOwningOnDemandSurface()
    {
        Scene2D root = new();
        Scene2D house = new();
        root.Children.Add(house);
        RenderSurface2D surface = new() { Scene = root, RedrawMode = RenderSurface2DRedrawMode.OnDemand };
        long version = ((IRenderSurface2DFrameSource)surface).FrameVersion;

        house.X = 23;

        long afterX = ((IRenderSurface2DFrameSource)surface).FrameVersion;
        Assert.True(afterX > version);

        house.Y = 9;

        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > afterX);
    }

    [Fact]
    public void NonFinitePlacementIsRejectedAndKeepsTheValue()
    {
        Scene2D group = new() { X = 4, Y = 6 };

        Assert.ThrowsAny<ArgumentException>(() => group.X = float.NaN);
        Assert.ThrowsAny<ArgumentException>(() => group.Y = float.PositiveInfinity);

        Assert.Equal(4, group.X);
        Assert.Equal(6, group.Y);
        Assert.Equal(0, new Scene2D().X);
        Assert.Equal(0, new Scene2D().Y);
    }

    private static UIRoot CreateRoot(out Scene2D scene)
    {
        UIRoot root = new(200, 200);
        RenderSurface2D surface = new() { Width = 200, Height = 200 };
        surface.Measure(new MeasureContext(new LayoutSize(200, 200)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        scene = new Scene2D();
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        return root;
    }

    private static void AssertVector(Vector2 expected, Vector2 actual)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0, 0.0001f);
    }

    private static void AssertMatrix(Matrix3x2 expected, Matrix3x2 actual)
    {
        Assert.InRange(MathF.Abs(expected.M11 - actual.M11), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.M12 - actual.M12), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.M21 - actual.M21), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.M22 - actual.M22), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.M31 - actual.M31), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.M32 - actual.M32), 0, 0.0001f);
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;
        public int Height => 16;
    }
}
