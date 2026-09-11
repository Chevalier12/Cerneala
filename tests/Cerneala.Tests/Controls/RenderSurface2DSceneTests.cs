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

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

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
            Image = new(image),
            X = 2, Y = 3, Width = 4, Height = 5
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
            Image = new(new TestImage()),
            X = 0, Y = 0, Width = 1, Height = 1
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
            Image = new(new TestImage()),
            X = 0, Y = 0, Width = 1, Height = 1
        };
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new()
        {
            RedrawMode = RenderSurface2DRedrawMode.OnDemand,
            Scene = scene
        };
        long frameVersion = ((IRenderSurface2DFrameSource)surface).FrameVersion;

        sprite.X = 1; sprite.Y = 2; sprite.Width = 3; sprite.Height = 4;

        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > frameVersion);
    }

    [Fact]
    public void SceneNodesRespectUiElementVisibility()
    {
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D
        {
            Image = new(new TestImage()),
            X = 0, Y = 0, Width = 1, Height = 1,
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
            Image = new(new TestImage()),
            X = destination.X, Y = destination.Y, Width = destination.Width, Height = destination.Height
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
    public void ContinuousStaticSceneKeepsItsPrismContentIdentityAcrossFrameTicks()
    {
        Sprite2D sprite = new() { Image = new(new TestImage()), Width = 4, Height = 5 };
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        using IDisposable prism = GeneratedMarkup.AttachPrism(sprite,
            () => new PrismInstance(PrismTestData.Composition("Static", PrismTestData.Layer(1, "Content"))));
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            IRenderSurface2DFrameSource source = surface;
            DrawRect bounds = new(0, 0, 10, 10);
            DrawCommandList first = Record(surface, bounds);
            PrismDrawScope before = first[0].PrismScope!.Value;
            long frameVersion = source.FrameVersion;

            Assert.True(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16)));
            DrawCommandList second = Record(surface, bounds);
            PrismDrawScope after = second[0].PrismScope!.Value;

            Assert.True(source.FrameVersion > frameVersion); // Continuous still records every frame.
            Assert.Equal(before.VisualContentVersion, after.VisualContentVersion);
            Assert.Equal(before.LowerUiVersion, after.LowerUiVersion);
            Assert.Equal(first.ToArray(), second.ToArray());

            sprite.Tint = Color.Black;
            PrismDrawScope changed = Record(surface, bounds)[0].PrismScope!.Value;
            Assert.NotEqual(after.LowerUiVersion, changed.LowerUiVersion);
            surface.ClearColor = Color.White;
            PrismDrawScope cleared = Record(surface, bounds)[0].PrismScope!.Value;
            Assert.NotEqual(changed.LowerUiVersion, cleared.LowerUiVersion);
            surface.InvalidateFrame();
            Assert.NotEqual(cleared.LowerUiVersion, Record(surface, bounds)[0].PrismScope!.Value.LowerUiVersion);
        }
        finally { ElementLifecycle.DetachSubtree(root, surface); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ContinuousSceneWithImperativeDrawingDoesNotAssumeItsContentIsUnchanged(bool useOverride)
    {
        Sprite2D sprite = new() { Image = new(new TestImage()), Width = 4, Height = 5 };
        Scene2D scene = new();
        scene.Children.Add(sprite);
        RenderSurface2D surface = useOverride ? new ImperativeSurface() : new RenderSurface2D();
        int callbacks = 0;
        if (!useOverride) surface.Draw += (_, _) => callbacks++;
        surface.Scene = scene;
        UIRoot root = new();
        using IDisposable prism = GeneratedMarkup.AttachPrism(sprite,
            () => new PrismInstance(PrismTestData.Composition("Mixed", PrismTestData.Layer(1, "Content"))));
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            DrawRect bounds = new(0, 0, 10, 10);
            PrismDrawScope before = Record(surface, bounds)[0].PrismScope!.Value;
            Assert.True(((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.FromMilliseconds(16)));
            PrismDrawScope after = Record(surface, bounds)[0].PrismScope!.Value;
            Assert.NotEqual(before.LowerUiVersion, after.LowerUiVersion);
            Assert.Equal(2, useOverride ? ((ImperativeSurface)surface).DrawCount : callbacks);
        }
        finally { ElementLifecycle.DetachSubtree(root, surface); }
    }

    [Fact]
    public void SpritePrismBoundsApplyTheSceneViewBoxTransformExactlyOnce()
    {
        Sprite2D sprite = new()
        {
            Image = new(new TestImage()),
            X = 1, Y = 2, Width = 3, Height = 4
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
            PrismGraph graph = new PrismGraphBuilder().Build(analysis);
            Assert.Equal(
                Matrix3x2.CreateScale(10) * Matrix3x2.CreateTranslation(50, 0),
                Assert.Single(graph.Scopes).EffectiveTransform);
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
            .Animate(Sprite2D.XProperty)
            .To(10f)
            .With(MotionFactory.Tween<float>(
                TimeSpan.FromMilliseconds(100)));
        using var yMotion = sprite.Motion().Animate(Sprite2D.YProperty).To(20f)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.InRange(sprite.X, 0.01f, 9.99f);
        Assert.InRange(sprite.Y, 0.01f, 19.99f);
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
                Image = new(image),
                X = context.Data!.Destination.X, Y = context.Data!.Destination.Y, Width = context.Data!.Destination.Width, Height = context.Data!.Destination.Height
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

    private sealed class ImperativeSurface : RenderSurface2D
    {
        public int DrawCount { get; private set; }
        protected override void OnDraw(RenderSurface2DFrame frame) => DrawCount++;
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;

        public int Height => 16;
    }
}
