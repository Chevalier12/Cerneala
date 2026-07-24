using System.Numerics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Hosting;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PrismWindowsDxConformanceTests
{
    private const int Width = 96;
    private const int Height = 64;
    private const string UpdateGoldensVariable =
        "CERNEALA_UPDATE_PRISM_GOLDENS";
    private static readonly CernealaColor ClearColor =
        new(9, 13, 21);
    private static readonly DrawRect ScopeBounds =
        new(0, 0, Width, Height);

    [Fact]
    public void BaselineScenesMatchVersionedWindowsDxGoldensAndUseMinimalPasses()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        GoldenManifest manifest = ReadManifest();
        AssertManifest(manifest);
        using WindowsDxFixture fixture = new();
        PrismScene[] scenes =
            CreateScenes(fixture.Session.GraphicsDevice);

        try
        {
            foreach (PrismScene scene in scenes)
            {
                RenderedScene cacheOn =
                    RenderPng(
                        fixture.Session,
                        scene,
                        retainedCacheEnabled: true);
                RenderedScene cacheOff =
                    RenderPng(
                        fixture.Session,
                        scene,
                        retainedCacheEnabled: false);

                Assert.True(
                    cacheOn.RendererDiagnostics
                        .RetainedCacheEnabled);
                Assert.False(
                    cacheOff.RendererDiagnostics
                        .RetainedCacheEnabled);
                AssertMinimalExecution(scene, cacheOn);
                AssertMinimalExecution(scene, cacheOff);
                AssertMatchesGolden(
                    scene,
                    cacheOn.Png,
                    manifest);
                AssertMatchesGolden(
                    scene,
                    cacheOff.Png,
                    manifest);
                AssertSemanticImage(
                    scene,
                    cacheOn.Png,
                    manifest.ChannelTolerance.Maximum);
                AssertSemanticImage(
                    scene,
                    cacheOff.Png,
                    manifest.ChannelTolerance.Maximum);
            }
        }
        finally
        {
            DisposeScenes(scenes);
        }
    }

    [Fact]
    public void OuterGlowFallsOffContinuouslyInsteadOfDrawingDetachedRectangles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Outer glow continuity",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        DrawRect elementBounds = new(24, 18, 48, 28);
        PrismCompositionDefinition composition = new(
            "Outer glow continuity",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_370,
            bounds: elementBounds);
        PrismStyleState glow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.OuterGlow);
        SetNumber("Size", 12);
        SetNumber("Range", 1);
        SetNumber("Opacity", 1);
        using PrismScene scene = BuildScene(
            "outer-glow-continuity",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.DrawRectangle(
                    elementBounds,
                    new CernealaColor(7, 12, 22, 208),
                    1),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 48,
            foregroundY: 24);
        using WindowsDxFixture fixture = new();

        RenderedScene rendered = RenderPng(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Png, scene.Name);
        int[] intensities = Enumerable
            .Range(1, 12)
            .Select(distance =>
            {
                SKColor pixel = bitmap.GetPixel(48, 18 - distance);
                return Math.Abs(pixel.Red - ClearColor.R) +
                    Math.Abs(pixel.Green - ClearColor.G) +
                    Math.Abs(pixel.Blue - ClearColor.B);
            })
            .ToArray();
        int[] interiorIntensities = Enumerable
            .Range(1, 12)
            .Select(distance =>
            {
                SKColor pixel = bitmap.GetPixel(48, 18 + distance);
                return Math.Abs(pixel.Red - ClearColor.R) +
                    Math.Abs(pixel.Green - ClearColor.G) +
                    Math.Abs(pixel.Blue - ClearColor.B);
            })
            .ToArray();

        Assert.All(intensities, intensity => Assert.True(intensity > 0));
        Assert.True(
            intensities.Max() >= 30,
            $"Expected a visible one-pixel outer glow, but found [{string.Join(", ", intensities)}].");
        Assert.True(
            intensities.Distinct().Count() >= 6,
            $"Expected a continuous glow falloff, but found bands [{string.Join(", ", intensities)}].");
        Assert.All(
            interiorIntensities,
            intensity => Assert.Equal(0, intensity));

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }

    [Fact]
    public void OuterGlowGaussianFalloffDoesNotFavorCardinalDirections()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Outer glow isotropy",
            styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)]);
        DrawRect elementBounds = new(48, 32, 1, 1);
        PrismCompositionDefinition composition = new(
            "Outer glow isotropy",
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1_371,
            bounds: elementBounds);
        PrismStyleState glow = Assert.Single(
            scope.Instance.GetLayerState(layer.Id).Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)PrismStyleId.OuterGlow);
        SetNumber("Size", 12);
        SetNumber("Spread", 0);
        SetNumber("Range", 1);
        SetNumber("Opacity", 1);
        using PrismScene scene = BuildScene(
            "outer-glow-isotropy",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    elementBounds,
                    new CernealaColor(255, 255, 255, 255)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 48,
            foregroundY: 32);
        using WindowsDxFixture fixture = new();

        RenderedScene rendered = RenderPng(fixture.Session, scene);
        using SKBitmap bitmap = Decode(rendered.Png, scene.Name);
        int cardinal = Intensity(bitmap.GetPixel(43, 32));
        int diagonal = Intensity(bitmap.GetPixel(45, 28));

        Assert.True(cardinal > 0 && diagonal > 0);
        Assert.InRange(
            Math.Abs(cardinal - diagonal),
            0,
            12);

        int Intensity(SKColor pixel) =>
            Math.Abs(pixel.Red - ClearColor.R) +
            Math.Abs(pixel.Green - ClearColor.G) +
            Math.Abs(pixel.Blue - ClearColor.B);

        void SetNumber(string name, float value)
        {
            PrismCatalogPropertyDescriptor property =
                entry.Properties.Single(candidate => candidate.Name == name);
            GeneratedMarkup.SetPrismStyleNumber(
                glow,
                entry.StableId,
                property.TypeSlot,
                value);
        }
    }

    [Fact]
    public void PrismFilterCatalogGalleryRendersThroughAutomatedCaptureApi()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismFilterConformanceGalleryEntry[] gallery =
            PrismFilterConformanceGallery.Entries.ToArray();
        Assert.NotEmpty(gallery);
        using WindowsDxFixture fixture = new();
        int capturedCount = 0;

        foreach (PrismFilterConformanceGalleryEntry entry in gallery)
        {
            PrismDrawScope scope = PrismTestData.Scope(
                entry.Composition,
                ownerToken: 10_000L + (int)entry.Filter,
                bounds: ScopeBounds);
            using PrismScene scene = BuildScene(
                $"filter-{entry.Symbol}",
                Commands(
                    DrawCommand.BeginPrism(scope),
                    RedRectangle(),
                    BlueRectangle(),
                    DrawCommand.EndPrism()),
                expectedFallbackCount: 0,
                foregroundX: 28,
                foregroundY: 22);
            RenderedScene rendered =
                RenderPng(fixture.Session, scene);

            Assert.True(
                rendered.Png.Length > 0,
                $"{entry.Symbol} produced no PNG capture.");
            using SKBitmap bitmap =
                Decode(rendered.Png, entry.Symbol);
            Assert.Equal(Width, bitmap.Width);
            Assert.Equal(Height, bitmap.Height);
            Assert.True(
                ContainsPixelOtherThanClearColor(bitmap),
                $"{entry.Symbol} produced a blank capture.");
            Assert.Equal(1, rendered.Counters.CaptureCount);
            Assert.True(
                rendered.Counters.PassCount >= 1,
                $"{entry.Symbol} executed no Prism passes.");
            capturedCount++;
        }

        Assert.Equal(gallery.Length, capturedCount);
    }

    [Fact]
    public void ExecutedGraphDumpIsDeterministicAndCorrelatesNestedTransformScopes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismScene[] scenes =
            CreateScenes(fixture.Session.GraphicsDevice);

        try
        {
            PrismScene nested = Assert.Single(
                scenes,
                scene => scene.Name == "nested-prism");
            PrismScene transform = Assert.Single(
                scenes,
                scene => scene.Name == "transform");
            RenderedScene nestedFirst =
                RenderPng(fixture.Session, nested);
            RenderedScene nestedSecond =
                RenderPng(fixture.Session, nested);
            RenderedScene transformFirst =
                RenderPng(fixture.Session, transform);
            RenderedScene transformSecond =
                RenderPng(fixture.Session, transform);

            Assert.Equal(
                nestedFirst.GraphDump,
                nestedSecond.GraphDump);
            Assert.Equal(
                transformFirst.GraphDump,
                transformSecond.GraphDump);
            Assert.StartsWith(
                "prism-execution v2 runtime-identifiers=redacted",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.Contains(
                "scope 1 commands=",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.Contains(
                "depth=1 parent=0 owner=scope-1",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.Contains(
                " NestedPresent scope=1 ",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.Contains(
                "transform=[1,0,0,1,12,8]",
                transformFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Texture2D",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "RenderTarget2D",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
            Assert.DoesNotContain(
                "GraphicsDevice",
                nestedFirst.GraphDump,
                StringComparison.Ordinal);
        }
        finally
        {
            DisposeScenes(scenes);
        }
    }

    [Fact]
    public void DeviceResetEvictsTransientSurfacesAndPrismRendersAgain()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismScene scene = CreateNormalBlendScene();
        using WindowsDxFixture fixture = new();
        int resettingCount = 0;
        int resetCount = 0;
        fixture.Session.GraphicsDevice.DeviceResetting += OnResetting;
        fixture.Session.GraphicsDevice.DeviceReset += OnReset;

        try
        {
            PrismExecutionCounters before =
                RenderOnScreen(fixture.Session, scene);
            Assert.Equal(
                scene.Plan.PeakLiveSurfaces,
                before.PeakLiveSurfaceCount);
            Assert.Equal(
                scene.Plan.ExecutionOrder.Length,
                before.CreatedSurfaceCount +
                    before.ReusedSurfaceCount);

            fixture.Session.Resize(112, 72, coordinateScale: 1f);

            PrismExecutionCounters after =
                RenderOnScreen(fixture.Session, scene);
            Assert.Equal(1, resettingCount);
            Assert.Equal(1, resetCount);
            Assert.Equal(
                scene.Plan.PeakLiveSurfaces,
                after.PeakLiveSurfaceCount);
            Assert.Equal(
                scene.Plan.ExecutionOrder.Length,
                after.CreatedSurfaceCount +
                    after.ReusedSurfaceCount);
            Assert.Equal(
                before.CreatedSurfaceCount,
                after.CreatedSurfaceCount);
            Assert.Equal(
                before.ReusedSurfaceCount,
                after.ReusedSurfaceCount);
            Assert.Equal(112, fixture.Session.GraphicsDevice.PresentationParameters.BackBufferWidth);
            Assert.Equal(72, fixture.Session.GraphicsDevice.PresentationParameters.BackBufferHeight);
        }
        finally
        {
            fixture.Session.GraphicsDevice.DeviceResetting -= OnResetting;
            fixture.Session.GraphicsDevice.DeviceReset -= OnReset;
        }

        void OnResetting(object? sender, EventArgs eventArgs)
        {
            resettingCount++;
        }

        void OnReset(object? sender, EventArgs eventArgs)
        {
            resetCount++;
        }
    }

    [Fact]
    public void NavigationDisposesPrismSessionBeforeTheNextWindowRenders()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismScene scene = CreateNormalBlendScene();
        WindowsDxFixture firstFixture = new();
        WindowsDxWindowGraphicsSession disposedSession =
            firstFixture.Session;
        try
        {
            RenderedScene first =
                RenderPng(disposedSession, scene);
            Assert.True(first.Png.Length > 0);
        }
        finally
        {
            firstFixture.Dispose();
        }

        using MemoryStream disposedOutput = new();
        Assert.Throws<ObjectDisposedException>(
            () => ((IWindowScreenshotSource)disposedSession).RenderPng(
                disposedOutput,
                ClearColor,
                _ => { }));

        using WindowsDxFixture secondFixture = new();
        RenderedScene second =
            RenderPng(secondFixture.Session, scene);

        Assert.True(second.Png.Length > 0);
        Assert.Equal(
            scene.Plan.ExecutionOrder.Length +
                scene.Analysis.Scopes.Length,
            second.Counters.PassCount);
    }

    private static RenderedScene RenderPng(
        WindowsDxWindowGraphicsSession session,
        PrismScene scene,
        bool retainedCacheEnabled = true)
    {
        using MemoryStream output = new();
        PrismExecutionCounters counters = default;
        PrismRendererDiagnostics rendererDiagnostics = default;
        string? dump = null;

        session.RenderPng(
            output,
            ClearColor,
            retainedCacheEnabled,
            drawingBackend =>
            {
                MonoGameDrawingBackend backend =
                    Assert.IsType<MonoGameDrawingBackend>(
                        drawingBackend);
                DrawingFrameContext frameContext =
                    new(scene.Analysis);
                backend.Render(scene.Commands, in frameContext);
                counters = backend.PrismDiagnostics.Counters;
                rendererDiagnostics =
                    backend.RendererDiagnostics;
                dump =
                    backend.PrismDiagnostics.DumpExecutedGraph();
            });

        return new RenderedScene(
            output.ToArray(),
            counters,
            rendererDiagnostics,
            Assert.IsType<string>(dump));
    }

    private static PrismExecutionCounters RenderOnScreen(
        WindowsDxWindowGraphicsSession session,
        PrismScene scene)
    {
        MonoGameDrawingBackend backend =
            Assert.IsType<MonoGameDrawingBackend>(
                session.DrawingBackend);
        DrawingFrameContext frameContext =
            new(scene.Analysis);

        session.BeginFrame(ClearColor);
        backend.Render(scene.Commands, in frameContext);
        session.Present();
        return backend.PrismDiagnostics.Counters;
    }

    private static void AssertMinimalExecution(
        PrismScene scene,
        RenderedScene rendered)
    {
        PrismExecutionCounters counters = rendered.Counters;
        int outerGlowCount = scene.Plan.ExecutionOrder.Count(
            nodeId => scene.Plan.OptimizedGraph
                .GetNode(nodeId)
                is { Kind: PrismGraphNodeKind.Style, Style: PrismStyleId.OuterGlow });
        int strokeCount = scene.Plan.ExecutionOrder.Count(
            nodeId => scene.Plan.OptimizedGraph
                .GetNode(nodeId)
                is { Kind: PrismGraphNodeKind.Style, Style: PrismStyleId.Stroke });
        int styleScratchCount =
            (outerGlowCount > 0 ? 2 : 0) +
            (strokeCount > 0 ? 2 : 0);
        int basePassCount =
            scene.Plan.ExecutionOrder.Length +
            scene.Analysis.Scopes.Length;

        Assert.InRange(
            counters.PassCount,
            basePassCount,
            basePassCount + styleScratchCount);
        Assert.Equal(
            scene.Analysis.Scopes.Length,
            counters.CaptureCount);
        Assert.InRange(
            counters.PeakLiveSurfaceCount,
            scene.Plan.PeakLiveSurfaces,
            scene.Plan.PeakLiveSurfaces +
                Math.Min(styleScratchCount, 2));
        Assert.InRange(
            counters.CreatedSurfaceCount +
                counters.ReusedSurfaceCount,
            scene.Plan.ExecutionOrder.Length,
            scene.Plan.ExecutionOrder.Length +
                styleScratchCount);
        Assert.Equal(
            scene.ExpectedFallbackCount,
            counters.FallbackCount);
        Assert.True(
            counters.CpuSubmitTime > TimeSpan.Zero,
            $"{scene.Name} did not report CPU submit time.");

        Assert.DoesNotContain(
            "reason=UnsupportedCapability",
            rendered.GraphDump,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "reason=MissingResource",
            rendered.GraphDump,
            StringComparison.Ordinal);
    }

    private static void AssertSemanticImage(
        PrismScene scene,
        byte[] png,
        int tolerance)
    {
        using SKBitmap bitmap = Decode(png, scene.Name);
        Assert.Equal(Width, bitmap.Width);
        Assert.Equal(Height, bitmap.Height);

        SKColor background =
            new(ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
        SKColor backgroundPixel =
            bitmap.GetPixel(scene.BackgroundX, scene.BackgroundY);
        Assert.True(
            IsWithinTolerance(
                backgroundPixel,
                background,
                tolerance),
            $"{scene.Name} changed the known background anchor.");

        SKColor foregroundPixel =
            bitmap.GetPixel(scene.ForegroundX, scene.ForegroundY);
        Assert.False(
            IsWithinTolerance(
                foregroundPixel,
                background,
                tolerance),
            $"{scene.Name} produced no content at its foreground anchor.");

        HashSet<uint> distinctColors = [];
        int contentPixelCount = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                distinctColors.Add(Pack(pixel));
                if (!IsWithinTolerance(pixel, background, tolerance))
                {
                    contentPixelCount++;
                }
            }
        }

        Assert.True(
            distinctColors.Count >= 3,
            $"{scene.Name} did not produce a meaningful color field.");
        Assert.True(
            contentPixelCount >= 300,
            $"{scene.Name} rendered too few content pixels.");
    }

    private static void AssertMatchesGolden(
        PrismScene scene,
        byte[] actualPng,
        GoldenManifest manifest)
    {
        string goldenPath = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Cerneala.Tests",
            "Golden",
            "Prism",
            $"{scene.Name}.png");
        if (string.Equals(
            Environment.GetEnvironmentVariable(
                UpdateGoldensVariable),
            "1",
            StringComparison.Ordinal))
        {
            Directory.CreateDirectory(
                Path.GetDirectoryName(goldenPath)!);
            File.WriteAllBytes(goldenPath, actualPng);
        }

        Assert.True(
            File.Exists(goldenPath),
            $"Missing Prism golden '{goldenPath}'. Set " +
            $"{UpdateGoldensVariable}=1 to regenerate it through WindowsDX.");

        using SKBitmap actual = Decode(actualPng, scene.Name);
        using SKBitmap expected =
            SKBitmap.Decode(goldenPath) ??
            throw new InvalidDataException(
                $"Could not decode Prism golden '{goldenPath}'.");
        Assert.Equal(manifest.Width, expected.Width);
        Assert.Equal(manifest.Height, expected.Height);
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        for (int y = 0; y < actual.Height; y++)
        {
            for (int x = 0; x < actual.Width; x++)
            {
                SKColor actualPixel = actual.GetPixel(x, y);
                SKColor expectedPixel = expected.GetPixel(x, y);
                Assert.True(
                    IsWithinTolerance(
                        actualPixel,
                        expectedPixel,
                        manifest.ChannelTolerance.Maximum),
                    $"{scene.Name} differs at ({x},{y}): " +
                    $"actual={actualPixel}, expected={expectedPixel}, " +
                    $"tolerance={manifest.ChannelTolerance.Maximum}.");
            }
        }
    }

    private static GoldenManifest ReadManifest()
    {
        string path = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Cerneala.Tests",
            "Golden",
            "Prism",
            "conformance.json");
        GoldenManifest? manifest = JsonSerializer.Deserialize<GoldenManifest>(
            File.ReadAllText(path),
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        return manifest ??
            throw new InvalidDataException(
                $"Could not read Prism golden manifest '{path}'.");
    }

    private static void AssertManifest(GoldenManifest manifest)
    {
        Assert.Equal("WindowsDX", manifest.Platform);
        Assert.Equal(
            "IWindowScreenshotSource.RenderPng",
            manifest.CaptureApi);
        Assert.Equal("R8G8B8A8_UNorm", manifest.PixelFormat);
        Assert.Equal("sRGB IEC61966-2.1", manifest.ColorProfile);
        Assert.Equal("LinearSrgb", manifest.WorkingColorProfile);
        Assert.Equal(
            "straight RGBA PNG output; premultiplied linear-light compositor inputs",
            manifest.AlphaConvention);
        Assert.Equal(12648430, manifest.Seed);
        Assert.Equal(2, manifest.ChannelTolerance.Red);
        Assert.Equal(2, manifest.ChannelTolerance.Green);
        Assert.Equal(2, manifest.ChannelTolerance.Blue);
        Assert.Equal(2, manifest.ChannelTolerance.Alpha);
        Assert.Equal(
            "Direct3D 11",
            manifest.SupportedHardware.GraphicsApi);
        Assert.Equal(
            "10_0",
            manifest.SupportedHardware.MinimumFeatureLevel);
        Assert.Equal(
            "ps_4_0",
            manifest.SupportedHardware.ShaderProfile);
        Assert.Equal(
            "WHQL-certified or current vendor production driver",
            manifest.SupportedHardware.DriverPolicy);
        Assert.Equal(Width, manifest.Width);
        Assert.Equal(Height, manifest.Height);
    }

    private static PrismScene[] CreateScenes(
        GraphicsDevice graphicsDevice)
    {
        return
        [
            CreateNormalBlendScene(),
            CreateOpacityScene(),
            CreateFillScene(),
            CreateMaskScene(graphicsDevice),
            CreateClipScene(),
            CreateNestedScene(),
            CreateTransformScene(),
            CreateBlendCombinationScene(graphicsDevice),
            CreateMaskTransformScene(graphicsDevice),
            CreateClippingChainScene(),
            CreateNestedGroupsScene(),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.DropShadow,
                "style-drop-shadow"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.InnerShadow,
                "style-inner-shadow"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.OuterGlow,
                "style-outer-glow"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.InnerGlow,
                "style-inner-glow"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.BevelEmboss,
                "style-bevel-emboss"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.Satin,
                "style-satin"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.ColorOverlay,
                "style-color-overlay"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.GradientOverlay,
                "style-gradient-overlay"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.PatternOverlay,
                "style-pattern-overlay"),
            CreateStyleScene(
                graphicsDevice,
                PrismStyleId.Stroke,
                "style-stroke")
        ];
    }

    private static PrismScene CreateNormalBlendScene()
    {
        PrismDrawScope scope = CreateScope(
            "Normal blend",
            ownerToken: 101,
            Layer(1, "Normal foreground"),
            Layer(2, "Normal background"));
        return BuildScene(
            "normal-blend",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateOpacityScene()
    {
        PrismDrawScope scope = CreateScope(
            "Opacity",
            ownerToken: 201,
            Layer(1, "Half opacity", opacity: 0.5f));
        return BuildScene(
            "opacity",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateFillScene()
    {
        PrismDrawScope scope = CreateScope(
            "Fill",
            ownerToken: 301,
            Layer(1, "Partial fill", fill: 0.35f));
        return BuildScene(
            "fill",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateMaskScene(
        GraphicsDevice graphicsDevice)
    {
        ImageResource resource = CreateImageResource(
            graphicsDevice,
            "ConformanceMask",
            static (x, y) =>
            {
                float horizontal = x / (float)(Width - 1);
                float vertical = y / (float)(Height - 1);
                byte alpha = ToByte(
                    Math.Clamp(
                        (horizontal * 0.75f) +
                        (vertical * 0.25f),
                        0,
                        1));
                return new XnaColor(alpha, alpha, alpha, alpha);
            });
        PrismMaskDefinition mask = new(
            resource.Id,
            density: 0.65f);
        PrismDrawScope scope = CreateScope(
            "Mask",
            ownerToken: 401,
            Matrix3x2.Identity,
            resource.Resources,
            Layer(1, "Masked layer", mask: mask));
        return BuildScene(
            "mask",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18,
            ownedResource: resource.Image);
    }

    private static PrismScene CreateClipScene()
    {
        PrismDrawScope scope = CreateScope(
            "Clip",
            ownerToken: 501,
            Layer(1, "Clipped foreground", clipToBelow: true),
            Layer(2, "Clip base"));
        return BuildScene(
            "clip",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateNestedScene()
    {
        PrismDrawScope outer = CreateScope(
            "Outer",
            ownerToken: 601,
            Layer(1, "Outer layer", opacity: 0.75f));
        PrismDrawScope inner = CreateScope(
            "Inner",
            ownerToken: 602,
            Layer(1, "Inner layer", fill: 0.55f));
        return BuildScene(
            "nested-prism",
            Commands(
                DrawCommand.BeginPrism(outer),
                DrawCommand.FillRectangle(
                    new DrawRect(10, 10, 72, 40),
                    new CernealaColor(210, 54, 74)),
                DrawCommand.BeginPrism(inner),
                DrawCommand.FillRectangle(
                    new DrawRect(28, 20, 40, 28),
                    new CernealaColor(37, 190, 126)),
                DrawCommand.EndPrism(),
                DrawCommand.FillRectangle(
                    new DrawRect(58, 38, 24, 14),
                    new CernealaColor(244, 191, 52)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 16,
            foregroundY: 16);
    }

    private static PrismScene CreateTransformScene()
    {
        PrismDrawScope scope = CreateScope(
            "Transform",
            ownerToken: 701,
            transform: Matrix3x2.CreateTranslation(12, 8),
            Layer(1, "Translated layer"));
        return BuildScene(
            "transform",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(22, 18, 50, 30),
                    new CernealaColor(222, 69, 83)),
                DrawCommand.FillRectangle(
                    new DrawRect(54, 32, 34, 22),
                    new CernealaColor(56, 129, 229)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 30,
            foregroundY: 26,
            backgroundX: 12,
            backgroundY: 12);
    }

    private static PrismScene CreateBlendCombinationScene(
        GraphicsDevice graphicsDevice)
    {
        ImageResource resource = CreateImageResource(
            graphicsDevice,
            "BlendCombinationMask",
            static (x, y) =>
            {
                float centerX = Width * 0.52f;
                float centerY = Height * 0.48f;
                float dx = (x - centerX) / (Width * 0.55f);
                float dy = (y - centerY) / (Height * 0.7f);
                byte alpha = ToByte(
                    Math.Clamp(
                        1 - MathF.Sqrt((dx * dx) + (dy * dy)),
                        0,
                        1));
                return new XnaColor(alpha, alpha, alpha, alpha);
            });
        PrismMaskDefinition mask = new(
            resource.Id,
            density: 0.72f,
            feather: 1.5f);
        PrismLayerDefinition clipped = Layer(
            11,
            "Masked vivid-light clip",
            opacity: 0.82f,
            fill: 0.68f,
            mask: mask,
            clipToBelow: true,
            blendMode: PrismBlendMode.VividLight,
            styles:
            [
                new PrismStyleDefinition(
                    PrismStyleId.DropShadow)
            ]);
        PrismGroupDefinition isolated = new(
            new PrismNodeId(10),
            "Isolated blend group",
            [
                clipped,
                Layer(
                    12,
                    "Multiply clip base",
                    blendMode: PrismBlendMode.Multiply)
            ],
            opacity: 0.88f,
            blendMode: PrismBlendMode.Normal);
        PrismDrawScope scope = CreateScope(
            "Blend combination",
            ownerToken: 801,
            Matrix3x2.Identity,
            resource.Resources,
            isolated,
            Layer(
                2,
                "Screen root base",
                blendMode: PrismBlendMode.Screen));
        return BuildScene(
            "blend-combination",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(8, 8, 58, 34),
                    new CernealaColor(224, 58, 92, 220)),
                DrawCommand.FillRectangle(
                    new DrawRect(34, 20, 50, 34),
                    new CernealaColor(43, 170, 224, 196)),
                DrawCommand.FillRectangle(
                    new DrawRect(20, 36, 58, 20),
                    new CernealaColor(238, 193, 51, 184)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 24,
            foregroundY: 18,
            ownedResource: resource.Image);
    }

    private static PrismScene CreateMaskTransformScene(
        GraphicsDevice graphicsDevice)
    {
        ImageResource resource = CreateImageResource(
            graphicsDevice,
            "TransformedMask",
            static (x, y) =>
            {
                bool high =
                    ((x / 12) + (y / 10)) % 2 == 0;
                byte value = high ? (byte)224 : (byte)36;
                return new XnaColor(
                    value,
                    value,
                    value,
                    byte.MaxValue);
            });
        PrismMaskDefinition mask = new(
            resource.Id,
            channel: PrismMaskChannel.Luminance,
            feather: 2,
            density: 0.8f,
            invert: true);
        PrismDrawScope scope = CreateScope(
            "Mask transform",
            ownerToken: 901,
            Matrix3x2.CreateTranslation(12, 8),
            resource.Resources,
            Layer(1, "Transformed mask layer", mask: mask));
        return BuildScene(
            "mask-transform",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(10, 10, 66, 38),
                    new CernealaColor(230, 72, 96)),
                DrawCommand.FillRectangle(
                    new DrawRect(38, 28, 46, 26),
                    new CernealaColor(48, 174, 219, 210)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 30,
            foregroundY: 24,
            ownedResource: resource.Image);
    }

    private static PrismScene CreateClippingChainScene()
    {
        PrismDrawScope scope = CreateScope(
            "Clipping chain",
            ownerToken: 1001,
            Layer(
                3,
                "Top clipped layer",
                opacity: 0.7f,
                clipToBelow: true,
                blendMode: PrismBlendMode.Screen),
            Layer(
                2,
                "Middle clipped layer",
                fill: 0.55f,
                clipToBelow: true,
                blendMode: PrismBlendMode.Multiply),
            Layer(
                1,
                "Partial alpha base",
                opacity: 0.62f));
        return BuildScene(
            "clipping-chain",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(8, 10, 58, 36),
                    new CernealaColor(232, 63, 95, 176)),
                DrawCommand.FillRectangle(
                    new DrawRect(34, 22, 50, 32),
                    new CernealaColor(47, 175, 225, 204)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 24,
            foregroundY: 20);
    }

    private static PrismScene CreateNestedGroupsScene()
    {
        PrismGroupDefinition inner = new(
            new PrismNodeId(20),
            "Inner isolated group",
            [
                Layer(
                    21,
                    "Inner top",
                    fill: 0.65f,
                    blendMode: PrismBlendMode.Overlay),
                Layer(
                    22,
                    "Inner base",
                    opacity: 0.8f,
                    blendMode: PrismBlendMode.Multiply)
            ],
            opacity: 0.82f,
            blendMode: PrismBlendMode.Normal);
        PrismGroupDefinition outer = new(
            new PrismNodeId(10),
            "Outer pass-through group",
            [
                inner,
                Layer(
                    11,
                    "Outer base",
                    blendMode: PrismBlendMode.Screen)
            ],
            opacity: 0.9f,
            blendMode: PrismBlendMode.PassThrough);
        PrismDrawScope scope = CreateScope(
            "Nested groups",
            ownerToken: 1101,
            outer);
        return BuildScene(
            "nested-groups",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(8, 8, 64, 38),
                    new CernealaColor(226, 61, 91, 218)),
                DrawCommand.FillRectangle(
                    new DrawRect(30, 22, 54, 34),
                    new CernealaColor(46, 171, 222, 194)),
                DrawCommand.FillRectangle(
                    new DrawRect(18, 38, 62, 18),
                    new CernealaColor(239, 194, 53, 180)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 22,
            foregroundY: 18);
    }

    private static PrismScene CreateStyleScene(
        GraphicsDevice graphicsDevice,
        PrismStyleId style,
        string sceneName)
    {
        PrismLayerDefinition layer = Layer(
            1,
            style.ToString(),
            styles:
            [
                new PrismStyleDefinition(style)
            ]);
        ImageResource? resource = null;
        if (style == PrismStyleId.PatternOverlay)
        {
            resource = CreateImageResource(
                graphicsDevice,
                "ConformanceStylePattern",
                static (x, y) =>
                {
                    bool first =
                        ((x / 8) + (y / 8)) % 2 == 0;
                    return first
                        ? new XnaColor(35, 202, 157)
                        : new XnaColor(244, 188, 52);
                });
        }
        PrismCompositionDefinition composition = new(
            style.ToString(),
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1200 + (int)style,
            bounds: new DrawRect(20, 14, 66, 42),
            resources: resource?.Resources);

        PrismStyleState state = Assert.Single(
            scope.Instance
                .GetLayerState(layer.Id)
                .Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)style);
        if (resource is not null)
        {
            PrismCatalogPropertyDescriptor pattern =
                entry.Properties.Single(property =>
                    property.Name == "Pattern");
            GeneratedMarkup.SetPrismStyleResource(
                state,
                entry.StableId,
                pattern.TypeSlot,
                resource.Id);
        }
        else if (style is PrismStyleId.DropShadow or
            PrismStyleId.Stroke)
        {
            PrismCatalogPropertyDescriptor color =
                entry.Properties.Single(property =>
                    property.Name == "Color");
            GeneratedMarkup.SetPrismStyleColor(
                state,
                entry.StableId,
                color.TypeSlot,
                style == PrismStyleId.DropShadow
                    ? new CernealaColor(74, 218, 188)
                    : new CernealaColor(255, 208, 62));
        }

        return BuildScene(
            sceneName,
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(20, 14, 52, 36),
                    new CernealaColor(222, 69, 83)),
                DrawCommand.FillRectangle(
                    new DrawRect(58, 32, 28, 24),
                    new CernealaColor(56, 129, 229, 210)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 28,
            foregroundY: 22,
            ownedResource: resource?.Image);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        params PrismNodeDefinition[] layers)
    {
        return CreateScope(
            name,
            ownerToken,
            Matrix3x2.Identity,
            resources: null,
            layers);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        Matrix3x2 transform,
        params PrismNodeDefinition[] layers)
    {
        return CreateScope(
            name,
            ownerToken,
            transform,
            resources: null,
            layers);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        Matrix3x2 transform,
        PrismDrawResources? resources,
        params PrismNodeDefinition[] layers)
    {
        PrismCompositionDefinition composition = new(
            name,
            layers,
            workingColorProfile: PrismColorProfile.LinearSrgb);
        return PrismTestData.Scope(
            composition,
            ownerToken,
            ScopeBounds,
            transform,
            resources: resources);
    }

    private static PrismLayerDefinition Layer(
        int id,
        string name,
        float opacity = 1,
        float fill = 1,
        PrismMaskDefinition? mask = null,
        bool clipToBelow = false,
        PrismBlendMode blendMode = PrismBlendMode.Normal,
        IEnumerable<PrismStyleDefinition>? styles = null)
    {
        return new PrismLayerDefinition(
            new PrismNodeId(id),
            name,
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Blur)
            ],
            styles: styles,
            mask: mask,
            opacity: opacity,
            fill: fill,
            blendMode: blendMode,
            clipToBelow: clipToBelow);
    }

    private static PrismScene BuildScene(
        string name,
        DrawCommandList commands,
        int expectedFallbackCount,
        int foregroundX,
        int foregroundY,
        int backgroundX = 2,
        int backgroundY = 2,
        IDisposable? ownedResource = null)
    {
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraph graph =
            new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);
        return new PrismScene(
            name,
            commands,
            analysis,
            plan,
            expectedFallbackCount,
            foregroundX,
            foregroundY,
            backgroundX,
            backgroundY,
            ownedResource);
    }

    private static ImageResource CreateImageResource(
        GraphicsDevice graphicsDevice,
        string key,
        Func<int, int, XnaColor> pixelFactory)
    {
        Texture2D texture = new(
            graphicsDevice,
            Width,
            Height,
            false,
            SurfaceFormat.Color);
        XnaColor[] pixels = new XnaColor[Width * Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                pixels[(y * Width) + x] =
                    pixelFactory(x, y);
            }
        }
        texture.SetData(pixels);

        MonoGameImage image = new(texture);
        PrismResourceId id = new(key);
        PrismDrawResources resources = PrismDrawResources.Create(
            [new PrismDrawImageResource(id, image)]);
        return new ImageResource(id, resources, image);
    }

    private static void DisposeScenes(
        IEnumerable<PrismScene> scenes)
    {
        foreach (PrismScene scene in scenes)
        {
            scene.Dispose();
        }
    }

    private static byte ToByte(float value)
    {
        return (byte)MathF.Round(
            Math.Clamp(value, 0, 1) * byte.MaxValue);
    }

    private static DrawCommandList Commands(
        params DrawCommand[] commands)
    {
        return PrismTestData.Commands(commands);
    }

    private static DrawCommand RedRectangle()
    {
        return DrawCommand.FillRectangle(
            new DrawRect(10, 10, 50, 30),
            new CernealaColor(222, 69, 83));
    }

    private static DrawCommand BlueRectangle()
    {
        return DrawCommand.FillRectangle(
            new DrawRect(42, 24, 42, 28),
            new CernealaColor(56, 129, 229));
    }

    private static SKBitmap Decode(byte[] png, string sceneName)
    {
        return SKBitmap.Decode(png) ??
            throw new InvalidDataException(
                $"Could not decode WindowsDX output for '{sceneName}'.");
    }

    private static bool IsWithinTolerance(
        SKColor left,
        SKColor right,
        int tolerance)
    {
        return Math.Abs(left.Red - right.Red) <= tolerance &&
            Math.Abs(left.Green - right.Green) <= tolerance &&
            Math.Abs(left.Blue - right.Blue) <= tolerance &&
            Math.Abs(left.Alpha - right.Alpha) <= tolerance;
    }

    private static uint Pack(SKColor color)
    {
        return ((uint)color.Alpha << 24) |
            ((uint)color.Red << 16) |
            ((uint)color.Green << 8) |
            color.Blue;
    }

    private static bool ContainsPixelOtherThanClearColor(SKBitmap bitmap)
    {
        SKColor clear = new(
            ClearColor.R,
            ClearColor.G,
            ClearColor.B,
            ClearColor.A);
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) != clear)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory =
            new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(
                Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the Cerneala repository root.");
    }

    private sealed record PrismScene(
        string Name,
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan,
        int ExpectedFallbackCount,
        int ForegroundX,
        int ForegroundY,
        int BackgroundX,
        int BackgroundY,
        IDisposable? OwnedResource = null) : IDisposable
    {
        public void Dispose()
        {
            OwnedResource?.Dispose();
        }
    }

    private sealed record ImageResource(
        PrismResourceId Id,
        PrismDrawResources Resources,
        MonoGameImage Image);

    private sealed record RenderedScene(
        byte[] Png,
        PrismExecutionCounters Counters,
        PrismRendererDiagnostics RendererDiagnostics,
        string GraphDump);

    private sealed class GoldenManifest
    {
        public string Platform { get; init; } = string.Empty;

        public string CaptureApi { get; init; } = string.Empty;

        public string PixelFormat { get; init; } = string.Empty;

        public string ColorProfile { get; init; } = string.Empty;

        public string WorkingColorProfile { get; init; } = string.Empty;

        public string AlphaConvention { get; init; } = string.Empty;

        public int Seed { get; init; }

        public ChannelTolerance ChannelTolerance { get; init; } = new();

        public SupportedHardware SupportedHardware { get; init; } = new();

        public int Width { get; init; }

        public int Height { get; init; }
    }

    private sealed class ChannelTolerance
    {
        public int Red { get; init; }

        public int Green { get; init; }

        public int Blue { get; init; }

        public int Alpha { get; init; }

        public int Maximum => Math.Max(
            Math.Max(Red, Green),
            Math.Max(Blue, Alpha));
    }

    private sealed class SupportedHardware
    {
        public string GraphicsApi { get; init; } = string.Empty;

        public string MinimumFeatureLevel { get; init; } = string.Empty;

        public string ShaderProfile { get; init; } = string.Empty;

        public string DriverPolicy { get; init; } = string.Empty;
    }

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new();
        private readonly IPlatformWindow window;
        private bool disposed;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title =
                        $"Cerneala Prism conformance {Guid.NewGuid():N}",
                    Width = Width,
                    Height = Height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session =
                Assert.IsType<WindowsDxWindowGraphicsSession>(
                    window.GraphicsSession);
            Session.Resize(Width, Height, coordinateScale: 1f);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            window.Dispose();
            platform.Dispose();
            disposed = true;
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested() { }
    }
}
