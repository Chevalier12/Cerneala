using System.Numerics;
using System.Collections.ObjectModel;
using System.Reflection;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class RenderSurface2DSceneFoundationContractTests
{
    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void ViewBoxAndGroupTransformsDoNotMagnifySpriteOrGroupStyleSizes(float groupScale)
    {
        Sprite2D sprite = new()
        {
            Image = new(new TestImage("styled sprite")),
            X = 1, Y = 1, Width = 2, Height = 2
        };
        Scene2D group = new() { Scale = groupScale };
        group.Children.Add(sprite);
        Scene2D scene = new();
        scene.Children.Add(group);
        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 10, 20),
            Stretch = DrawBrushStretch.Uniform
        };
        UIRoot root = new();
        using IDisposable groupPrism = AttachStyles(group);
        using IDisposable spritePrism = AttachStyles(sprite);
        ElementLifecycle.AttachSubtree(root, surface);
        try
        {
            PrismGraph graph = new PrismGraphBuilder().Build(
                new PrismFrameAnalyzer().Analyze(
                    Record(surface, new DrawRect(0, 0, 563, 755))));
            PrismGraph optimized = new PrismGraphOptimizer().Optimize(graph).OptimizedGraph;
            Assert.Equal(2, optimized.Scopes.Length);
            PrismGraphNode[] styles = optimized.Nodes.Where(
                node => node.Kind == PrismGraphNodeKind.Style).ToArray();
            Assert.Equal(4, styles.Length);
            foreach (PrismGraphNode node in styles)
            {
                PrismGraphScope scope = optimized.Scopes.Single(
                    candidate => candidate.AnalysisScopeIndex == node.AnalysisScopeIndex);
                Assert.Equal(37.75f * groupScale, scope.EffectiveTransform.M11);
                PrismStylePlan plan = PrismStylePlanner.Create(node, scope);
                Assert.Equal(5f * scope.PixelScale,
                    PrismStylePlanner.ResolveSamplingGeometry(plan, scope).Size);
            }
        }
        finally
        {
            ElementLifecycle.DetachSubtree(root, surface);
        }

        static IDisposable AttachStyles(UIElement owner) => GeneratedMarkup.AttachPrism(
            owner,
            () => new PrismInstance(PrismTestData.Composition(
                "Scene style units",
                new PrismLayerDefinition(new PrismNodeId(1), "Content", styles:
                [
                    new PrismStyleDefinition(PrismStyleId.OuterGlow),
                    new PrismStyleDefinition(PrismStyleId.BevelEmboss)
                ]))));
    }

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
        sprite.X = destination.X; sprite.Y = destination.Y; sprite.Width = destination.Width; sprite.Height = destination.Height;
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
            Image = new(new TestImage("sprite")),
            X = 2, Y = 3, Width = 4, Height = 5
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
            Image = new(hidden),
            X = 0, Y = -100, Width = 1, Height = 1,
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

    [Theory]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void OrderedModesNeitherOrderNorRecordClearlyOffscreenSprites(SceneOrderMode mode)
    {
        Scene2D scene = new() { OrderMode = mode };
        Sprite2D visibleLow = AreaSprite("visible low", 10, 60);
        Sprite2D visibleHigh = AreaSprite("visible high", 30, 20);
        Sprite2D visibleSide = AreaSprite("visible side", 50, 20);
        scene.Children.Add(AreaSprite("far right", 1000, 20));
        scene.Children.Add(visibleLow);
        scene.Children.Add(AreaSprite("far left", -1000, 20));
        scene.Children.Add(visibleHigh);
        scene.Children.Add(AreaSprite("far below", 20, 1000));
        scene.Children.Add(visibleSide);
        scene.Children.Add(AreaSprite("far above", 20, -1000));
        RenderSurface2D surface = new() { Scene = scene };

        DrawCommandList commands = Record(surface, new DrawRect(0, 0, 100, 100));

        SceneNode2D[] expected = mode == SceneOrderMode.Layer
            ? [visibleLow, visibleHigh, visibleSide]
            : [visibleHigh, visibleSide, visibleLow];
        Assert.Equal(expected.Length, scene.RecordedOrder.Count);
        Assert.Equal(expected, scene.RecordedOrder.Select(entry => entry.Node));
        Assert.Equal(
            mode == SceneOrderMode.Layer
                ? ["visible low", "visible high", "visible side"]
                : ["visible high", "visible side", "visible low"],
            ImageNames(commands));
    }

    [Theory]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void CulledOrderedPathKeepsTheFullSortOrderOfTheVisibleChildren(SceneOrderMode mode)
    {
        // Name, layer, x, y; every sprite is 10x10, so the Y anchor is y + 10.
        (string Name, int Layer, float X, float Y)[] sprites =
        [
            ("a", 0, 10, 50), ("far f", 0, 500, 10), ("b", 0, 30, 20),
            ("far g", -1, -500, 0), ("c tie of a", 0, 50, 50), ("live", 0, 0, 0),
            ("d", 1, 0, 0), ("far h", 1, 20, 900), ("e edge", -1, 95, 80),
            ("i", -1, 70, 20), ("j tie of b", 0, 70, 20)
        ];
        Scene2D scene = new() { OrderMode = mode };
        Scene2D live = new();
        live.Children.Add(AreaSprite("live sprite", 60, 60));
        List<(SceneNode2D Node, string Name, int Layer, float Bottom, bool Visible)> all = [];
        foreach ((string name, int layer, float x, float y) in sprites)
        {
            SceneNode2D node = name == "live" ? live : AreaSprite(name, x, y);
            node.Layer = layer;
            scene.Children.Add(node);
            all.Add((node, name == "live" ? "live sprite" : name, layer,
                name == "live" ? 70 : y + 10, !name.StartsWith("far")));
        }
        RenderSurface2D surface = new() { Scene = scene };

        DrawCommandList commands = Record(surface, new DrawRect(0, 0, 100, 100));

        var expected = all
            .Select((item, index) => (item, index))
            .OrderBy(pair => pair.item.Layer)
            .ThenBy(pair => mode == SceneOrderMode.LayerThenY ? pair.item.Bottom : 0)
            .ThenBy(pair => pair.index)
            .Where(pair => pair.item.Visible)
            .Select(pair => pair.item)
            .ToArray();
        // The recorded order may also hold culled children; the visible ones keep their full-sort order.
        Assert.Equal(
            expected.Select(item => item.Node),
            scene.RecordedOrder.Select(entry => entry.Node).Where(node => expected.Any(item => item.Node == node)));
        Assert.Equal(expected.Select(item => item.Name), ImageNames(commands));
    }

    [Fact]
    public void DebugOverlayKeepsTheFullOrderedPathAndEveryChildOrdered()
    {
        Scene2D scene = new() { OrderMode = SceneOrderMode.LayerThenY };
        Sprite2D visible = AreaSprite("visible", 10, 10);
        Sprite2D offscreen = AreaSprite("offscreen", 1000, 10);
        Scene2DDebugOverlay overlay = new();
        scene.Children.Add(offscreen);
        scene.Children.Add(overlay);
        scene.Children.Add(visible);
        RenderSurface2D surface = new() { Scene = scene };

        Record(surface, new DrawRect(0, 0, 100, 100));

        Assert.Equal(
            [offscreen, visible],
            scene.RecordedOrder.Select(entry => entry.Node));
    }

    [Theory]
    [InlineData(SceneOrderMode.Source)]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void RecordCountersCountOnlyTheVisibleChildrenRecordedAndOrderedPerFrame(SceneOrderMode mode)
    {
        Scene2D scene = new() { OrderMode = mode };
        for (int index = 0; index < 5; index++)
        {
            scene.Children.Add(AreaSprite($"visible {index}", 10 + index * 15, 20));
            scene.Children.Add(AreaSprite($"far right {index}", 1000 + index * 15, 20));
            scene.Children.Add(AreaSprite($"far below {index}", 20, 1000 + index * 15));
        }
        RenderSurface2D surface = new() { Scene = scene };
        SceneRecordCounters2D counters = scene.RecordCounters;
        Assert.Equal(0, counters.RecordedChildren);
        Assert.Equal(0, counters.OrderedChildren);

        for (int frame = 0; frame < 3; frame++)
        {
            long recorded = counters.RecordedChildren;
            long ordered = counters.OrderedChildren;

            DrawCommandList commands = Record(surface, new DrawRect(0, 0, 100, 100));

            Assert.Equal(5, ImageNames(commands).Count());
            Assert.Equal(5, counters.RecordedChildren - recorded);
            // The Source fast path builds no order entries; the ordered modes build one per kept child.
            Assert.Equal(mode == SceneOrderMode.Source ? 0 : 5, counters.OrderedChildren - ordered);
        }
    }

    [Theory]
    [InlineData(SceneOrderMode.Source)]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void RecordCountersCoverEveryChildWhenADebugOverlayKeepsTheFullPath(SceneOrderMode mode)
    {
        Scene2D scene = new() { OrderMode = mode };
        for (int index = 0; index < 4; index++)
        {
            scene.Children.Add(AreaSprite($"visible {index}", 10 + index * 15, 20));
            scene.Children.Add(AreaSprite($"far right {index}", 1000 + index * 15, 20));
        }
        scene.Children.Add(new Scene2DDebugOverlay());
        RenderSurface2D surface = new() { Scene = scene };
        SceneRecordCounters2D counters = scene.RecordCounters;

        for (int frame = 0; frame < 2; frame++)
        {
            long recorded = counters.RecordedChildren;
            long ordered = counters.OrderedChildren;

            DrawCommandList commands = Record(surface, new DrawRect(0, 0, 100, 100));

            Assert.Equal(4, ImageNames(commands).Count());
            // Every sprite is ordered, the overlay is not an order entry; the culled sprites are not recorded, the overlay is.
            Assert.Equal(8, counters.OrderedChildren - ordered);
            Assert.Equal(4 + 1, counters.RecordedChildren - recorded);
        }
    }

    [Fact]
    public void SceneItemsRecordCountersCountOnlyTheVisibleItemsRecordedPerFrame()
    {
        // Ten items in a row; the 100 x 100 view shows those at x 0, 15, 30, 45, 60 and 75.
        int[] xs = Enumerable.Range(0, 10).Select(index => index * 15 + (index >= 6 ? 1000 : 0)).ToArray();
        SceneItems2D items = new() { ItemsSource = xs };
        items.Templates.Add(new ContentTemplate<int>(
            "sprite", null, 0, context => AreaSprite($"item {context.Data}", context.Data, 20)));
        Scene2D scene = new();
        scene.Children.Add(items);
        RenderSurface2D surface = new() { Scene = scene };
        UIRoot root = new();
        root.VisualChildren.Add(surface);
        try
        {
            for (int guard = 0; guard < 100 && items.RealizedItemCount < xs.Length; guard++) { root.ProcessFrame(); }
            Assert.Equal(xs.Length, items.RealizedItemCount);
            SceneRecordCounters2D counters = items.RecordCounters;

            for (int frame = 0; frame < 3; frame++)
            {
                long recorded = counters.RecordedChildren;
                long ordered = counters.OrderedChildren;

                DrawCommandList commands = Record(surface, new DrawRect(0, 0, 100, 100));

                Assert.Equal(6, ImageNames(commands).Count());
                Assert.Equal(6, counters.RecordedChildren - recorded);
                Assert.Equal(0, counters.OrderedChildren - ordered);
            }
        }
        finally
        {
            root.VisualChildren.Remove(surface);
        }
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
            Image = new(first),
            X = 0, Y = 0, Width = 1, Height = 1,
            Layer = 1
        });
        nested.Children.Add(new Sprite2D
        {
            Image = new(second),
            X = 0, Y = 0, Width = 1, Height = 1,
            Layer = 0
        });
        Scene2D scene = new() { OrderMode = SceneOrderMode.Layer };
        scene.Children.Add(nested);
        scene.Children.Add(new Sprite2D
        {
            Image = new(sibling),
            X = 0, Y = 0, Width = 1, Height = 1,
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
        Sprite2D movingSprite = new() { Image = new(moving) };
        Sprite2D fixedSprite = Sprite(fixedImage, y: 10);
        Scene2D scene = new() { OrderMode = SceneOrderMode.LayerThenY };
        scene.Children.Add(movingSprite);
        scene.Children.Add(fixedSprite);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
        root.ProcessFrame();

        Cerneala.UI.Motion.Core.MotionHandle handle = movingSprite.Motion()
            .Animate(Sprite2D.YProperty)
            .To(20f)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        Assert.True(handle.IsActive);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(75));
        root.ProcessFrame();

        Assert.InRange(movingSprite.Y, 10.01f, 19.99f);
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
        ObservableCollection<string> items = [];
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
                    Image = new(new TestImage(context.Data!))
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
                .Animate(Sprite2D.XProperty)
                .To(10f)
                .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
            root.ProcessFrame();
            clock.Advance(TimeSpan.FromMilliseconds(50));
            root.ProcessFrame();
            Assert.InRange(sprite.X, 0.01f, 9.99f);
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
            Assert.True(sprite.IsAttached);
            Sprite2D[] moved = sceneItems.LogicalChildren
                .Cast<Sprite2D>()
                .ToArray();
            Assert.Equal(2, moved.Length);
            Assert.Same(sprite, moved[1]);
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
            Assert.False(replaced[0].IsAttached);
            Assert.True(replaced[1].IsAttached);
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
            Image = new(image),
            X = 0, Y = y, Width = 1, Height = 1
        };
    }

    private static Sprite2D AreaSprite(string name, float x, float y) =>
        new()
        {
            Image = new(new TestImage(name)),
            X = x, Y = y, Width = 10, Height = 10
        };

    private static IEnumerable<string> ImageNames(DrawCommandList commands) =>
        Images(commands).Select(image => ((TestImage)image!).Name);

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
