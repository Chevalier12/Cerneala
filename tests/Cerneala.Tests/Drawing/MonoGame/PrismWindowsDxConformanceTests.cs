using System.Numerics;
using System.Text.Json;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Hosting;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Prism.Definitions;
using SkiaSharp;
using CernealaColor = Cerneala.Drawing.Color;

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

        foreach (PrismScene scene in CreateScenes())
        {
            RenderedScene rendered = RenderPng(fixture.Session, scene);

            AssertMinimalExecution(scene, rendered);
            AssertMatchesGolden(
                scene,
                rendered.Png,
                manifest);
            AssertSemanticImage(
                scene,
                rendered.Png,
                manifest.ChannelTolerance);
        }
    }

    [Fact]
    public void ExecutedGraphDumpIsDeterministicAndCorrelatesNestedTransformScopes()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismScene[] scenes = CreateScenes();
        PrismScene nested = Assert.Single(
            scenes,
            scene => scene.Name == "nested-prism");
        PrismScene transform = Assert.Single(
            scenes,
            scene => scene.Name == "transform");
        using WindowsDxFixture fixture = new();

        RenderedScene nestedFirst = RenderPng(fixture.Session, nested);
        RenderedScene nestedSecond = RenderPng(fixture.Session, nested);
        RenderedScene transformFirst =
            RenderPng(fixture.Session, transform);
        RenderedScene transformSecond =
            RenderPng(fixture.Session, transform);

        Assert.Equal(nestedFirst.GraphDump, nestedSecond.GraphDump);
        Assert.Equal(
            transformFirst.GraphDump,
            transformSecond.GraphDump);
        Assert.StartsWith(
            "prism-execution v1",
            nestedFirst.GraphDump,
            StringComparison.Ordinal);
        Assert.Contains(
            "scope 1 commands=",
            nestedFirst.GraphDump,
            StringComparison.Ordinal);
        Assert.Contains(
            "depth=1 parent=0 owner=602",
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
                before.CreatedSurfaceCount);

            fixture.Session.Resize(112, 72, coordinateScale: 1f);

            PrismExecutionCounters after =
                RenderOnScreen(fixture.Session, scene);
            Assert.Equal(1, resettingCount);
            Assert.Equal(1, resetCount);
            Assert.Equal(
                scene.Plan.PeakLiveSurfaces,
                after.CreatedSurfaceCount);
            Assert.Equal(
                scene.Plan.PeakLiveSurfaces,
                after.PeakLiveSurfaceCount);
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
        PrismScene scene)
    {
        using MemoryStream output = new();
        PrismExecutionCounters counters = default;
        string? dump = null;
        IWindowScreenshotSource screenshotSource = session;

        screenshotSource.RenderPng(
            output,
            ClearColor,
            drawingBackend =>
            {
                MonoGameDrawingBackend backend =
                    Assert.IsType<MonoGameDrawingBackend>(
                        drawingBackend);
                DrawingFrameContext frameContext =
                    new(scene.Analysis);
                backend.Render(scene.Commands, in frameContext);
                counters = backend.PrismDiagnostics.Counters;
                dump =
                    backend.PrismDiagnostics.DumpExecutedGraph();
            });

        return new RenderedScene(
            output.ToArray(),
            counters,
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
        int expectedPassCount =
            scene.Plan.ExecutionOrder.Length +
            scene.Analysis.Scopes.Length;

        Assert.Equal(expectedPassCount, counters.PassCount);
        Assert.Equal(
            scene.Analysis.Scopes.Length,
            counters.CaptureCount);
        Assert.Equal(
            scene.Plan.PeakLiveSurfaces,
            counters.PeakLiveSurfaceCount);
        Assert.Equal(
            scene.Plan.PeakLiveSurfaces,
            counters.CreatedSurfaceCount);
        Assert.Equal(
            scene.Plan.ExecutionOrder.Length -
                scene.Plan.PeakLiveSurfaces,
            counters.ReusedSurfaceCount);
        Assert.Equal(
            scene.Plan.ExecutionOrder.Length,
            counters.CreatedSurfaceCount +
                counters.ReusedSurfaceCount);
        Assert.Equal(
            scene.ExpectedFallbackCount,
            counters.FallbackCount);
        Assert.True(
            counters.CpuSubmitTime > TimeSpan.Zero,
            $"{scene.Name} did not report CPU submit time.");

        if (scene.Name == "mask")
        {
            Assert.Contains(
                "reason=UnsupportedCapability",
                rendered.GraphDump,
                StringComparison.Ordinal);
        }
        else
        {
            Assert.DoesNotContain(
                "reason=UnsupportedCapability",
                rendered.GraphDump,
                StringComparison.Ordinal);
        }
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
                        manifest.ChannelTolerance),
                    $"{scene.Name} differs at ({x},{y}): " +
                    $"actual={actualPixel}, expected={expectedPixel}, " +
                    $"tolerance={manifest.ChannelTolerance}.");
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
        Assert.Equal("sRGB IEC61966-2.1", manifest.ColorProfile);
        Assert.Equal(
            "straight RGBA PNG; premultiplied-alpha compositor inputs",
            manifest.AlphaConvention);
        Assert.Equal(2, manifest.ChannelTolerance);
        Assert.Equal(Width, manifest.Width);
        Assert.Equal(Height, manifest.Height);
    }

    private static PrismScene[] CreateScenes()
    {
        return
        [
            CreateNormalBlendScene(),
            CreateOpacityScene(),
            CreateFillScene(),
            CreateMaskScene(),
            CreateClipScene(),
            CreateNestedScene(),
            CreateTransformScene()
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
            expectedFallbackCount: 2,
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
            expectedFallbackCount: 1,
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
            expectedFallbackCount: 1,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateMaskScene()
    {
        PrismMaskDefinition mask = new(
            new PrismResourceId(7001),
            density: 0.65f);
        PrismDrawScope scope = CreateScope(
            "Mask",
            ownerToken: 401,
            Layer(1, "Masked layer", mask: mask));
        return BuildScene(
            "mask",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 2,
            foregroundX: 20,
            foregroundY: 18);
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
            expectedFallbackCount: 2,
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
            expectedFallbackCount: 2,
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
            expectedFallbackCount: 1,
            foregroundX: 30,
            foregroundY: 26,
            backgroundX: 12,
            backgroundY: 12);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        params PrismLayerDefinition[] layers)
    {
        return CreateScope(
            name,
            ownerToken,
            Matrix3x2.Identity,
            layers);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        Matrix3x2 transform,
        params PrismLayerDefinition[] layers)
    {
        PrismCompositionDefinition composition = new(
            name,
            layers,
            workingColorProfile: PrismColorProfile.Srgb);
        return PrismTestData.Scope(
            composition,
            ownerToken,
            ScopeBounds,
            transform);
    }

    private static PrismLayerDefinition Layer(
        int id,
        string name,
        float opacity = 1,
        float fill = 1,
        PrismMaskDefinition? mask = null,
        bool clipToBelow = false)
    {
        return new PrismLayerDefinition(
            new PrismNodeId(id),
            name,
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Blur)
            ],
            mask: mask,
            opacity: opacity,
            fill: fill,
            blendMode: PrismBlendMode.Normal,
            clipToBelow: clipToBelow);
    }

    private static PrismScene BuildScene(
        string name,
        DrawCommandList commands,
        int expectedFallbackCount,
        int foregroundX,
        int foregroundY,
        int backgroundX = 2,
        int backgroundY = 2)
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
            backgroundY);
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
        int BackgroundY);

    private sealed record RenderedScene(
        byte[] Png,
        PrismExecutionCounters Counters,
        string GraphDump);

    private sealed class GoldenManifest
    {
        public string Platform { get; init; } = string.Empty;

        public string CaptureApi { get; init; } = string.Empty;

        public string ColorProfile { get; init; } = string.Empty;

        public string AlphaConvention { get; init; } = string.Empty;

        public int ChannelTolerance { get; init; }

        public int Width { get; init; }

        public int Height { get; init; }
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
