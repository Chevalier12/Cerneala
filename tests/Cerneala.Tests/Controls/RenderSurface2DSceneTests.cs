using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

public sealed class RenderSurface2DSceneTests
{
    [Fact]
    public void SceneActivatesDrawingAndRecordsAfterImperativeDrawing()
    {
        TestImage image = new();
        RenderSurface2D surface = new();
        surface.Draw += (_, frame) =>
            frame.FillRectangle(new DrawRect(0, 0, 1, 1), Color.Black);
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D
        {
            Source = image,
            Destination = new DrawRect(2, 3, 4, 5)
        });
        surface.Scene = scene;

        DrawCommandList commands = Record(surface, new DrawRect(0, 0, 100, 80));

        Assert.True(surface.IsDrawingActiveForTests);
        Assert.Equal(
            [DrawCommandKind.FillRectangle, DrawCommandKind.DrawImage],
            commands.Select(command => command.Kind));
        Assert.Same(image, commands[1].Image);
        Assert.Equal(new DrawRect(2, 3, 4, 5), commands[1].Rect);
    }

    [Fact]
    public void ViewBoxUniformCentersLogicalCoordinatesInsideSurfaceBounds()
    {
        RenderSurface2D surface = new()
        {
            ViewBox = new DrawRect(0, 0, 10, 20),
            Stretch = DrawBrushStretch.Uniform,
            Scene = new Scene2D()
        };
        surface.Scene.Children.Add(new Sprite2D
        {
            Source = new TestImage(),
            Destination = new DrawRect(0, 0, 1, 1)
        });

        DrawCommandList commands = Record(surface, new DrawRect(0, 0, 200, 200));

        DrawCommand transform = Assert.Single(
            commands.Where(command => command.Kind == DrawCommandKind.PushTransform));
        Assert.Equal(
            Matrix3x2.CreateScale(10) * Matrix3x2.CreateTranslation(50, 0),
            transform.Transform);
    }

    [Fact]
    public void ScenePropertyMutationInvalidatesAnOnDemandSurface()
    {
        Sprite2D sprite = new()
        {
            Source = new TestImage(),
            Destination = new DrawRect(0, 0, 1, 1)
        };
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand,
            Scene = scene
        };
        long frameVersion = ((IRenderSurface2DFrameSource)surface).FrameVersion;

        sprite.Destination = new DrawRect(1, 2, 3, 4);

        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > frameVersion);
    }

    [Fact]
    public void SceneNodesRespectUiElementVisibility()
    {
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D
        {
            Source = new TestImage(),
            Destination = new DrawRect(0, 0, 1, 1),
            Visibility = Visibility.Hidden
        });
        RenderSurface2D surface = new() { Scene = scene };

        DrawCommandList commands = Record(surface, new DrawRect(0, 0, 10, 10));

        Assert.DoesNotContain(
            commands,
            command => command.Kind == DrawCommandKind.DrawImage);
    }

    [Fact]
    public void SpritePrismCapturesOnlyTheSpriteDrawUsingDestinationBounds()
    {
        DrawRect destination = new(2, 3, 4, 5);
        Sprite2D sprite = new()
        {
            Source = new TestImage(),
            Destination = destination
        };
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            sprite,
            () => new PrismInstance(
                PrismTestData.Composition(
                    "Sprite",
                    PrismTestData.Layer(1, "Content"))));
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            DrawCommandList commands = Record(
                surface,
                new DrawRect(0, 0, 10, 10));

            Assert.Equal(
                [
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism
                ],
                commands.Select(command => command.Kind));
            PrismDrawScope scope = Assert.IsType<PrismDrawScope>(
                commands[0].PrismScope);
            Assert.Equal(destination, scope.ControlBounds);
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    public void SpritePrismBoundsApplyTheSceneViewBoxTransformExactlyOnce()
    {
        Sprite2D sprite = new()
        {
            Source = new TestImage(),
            Destination = new DrawRect(1, 2, 3, 4)
        };
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 10, 20),
            Stretch = DrawBrushStretch.Uniform
        };
        UIRoot root = new();
        using IDisposable prism = GeneratedMarkup.AttachPrism(
            sprite,
            () => new PrismInstance(
                PrismTestData.Composition(
                    "Sprite",
                    PrismTestData.Layer(1, "Content"))));
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            DrawCommandList commands = Record(
                surface,
                new DrawRect(0, 0, 200, 200));

            PrismFrameAnalysis analysis =
                new PrismFrameAnalyzer().Analyze(commands);
            PrismAnalyzedScope scope = Assert.Single(analysis.Scopes);
            Assert.Equal(
                new DrawRect(60, 20, 30, 40),
                scope.Bounds);
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    public void SpriteAspectAppliesSpritePropertiesThroughTheSceneLogicalTree()
    {
        Sprite2D sprite = new();
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        ElementAspect aspect = new(
            [new ElementAspectValue(Sprite2D.TintProperty, Color.Black)]);

        sprite.Aspect = aspect;
        root.ProcessFrame();

        Assert.Equal(Color.Black, sprite.Tint);
        Assert.Equal(
            UiPropertyValueSource.AspectBase,
            sprite.GetValueSource(Sprite2D.TintProperty));
    }

    [Fact]
    public void SpriteMotionAnimatesSpritePropertiesThroughTheSceneLogicalTree()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Sprite2D sprite = new();
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
        root.ProcessFrame();
        Assert.True(sprite.IsAttached);
        Assert.True(sprite.IsVisible);

        Cerneala.UI.Motion.Core.MotionHandle handle = sprite.Motion()
            .Animate(Sprite2D.DestinationProperty)
            .To(new DrawRect(10, 20, 3, 4))
            .With(MotionFactory.Tween<DrawRect>(
                TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.InRange(sprite.Destination.X, 0.01f, 9.99f);
        Assert.InRange(sprite.Destination.Y, 0.01f, 19.99f);
    }

    [Fact]
    public void SceneItemsMaterializeTemplatesInSourceOrderAndTrackChanges()
    {
        TestImage image = new();
        ObservableList<TestSprite> items =
        [
            new TestSprite(new DrawRect(1, 0, 1, 1)),
            new TestSprite(new DrawRect(2, 0, 1, 1))
        ];
        SceneItems2D sceneItems = new();
        sceneItems.Templates.Add(new ContentTemplate<TestSprite>(
            "test-sprite",
            key: null,
            priority: 0,
            context => new Sprite2D
            {
                Source = image,
                Destination = context.Data!.Destination
            }));
        sceneItems.ItemsSource = items;
        Scene2D scene = new();
        scene.Children.Add(sceneItems);
        RenderSurface2D surface = new() { Scene = scene };

        DrawCommandList first = Record(surface, new DrawRect(0, 0, 10, 10));
        Assert.Equal(
            [new DrawRect(1, 0, 1, 1), new DrawRect(2, 0, 1, 1)],
            first.Where(command => command.Kind == DrawCommandKind.DrawImage)
                .Select(command => command.Rect));

        items.Insert(1, new TestSprite(new DrawRect(3, 0, 1, 1)));

        DrawCommandList second = Record(surface, new DrawRect(0, 0, 10, 10));
        Assert.Equal(
            [
                new DrawRect(1, 0, 1, 1),
                new DrawRect(3, 0, 1, 1),
                new DrawRect(2, 0, 1, 1)
            ],
            second.Where(command => command.Kind == DrawCommandKind.DrawImage)
                .Select(command => command.Rect));
    }

    private static DrawCommandList Record(RenderSurface2D surface, DrawRect bounds)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, bounds);
        return commands;
    }

    private sealed record TestSprite(DrawRect Destination);

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;

        public int Height => 16;
    }
}
