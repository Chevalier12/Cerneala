using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Markup;
using Cerneala.UI.Motion;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using SkiaSharp;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

namespace Cerneala.SmokeTests;

internal sealed class TileMapStage5ConformanceFixture : IDisposable
{
    internal const string InitialCaptureName = "tilemap-conformance-initial.png";
    internal const string MotionCaptureName = "tilemap-conformance-motion-prism.png";
    internal const string PanZoomCaptureName = "tilemap-conformance-pan-zoom.png";

    private const int LogicalWidth = 640;
    private const int LogicalHeight = 420;
    private const int ChannelTolerance = 3;
    private const int RasterEdgeTolerance = 1;

    private readonly IDisposable prismLifetime;
    private bool motionStarted;

    private TileMapStage5ConformanceFixture(
        RenderSurface2D surface,
        TileInstance2D promotedTile,
        IDisposable prismLifetime)
    {
        Surface = surface;
        PromotedTile = promotedTile;
        this.prismLifetime = prismLifetime;
    }

    internal RenderSurface2D Surface { get; }

    internal TileInstance2D PromotedTile { get; }

    internal static TileMapStage5ConformanceFixture Create(string artifactDirectory)
    {
        string fullDirectory = Path.GetFullPath(artifactDirectory);
        Directory.CreateDirectory(fullDirectory);
        string terrainPath = Path.Combine(fullDirectory, "tilemap-conformance-terrain.png");
        string structuresPath = Path.Combine(fullDirectory, "tilemap-conformance-structures.png");
        WriteAtlas(terrainPath, terrain: true);
        WriteAtlas(structuresPath, terrain: false);

        ResourceId<ImageResource> terrainId = new("TileMapStage5Terrain");
        ResourceId<ImageResource> structuresId = new("TileMapStage5Structures");
        TileMap2D map = new()
        {
            Model = new TileMap2DModel(
                new DrawSize(8, 8),
                [
                    new TileSet2D(
                        "Terrain",
                        terrainId,
                        [
                            new TileDefinition2D(1, new DrawRect(0, 0, 16, 16)),
                            new TileDefinition2D(2, new DrawRect(16, 0, 16, 16))
                        ]),
                    new TileSet2D(
                        "Structures",
                        structuresId,
                        [
                            new TileDefinition2D(100, new DrawRect(0, 0, 16, 16)),
                            new TileDefinition2D(101, new DrawRect(16, 0, 16, 16))
                        ])
                ],
                [
                    new TileLayer2DModel(
                        "Ground",
                        CreateGroundChunks(),
                        order: 0,
                        tint: new Color(226, 245, 232)),
                    new TileLayer2DModel(
                        "Structures",
                        CreateStructureChunks(),
                        order: 10,
                        offset: new DrawPoint(1, 1),
                        opacity: 0.72f,
                        tint: new Color(235, 224, 255))
                ],
                new TileMapBounds2D(0, 0, 16, 10))
        };
        map.TranslateX = 4;
        map.TranslateY = 2;

        TileLayer2D structureLayer = map.Layers.Single(static layer =>
            string.Equals(layer.LayerId, "Structures", StringComparison.Ordinal));
        structureLayer.Offset = new DrawPoint(1, 0);
        structureLayer.Tint = new Color(255, 232, 220);
        structureLayer.ScaleX = 1.05f;
        structureLayer.TransformOrigin = new DrawPoint(64, 40);

        TileInstance2D promoted = map.Promote(new TileCellKey2D("Structures", 7, 4));
        promoted.Aspect = new ElementAspect(
            [new ElementAspectValue(TileInstance2D.TintProperty, new Color(255, 150, 64))]);
        promoted.Scale = 1.12f;
        promoted.TranslateX = 1;
        promoted.TranslateY = -1;
        promoted.TransformOrigin = new DrawPoint(4, 4);

        IDisposable prism = GeneratedMarkup.AttachPrism(
            promoted,
            static () => new PrismInstance(
                new PrismCompositionDefinition(
                    "TileMapStage5Promoted",
                    [new PrismLayerDefinition(
                        new PrismNodeId(1),
                        "PromotedTile",
                        filters: [new PrismFilterDefinition(PrismFilterId.Invert)])])));

        Scene2D scene = new();
        scene.Children.Add(map);
        RenderSurface2D surface = new()
        {
            Scene = scene,
            ViewBox = new DrawRect(0, 0, 128, 84)
        };
        surface.Resources.SetResource(terrainId, new ImageResource(terrainPath));
        surface.Resources.SetResource(structuresId, new ImageResource(structuresPath));
        return new TileMapStage5ConformanceFixture(surface, promoted, prism);
    }

    internal void StartMotion()
    {
        if (motionStarted)
        {
            return;
        }

        motionStarted = true;
        _ = PromotedTile.Motion()
            .Animate(TileInstance2D.TintProperty)
            .To(new Color(96, 220, 255))
            .With(MotionFactory.Tween<Color>(TimeSpan.FromMilliseconds(1)));
    }

    internal void PanAndZoom()
    {
        Surface.ViewBox = new DrawRect(16, 8, 96, 63);
    }

    internal static void VerifyCaptures(string artifactDirectory, string backend)
    {
        string fullDirectory = Path.GetFullPath(artifactDirectory);
        string initialPath = Path.Combine(fullDirectory, InitialCaptureName);
        string motionPath = Path.Combine(fullDirectory, MotionCaptureName);
        string panZoomPath = Path.Combine(fullDirectory, PanZoomCaptureName);
        using SKBitmap initial = Decode(initialPath, backend);
        using SKBitmap motion = Decode(motionPath, backend);
        using SKBitmap panZoom = Decode(panZoomPath, backend);
        RequireSameSize(initial, motion, backend);
        RequireSameSize(initial, panZoom, backend);
        if (initial.Width < 320 || initial.Height < 200)
        {
            throw new InvalidOperationException(
                $"{backend} TileMap2D conformance capture is unexpectedly small: {initial.Width}x{initial.Height}.");
        }

        int initialColors = CountDistinctColors(initial);
        if (initialColors < 12)
        {
            throw new InvalidOperationException(
                $"{backend} TileMap2D conformance scene produced only {initialColors} colors; atlas, tint, opacity, flip, and layer coverage is missing.");
        }

        long motionDifference = CountDifferentPixels(initial, motion, 4);
        if (motionDifference < 64)
        {
            throw new InvalidOperationException(
                $"{backend} promoted-tile Motion/Prism capture changed only {motionDifference} pixels.");
        }

        long panZoomDifference = CountDifferentPixels(motion, panZoom, 4);
        long minimumPanZoomDifference = ((long)initial.Width * initial.Height) / 20;
        if (panZoomDifference < minimumPanZoomDifference)
        {
            throw new InvalidOperationException(
                $"{backend} pan/zoom capture changed {panZoomDifference} pixels; expected at least {minimumPanZoomDifference}.");
        }

        WriteBackendReport(
            Path.Combine(fullDirectory, "tilemap-conformance-backend.json"),
            backend,
            initialColors,
            motionDifference,
            panZoomDifference,
            [initialPath, motionPath, panZoomPath]);
    }

    internal static void CompareBackends(
        string windowsDirectory,
        string sdlGpuDirectory,
        string reportPath,
        IReadOnlyList<string>? captureNames = null,
        string scenario = "TileMap2D")
    {
        IReadOnlyList<string> captures = captureNames ?? [InitialCaptureName, MotionCaptureName, PanZoomCaptureName];
        List<object> comparisons = [];
        foreach (string capture in captures)
        {
            string windowsPath = Path.Combine(Path.GetFullPath(windowsDirectory), capture);
            string sdlGpuPath = Path.Combine(Path.GetFullPath(sdlGpuDirectory), capture);
            using SKBitmap windows = Decode(windowsPath, "WindowsDX");
            using SKBitmap sdlGpu = Decode(sdlGpuPath, "SDL_GPU");
            RequireSameSize(windows, sdlGpu, $"{scenario} backend parity '{capture}'");

            long pixelsOverTolerance = 0;
            long rasterEdgeDifferences = 0;
            long unresolvedPixels = 0;
            int maximumDelta = 0;
            for (int y = 0; y < windows.Height; y++)
            {
                for (int x = 0; x < windows.Width; x++)
                {
                    int delta = MaximumChannelDelta(
                        windows.GetPixel(x, y),
                        sdlGpu.GetPixel(x, y));
                    maximumDelta = Math.Max(maximumDelta, delta);
                    if (delta > ChannelTolerance)
                    {
                        pixelsOverTolerance++;
                        bool rasterEdgeDifference =
                            IsRasterEdge(windows, x, y) &&
                            IsRasterEdge(sdlGpu, x, y) &&
                            (HasNeighborhoodMatch(windows.GetPixel(x, y), sdlGpu, x, y) ||
                             HasNeighborhoodMatch(sdlGpu.GetPixel(x, y), windows, x, y));
                        if (rasterEdgeDifference)
                        {
                            rasterEdgeDifferences++;
                        }
                        else
                        {
                            unresolvedPixels++;
                        }
                    }
                }
            }

            comparisons.Add(new
            {
                Capture = capture,
                LogicalWidth,
                LogicalHeight,
                ComparisonWidth = windows.Width,
                ComparisonHeight = windows.Height,
                WindowsPhysicalWidth = windows.Width,
                WindowsPhysicalHeight = windows.Height,
                SdlGpuPhysicalWidth = sdlGpu.Width,
                SdlGpuPhysicalHeight = sdlGpu.Height,
                ChannelTolerance,
                RasterEdgeTolerance,
                MaximumChannelDelta = maximumDelta,
                PixelsOverTolerance = pixelsOverTolerance,
                RasterEdgeDifferences = rasterEdgeDifferences,
                UnresolvedPixels = unresolvedPixels,
                WindowsSha256 = Sha256(windowsPath),
                SdlGpuSha256 = Sha256(sdlGpuPath)
            });
            if (unresolvedPixels != 0)
            {
                throw new InvalidOperationException(
                    $"{scenario} backend mismatch in '{capture}': {unresolvedPixels} pixels remain outside channel tolerance {ChannelTolerance} and the one-pixel raster-edge tolerance; maximum delta is {maximumDelta}.");
            }
        }

        string fullReportPath = Path.GetFullPath(reportPath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullReportPath)!);
        File.WriteAllText(
            fullReportPath,
            JsonSerializer.Serialize(
                new
                {
                    Contract = $"{scenario} scene-space output parity with exact interiors and a one-pixel backend raster-edge tolerance",
                    CaptureApi = "Window.SaveScreenshot(string)",
                    GoldenUpdated = false,
                    Comparisons = comparisons
                },
                new JsonSerializerOptions { WriteIndented = true }));
    }

    public void Dispose()
    {
        prismLifetime.Dispose();
    }

    private static TileChunk2D[] CreateGroundChunks()
    {
        return
        [
            new TileChunk2D(
                new TileCoordinate2D(0, 0),
                8,
                10,
                CreateCells(0, 8, 10, structures: false)),
            new TileChunk2D(
                new TileCoordinate2D(8, 0),
                8,
                10,
                CreateCells(8, 8, 10, structures: false))
        ];
    }

    private static TileChunk2D[] CreateStructureChunks()
    {
        return
        [
            new TileChunk2D(
                new TileCoordinate2D(0, 0),
                8,
                10,
                CreateCells(0, 8, 10, structures: true)),
            new TileChunk2D(
                new TileCoordinate2D(8, 0),
                8,
                10,
                CreateCells(8, 8, 10, structures: true))
        ];
    }

    private static TileCell2D[] CreateCells(
        int originX,
        int width,
        int height,
        bool structures)
    {
        TileCell2D[] cells = new TileCell2D[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int localX = 0; localX < width; localX++)
            {
                int x = originX + localX;
                int index = (y * width) + localX;
                if (!structures)
                {
                    int tileId = ((x + y) & 1) == 0 ? 1 : 2;
                    TileFlip2D flip = (x % 4, y % 3) switch
                    {
                        (1, _) => TileFlip2D.Horizontal,
                        (_, 1) => TileFlip2D.Vertical,
                        (3, 2) => TileFlip2D.Horizontal | TileFlip2D.Vertical,
                        _ => TileFlip2D.None
                    };
                    cells[index] = new TileCell2D(tileId, flip);
                    continue;
                }

                bool occupied = (x + (y * 3)) % 7 == 0 ||
                    (x is 7 or 8 && y is 3 or 4 or 5);
                if (!occupied)
                {
                    cells[index] = new TileCell2D(0);
                    continue;
                }

                int structureId = ((x + y) & 1) == 0 ? 100 : 101;
                TileFlip2D structureFlip = (x & 1) == 0
                    ? TileFlip2D.Vertical
                    : TileFlip2D.Horizontal;
                cells[index] = new TileCell2D(structureId, structureFlip);
            }
        }

        return cells;
    }

    private static void WriteAtlas(string path, bool terrain)
    {
        using SKBitmap bitmap = new(new SKImageInfo(
            32,
            16,
            SKColorType.Rgba8888,
            SKAlphaType.Premul));
        for (int tile = 0; tile < 2; tile++)
        {
            int offset = tile * 16;
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool left = x < 5;
                    bool top = y < 6;
                    SKColor color = terrain
                        ? TerrainColor(tile, left, top)
                        : StructureColor(tile, left, top);
                    bitmap.SetPixel(offset + x, y, color);
                }
            }
        }

        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, encoded.ToArray());
    }

    private static SKColor TerrainColor(int tile, bool left, bool top) =>
        (tile, left, top) switch
        {
            (0, true, true) => new SKColor(30, 130, 55),
            (0, true, false) => new SKColor(55, 190, 85),
            (0, false, true) => new SKColor(100, 210, 75),
            (0, false, false) => new SKColor(35, 155, 125),
            (1, true, true) => new SKColor(155, 100, 45),
            (1, true, false) => new SKColor(205, 150, 65),
            (1, false, true) => new SKColor(225, 190, 95),
            _ => new SKColor(120, 75, 35)
        };

    private static SKColor StructureColor(int tile, bool left, bool top) =>
        (tile, left, top) switch
        {
            (0, true, true) => new SKColor(65, 90, 205),
            (0, true, false) => new SKColor(110, 145, 245),
            (0, false, true) => new SKColor(210, 75, 100),
            (0, false, false) => new SKColor(245, 135, 150),
            (1, true, true) => new SKColor(75, 190, 210),
            (1, true, false) => new SKColor(35, 120, 155),
            (1, false, true) => new SKColor(225, 115, 35),
            _ => new SKColor(250, 190, 70)
        };

    private static SKBitmap Decode(string path, string backend) =>
        SKBitmap.Decode(path) ?? throw new InvalidOperationException(
            $"{backend} could not decode TileMap2D conformance capture '{path}'.");

    private static void RequireSameSize(SKBitmap left, SKBitmap right, string description)
    {
        if (left.Width != right.Width || left.Height != right.Height)
        {
            throw new InvalidOperationException(
                $"{description} capture sizes differ: {left.Width}x{left.Height} versus {right.Width}x{right.Height}.");
        }
    }

    private static int CountDistinctColors(SKBitmap bitmap)
    {
        HashSet<uint> colors = [];
        for (int y = 0; y < bitmap.Height; y += 2)
        {
            for (int x = 0; x < bitmap.Width; x += 2)
            {
                colors.Add((uint)bitmap.GetPixel(x, y));
            }
        }
        return colors.Count;
    }

    private static long CountDifferentPixels(SKBitmap left, SKBitmap right, int tolerance)
    {
        long different = 0;
        for (int y = 0; y < left.Height; y++)
        {
            for (int x = 0; x < left.Width; x++)
            {
                if (MaximumChannelDelta(left.GetPixel(x, y), right.GetPixel(x, y)) > tolerance)
                {
                    different++;
                }
            }
        }
        return different;
    }

    private static int MaximumChannelDelta(SKColor left, SKColor right) =>
        Math.Max(
            Math.Max(Math.Abs(left.Red - right.Red), Math.Abs(left.Green - right.Green)),
            Math.Max(Math.Abs(left.Blue - right.Blue), Math.Abs(left.Alpha - right.Alpha)));

    private static bool IsRasterEdge(SKBitmap bitmap, int x, int y)
    {
        SKColor center = bitmap.GetPixel(x, y);
        for (int offsetY = -RasterEdgeTolerance; offsetY <= RasterEdgeTolerance; offsetY++)
        {
            for (int offsetX = -RasterEdgeTolerance; offsetX <= RasterEdgeTolerance; offsetX++)
            {
                int neighborX = Math.Clamp(x + offsetX, 0, bitmap.Width - 1);
                int neighborY = Math.Clamp(y + offsetY, 0, bitmap.Height - 1);
                if (MaximumChannelDelta(center, bitmap.GetPixel(neighborX, neighborY)) >
                    ChannelTolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasNeighborhoodMatch(
        SKColor color,
        SKBitmap bitmap,
        int x,
        int y)
    {
        for (int offsetY = -RasterEdgeTolerance; offsetY <= RasterEdgeTolerance; offsetY++)
        {
            for (int offsetX = -RasterEdgeTolerance; offsetX <= RasterEdgeTolerance; offsetX++)
            {
                int neighborX = Math.Clamp(x + offsetX, 0, bitmap.Width - 1);
                int neighborY = Math.Clamp(y + offsetY, 0, bitmap.Height - 1);
                if (MaximumChannelDelta(color, bitmap.GetPixel(neighborX, neighborY)) <=
                    ChannelTolerance)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void WriteBackendReport(
        string reportPath,
        string backend,
        int distinctColors,
        long motionDifference,
        long panZoomDifference,
        IReadOnlyList<string> captures)
    {
        File.WriteAllText(
            reportPath,
            JsonSerializer.Serialize(
                new
                {
                    Backend = backend,
                    CaptureApi = "Window.SaveScreenshot(string)",
                    Features = new[]
                    {
                        "multiple atlases",
                        "opacity and tint",
                        "horizontal and vertical flips",
                        "map, layer, and promoted-tile transforms",
                        "semantic layers",
                        "chunk edge x=8 and promoted edge tile x=7",
                        "pan and zoom",
                        "promoted tile with Aspect, Motion, and Prism Invert"
                    },
                    DistinctColors = distinctColors,
                    MotionChangedPixels = motionDifference,
                    PanZoomChangedPixels = panZoomDifference,
                    Captures = captures.Select(path => new
                    {
                        File = Path.GetFileName(path),
                        Sha256 = Sha256(path)
                    })
                },
                new JsonSerializerOptions { WriteIndented = true }));
    }

    private static string Sha256(string path) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
}
