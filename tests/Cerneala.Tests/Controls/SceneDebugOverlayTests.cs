using System.Numerics;
using System.Globalization;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.Tests.Controls;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class SceneDebugOverlayTests
{
    [Fact]
    public void CachedTileLabelsFollowCellValuesFlagsAndMutableCultureFormatting()
    {
        TileMap2DModel Model(int tileId) => new("ground", new DrawSize(16, 16),
            [new TileSet2D("atlas", new ResourceId<ImageResource>("atlas"),
                [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16)),
                 new TileDefinition2D(2, new DrawRect(0, 0, 16, 16))])],
            [new TileChunk2D(new TileCoordinate2D(-1, -1), 1, 1, [new TileCell2D(tileId)], version: tileId)]);
        TileMap2D map = new() { Source = TileMapTestSource.Create(Model(1)), TranslateX = 16, TranslateY = 16 };
        map.Resources.SetResource(new ResourceId<ImageResource>("atlas"), new ImageResource(new TestImage()));
        Scene2DDebugOverlay overlay = new() { Flags = Scene2DDebugFlags.TileCoordinates | Scene2DDebugFlags.TileIds };
        Scene2D scene = new();
        scene.Children.Add(map);
        scene.Children.Add(overlay);
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommand Label()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
            return Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        }
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        try
        {
            CultureInfo.CurrentCulture = culture;
            culture.NumberFormat.NegativeSign = "~";
            DrawCommand first = Label();
            Assert.Equal("~1,~1 #1", first.Text);
            Assert.Same(first.TextRun, Label().TextRun);
            overlay.Flags |= Scene2DDebugFlags.ChunkBounds;
            Assert.Same(first.TextRun, Label().TextRun);

            // The same CultureInfo can change in place; identity is not a sufficient cache key.
            culture.NumberFormat.NegativeSign = "minus";
            DrawCommand reformatted = Label();
            Assert.Equal("minus1,minus1 #1", reformatted.Text);
            Assert.NotSame(first.TextRun, reformatted.TextRun);
            overlay.Flags = Scene2DDebugFlags.TileCoordinates;
            Assert.Equal("minus1,minus1", Label().Text);
            overlay.Flags = Scene2DDebugFlags.TileIds;
            Assert.Equal(" #1", Label().Text);
            map.PublishModel(Model(2));
            Assert.Equal(" #2", Label().Text);
            overlay.Flags = Scene2DDebugFlags.TileCoordinates | Scene2DDebugFlags.TileIds;
            Assert.Equal("minus1,minus1 #2", Label().Text);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData(Scene2DDebugFlags.TileCoordinates)]
    [InlineData(Scene2DDebugFlags.TileIds)]
    [InlineData(Scene2DDebugFlags.TileCoordinates | Scene2DDebugFlags.TileIds)]
    public void UnchangedTileLabelsDoNotAllocateTheirTextAgain(Scene2DDebugFlags flags)
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommandList commands = new();
        long BytesPerFrame(Scene2DDebugFlags selected)
        {
            overlay.Flags = selected;
            void RecordFrame()
            {
                commands.Clear();
                ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
            }
            for (int warmup = 0; warmup < 64; warmup++) { RecordFrame(); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 64; index++) { RecordFrame(); }
            return (GC.GetAllocatedBytesForCurrentThread() - before) / 64;
        }

        long outlines = BytesPerFrame(Scene2DDebugFlags.ChunkBounds);
        long labels = BytesPerFrame(Scene2DDebugFlags.ChunkBounds | flags);
        Assert.Equal(16, commands.Count(command => command.Kind == DrawCommandKind.DrawText));
        Assert.True(labels - outlines <= 128,
            $"Unchanged labels allocated {labels - outlines:N0} extra bytes/frame for 16 tiles.");
    }

    [Fact]
    public void CombinedTileLabelsDoNotAllocateIntermediateFormattedStrings()
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommandList commands = new();
        long BytesPerFrame(Scene2DDebugFlags flags)
        {
            overlay.Flags = flags;
            void RecordFrame()
            {
                commands.Clear();
                ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
            }
            for (int warmup = 0; warmup < 64; warmup++) { RecordFrame(); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 64; index++) { RecordFrame(); }
            return (GC.GetAllocatedBytesForCurrentThread() - before) / 64;
        }

        long coordinates = BytesPerFrame(Scene2DDebugFlags.TileCoordinates);
        long combined = BytesPerFrame(Scene2DDebugFlags.TileCoordinates | Scene2DDebugFlags.TileIds);
        // Six-character labels may occupy a larger string allocation than
        // three-character labels, but do not need separate ID/coordinate strings.
        Assert.True(combined - coordinates <= 16 * 16 + 128,
            $"Adding IDs allocated {combined - coordinates:N0} extra bytes/frame for 16 labels.");
        Assert.Equal("0,0 #1", commands.First(command => command.Kind == DrawCommandKind.DrawText).Text);
        BytesPerFrame(Scene2DDebugFlags.TileIds);
        Assert.All(commands.Where(command => command.Kind == DrawCommandKind.DrawText),
            command => Assert.Equal(" #1", command.Text));
    }

    [Fact]
    public void ConsecutiveFramesRetainOnlyUnchangedVisibleLabelPayloads()
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        const Scene2DDebugFlags labels = Scene2DDebugFlags.TileCoordinates | Scene2DDebugFlags.TileIds;
        overlay.Flags = labels;
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommand[] RecordLabels()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
            return commands.Where(command => command.Kind == DrawCommandKind.DrawText).ToArray();
        }

        DrawCommand[] first = RecordLabels();
        DrawCommand[] second = RecordLabels();
        Assert.Equal(16, first.Length);
        Assert.Equal(first.Length, second.Length);
        for (int index = 0; index < first.Length; index++)
        {
            Assert.Same(first[index].TextRun, second[index].TextRun);
            Assert.Equal(first[index], second[index]);
        }

        overlay.FontSize = 12;
        DrawCommand[] resized = RecordLabels();
        Assert.NotSame(first[0].TextRun, resized[0].TextRun);
        Assert.Equal(12, resized[0].TextRun!.Size);
        Assert.Equal(first[0].Text, resized[0].Text);

        // A label absent for one complete frame must not remain in a history cache.
        overlay.Flags = Scene2DDebugFlags.ChunkBounds;
        Assert.Empty(RecordLabels());
        overlay.Flags = labels;
        DrawCommand[] returned = RecordLabels();
        Assert.NotSame(resized[0].TextRun, returned[0].TextRun);

        overlay.Flags = Scene2DDebugFlags.None;
        Assert.Empty(RecordLabels());
        overlay.Flags = labels;
        Assert.NotSame(returned[0].TextRun, RecordLabels()[0].TextRun);
    }

    [Fact]
    public void DebugOutlinesUseExplicitCenteredVectorStrokesWithoutIdentityOpacityLayers()
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        overlay.Flags = Scene2DDebugFlags.ChunkBounds;
        overlay.LineThickness = 0.75f;
        DrawCommandList commands = Record(scene);
        DrawCommand rectangle = Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.DrawRectangle));
        Assert.NotNull(rectangle.Pen);
        Assert.Equal(0.75f, rectangle.Pen.Thickness);
        Assert.Equal(DrawStrokeAlignment.Center, rectangle.Pen.Style.Alignment);
        Assert.DoesNotContain(commands, c => c.Kind == DrawCommandKind.PushOpacity);
    }

    [Fact]
    public void DisabledOverlayRecordHasNoCommandsOrAllocationsAfterWarmup()
    {
        Scene2DDebugOverlay overlay = new();
        Scene2D scene = new();
        scene.Children.Add(overlay);
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(commands, new DrawRect(0, 0, 100, 100), TimeSpan.Zero);
        Scene2DRecordContext context = new(surface, frame, Matrix3x2.Identity, frame.Bounds);
        for (int i = 0; i < 256; i++) { overlay.Record(context); }
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++) { overlay.Record(context); }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.Equal(0, allocated);
        Assert.Empty(commands);
        Assert.Equal(default, overlay.GetDiagnosticsSnapshot());
    }

    [Theory]
    [InlineData(Scene2DDebugFlags.Colliders)]
    [InlineData(Scene2DDebugFlags.ChunkBounds)]
    [InlineData(Scene2DDebugFlags.TileCoordinates)]
    [InlineData(Scene2DDebugFlags.TileIds)]
    [InlineData(Scene2DDebugFlags.Order)]
    [InlineData(Scene2DDebugFlags.Navigation)]
    public void EveryFlagEmitsOnlyItsOwnCategory(Scene2DDebugFlags flag)
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        overlay.Flags = flag;
        DrawCommandList commands = Record(scene);
        Scene2DDebugOverlayDiagnostics d = overlay.GetDiagnosticsSnapshot();
        Assert.True(d.Primitives > 0);
        Assert.Equal(flag == Scene2DDebugFlags.Colliders, d.Colliders > 0);
        Assert.Equal(flag == Scene2DDebugFlags.Navigation, d.NavigationCells > 0);
        Assert.Equal(flag is Scene2DDebugFlags.TileCoordinates or Scene2DDebugFlags.TileIds, d.VisitedTiles > 0);
        Assert.Contains(commands, c => c.Kind is DrawCommandKind.DrawRectangle or DrawCommandKind.DrawText);
    }

    [Fact]
    public void SparseMapAndExternalGridQueriesStayViewportBounded()
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture(remoteChunks: 4096);
        overlay.Flags = Scene2DDebugFlags.All;
        Record(scene);
        Scene2DDebugOverlayDiagnostics d = overlay.GetDiagnosticsSnapshot();
        Assert.InRange(d.CandidateChunks, 1, 4);
        Assert.Equal(16, d.VisitedTiles);
        Assert.Equal(16, d.NavigationCells);
        Assert.Equal(1, d.Colliders);
    }

    [Fact]
    public void OverlayUsesPostPassAndNeverChangesGameplayOrderOrBounds()
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        scene.OrderMode = SceneOrderMode.LayerThenY;
        overlay.Layer = int.MinValue;
        SceneBounds2D before = scene.GetLocalBounds();
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommandList commands = new();
        RenderSurface2DFrame frame = new(commands, new DrawRect(0, 0, 64, 64), TimeSpan.Zero);
        Scene2DRecordContext context = new(surface, frame, Matrix3x2.Identity, frame.Bounds);
        SceneOrderEntry[] order = scene.GetEffectiveOrder(context).ToArray();
        Assert.DoesNotContain(order, entry => ReferenceEquals(entry.Node, overlay));
        overlay.Flags = Scene2DDebugFlags.All;
        overlay.TranslateY = 1000;
        Assert.Equal(before, scene.GetLocalBounds());
        Assert.Equal(order, scene.GetEffectiveOrder(context));
        Assert.False(overlay.ParticipatesInInputRoute);
    }

    [Fact]
    public void AspectMotionAndPrismChangeOnlyDebugPresentation()
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, TileMap2D map) = Fixture();
        overlay.Flags = Scene2DDebugFlags.Colliders;
        TileMap2DModel model = map.GetSourceModel()!;
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64), root);
        overlay.Aspect = new ElementAspect([new ElementAspectValue(Scene2DDebugOverlay.LineThicknessProperty, 2f)]);
        root.ProcessFrame();
        Assert.Equal(2, overlay.LineThickness);
        Assert.Equal(UiPropertyValueSource.AspectBase, overlay.GetValueSource(Scene2DDebugOverlay.LineThicknessProperty));
        using IDisposable prism = GeneratedMarkup.AttachPrism(overlay, () => new PrismInstance(
            PrismTestData.Composition("Debug", PrismTestData.Layer(1, "Content"))));
        var before = scene.CollisionWorld.GetDiagnosticsSnapshot();
        var hits = scene.CollisionWorld.Raycast(new Vector2(0, 8), Vector2.UnitX, 100).Select(Hit).ToArray();
        overlay.Motion().Animate(Scene2DDebugOverlay.LineThicknessProperty).To(4f)
            .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)));
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(50));
        root.ProcessFrame();
        Assert.InRange(overlay.LineThickness, 2.01f, 3.99f);
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
        Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.BeginPrism));
        Assert.Single(commands.Where(c => c.Kind == DrawCommandKind.EndPrism));
        Assert.Equal(hits, scene.CollisionWorld.Raycast(new Vector2(0, 8), Vector2.UnitX, 100).Select(Hit));
        var after = scene.CollisionWorld.GetDiagnosticsSnapshot();
        Assert.Equal(before.RebuildCount, after.RebuildCount);
        Assert.Equal(before.IncrementalUpdateCount, after.IncrementalUpdateCount);
        Assert.Same(model, map.GetSourceModel());
    }

    // The overlay labels its whole scene subtree, so a nested group's children get labels in the order the group
    // recorded them, in every order mode (#945: the Source fast path left the group's recorded order empty).
    [Theory]
    [InlineData("direct", SceneOrderMode.Source, "0: L1 S0 Y0", "1: L0 S1 Y0")]
    [InlineData("nested", SceneOrderMode.Source, "0: L1 S0 Y0", "1: L0 S1 Y0")]
    [InlineData("direct", SceneOrderMode.Layer, "0: L0 S1 Y0", "1: L1 S0 Y0")]
    [InlineData("nested", SceneOrderMode.Layer, "0: L0 S1 Y0", "1: L1 S0 Y0")]
    [InlineData("direct", SceneOrderMode.LayerThenY, "0: L0 S1 Y10", "1: L1 S0 Y10")]
    [InlineData("nested", SceneOrderMode.LayerThenY, "0: L0 S1 Y10", "1: L1 S0 Y10")]
    public void OrderLabelIsDrawnForTheSpriteWhetherItIsADirectChildOrInANestedGroup(
        string row, SceneOrderMode orderMode, string first, string second)
    {
        Scene2D scene = new();
        Scene2D owner = scene;
        if (row == "nested")
        {
            owner = new Scene2D();
            scene.Children.Add(owner);
        }

        owner.OrderMode = orderMode;
        owner.Children.Add(new Sprite2D { Width = 10, Height = 10, Layer = 1 });
        owner.Children.Add(new Sprite2D { X = 20, Width = 10, Height = 10 });
        scene.Children.Add(new Scene2DDebugOverlay { Flags = Scene2DDebugFlags.Order });

        DrawCommandList commands = Record(scene);

        string[] labels = [.. commands
            .Where(command => command.Kind == DrawCommandKind.DrawText)
            .Select(command => command.Text!)];
        Assert.Equal([first, second], labels);
    }

    // A group the gameplay pass did not record in this frame (here: emptied, so its empty bounds are culled) has no
    // recorded order in this frame; the overlay must not label the sprites it recorded in an earlier frame.
    [Theory]
    [InlineData(SceneOrderMode.Source)]
    [InlineData(SceneOrderMode.Layer)]
    [InlineData(SceneOrderMode.LayerThenY)]
    public void OrderLabelsOfANestedGroupComeOnlyFromTheFrameBeingRecorded(SceneOrderMode orderMode)
    {
        Scene2D scene = new();
        Scene2D group = new() { OrderMode = orderMode };
        Sprite2D[] sprites = [new() { Width = 10, Height = 10, Layer = 1 }, new() { X = 20, Width = 10, Height = 10 }];
        foreach (Sprite2D sprite in sprites) { group.Children.Add(sprite); }
        scene.Children.Add(group);
        scene.Children.Add(new Scene2DDebugOverlay { Flags = Scene2DDebugFlags.Order });
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        string[] RecordLabels()
        {
            DrawCommandList commands = new();
            ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
            return [.. commands.Where(command => command.Kind == DrawCommandKind.DrawText).Select(command => command.Text!)];
        }

        Assert.Equal(2, RecordLabels().Length);
        foreach (Sprite2D sprite in sprites)
        {
            group.Children.Remove(sprite);
            scene.Children.Add(sprite);
        }

        // Outer scene slots: the empty group (S0), the overlay (S1, never ordered), then the two sprites (S2, S3).
        Assert.Equal(["1: L1 S2 Y0", "2: L0 S3 Y0"], RecordLabels());
    }

    // A Vision is a sensor, so the overlay draws it in a colour of its own: not a layer colour, not the trigger
    // orange and not the grey of a collider with an empty mask. IsTrigger does not matter on a Vision.
    private static readonly Color VisionColor = new(190, 120, 255);
    private static readonly Color TriggerColor = new(255, 180, 40);
    private static readonly Color MaskedColor = new(150, 150, 150);

    [Fact]
    public void ASpritesVisionIsDrawnAsTwoRaysAndAnArcInTheVisionColourAndItsBodyKeepsItsUsualColour()
    {
        Scene2DDebugOverlay overlay = new() { Flags = Scene2DDebugFlags.Colliders };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D
        {
            X = 20,
            Y = 20,
            Collider = new BoxCollider2D { Width = 8, Height = 8 },
            Vision = new ConeCollider2D { Radius = 30, Angle = 90, Direction = 0 }
        });
        scene.Children.Add(overlay);

        DrawCommandList commands = Record(scene);

        Assert.Equal(2, overlay.GetDiagnosticsSnapshot().Colliders);
        DrawCommand[] lines = [.. commands.Where(command => command.Kind == DrawCommandKind.DrawLine)];
        Assert.Equal(11, lines.Length); // 2 rays and an arc of 9 segments of 10 degrees
        Assert.All(lines, line => Assert.Equal(VisionColor, PenColor(line)));

        DrawCommand body = Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawRectangle));
        Color bodyColor = PenColor(body);
        Assert.NotEqual(VisionColor, bodyColor);
        Assert.NotEqual(TriggerColor, bodyColor);
        Assert.NotEqual(MaskedColor, bodyColor);

        DrawCommand[] labels = [.. commands.Where(command => command.Kind == DrawCommandKind.DrawText)];
        Assert.Equal(2, labels.Length);
        DrawCommand visionLabel = Assert.Single(labels, label => label.Text!.EndsWith(" vision", StringComparison.Ordinal));
        DrawCommand bodyLabel = Assert.Single(labels, label => label.Text!.EndsWith(" solid", StringComparison.Ordinal));
        Assert.Equal(VisionColor, visionLabel.Color);
        Assert.Equal(bodyColor, bodyLabel.Color);
    }

    [Fact]
    public void AFullCircleVisionThatIsATriggerIsStillDrawnInTheVisionColourAndLabelledVision()
    {
        // An angle of 360 degrees is the shape of a circle; the colour comes from the slot, not from the shape.
        Scene2DDebugOverlay overlay = new() { Flags = Scene2DDebugFlags.Colliders };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D
        {
            X = 32,
            Y = 32,
            Vision = new ConeCollider2D { Radius = 20, Angle = 360, IsTrigger = true }
        });
        scene.Children.Add(overlay);

        DrawCommandList commands = Record(scene);

        Assert.Equal(1, overlay.GetDiagnosticsSnapshot().Colliders);
        DrawCommand circle = Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawEllipse));
        Assert.Equal(VisionColor, PenColor(circle));
        DrawCommand label = Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.EndsWith(" vision", label.Text);
        Assert.Equal(VisionColor, label.Color);
    }

    [Fact]
    public void AVisionWithAnEmptyMaskKeepsTheMaskedGreyAndTheMaskedLabel()
    {
        Scene2DDebugOverlay overlay = new() { Flags = Scene2DDebugFlags.Colliders };
        Scene2D scene = new();
        scene.Children.Add(new Sprite2D
        {
            X = 20,
            Y = 20,
            Vision = new ConeCollider2D { Radius = 30, Angle = 90, CollisionMask = 0 }
        });
        scene.Children.Add(overlay);

        DrawCommandList commands = Record(scene);

        Assert.Equal(1, overlay.GetDiagnosticsSnapshot().Colliders);
        Assert.All(
            commands.Where(command => command.Kind == DrawCommandKind.DrawLine),
            line => Assert.Equal(MaskedColor, PenColor(line)));
        DrawCommand label = Assert.Single(commands.Where(command => command.Kind == DrawCommandKind.DrawText));
        Assert.EndsWith(" masked", label.Text);
        Assert.Equal(MaskedColor, label.Color);
    }

    private static Color PenColor(DrawCommand command) =>
        ((Cerneala.UI.Media.SolidColorBrush)command.Pen!.Brush).Color;

    private static object Hit(CollisionHit2D hit) => (hit.Collider, hit.Entity, hit.Point, hit.Normal, hit.Distance, hit.Fraction, hit.IsTrigger);

    private static (Scene2D Scene, Scene2DDebugOverlay Overlay, TileMap2D Map) Fixture(int remoteChunks = 0)
    {
        List<TileChunk2D> chunks = [new(new TileCoordinate2D(0, 0), 4, 4, Enumerable.Repeat(new TileCell2D(1), 16))];
        for (int i = 0; i < remoteChunks; i++) { chunks.Add(new TileChunk2D(new TileCoordinate2D(1000 + i * 4, 1000), 4, 4, Enumerable.Repeat(new TileCell2D(1), 16))); }
        TileMap2D map = new() { Source = TileMapTestSource.Create(new TileMap2DModel("ground", new DrawSize(16, 16),
            [new TileSet2D("atlas", new ResourceId<ImageResource>("atlas"), [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
            chunks)) };
        map.Resources.SetResource(new ResourceId<ImageResource>("atlas"), new ImageResource(new TestImage()));
        Scene2DDebugOverlay overlay = new() { NavigationGrid = new Grid() };
        Scene2D scene = new();
        scene.Children.Add(map);
        scene.Children.Add(new Sprite2D { Collider = new BoxCollider2D { Width = 16, Height = 16 } });
        scene.Children.Add(new Sprite2D { X = 10000, Collider = new BoxCollider2D { Width = 16, Height = 16 } });
        scene.Children.Add(overlay);
        return (scene, overlay, map);
    }

    private static DrawCommandList Record(Scene2D scene)
    {
        RenderSurface2D surface = new() { Scene = scene };
        using TileMapTestSurface host = new(surface, new(0, 0, 64, 64));
        DrawCommandList commands = new();
        ((IRenderSurface2DFrameSource)surface).RecordFrame(commands, new DrawRect(0, 0, 64, 64));
        return commands;
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;
        public int Height => 16;
    }

    private sealed class Grid : IScene2DDebugNavigationGrid
    {
        public TileMapBounds2D Bounds => new(-10000, -10000, 20000, 20000);
        public DrawPoint Origin => default;
        public DrawSize CellSize => new(16, 16);
        public bool TryGetCell(int x, int y, out bool blocked) { blocked = x == y; return true; }
    }
}
