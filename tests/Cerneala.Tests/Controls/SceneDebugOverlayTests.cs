using System.Numerics;
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
    [InlineData(Scene2DDebugFlags.PromotedTiles)]
    public void EveryFlagEmitsOnlyItsOwnCategory(Scene2DDebugFlags flag)
    {
        (Scene2D scene, Scene2DDebugOverlay overlay, _) = Fixture();
        overlay.Flags = flag;
        DrawCommandList commands = Record(scene);
        Scene2DDebugOverlayDiagnostics d = overlay.GetDiagnosticsSnapshot();
        Assert.True(d.Primitives > 0);
        Assert.Equal(flag == Scene2DDebugFlags.Colliders, d.Colliders > 0);
        Assert.Equal(flag == Scene2DDebugFlags.Navigation, d.NavigationCells > 0);
        Assert.Equal(flag == Scene2DDebugFlags.PromotedTiles, d.PromotedTiles > 0);
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
        TileMap2DModel model = map.Model!;
        ManualMotionClock clock = new();
        UIRoot root = new(motionClock: clock);
        RenderSurface2D surface = new() { Scene = scene };
        root.VisualChildren.Add(surface);
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
        Assert.Same(model, map.Model);
    }

    private static object Hit(CollisionHit2D hit) => (hit.Collider, hit.Entity, hit.Point, hit.Normal, hit.Distance, hit.Fraction, hit.IsTrigger);

    private static (Scene2D Scene, Scene2DDebugOverlay Overlay, TileMap2D Map) Fixture(int remoteChunks = 0)
    {
        List<TileChunk2D> chunks = [new(new TileCoordinate2D(0, 0), 4, 4, Enumerable.Repeat(new TileCell2D(1), 16))];
        for (int i = 0; i < remoteChunks; i++) { chunks.Add(new TileChunk2D(new TileCoordinate2D(1000 + i * 4, 1000), 4, 4, Enumerable.Repeat(new TileCell2D(1), 16))); }
        TileMap2D map = new() { Model = new TileMap2DModel(new DrawSize(16, 16),
            [new TileSet2D("atlas", new ResourceId<ImageResource>("atlas"), [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16))])],
            [new TileLayer2DModel("ground", chunks)]) };
        map.Resources.SetResource(new ResourceId<ImageResource>("atlas"), new ImageResource(new TestImage()));
        map.Promote(new TileCellKey2D("ground", 1, 1)).TranslateX = 8;
        Scene2DDebugOverlay overlay = new() { NavigationGrid = new Grid() };
        Scene2D scene = new();
        scene.Children.Add(map);
        scene.Children.Add(new BoxCollider2D { Width = 16, Height = 16 });
        scene.Children.Add(new BoxCollider2D { Width = 16, Height = 16, TranslateX = 10000 });
        scene.Children.Add(overlay);
        return (scene, overlay, map);
    }

    private static DrawCommandList Record(Scene2D scene)
    {
        RenderSurface2D surface = new() { Scene = scene };
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
