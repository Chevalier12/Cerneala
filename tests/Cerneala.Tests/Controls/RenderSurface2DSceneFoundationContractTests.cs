using System.Numerics;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class RenderSurface2DSceneFoundationContractTests
{
    [Fact]
    public void SceneRecordsChildrenInCollectionOrder()
    {
        TestImage first = new("first");
        TestImage second = new("second");
        TestImage third = new("third");
        Scene2D scene = new();
        scene.Children.Add(Sprite(first, y: 30));
        scene.Children.Add(Sprite(second, y: 10));
        scene.Children.Add(Sprite(third, y: 20));

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        Assert.Equal(
            [first, second, third],
            commands.Where(command => command.Kind == DrawCommandKind.DrawImage)
                .Select(command => command.Image));
    }

    [Fact]
    public void ReplacingAndDetachingSceneMaintainsLogicalLifecycle()
    {
        Sprite2D firstSprite = Sprite(new TestImage("first"), y: 0);
        Scene2D firstScene = new();
        firstScene.Children.Add(firstSprite);
        Sprite2D secondSprite = Sprite(new TestImage("second"), y: 0);
        Scene2D secondScene = new();
        secondScene.Children.Add(secondSprite);
        RenderSurface2D surface = new() { Scene = firstScene };
        UIRoot root = new();

        root.VisualChildren.Add(surface);
        Assert.True(firstScene.IsAttached);
        Assert.True(firstSprite.IsAttached);

        surface.Scene = secondScene;
        Assert.False(firstScene.IsAttached);
        Assert.False(firstSprite.IsAttached);
        Assert.True(secondScene.IsAttached);
        Assert.True(secondSprite.IsAttached);
        Assert.DoesNotContain(firstScene, surface.LogicalChildren);
        Assert.Contains(secondScene, surface.LogicalChildren);

        root.VisualChildren.Remove(surface);
        Assert.False(secondScene.IsAttached);
        Assert.False(secondSprite.IsAttached);
    }

    [Fact]
    public void UndocumentedUiTransformsDoNotCurrentlyAlterSpriteCommands()
    {
        DrawRect destination = new(2, 3, 4, 5);
        Sprite2D sprite = Sprite(new TestImage("sprite"), y: destination.Y);
        sprite.Destination = destination;
        sprite.TranslateX = 17;
        sprite.TranslateY = 23;
        sprite.Scale = 2;
        sprite.SkewX = 0.25f;
        Scene2D scene = new();
        scene.Children.Add(sprite);

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        DrawCommand draw = Assert.Single(
            commands.Where(command => command.Kind == DrawCommandKind.DrawImage));
        Assert.Equal(destination, draw.Rect);
        Assert.DoesNotContain(
            commands,
            command => command.Kind == DrawCommandKind.PushTransform);
    }

    [Fact]
    public void SceneTransformOriginIsAnExplicitSceneSpacePoint()
    {
        PropertyInfo? property = typeof(Scene2D).GetProperty(
            "TransformOrigin",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.NotNull(property);
        Assert.Equal(typeof(DrawPoint), property.PropertyType);
    }

    [Fact]
    public void MotionAnimatesSceneTransformOrigin()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Scene2D group = new() { Scale = 2 };
        group.Children.Add(Sprite(new TestImage("sprite"), y: 0));
        Scene2D scene = new();
        scene.Children.Add(group);
        root.VisualChildren.Add(new RenderSurface2D { Scene = scene });
        root.ProcessFrame();

        Cerneala.UI.Motion.Core.MotionHandle handle = group.Motion()
            .Animate(Scene2D.TransformOriginProperty)
            .To(new DrawPoint(10, 20))
            .With(MotionFactory.Tween<DrawPoint>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.InRange(group.TransformOrigin.X, 0.01f, 9.99f);
        Assert.InRange(group.TransformOrigin.Y, 0.01f, 19.99f);
    }

    [Fact]
    public void NestedSceneGroupsComposeTransformsInsideViewBoxClip()
    {
        Scene2D nested = new()
        {
            ScaleX = 2,
            ScaleY = 3,
            Rotation = 0.25f
        };
        nested.Children.Add(Sprite(new TestImage("nested"), y: 2));
        Scene2D scene = new()
        {
            TranslateX = 7,
            // Keep the transformed sprite inside the 20x10 ViewBox: this
            // contract tests transform composition, not offscreen recording.
            TranslateY = 1
        };
        scene.Children.Add(nested);
        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 20, 10),
            Stretch = DrawBrushStretch.Fill
        };

        DrawCommandList commands = Record(
            surface,
            new DrawRect(0, 0, 200, 100));

        Assert.Equal(
            [
                DrawCommandKind.PushClip,
                DrawCommandKind.PushTransform,
                DrawCommandKind.PushTransform,
                DrawCommandKind.PushTransform,
                DrawCommandKind.DrawImage,
                DrawCommandKind.PopTransform,
                DrawCommandKind.PopTransform,
                DrawCommandKind.PopTransform,
                DrawCommandKind.PopClip
            ],
            commands.Select(command => command.Kind));
        DrawCommand[] transforms = commands
            .Where(command => command.Kind == DrawCommandKind.PushTransform)
            .ToArray();
        Assert.Equal(Matrix3x2.CreateScale(10), transforms[0].Transform);
        Assert.Equal(Matrix3x2.CreateTranslation(7, 1), transforms[1].Transform);
        Assert.Equal(
            Matrix3x2.CreateScale(2, 3) * Matrix3x2.CreateRotation(0.25f),
            transforms[2].Transform);
    }

    [Fact]
    public void NonInvertibleSceneTransformStillRecordsConservativeContent()
    {
        Scene2D scene = new() { ScaleX = 0 };
        scene.Children.Add(Sprite(new TestImage("flat"), y: 0));

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        DrawCommand transform = Assert.Single(
            commands.Where(command => command.Kind == DrawCommandKind.PushTransform));
        Assert.Equal(0, transform.Transform.M11);
        Assert.Contains(commands, command => command.Kind == DrawCommandKind.DrawImage);
    }

    [Fact]
    public void SceneTransformHelperUsesSceneOriginAndRoundTripsWorldToLocal()
    {
        Scene2D scene = new()
        {
            TransformOrigin = new DrawPoint(2, 3),
            ScaleX = -2,
            ScaleY = 3,
            SkewX = 0.1f,
            Rotation = 0.25f,
            TranslateX = 7,
            TranslateY = 11,
            RenderTransform = new Cerneala.UI.Media.Transform(
                Cerneala.UI.Media.Matrix3x2.CreateTranslation(5, 6))
        };
        Matrix3x2 expected =
            Matrix3x2.CreateTranslation(-2, -3) *
            Matrix3x2.CreateScale(-2, 3) *
            Matrix3x2.CreateSkew(0.1f, 0) *
            Matrix3x2.CreateRotation(0.25f) *
            Matrix3x2.CreateTranslation(7, 11) *
            Matrix3x2.CreateTranslation(5, 6) *
            Matrix3x2.CreateTranslation(2, 3);

        Matrix3x2 actual = SceneGeometry2D.CreateLocalTransform(scene);
        Vector2 expectedWorld = Vector2.Transform(new Vector2(4, 5), expected);

        Assert.Equal(expected, actual);
        Assert.True(SceneGeometry2D.TryTransformToLocal(
            new DrawPoint(expectedWorld.X, expectedWorld.Y),
            actual,
            out DrawPoint local));
        Assert.InRange(local.X, 3.9999f, 4.0001f);
        Assert.InRange(local.Y, 4.9999f, 5.0001f);

        scene.ScaleX = 0;
        Assert.False(SceneGeometry2D.TryTransformToLocal(
            new DrawPoint(1, 1),
            SceneGeometry2D.CreateLocalTransform(scene),
            out _));
    }

    [Fact]
    public void UniformToFillComposesWithNegativeSceneTransform()
    {
        Scene2D group = new() { ScaleX = -1 };
        group.Children.Add(Sprite(new TestImage("mirrored"), y: 0));
        Scene2D scene = new();
        scene.Children.Add(group);
        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 10, 20),
            Stretch = DrawBrushStretch.UniformToFill
        };

        DrawCommand[] transforms = Record(
                surface,
                new DrawRect(0, 0, 200, 200))
            .Where(command => command.Kind == DrawCommandKind.PushTransform)
            .ToArray();

        Assert.Equal(2, transforms.Length);
        Assert.Equal(
            Matrix3x2.CreateScale(20) * Matrix3x2.CreateTranslation(0, -100),
            transforms[0].Transform);
        Assert.Equal(Matrix3x2.CreateScale(-1, 1), transforms[1].Transform);
    }

    [Fact]
    public void GroupPrismUsesAggregateTransformedBoundsAndNestsChildPrism()
    {
        Sprite2D sprite = new()
        {
            Source = new TestImage("sprite"),
            Destination = new DrawRect(2, 3, 4, 5)
        };
        Scene2D group = new() { TranslateX = 10 };
        group.Children.Add(sprite);
        Scene2D scene = new();
        scene.Children.Add(group);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        using IDisposable groupPrism = AttachPrism(group, "Group");
        using IDisposable spritePrism = AttachPrism(sprite, "Sprite");
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            DrawCommandList commands = Record(
                surface,
                new DrawRect(0, 0, 100, 100));

            PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
            PrismAnalyzedScope[] scopes = analysis.Scopes.ToArray();
            Assert.Equal(2, scopes.Length);
            Assert.Equal(new DrawRect(12, 3, 4, 5), scopes[0].Bounds);
            Assert.Equal(new DrawRect(12, 3, 4, 5), scopes[1].Bounds);
            Assert.Equal(
                [
                    DrawCommandKind.PushTransform,
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism,
                    DrawCommandKind.EndPrism,
                    DrawCommandKind.PopTransform
                ],
                commands.Select(command => command.Kind));
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    public void GroupPrismAndTransformScopesCloseWhenAChildThrows()
    {
        Scene2D group = new() { TranslateX = 10 };
        group.Children.Add(new ThrowingNode());
        Scene2D scene = new();
        scene.Children.Add(group);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        using IDisposable prism = AttachPrism(group, "Group");
        ElementLifecycle.AttachSubtree(root, surface);
        DrawCommandList commands = new();
        try
        {
            Assert.Throws<InvalidOperationException>(() =>
                ((IRenderSurface2DFrameSource)surface).RecordFrame(
                    commands,
                    new DrawRect(0, 0, 100, 100)));

            Assert.Equal(
                [
                    DrawCommandKind.PushTransform,
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.EndPrism,
                    DrawCommandKind.PopTransform
                ],
                commands.Select(command => command.Kind));
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    public void GroupOpacityScopesOnlyItsDescendants()
    {
        Scene2D group = new() { Opacity = 0.5f };
        group.Children.Add(Sprite(new TestImage("faded"), y: 0));
        Scene2D scene = new();
        scene.Children.Add(group);

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        Assert.Equal(
            [
                DrawCommandKind.PushOpacity,
                DrawCommandKind.DrawImage,
                DrawCommandKind.PopOpacity
            ],
            commands.Select(command => command.Kind));
    }

    [Fact]
    public void ExplicitLayerOrderingIsStableAndIgnoresSpriteLayerDepth()
    {
        TestImage first = new("first");
        TestImage second = new("second");
        TestImage third = new("third");
        Sprite2D firstSprite = Sprite(first, y: 0);
        Sprite2D secondSprite = Sprite(second, y: 0);
        Sprite2D thirdSprite = Sprite(third, y: 0);
        firstSprite.LayerDepth = 1;
        secondSprite.LayerDepth = 0;
        thirdSprite.LayerDepth = 0.5f;
        firstSprite.Layer = 2;
        secondSprite.Layer = -1;
        thirdSprite.Layer = 2;
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(firstSprite);
        scene.Children.Add(secondSprite);
        scene.Children.Add(thirdSprite);

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        Assert.Equal(
            [second, first, third],
            commands.Where(command => command.Kind == DrawCommandKind.DrawImage)
                .Select(command => command.Image));
    }

    [Fact]
    public void LayerThenYUsesSceneBoundsAndKeepsSourceOrderForTies()
    {
        TestImage first = new("first");
        TestImage hidden = new("hidden");
        TestImage second = new("second");
        TestImage third = new("third");
        Scene2D scene = new() { OrderMode = SceneOrderMode.LayerThenY };
        scene.Children.Add(Sprite(first, y: 20));
        scene.Children.Add(new Sprite2D
        {
            Source = hidden,
            Destination = new DrawRect(0, -100, 1, 1),
            Visibility = Cerneala.UI.Layout.Visibility.Hidden
        });
        scene.Children.Add(Sprite(second, y: 10));
        scene.Children.Add(Sprite(third, y: 20));

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        Assert.Equal(
            [second, first, third],
            commands.Where(command => command.Kind == DrawCommandKind.DrawImage)
                .Select(command => command.Image));
    }

    [Fact]
    public void LayerThenYAppliesTheParentSceneTransformToItsAnchor()
    {
        TestImage lowerInSourceSpace = new("lower");
        TestImage higherInSourceSpace = new("higher");
        Scene2D scene = new()
        {
            OrderMode = SceneOrderMode.LayerThenY,
            ScaleY = -1,
            // Mirroring reverses the Y ordering; translation keeps both
            // sprites visible so viewport culling does not hide that ordering.
            TranslateY = 50
        };
        scene.Children.Add(Sprite(lowerInSourceSpace, y: 20));
        scene.Children.Add(Sprite(higherInSourceSpace, y: 10));

        DrawCommandList commands = Record(
            new RenderSurface2D { Scene = scene },
            new DrawRect(0, 0, 100, 100));

        Assert.Equal(
            [lowerInSourceSpace, higherInSourceSpace],
            Images(commands));
    }

    [Fact]
    public void NestedScenesOrderIndependentlyAndReactToRuntimeLayerChanges()
    {
        TestImage first = new("first");
        TestImage second = new("second");
        TestImage sibling = new("sibling");
        Scene2D nested = new()
        {
            Layer = -1,
            OrderMode = SceneOrderMode.Layer
        };
        nested.Children.Add(new Sprite2D
        {
            Source = first,
            Destination = new DrawRect(0, 0, 1, 1),
            Layer = 1
        });
        nested.Children.Add(new Sprite2D
        {
            Source = second,
            Destination = new DrawRect(0, 0, 1, 1),
            Layer = 0
        });
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(nested);
        scene.Children.Add(new Sprite2D
        {
            Source = sibling,
            Destination = new DrawRect(0, 0, 1, 1),
            Layer = 0
        });
        RenderSurface2D surface = new() { Scene = scene };

        Assert.Equal(
            [second, first, sibling],
            Images(Record(surface, new DrawRect(0, 0, 100, 100))));

        long version = ((IRenderSurface2DFrameSource)surface).FrameVersion;
        nested.Layer = 2;

        Assert.True(((IRenderSurface2DFrameSource)surface).FrameVersion > version);
        Assert.Equal(
            [sibling, second, first],
            Images(Record(surface, new DrawRect(0, 0, 100, 100))));
    }

    [Fact]
    public void AspectControlsLayerAndOrderMode()
    {
        TestImage first = new("first");
        TestImage second = new("second");
        Sprite2D firstSprite = Sprite(first, y: 0);
        Sprite2D secondSprite = Sprite(second, y: 0);
        Scene2D scene = new();
        scene.Children.Add(firstSprite);
        scene.Children.Add(secondSprite);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        scene.Aspect = new ElementAspect(
            [new ElementAspectValue(Scene2D.OrderModeProperty, SceneOrderMode.Layer)]);
        firstSprite.Aspect = new ElementAspect(
            [new ElementAspectValue(SceneNode2D.LayerProperty, 1)]);
        secondSprite.Aspect = new ElementAspect(
            [new ElementAspectValue(SceneNode2D.LayerProperty, -1)]);

        root.ProcessFrame();

        Assert.Equal(SceneOrderMode.Layer, scene.OrderMode);
        Assert.Equal(1, firstSprite.Layer);
        Assert.Equal(-1, secondSprite.Layer);
        Assert.Equal(
            [second, first],
            Images(Record(surface, new DrawRect(0, 0, 100, 100))));
    }

    [Fact]
    public void MotionOnSpriteYResortsLayerThenYInTheSameLogicalFrame()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        TestImage moving = new("moving");
        TestImage fixedImage = new("fixed");
        Sprite2D movingSprite = new() { Source = moving };
        Sprite2D fixedSprite = Sprite(fixedImage, y: 10);
        Scene2D scene = new() { OrderMode = SceneOrderMode.LayerThenY };
        scene.Children.Add(movingSprite);
        scene.Children.Add(fixedSprite);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
        root.ProcessFrame();

        Cerneala.UI.Motion.Core.MotionHandle handle = movingSprite.Motion()
            .Animate(Sprite2D.DestinationProperty)
            .To(new DrawRect(10, 20, 1, 1))
            .With(MotionFactory.Tween<DrawRect>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(75));
        root.ProcessFrame();

        Assert.InRange(movingSprite.Destination.Y, 10.01f, 19.99f);
        Assert.Equal(
            [fixedImage, moving],
            Images(Record(surface, new DrawRect(0, 0, 100, 100))));
    }

    [Fact]
    public void MotionOnLayerOpacityScopesItsSortedDescendants()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Scene2D layer = new();
        layer.Children.Add(Sprite(new TestImage("sprite"), y: 0));
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(layer);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
        root.ProcessFrame();

        Cerneala.UI.Motion.Core.MotionHandle handle = layer.Motion()
            .Animate(UIElement.OpacityProperty)
            .To(0.5f)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        Assert.InRange(layer.Opacity, 0.5f, 0.99f);
        Assert.Equal(
            [
                DrawCommandKind.PushOpacity,
                DrawCommandKind.DrawImage,
                DrawCommandKind.PopOpacity
            ],
            Record(surface, new DrawRect(0, 0, 100, 100))
                .Select(command => command.Kind));
    }

    [Fact]
    public void LayerPrismCapturesOnlyThatLayersDescendants()
    {
        Sprite2D before = Sprite(new TestImage("before"), y: 0);
        Sprite2D inside = Sprite(new TestImage("inside"), y: 0);
        Sprite2D after = Sprite(new TestImage("after"), y: 0);
        Scene2D layer = new() { Layer = 1 };
        layer.Children.Add(inside);
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(before);
        scene.Children.Add(layer);
        scene.Children.Add(after);
        before.Layer = 0;
        after.Layer = 2;
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        using IDisposable prism = AttachPrism(layer, "Layer");
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            Assert.Equal(
                [
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism,
                    DrawCommandKind.DrawImage
                ],
                Record(surface, new DrawRect(0, 0, 100, 100))
                    .Select(command => command.Kind));
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }
    }

    [Fact]
    public void GroupAspectAndMotionDriveTheTransformUsedByRendering()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        Scene2D group = new();
        group.Children.Add(Sprite(new TestImage("sprite"), y: 0));
        Scene2D scene = new();
        scene.Children.Add(group);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
        group.Aspect = new ElementAspect(
            [new ElementAspectValue(UIElement.TranslateXProperty, 4f)]);
        root.ProcessFrame();
        Assert.Equal(4, group.TranslateX);

        Cerneala.UI.Motion.Core.MotionHandle handle = group.Motion()
            .Animate(UIElement.TranslateXProperty)
            .To(12)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();

        DrawCommand transform = Assert.Single(
            Record(surface, new DrawRect(0, 0, 100, 100))
                .Where(command => command.Kind == DrawCommandKind.PushTransform));
        Assert.InRange(transform.Transform.M31, 4.01f, 11.99f);
    }

    [Fact]
    public void TemplatedSceneNodeKeepsAspectMotionPrismAndLifecycle()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        ObservableList<string> items = [];
        List<IDisposable> prismAttachments = [];
        SceneItems2D sceneItems = new() { ItemsSource = items };
        sceneItems.Templates.Add(new ContentTemplate<string>(
            "sprite",
            key: null,
            priority: 0,
            context =>
            {
                Sprite2D sprite = new()
                {
                    Source = new TestImage(context.Data!)
                };
                sprite.Aspect = new ElementAspect(
                    [new ElementAspectValue(Sprite2D.TintProperty, Color.Black)]);
                prismAttachments.Add(AttachPrism(sprite, "TemplateSprite"));
                return sprite;
            }));
        Scene2D scene = new();
        scene.Children.Add(sceneItems);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);

        try
        {
            items.Add("item");
            root.ProcessFrame();
            Sprite2D sprite = Assert.IsType<Sprite2D>(Assert.Single(sceneItems.LogicalChildren));
            Assert.True(sprite.IsAttached);
            Assert.Equal(Color.Black, sprite.Tint);

            Cerneala.UI.Motion.Core.MotionHandle handle = sprite.Motion()
                .Animate(Sprite2D.DestinationProperty)
                .To(new DrawRect(10, 0, 1, 1))
                .With(MotionFactory.Tween<DrawRect>(TimeSpan.FromMilliseconds(100)));
            root.ProcessFrame();
            clock.Advance(TimeSpan.FromMilliseconds(50));
            root.ProcessFrame();
            Assert.InRange(sprite.Destination.X, 0.01f, 9.99f);
            Assert.Equal(
                [
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism
                ],
                Record(surface, new DrawRect(0, 0, 100, 100))
                    .Select(command => command.Kind));

            items.Add("second");
            items.Move(0, 1);
            Assert.False(sprite.IsAttached);
            Sprite2D[] moved = sceneItems.LogicalChildren
                .Cast<Sprite2D>()
                .ToArray();
            Assert.Equal(2, moved.Length);
            Assert.All(moved, node => Assert.True(node.IsAttached));
            Assert.All(moved, node => Assert.Equal(Color.Black, node.Tint));
            Assert.Equal(
                [
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism,
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism
                ],
                Record(surface, new DrawRect(0, 0, 100, 100))
                    .Select(command => command.Kind));

            Sprite2D unaffectedByReplace = moved[1];
            items[0] = "replacement";
            Sprite2D[] replaced = sceneItems.LogicalChildren
                .Cast<Sprite2D>()
                .ToArray();
            Assert.False(moved[0].IsAttached);
            Assert.Same(unaffectedByReplace, replaced[1]);
            Assert.Equal(Color.Black, replaced[0].Tint);

            items.RemoveAt(0);
            Assert.All(replaced, node => Assert.False(node.IsAttached));
            Sprite2D beforeReattach = Assert.IsType<Sprite2D>(
                Assert.Single(sceneItems.LogicalChildren));
            Assert.True(beforeReattach.IsAttached);
            root.VisualChildren.Remove(surface);
            Assert.False(beforeReattach.IsAttached);
            root.VisualChildren.Add(surface);
            Sprite2D afterReattach = Assert.IsType<Sprite2D>(
                Assert.Single(sceneItems.LogicalChildren));
            Assert.NotSame(beforeReattach, afterReattach);
            Assert.True(afterReattach.IsAttached);
            Assert.Equal(Color.Black, afterReattach.Tint);
            Assert.Equal(
                [
                    DrawCommandKind.BeginPrism,
                    DrawCommandKind.DrawImage,
                    DrawCommandKind.EndPrism
                ],
                Record(surface, new DrawRect(0, 0, 100, 100))
                    .Select(command => command.Kind));
        }
        finally
        {
            foreach (IDisposable attachment in prismAttachments)
            {
                attachment.Dispose();
            }
        }
    }

    private static Sprite2D Sprite(TestImage image, float y)
    {
        return new Sprite2D
        {
            Source = image,
            Destination = new DrawRect(0, y, 1, 1)
        };
    }

    private static IDisposable AttachPrism(UIElement element, string name)
    {
        return GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                PrismTestData.Composition(
                    name,
                    PrismTestData.Layer(1, "Content"))));
    }

    private static IEnumerable<IDrawImage?> Images(DrawCommandList commands) =>
        commands.Where(command => command.Kind == DrawCommandKind.DrawImage)
            .Select(command => command.Image);

    private static DrawCommandList Record(RenderSurface2D surface, DrawRect bounds)
    {
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, bounds);
        return commands;
    }

    private sealed class TestImage(string name) : IDrawImage
    {
        public string Name { get; } = name;

        public int Width => 16;

        public int Height => 16;
    }

    private sealed class ThrowingNode : SceneNode2D
    {
        internal override void Record(Scene2DRecordContext context) =>
            throw new InvalidOperationException("Expected test failure.");

        internal override SceneBounds2D GetVisibleLocalBounds() =>
            SceneBounds2D.Known(new DrawRect(2, 3, 4, 5));
    }
}
