using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class CollisionStageFourInputTests
{
    [Fact]
    [Trait("CollisionStage", "4")]
    public void ServoInputDriverRoutesPointerKeyboardAndTextThroughSceneNodes()
    {
        UIRoot root = CreateRoot(out RenderSurface2D surface, out Scene2D scene);
        BoxCollider2D collider = new()
        {
            Width = 30,
            Height = 30,
            TranslateX = 10,
            TranslateY = 10,
            Focusable = true
        };
        scene.Children.Add(collider);
        int clicks = 0;
        int commandExecutions = 0;
        string text = string.Empty;
        Vector2 surfacePosition = default;
        Vector2 scenePosition = default;
        Vector2 colliderPosition = default;
        int legacyX = 0;
        int legacyY = 0;
        collider.AddHandler(InputEvents.MouseDownEvent, (_, args) =>
        {
            MouseEventArgs mouse = Assert.IsAssignableFrom<MouseEventArgs>(args);
            clicks++;
            surfacePosition = mouse.GetPosition(surface);
            scenePosition = mouse.GetPosition(scene);
            colliderPosition = mouse.GetPosition(collider);
            legacyX = mouse.X;
            legacyY = mouse.Y;
        });
        collider.AddHandler(
            InputEvents.TextInputEvent,
            (_, args) => text += Assert.IsType<TextCompositionEventArgs>(args).Text);
        scene.InputBindings.Add(new KeyBinding(
            new ActionCommand(_ => commandExecutions++),
            InputKey.Enter));
        UiHost host = CreateHost(root);
        RetainedServoInputDriver input = new(host);

        input.ClickAt(20.25f, 20.75f);
        input.PressKey(InputKey.Enter, ServoModifiers.None);
        input.SendText("door");

        Assert.Equal(1, clicks);
        Assert.Same(collider, host.InputBridge.FocusManager.FocusedElement);
        Assert.Equal(1, commandExecutions);
        Assert.Equal("door", text);
        Assert.Equal(new Vector2(20.25f, 20.75f), surfacePosition);
        Assert.Equal(new Vector2(20.25f, 20.75f), scenePosition);
        Assert.Equal(new Vector2(10.25f, 10.75f), colliderPosition);
        Assert.Equal(20, legacyX);
        Assert.Equal(21, legacyY);
    }

    [Fact]
    [Trait("CollisionStage", "4")]
    public void PickingUsesExactGeometryAndReverseEffectiveLayerOrder()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        BoxCollider2D lower = new()
        {
            Width = 40,
            Height = 40,
            Layer = 1
        };
        CircleCollider2D upperCircle = new()
        {
            Radius = 20,
            TranslateX = 20,
            TranslateY = 20,
            Layer = 2
        };
        scene.OrderMode = SceneOrderMode.Layer;
        scene.Children.Add(lower);
        scene.Children.Add(upperCircle);

        HitTestService hitTest = new();
        HitTestResult? center = hitTest.HitTest(root, 20, 20);
        HitTestResult? outsideCircleInsideBounds = hitTest.HitTest(root, 2, 2);

        Assert.Same(upperCircle, center?.Element);
        Assert.Same(lower, outsideCircleInsideBounds?.Element);
    }

    [Fact]
    [Trait("CollisionStage", "4")]
    public void NestedEntityUsesItsColliderUnionAsTheRealRoutedTarget()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        Scene2D house = new() { TranslateX = 30, TranslateY = 20 };
        house.Children.Add(new BoxCollider2D { Width = 40, Height = 30 });
        scene.Children.Add(house);

        HitTestResult? hit = new HitTestService().HitTest(root, 35, 25);
        ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);

        Assert.Same(house, hit?.Element);
        Assert.True(routeMap.TryGetId(house, out UiElementId houseId));
        Assert.Equal(houseId, hit?.ElementId);
        Assert.Equal(
            [house, scene, scene.Surface!, root],
            routeMap.GetRouteToRoot(house));
    }

    [Fact]
    [Trait("CollisionStage", "4")]
    public void ViewBoxConversionIsTheRenderTransformInverseAndRejectsSingularVisuals()
    {
        UIRoot root = CreateRoot(out RenderSurface2D surface, out Scene2D scene);
        surface.ViewBox = new DrawRect(100, 50, 50, 50);
        surface.Stretch = DrawBrushStretch.Fill;
        BoxCollider2D collider = new()
        {
            Width = 5,
            Height = 5,
            TranslateX = 110,
            TranslateY = 60
        };
        scene.Children.Add(collider);
        Vector2 rootPoint = new(50.5f, 50.25f);

        Assert.True(surface.TryRootToScene(rootPoint, out Vector2 scenePoint));
        AssertVector(new Vector2(112.625f, 62.5625f), scenePoint);
        AssertVector(rootPoint, surface.SceneToRoot(scenePoint));
        Assert.Same(collider, new HitTestService().HitTest(root, 50.5f, 50.25f)?.Element);

        surface.ScaleX = 0;

        Assert.False(surface.TryRootToScene(rootPoint, out _));
        Assert.NotSame(collider, new HitTestService().HitTest(root, 50.5f, 50.25f)?.Element);
    }

    [Fact]
    [Trait("CollisionStage", "4")]
    public void RouteMapRebuildsForStructureAndParticipationButNotGeometrySamples()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        BoxCollider2D first = new() { Width = 20, Height = 20 };
        scene.Children.Add(first);
        root.InputCache.EnsureCurrent(root);
        int baseline = root.InputCache.RebuildCount;

        first.OffsetX = 2.5f;
        first.TranslateX = 3.5f;
        root.InputCache.EnsureCurrent(root);

        Assert.Equal(baseline, root.InputCache.RebuildCount);

        BoxCollider2D second = new() { Width = 10, Height = 10 };
        scene.Children.Add(second);
        root.InputCache.EnsureCurrent(root);

        Assert.Equal(baseline + 1, root.InputCache.RebuildCount);
        Assert.True(root.InputCache.RouteMap.TryGetId(second, out _));

        second.IsVisible = false;
        root.InputCache.EnsureCurrent(root);

        Assert.Equal(baseline + 2, root.InputCache.RebuildCount);
        Assert.False(root.InputCache.RouteMap.TryGetId(second, out _));
    }

    [Fact]
    [Trait("CollisionStage", "4")]
    public void OpacityDoesNotChangeColliderPickingButVisibilityAndHitTestVisibilityDo()
    {
        UIRoot root = CreateRoot(out _, out Scene2D scene);
        BoxCollider2D collider = new()
        {
            Width = 20,
            Height = 20,
            Opacity = 0
        };
        scene.Children.Add(collider);
        HitTestService hitTest = new();

        Assert.Same(collider, hitTest.HitTest(root, 10, 10)?.Element);

        collider.IsHitTestVisible = false;
        Assert.NotSame(collider, hitTest.HitTest(root, 10, 10)?.Element);

        collider.IsHitTestVisible = true;
        collider.Visibility = Visibility.Hidden;
        Assert.NotSame(collider, hitTest.HitTest(root, 10, 10)?.Element);
    }

    private static UIRoot CreateRoot(
        out RenderSurface2D surface,
        out Scene2D scene)
    {
        UIRoot root = new(200, 200);
        surface = new RenderSurface2D { Width = 200, Height = 200 };
        surface.Measure(new MeasureContext(new LayoutSize(200, 200)));
        surface.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 200)));
        scene = new Scene2D();
        surface.Scene = scene;
        root.VisualChildren.Add(surface);
        return root;
    }

    private static UiHost CreateHost(UIRoot root)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(200, 200)
        });
        host.Update(
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }

    private static void AssertVector(Vector2 expected, Vector2 actual)
    {
        Assert.InRange(MathF.Abs(expected.X - actual.X), 0, 0.0001f);
        Assert.InRange(MathF.Abs(expected.Y - actual.Y), 0, 0.0001f);
    }
}
