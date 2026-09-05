using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using SkiaSharp;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.SmokeTests;

internal sealed class SceneDebugOverlayConformanceFixture : IDisposable
{
    internal static readonly string[] CaptureNames =
    [
        "debug-off.png", "debug-colliders.png", "debug-chunks.png", "debug-coordinates.png",
        "debug-ids.png", "debug-order.png", "debug-navigation.png", "debug-promoted.png",
        "debug-all.png", "debug-effects.png", "debug-zoom.png", "debug-off-restored.png"
    ];
    private readonly Scene2DDebugOverlay overlay;
    private readonly BoxCollider2D pickTarget;
    private readonly TileMap2D map;
    private readonly TileMap2DModel model;
    private readonly List<object> samples = [];
    private readonly string directory;
    private IDisposable? prism;
    private object[]? initialHits;
    private long initialVersion;
    private DrawRect recordedFrameBounds;
    internal RenderSurface2D Surface { get; }

    internal SceneDebugOverlayConformanceFixture(string directory)
    {
        this.directory = Path.GetFullPath(directory);
        Directory.CreateDirectory(this.directory);
        string atlasPath = Path.Combine(this.directory, "debug-atlas.png");
        WriteAtlas(atlasPath);
        ResourceId<ImageResource> atlas = new("DebugAtlas");
        List<TileChunk2D> chunks = [];
        for (int x = 0; x < 3; x++)
        {
            chunks.Add(new TileChunk2D(new TileCoordinate2D(x * 4, 0), 4, 3,
                Enumerable.Range(0, 12).Select(index => new TileCell2D(index % 2 + 1))));
        }
        for (int x = 0; x < 1024; x++)
        {
            chunks.Add(new TileChunk2D(new TileCoordinate2D(10000 + x * 4, 10000), 4, 3,
                Enumerable.Repeat(new TileCell2D(1), 12)));
        }
        model = new TileMap2DModel(new DrawSize(24, 24),
            [new TileSet2D("Atlas", atlas, [new TileDefinition2D(1, new DrawRect(0, 0, 16, 16)), new TileDefinition2D(2, new DrawRect(16, 0, 16, 16))])],
            [new TileLayer2DModel("Ground", chunks, order: 2, offset: new DrawPoint(8, 108))]);
        map = new TileMap2D { Model = model };
        TileInstance2D tile = map.Promote(new TileCellKey2D("Ground", 6, 1));
        tile.TranslateX = 9;
        tile.TranslateY = -5;
        tile.Rotation = 0.2f;
        tile.TransformOrigin = new DrawPoint(12, 12);
        tile.Colliders.Add(new BoxCollider2D { Width = 24, Height = 24 });
        Scene2D scene = new() { OrderMode = SceneOrderMode.LayerThenY };
        scene.Children.Add(map);
        pickTarget = new BoxCollider2D { Width = 40, Height = 24, TranslateX = 12, TranslateY = 20, CollisionLayer = 1 };
        scene.Children.Add(pickTarget);
        scene.Children.Add(new CircleCollider2D { Radius = 14, TranslateX = 96, TranslateY = 36, ScaleX = 1.5f, IsTrigger = true });
        scene.Children.Add(new PolygonCollider2D { Points = "0,0 35,8 20,32", TranslateX = 164, TranslateY = 20, CollisionLayer = 4 });
        scene.Children.Add(new SegmentCollider2D { EndX = 40, EndY = 22, TranslateX = 244, TranslateY = 24, CollisionMask = 0 });
        scene.Children.Add(new BoxCollider2D { Width = 100, Height = 100, TranslateX = 10000 });
        overlay = new Scene2DDebugOverlay { FontSize = 5, NavigationGrid = new Navigation() };
        overlay.Aspect = new ElementAspect([new ElementAspectValue(Scene2DDebugOverlay.LineThicknessProperty, 0.75f)]);
        scene.Children.Add(overlay);
        Surface = new RenderSurface2D
        {
            Scene = scene, ViewBox = new DrawRect(0, 0, 320, 210),
            ClearColor = new Color(12, 20, 32)
        };
        Surface.Resources.SetResource(atlas, new ImageResource(atlasPath));
        Surface.Draw += (_, frame) => recordedFrameBounds = frame.Bounds;
    }

    internal void SelectSample(int sample)
    {
        overlay.Flags = sample switch
        {
            0 or 11 => Scene2DDebugFlags.None,
            1 => Scene2DDebugFlags.Colliders,
            2 => Scene2DDebugFlags.ChunkBounds,
            3 => Scene2DDebugFlags.TileCoordinates,
            4 => Scene2DDebugFlags.TileIds,
            5 => Scene2DDebugFlags.Order,
            6 => Scene2DDebugFlags.Navigation,
            7 => Scene2DDebugFlags.PromotedTiles,
            _ => Scene2DDebugFlags.All
        };
        if (sample == 9)
        {
            prism = GeneratedMarkup.AttachPrism(overlay, () => new PrismInstance(
                new PrismCompositionDefinition("DebugOnly", [new PrismLayerDefinition(new PrismNodeId(1), "Ink",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])])));
            overlay.Motion().Animate(Scene2DDebugOverlay.LineThicknessProperty).To(1.25f)
                .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100))).Complete();
            overlay.Motion().Animate(UIElement.OpacityProperty).To(0.7f)
                .With(MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100))).Complete();
        }
        if (sample == 10) { Surface.ViewBox = new DrawRect(16, 8, 240, 157.5f); }
        if (sample == 11)
        {
            prism?.Dispose();
            prism = null;
            Surface.ViewBox = new DrawRect(0, 0, 320, 210);
        }
    }

    internal void VerifySample(int sample)
    {
        Scene2D scene = Surface.Scene!;
        CollisionWorld2DDiagnosticsSnapshot world = scene.CollisionWorld.GetDiagnosticsSnapshot();
        object[] hits = scene.CollisionWorld.Raycast(new Vector2(0, 30), Vector2.UnitX, 320)
            .Select(hit => (object)(hit.Collider, hit.Entity, hit.Point, hit.Normal, hit.Distance, hit.Fraction, hit.IsTrigger)).ToArray();
        if (sample == 0)
        {
            initialHits = hits;
            initialVersion = world.IncrementalUpdateCount;
            if (overlay.GetValueSource(Scene2DDebugOverlay.LineThicknessProperty) != UiPropertyValueSource.AspectBase)
                throw new InvalidOperationException("Overlay Aspect did not apply through the scene tree.");
        }
        if (!hits.SequenceEqual(initialHits!) || world.IncrementalUpdateCount != initialVersion || !ReferenceEquals(model, map.Model))
            throw new InvalidOperationException("Debug presentation changed collision results, mutations, or map data.");
        Vector2 rootPoint = Surface.SceneToRoot(new Vector2(24, 30));
        if (!ReferenceEquals(new HitTestService().HitTest(Surface.Root!, rootPoint.X, rootPoint.Y)?.Element, pickTarget))
            throw new InvalidOperationException("Debug presentation changed geometric picking.");
        Scene2DDebugOverlayDiagnostics debug = overlay.GetDiagnosticsSnapshot();
        if ((sample is 0 or 11) != (debug.Primitives == 0))
            throw new InvalidOperationException("The selected debug flag did not produce its expected ink state.");
        if (debug.CandidateChunks > 4 || debug.VisitedTiles > 36 || debug.Colliders > 5 || debug.NavigationCells > 24)
            throw new InvalidOperationException("Overlay diagnostic work escaped the bounded viewport fixture.");
        if (sample is 9 or 10 && (Math.Abs(overlay.LineThickness - 1.25f) > 0.001f || Math.Abs(overlay.Opacity - 0.7f) > 0.001f))
            throw new InvalidOperationException("Overlay Motion did not commit the deterministic endpoint.");
        samples.Add(new { Sample = sample, File = CaptureNames[sample], Flags = overlay.Flags.ToString(), Debug = debug,
            world.EntryCount, world.RebuildCount, world.IncrementalUpdateCount, Picking = "PASS", Collision = "PASS" });
    }

    internal void VerifyCaptures(string backend)
    {
        using SKBitmap off = SKBitmap.Decode(Path.Combine(directory, CaptureNames[0]));
        List<object> captures = [];
        for (int sample = 0; sample < CaptureNames.Length; sample++)
        {
            string path = Path.Combine(directory, CaptureNames[sample]);
            using SKBitmap image = SKBitmap.Decode(path) ?? throw new InvalidOperationException($"Cannot decode {path}.");
            if (image.Width != off.Width || image.Height != off.Height || image.Width < 640 || image.Height < 420)
                throw new InvalidOperationException("Unexpected debug capture dimensions.");
            long changed = 0;
            for (int y = 0; y < image.Height; y++)
            for (int x = 0; x < image.Width; x++)
                if (image.GetPixel(x, y) != off.GetPixel(x, y)) { changed++; }
            if (sample is > 0 and < 11 && changed < 64)
                throw new InvalidOperationException($"{CaptureNames[sample]} did not show its flag (only {changed} changed pixels).");
            if (sample == 11 && changed != 0)
                throw new InvalidOperationException("Turning debug off did not restore the exact initial gameplay pixels.");
            captures.Add(new { File = CaptureNames[sample], image.Width, image.Height, ChangedFromOff = changed,
                Sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) });
        }
        File.WriteAllText(Path.Combine(directory, "debug-backend.json"), JsonSerializer.Serialize(new
        {
            Backend = backend, CaptureApi = "Window.SaveScreenshot", Captures = captures, Samples = samples,
            RecordedFrameBounds = recordedFrameBounds,
            Motion = "Explicit completion endpoints; interpolation separately verified by core tests.",
            ManualValidation = "Not performed"
        }, new JsonSerializerOptions { WriteIndented = true }));
        if (recordedFrameBounds != new DrawRect(0, 0, off.Width, off.Height))
            throw new InvalidOperationException($"RenderSurface2D frame bounds must use local surface pixels: expected {off.Width}x{off.Height}, recorded {recordedFrameBounds.Width}x{recordedFrameBounds.Height}.");
    }

    public void Dispose() { prism?.Dispose(); }

    private static void WriteAtlas(string path)
    {
        using SKBitmap bitmap = new(32, 16);
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 32; x++)
            bitmap.SetPixel(x, y, x < 16 ? new SKColor(32, (byte)(65 + y * 2), 48) : new SKColor(65, 58, (byte)(40 + x)));
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData png = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, png.ToArray());
    }

    private sealed class Navigation : IScene2DDebugNavigationGrid
    {
        public TileMapBounds2D Bounds => new(0, 0, 12, 2);
        public DrawPoint Origin => new(8, 60);
        public DrawSize CellSize => new(24, 20);
        public bool TryGetCell(int x, int y, out bool blocked) { blocked = (x + y) % 3 == 0; return x != 11; }
    }
}
