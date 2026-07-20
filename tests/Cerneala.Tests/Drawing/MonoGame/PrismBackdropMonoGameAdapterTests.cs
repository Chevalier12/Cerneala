using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.UI.Hosting;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.MonoGame;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using Microsoft.Xna.Framework.Graphics;
using SkiaSharp;
using CernealaColor = Cerneala.Drawing.Color;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

[Collection(WindowRuntimeTestCollection.Name)]
public sealed class PrismBackdropMonoGameAdapterTests
{
    private const int Width = 96;
    private const int Height = 64;
    private const string UpdateGoldensVariable =
        "CERNEALA_UPDATE_PRISM_GOLDENS";

    [Fact]
    public void WindowsDxLeaseBorrowsTheActiveFrameTargetAndExpiresAtPresent()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        WindowsDxWindowGraphicsSession session = fixture.Session;
        RenderTarget2D originalTarget = session.FrameTarget!;
        Assert.NotNull(originalTarget);

        session.BeginFrame(CernealaColor.Black);
        BackdropFrameRequest request =
            CreateRequest(Width, Height, 1f);
        IBackdropFrameLease lease =
            ((IBackdropFrameSource)session).AcquireFrame(in request);
        IMonoGameBackdropFrameLease monoGameLease =
            Assert.IsAssignableFrom<IMonoGameBackdropFrameLease>(lease);
        BackdropFrameMetadata metadata = lease.Metadata;

        Assert.Same(originalTarget, monoGameLease.Texture);
        Assert.Equal(Width, metadata.PixelWidth);
        Assert.Equal(Height, metadata.PixelHeight);
        Assert.Equal(1f, metadata.PixelScale);
        Assert.Equal(PrismColorProfile.Srgb, metadata.ColorProfile);
        Assert.Equal(BackdropPixelFormat.Rgba8Unorm, metadata.PixelFormat);
        Assert.Equal(BackdropAlphaMode.Premultiplied, metadata.AlphaMode);
        Assert.Equal(Matrix3x2.Identity, metadata.CoordinateTransform);
        Assert.Equal(1, session.ActiveBackdropLeaseCount);

        Assert.Throws<InvalidOperationException>(() => session.Present());
        Assert.False(session.IsFrameActive);
        Assert.Throws<InvalidOperationException>(
            () => _ = monoGameLease.Texture);
        lease.Dispose();
        Assert.Equal(0, session.ActiveBackdropLeaseCount);
        Assert.Throws<ObjectDisposedException>(
            () => _ = monoGameLease.Texture);

        session.Resize(112, 72, coordinateScale: 1.5f);
        Assert.True(originalTarget.IsDisposed);
        RenderTarget2D resizedTarget = session.FrameTarget!;
        Assert.NotNull(resizedTarget);
        Assert.Equal(112, resizedTarget.Width);
        Assert.Equal(72, resizedTarget.Height);
        Assert.Contains(
            resizedTarget.MultiSampleCount,
            new[]
            {
                0,
                session.GraphicsDevice.PresentationParameters.MultiSampleCount
            });

        session.BeginFrame(CernealaColor.Black);
        BackdropFrameRequest resizedRequest =
            CreateRequest(112, 72, 1.5f);
        using IBackdropFrameLease resizedLease =
            ((IBackdropFrameSource)session).AcquireFrame(
                in resizedRequest);
        Assert.True(
            resizedLease.Metadata.ContentVersion >
                metadata.ContentVersion);
        Assert.Equal(
            Matrix3x2.CreateScale(1.5f),
            resizedLease.Metadata.CoordinateTransform);
        resizedLease.Dispose();
        session.Present();
        Assert.Equal(0, session.ActiveBackdropLeaseCount);
    }

    [Fact]
    public void RenderPngKeepsItsDelegateContractAndExposesTheCaptureTarget()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        WindowsDxWindowGraphicsSession session = fixture.Session;
        RenderTarget2D persistentTarget = session.FrameTarget!;
        using MemoryStream output = new();
        long captureVersion = 0;

        ((IWindowScreenshotSource)session).RenderPng(
            output,
            CernealaColor.Black,
            drawingBackend =>
            {
                Assert.True(session.IsCompatibleWith(drawingBackend));
                BackdropFrameRequest request =
                    CreateRequest(Width, Height, 1f);
                using IBackdropFrameLease lease =
                    ((IBackdropFrameSource)session).AcquireFrame(
                        in request);
                IMonoGameBackdropFrameLease monoGameLease =
                    Assert.IsAssignableFrom<IMonoGameBackdropFrameLease>(
                        lease);
                Assert.NotSame(
                    persistentTarget,
                    monoGameLease.Texture);
                Assert.Equal(
                    SurfaceFormat.Color,
                    monoGameLease.Texture.Format);
                captureVersion = lease.Metadata.ContentVersion;
            });

        Assert.True(output.Length > 0);
        Assert.True(captureVersion > 0);
        Assert.False(session.IsFrameActive);
        Assert.Equal(0, session.ActiveBackdropLeaseCount);
        Assert.Same(persistentTarget, session.FrameTarget);
    }

    [Fact]
    public void BackdropSamplesLowerUiOnGpuAndUpperUiRemainsUnaffected()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        WindowsDxWindowGraphicsSession session = fixture.Session;
        DrawCommandList commands = CreateBackdropCommands();
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        Assert.NotNull(analysis.BackdropRequirement);
        byte[] png;
        string graphDump = string.Empty;

        using (MemoryStream output = new())
        {
            ((IWindowScreenshotSource)session).RenderPng(
                output,
                CernealaColor.Black,
                drawingBackend =>
                {
                    MonoGameDrawingBackend backend =
                        Assert.IsType<MonoGameDrawingBackend>(
                            drawingBackend);
                    BackdropFrameRequest request = new(
                        Width,
                        Height,
                        1f,
                        analysis.BackdropRequirement!);
                    using IBackdropFrameLease lease =
                        ((IBackdropFrameSource)session).AcquireFrame(
                            in request);
                    DrawingFrameContext frameContext =
                        new(analysis, lease);
                    backend.Render(commands, in frameContext);
                    Assert.Equal(
                        0,
                        backend.PrismDiagnostics.Counters.FallbackCount);
                    graphDump =
                        backend.PrismDiagnostics.DumpExecutedGraph();
                });
            png = output.ToArray();
        }

        using SKBitmap bitmap =
            SKBitmap.Decode(png) ??
            throw new InvalidDataException(
                "Could not decode the WindowsDX backdrop capture.");
        AssertNear(
            bitmap.GetPixel(4, 4),
            new SKColor(200, 40, 20),
            tolerance: 3);
        SKColor processedBackdrop = bitmap.GetPixel(22, 14);
        Assert.True(
            ColorDistance(
                processedBackdrop,
                new SKColor(200, 40, 20)) > 80,
            $"Backdrop anchor was not processed: {processedBackdrop}.");
        AssertNear(
            bitmap.GetPixel(31, 23),
            new SKColor(20, 220, 50),
            tolerance: 3);
        Assert.DoesNotContain(
            "reason=UnsupportedCapability",
            graphDump,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "reason=MissingBackdrop",
            graphDump,
            StringComparison.Ordinal);
        Assert.Equal(0, session.ActiveBackdropLeaseCount);
    }

    [Fact]
    public void TextureFormatMismatchFallsBackObservablyWithoutCpuReadback()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        WindowsDxWindowGraphicsSession session = fixture.Session;
        DrawCommandList commands = CreateBackdropCommands();
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        using Texture2D texture = new(
            session.GraphicsDevice,
            Width,
            Height,
            false,
            SurfaceFormat.Color);
        using FakeMonoGameBackdropLease lease = new(
            texture,
            new BackdropFrameMetadata(
                Width,
                Height,
                1f,
                PrismColorProfile.Srgb,
                BackdropPixelFormat.Bgra8Unorm,
                BackdropAlphaMode.Premultiplied,
                Matrix3x2.Identity,
                ContentVersion: 7));
        string graphDump = string.Empty;

        using MemoryStream output = new();
        ((IWindowScreenshotSource)session).RenderPng(
            output,
            CernealaColor.Black,
            drawingBackend =>
            {
                MonoGameDrawingBackend backend =
                    Assert.IsType<MonoGameDrawingBackend>(
                        drawingBackend);
                DrawingFrameContext frameContext =
                    new(analysis, lease);
                backend.Render(commands, in frameContext);
                Assert.True(
                    backend.PrismDiagnostics.Counters.FallbackCount > 0);
                graphDump =
                    backend.PrismDiagnostics.DumpExecutedGraph();
            });

        Assert.Contains(
            "reason=UnsupportedCapability",
            graphDump,
            StringComparison.Ordinal);
        string adapterSource = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                "UI",
                "Hosting",
                "Windows",
                "WindowsDxWindowGraphicsSession.cs"));
        string executorSource = File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                "Drawing",
                "MonoGame",
                "Prism",
                "Execution",
                "PrismGraphExecutor.cs"));
        Assert.DoesNotContain(
            ".GetData",
            adapterSource,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            ".GetData",
            executorSource,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MonoGameHostReplacesProvidersTransactionallyWithoutTakingOwnership()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D whitePixel = new(graphicsDevice, 1, 1);
        whitePixel.SetData([XnaColor.White]);
        TrackingBackdropSource initial = new(compatible: true);
        TrackingBackdropSource replacement = new(compatible: true);
        TrackingBackdropSource incompatible =
            new(compatible: false);
        using MonoGameUiHost host = new(
            new MonoGameUiHostOptions
            {
                SpriteBatch = spriteBatch,
                WhitePixel = whitePixel,
                Viewport = new UiViewport(Width, Height),
                BackdropFrameSource = initial
            });

        Assert.Same(initial, host.BackdropFrameSource);
        host.BackdropFrameSource = replacement;
        Assert.Same(replacement, host.BackdropFrameSource);
        Assert.Throws<InvalidOperationException>(
            () => host.BackdropFrameSource = incompatible);
        Assert.Same(replacement, host.BackdropFrameSource);
        host.BackdropFrameSource = null;
        host.Dispose();

        Assert.False(initial.IsDisposed);
        Assert.False(replacement.IsDisposed);
        Assert.False(incompatible.IsDisposed);
    }

    [Fact]
    public void MonoGameUiHostBackdropSceneMatchesGoldensAndSharesOneLeaseAtEveryScale()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        WindowsDxWindowGraphicsSession session = fixture.Session;
        using BackdropScene scene = CreateBackdropHostingScene();
        CountingBackdropSource source = new(session);
        using SpriteBatch spriteBatch =
            new(session.GraphicsDevice);
        using Texture2D whitePixel =
            new(session.GraphicsDevice, 1, 1);
        whitePixel.SetData([XnaColor.White]);
        using MonoGameUiHost host = new(
            new MonoGameUiHostOptions
            {
                SpriteBatch = spriteBatch,
                WhitePixel = whitePixel,
                Root = scene.Root,
                Viewport = new UiViewport(Width, Height),
                BackdropFrameSource = source
            });

        BackdropCapture native = CaptureHostedBackdrop(
            session,
            host,
            new UiViewport(Width, Height));
        AssertBackdropGolden(
            "backdrop-hosting-native.png",
            native.Png);
        AssertBackdropSceneSemantics(
            native.Png,
            Width,
            Height,
            scale: 1);

        session.Resize(
            Width * 3 / 2,
            Height * 3 / 2,
            coordinateScale: 1.5f);
        BackdropCapture scaled = CaptureHostedBackdrop(
            session,
            host,
            new UiViewport(Width, Height, 1.5f));
        AssertBackdropGolden(
            "backdrop-hosting-150.png",
            scaled.Png);
        AssertBackdropSceneSemantics(
            scaled.Png,
            Width * 3 / 2,
            Height * 3 / 2,
            scale: 1.5f);
        AssertScaledAnchorMatches(
            native.Png,
            scaled.Png,
            logicalX: 18,
            logicalY: 30,
            scale: 1.5f);

        Assert.Equal(2, source.AcquireCalls);
        Assert.Equal(2, source.ReleasedLeases);
        Assert.Equal(0, source.ActiveLeases);
        Assert.Equal(1, source.PeakActiveLeases);
        Assert.Equal(2, host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(2, host.BackdropFrameCounters.AcquiredFrames);
        Assert.Equal(2, host.BackdropFrameCounters.SharedScopeUses);
        Assert.Equal(0, host.BackdropFrameCounters.FailedFrames);
        AssertResourceBudget(native.Counters);
        AssertResourceBudget(scaled.Counters);
        Assert.Equal(0, session.ActiveBackdropLeaseCount);
    }

    [Fact]
    public void BackdropLifecycleStressReleasesResourcesAcrossVisibilityReplacementResetAndNavigation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        LifecycleReferences references =
            RunBackdropLifecycleStress();
        foreach (WeakReference provider in
                 references.ReplacedProviders)
        {
            AssertEventuallyCollected(
                provider,
                "A replaced backdrop provider was retained.");
        }
        AssertEventuallyCollected(
            references.Session,
            "The disposed navigation session was retained.");

        using WindowsDxFixture replacementFixture = new();
        WindowsDxWindowGraphicsSession replacementSession =
            replacementFixture.Session;
        DrawCommandList commands = CreateBackdropCommands();
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        using MemoryStream output = new();
        ((IWindowScreenshotSource)replacementSession).RenderPng(
            output,
            CernealaColor.Black,
            drawingBackend =>
            {
                BackdropFrameRequest request = new(
                    Width,
                    Height,
                    1,
                    analysis.BackdropRequirement!);
                using IBackdropFrameLease lease =
                    ((IBackdropFrameSource)replacementSession)
                        .AcquireFrame(in request);
                DrawingFrameContext frameContext =
                    new(analysis, lease);
                drawingBackend.Render(
                    commands,
                    in frameContext);
            });

        Assert.True(output.Length > 0);
        Assert.Equal(
            0,
            replacementSession.ActiveBackdropLeaseCount);
    }

    private static BackdropFrameRequest CreateRequest(
        int width,
        int height,
        float scale)
    {
        PrismBackdropRequirement requirement = new(
            ImmutableArray.Create(0));
        return new BackdropFrameRequest(
            width,
            height,
            scale,
            requirement);
    }

    private static DrawCommandList CreateBackdropCommands()
    {
        PrismCompositionDefinition composition = new(
            "GPU backdrop order",
            [
                new PrismBackdropDefinition(
                    new PrismNodeId(1),
                    "Invert lower UI",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.Invert)
                    ])
            ],
            workingColorProfile:
                PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 4_001,
            bounds: new DrawRect(18, 10, 50, 36));
        return PrismTestData.Commands(
        [
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, Width, Height),
                new CernealaColor(200, 40, 20)),
            DrawCommand.BeginPrism(scope),
            DrawCommand.EndPrism(),
            DrawCommand.FillRectangle(
                new DrawRect(28, 20, 8, 8),
                new CernealaColor(20, 220, 50))
        ]);
    }

    private static BackdropScene CreateBackdropHostingScene()
    {
        UIRoot root = new();
        LayoutCanvas canvas = new();
        root.VisualChildren.Add(canvas);

        SceneElement world = new(
            Width,
            Height,
            [
                new SceneRectangle(
                    new DrawRect(0, 0, Width, Height),
                    new CernealaColor(14, 34, 66)),
                new SceneRectangle(
                    new DrawRect(0, 36, Width, 28),
                    new CernealaColor(34, 126, 76)),
                new SceneRectangle(
                    new DrawRect(0, 47, Width, 9),
                    new CernealaColor(99, 75, 53)),
                new SceneRectangle(
                    new DrawRect(19, 7, 13, 13),
                    new CernealaColor(249, 204, 61)),
                new SceneRectangle(
                    new DrawRect(25, 16, 9, 25),
                    new CernealaColor(222, 61, 76)),
                new SceneRectangle(
                    new DrawRect(34, 12, 9, 29),
                    new CernealaColor(49, 142, 224)),
                new SceneRectangle(
                    new DrawRect(56, 29, 11, 12),
                    new CernealaColor(245, 134, 43)),
                new SceneRectangle(
                    new DrawRect(75, 15, 4, 27),
                    new CernealaColor(235, 241, 250)),
                new SceneRectangle(
                    new DrawRect(72, 15, 11, 4),
                    new CernealaColor(235, 241, 250)),
                new SceneRectangle(
                    new DrawRect(10, 49, 10, 5),
                    new CernealaColor(255, 227, 82))
            ]);
        SceneElement lowerUi = new(
            Width,
            Height,
            [
                new SceneRectangle(
                    new DrawRect(2, 2, 30, 8),
                    new CernealaColor(213, 49, 70)),
                new SceneRectangle(
                    new DrawRect(5, 4, 13, 3),
                    new CernealaColor(255, 222, 73)),
                new SceneRectangle(
                    new DrawRect(66, 2, 27, 8),
                    new CernealaColor(45, 129, 222)),
                new SceneRectangle(
                    new DrawRect(70, 4, 17, 3),
                    new CernealaColor(232, 241, 251))
            ]);
        SceneElement firstControl = new(
            36,
            30,
            [
                new SceneRectangle(
                    new DrawRect(1, 1, 34, 28),
                    new CernealaColor(226, 239, 252, 82)),
                new SceneRectangle(
                    new DrawRect(4, 4, 28, 5),
                    new CernealaColor(255, 255, 255, 86))
            ]);
        SceneElement secondControl = new(
            32,
            28,
            [
                new SceneRectangle(
                    new DrawRect(1, 1, 30, 26),
                    new CernealaColor(245, 231, 255, 76)),
                new SceneRectangle(
                    new DrawRect(5, 18, 22, 5),
                    new CernealaColor(255, 255, 255, 78))
            ]);
        SceneElement upperUi = new(
            Width,
            Height,
            [
                new SceneRectangle(
                    new DrawRect(28, 18, 10, 8),
                    new CernealaColor(24, 238, 93)),
                new SceneRectangle(
                    new DrawRect(69, 30, 9, 8),
                    new CernealaColor(245, 67, 178)),
                new SceneRectangle(
                    new DrawRect(82, 57, 10, 4),
                    new CernealaColor(244, 248, 253))
            ]);

        canvas.VisualChildren.Add(world);
        canvas.VisualChildren.Add(lowerUi);
        canvas.VisualChildren.Add(firstControl);
        canvas.VisualChildren.Add(secondControl);
        canvas.VisualChildren.Add(upperUi);
        LayoutCanvas.SetLeft(firstControl, 10);
        LayoutCanvas.SetTop(firstControl, 12);
        LayoutCanvas.SetLeft(secondControl, 52);
        LayoutCanvas.SetTop(secondControl, 22);

        IDisposable firstAttachment =
            AttachHostingBackdrop(
                firstControl,
                "Navigation glass A",
                nodeId: 100);
        IDisposable secondAttachment =
            AttachHostingBackdrop(
                secondControl,
                "Navigation glass B",
                nodeId: 200);
        return new BackdropScene(
            root,
            firstControl,
            secondControl,
            firstAttachment,
            secondAttachment);
    }

    private static IDisposable AttachHostingBackdrop(
        UIElement element,
        string name,
        int nodeId)
    {
        return GeneratedMarkup.AttachPrism(
            element,
            () => new PrismInstance(
                new PrismCompositionDefinition(
                    name,
                    [
                        new PrismLayerDefinition(
                            new PrismNodeId(nodeId),
                            "Glass content",
                            filters:
                            [
                                new PrismFilterDefinition(
                                    PrismFilterId
                                        .BrightnessContrast)
                            ]),
                        new PrismBackdropDefinition(
                            new PrismNodeId(nodeId + 1),
                            "Blurred color backdrop",
                            filters:
                            [
                                new PrismFilterDefinition(
                                    PrismFilterId.GaussianBlur),
                                new PrismFilterDefinition(
                                    PrismFilterId.Invert)
                            ],
                            opacity: 0.76f)
                    ],
                    workingColorProfile:
                        PrismColorProfile.LinearSrgb)));
    }

    private static BackdropCapture CaptureHostedBackdrop(
        WindowsDxWindowGraphicsSession session,
        MonoGameUiHost host,
        UiViewport viewport)
    {
        host.Update(
            FakeInputSource.CreateFrame(),
            viewport,
            TimeSpan.FromMilliseconds(16));
        using MemoryStream output = new();
        PrismExecutionCounters counters = default;
        ((IWindowScreenshotSource)session).RenderPng(
            output,
            CernealaColor.Black,
            _ =>
            {
                host.Draw();
                counters = host.PrismExecutionCounters;
            });

        Assert.Equal(0, session.ActiveBackdropLeaseCount);
        Assert.False(session.IsFrameActive);
        return new BackdropCapture(
            output.ToArray(),
            counters);
    }

    private static void AssertBackdropGolden(
        string fileName,
        byte[] actualPng)
    {
        string goldenPath = Path.Combine(
            FindRepositoryRoot(),
            "tests",
            "Cerneala.Tests",
            "Golden",
            "Prism",
            fileName);
        if (string.Equals(
            Environment.GetEnvironmentVariable(
                UpdateGoldensVariable),
            "1",
            StringComparison.Ordinal))
        {
            File.WriteAllBytes(goldenPath, actualPng);
        }

        Assert.True(
            File.Exists(goldenPath),
            $"Missing backdrop golden '{goldenPath}'. Set " +
            $"{UpdateGoldensVariable}=1 to regenerate it through WindowsDX.");
        using SKBitmap actual =
            SKBitmap.Decode(actualPng) ??
            throw new InvalidDataException(
                "Could not decode the actual backdrop capture.");
        using SKBitmap expected =
            SKBitmap.Decode(goldenPath) ??
            throw new InvalidDataException(
                $"Could not decode backdrop golden '{goldenPath}'.");
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);

        const int tolerance = 2;
        for (int y = 0; y < actual.Height; y++)
        {
            for (int x = 0; x < actual.Width; x++)
            {
                SKColor actualPixel = actual.GetPixel(x, y);
                SKColor expectedPixel = expected.GetPixel(x, y);
                Assert.True(
                    Math.Abs(actualPixel.Red - expectedPixel.Red) <=
                        tolerance &&
                    Math.Abs(actualPixel.Green - expectedPixel.Green) <=
                        tolerance &&
                    Math.Abs(actualPixel.Blue - expectedPixel.Blue) <=
                        tolerance &&
                    Math.Abs(actualPixel.Alpha - expectedPixel.Alpha) <=
                        tolerance,
                    $"{fileName} differs at ({x},{y}): " +
                    $"actual={actualPixel}, expected={expectedPixel}.");
            }
        }
    }

    private static void AssertBackdropSceneSemantics(
        byte[] png,
        int expectedWidth,
        int expectedHeight,
        float scale)
    {
        using SKBitmap bitmap =
            SKBitmap.Decode(png) ??
            throw new InvalidDataException(
                "Could not decode the backdrop hosting capture.");
        Assert.Equal(expectedWidth, bitmap.Width);
        Assert.Equal(expectedHeight, bitmap.Height);

        SKColor upperUi = PixelAtLogical(
            bitmap,
            logicalX: 31,
            logicalY: 21,
            scale);
        AssertNear(
            upperUi,
            new SKColor(24, 238, 93),
            tolerance: 3);
        SKColor upperUiOverSecondControl = PixelAtLogical(
            bitmap,
            logicalX: 72,
            logicalY: 33,
            scale);
        AssertNear(
            upperUiOverSecondControl,
            new SKColor(245, 67, 178),
            tolerance: 3);

        SKColor outsideFirstControl = PixelAtLogical(
            bitmap,
            logicalX: 8,
            logicalY: 30,
            scale);
        SKColor insideFirstControl = PixelAtLogical(
            bitmap,
            logicalX: 18,
            logicalY: 30,
            scale);
        SKColor insideSecondControl = PixelAtLogical(
            bitmap,
            logicalX: 60,
            logicalY: 34,
            scale);
        Assert.True(
            ColorDistance(
                outsideFirstControl,
                insideFirstControl) > 35,
            "The first backdrop did not alter its gameplay sample.");
        Assert.True(
            ColorDistance(
                outsideFirstControl,
                insideSecondControl) > 35,
            "The second backdrop did not alter its gameplay sample.");
        Assert.Equal(byte.MaxValue, insideFirstControl.Alpha);
        Assert.Equal(byte.MaxValue, insideSecondControl.Alpha);

        SKColor blurEdge = PixelAtLogical(
            bitmap,
            logicalX: 11,
            logicalY: 28,
            scale);
        SKColor blurCenter = PixelAtLogical(
            bitmap,
            logicalX: 26,
            logicalY: 28,
            scale);
        Assert.True(
            ColorDistance(blurEdge, blurCenter) > 15,
            "The backdrop blur edge collapsed to a flat fill.");
    }

    private static void AssertScaledAnchorMatches(
        byte[] nativePng,
        byte[] scaledPng,
        int logicalX,
        int logicalY,
        float scale)
    {
        using SKBitmap native =
            SKBitmap.Decode(nativePng) ??
            throw new InvalidDataException(
                "Could not decode the native backdrop capture.");
        using SKBitmap scaled =
            SKBitmap.Decode(scaledPng) ??
            throw new InvalidDataException(
                "Could not decode the scaled backdrop capture.");
        AssertNear(
            PixelAtLogical(
                scaled,
                logicalX,
                logicalY,
                scale),
            native.GetPixel(logicalX, logicalY),
            tolerance: 12);
    }

    private static SKColor PixelAtLogical(
        SKBitmap bitmap,
        int logicalX,
        int logicalY,
        float scale)
    {
        int x = Math.Clamp(
            (int)MathF.Round(logicalX * scale),
            0,
            bitmap.Width - 1);
        int y = Math.Clamp(
            (int)MathF.Round(logicalY * scale),
            0,
            bitmap.Height - 1);
        return bitmap.GetPixel(x, y);
    }

    private static void AssertResourceBudget(
        PrismExecutionCounters counters)
    {
        Assert.True(counters.PassCount > 0);
        Assert.Equal(2, counters.CaptureCount);
        Assert.True(counters.PeakLiveSurfaceCount > 0);
        Assert.True(
            counters.CreatedSurfaceCount +
                counters.ReusedSurfaceCount > 0);
        Assert.Equal(0, counters.FallbackCount);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static LifecycleReferences RunBackdropLifecycleStress()
    {
        using WindowsDxFixture fixture = new();
        WindowsDxWindowGraphicsSession session = fixture.Session;
        WeakReference sessionReference = new(session);
        using BackdropScene scene = CreateBackdropHostingScene();
        using SpriteBatch spriteBatch =
            new(session.GraphicsDevice);
        using Texture2D whitePixel =
            new(session.GraphicsDevice, 1, 1);
        whitePixel.SetData([XnaColor.White]);
        CountingBackdropSource? initialSource = new(session);
        using MonoGameUiHost host = new(
            new MonoGameUiHostOptions
            {
                SpriteBatch = spriteBatch,
                WhitePixel = whitePixel,
                Root = scene.Root,
                Viewport = new UiViewport(Width, Height),
                BackdropFrameSource = initialSource
            });
        ProviderSlot providers =
            new(host, initialSource);
        initialSource = null;
        int visibleFrames = 0;
        int hiddenFrames = 0;
        int pixelWidth = Width;
        int pixelHeight = Height;
        int surfaceBudget = 0;
        List<WeakReference> replacedProviders = [];

        for (int iteration = 0; iteration < 16; iteration++)
        {
            bool visible = iteration % 4 != 0;
            scene.FirstControl.Visibility =
                visible
                    ? Visibility.Visible
                    : Visibility.Hidden;
            scene.SecondControl.Visibility =
                visible
                    ? Visibility.Visible
                    : Visibility.Hidden;

            if (iteration % 3 == 0)
            {
                fixture.HideAndShow();
            }

            if (iteration == 5)
            {
                pixelWidth = 112;
                pixelHeight = 72;
                session.Resize(
                    pixelWidth,
                    pixelHeight,
                    coordinateScale: 1);
            }
            else if (iteration == 11)
            {
                pixelWidth = Width;
                pixelHeight = Height;
                session.Resize(
                    pixelWidth,
                    pixelHeight,
                    coordinateScale: 1);
            }

            if (iteration is 3 or 8 or 13)
            {
                replacedProviders.Add(
                    providers.Replace(session));
            }

            BackdropCapture capture = CaptureHostedBackdrop(
                session,
                host,
                new UiViewport(pixelWidth, pixelHeight));
            Assert.True(capture.Png.Length > 0);
            Assert.Equal(0, providers.Current.ActiveLeases);
            Assert.InRange(
                providers.Current.PeakActiveLeases,
                0,
                1);
            Assert.Equal(
                0,
                session.ActiveBackdropLeaseCount);

            if (visible)
            {
                visibleFrames++;
                AssertResourceBudget(capture.Counters);
                surfaceBudget = Math.Max(
                    surfaceBudget,
                    capture.Counters.PeakLiveSurfaceCount);
                Assert.InRange(
                    capture.Counters.CreatedSurfaceCount,
                    0,
                    surfaceBudget);
            }
            else
            {
                hiddenFrames++;
                Assert.Equal(
                    0,
                    capture.Counters.CaptureCount);
            }
        }

        providers.Complete();
        Assert.Equal(
            visibleFrames,
            providers.TotalAcquireCalls);
        Assert.Equal(
            visibleFrames,
            providers.TotalReleasedLeases);
        Assert.Equal(
            visibleFrames,
            host.BackdropFrameCounters.RequestedFrames);
        Assert.Equal(
            visibleFrames,
            host.BackdropFrameCounters.AcquiredFrames);
        Assert.Equal(
            visibleFrames,
            host.BackdropFrameCounters.SharedScopeUses);
        Assert.Equal(
            hiddenFrames,
            host.BackdropFrameCounters.SkippedFrames);
        Assert.Equal(
            0,
            host.BackdropFrameCounters.FailedFrames);
        Assert.True(surfaceBudget > 0);

        return new LifecycleReferences(
            sessionReference,
            replacedProviders.ToArray());
    }

    private static void AssertEventuallyCollected(
        WeakReference reference,
        string message)
    {
        for (int attempt = 0;
             attempt < 8 && reference.IsAlive;
             attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Thread.Sleep(10);
        }

        Assert.False(reference.IsAlive, message);
    }

    private static void AssertNear(
        SKColor actual,
        SKColor expected,
        int tolerance)
    {
        Assert.True(
            Math.Abs(actual.Red - expected.Red) <= tolerance &&
            Math.Abs(actual.Green - expected.Green) <= tolerance &&
            Math.Abs(actual.Blue - expected.Blue) <= tolerance &&
            Math.Abs(actual.Alpha - expected.Alpha) <= tolerance,
            $"Expected {expected} (+/- {tolerance}), got {actual}.");
    }

    private static int ColorDistance(
        SKColor left,
        SKColor right)
    {
        return Math.Abs(left.Red - right.Red) +
            Math.Abs(left.Green - right.Green) +
            Math.Abs(left.Blue - right.Blue);
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

    private sealed class FakeMonoGameBackdropLease :
        IMonoGameBackdropFrameLease
    {
        public FakeMonoGameBackdropLease(
            Texture2D texture,
            BackdropFrameMetadata metadata)
        {
            Texture = texture;
            Metadata = metadata;
        }

        public Texture2D Texture { get; }

        public BackdropFrameMetadata Metadata { get; }

        public void Dispose()
        {
        }
    }

    private sealed class TrackingBackdropSource :
        IBackdropFrameSource,
        IDisposable
    {
        private readonly bool compatible;

        public TrackingBackdropSource(bool compatible)
        {
            this.compatible = compatible;
        }

        public bool IsDisposed { get; private set; }

        public bool IsCompatibleWith(
            IDrawingBackend drawingBackend) =>
            compatible &&
            drawingBackend is MonoGameDrawingBackend;

        public IBackdropFrameLease AcquireFrame(
            in BackdropFrameRequest request)
        {
            throw new InvalidOperationException(
                "The provider-replacement test must not acquire a frame.");
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class CountingBackdropSource :
        IBackdropFrameSource
    {
        private readonly IBackdropFrameSource inner;

        public CountingBackdropSource(
            IBackdropFrameSource inner)
        {
            this.inner = inner;
        }

        public int AcquireCalls { get; private set; }

        public int ActiveLeases { get; private set; }

        public int PeakActiveLeases { get; private set; }

        public int ReleasedLeases { get; private set; }

        public bool IsCompatibleWith(
            IDrawingBackend drawingBackend) =>
            inner.IsCompatibleWith(drawingBackend);

        public IBackdropFrameLease AcquireFrame(
            in BackdropFrameRequest request)
        {
            IMonoGameBackdropFrameLease lease =
                Assert.IsAssignableFrom<
                    IMonoGameBackdropFrameLease>(
                    inner.AcquireFrame(in request));
            AcquireCalls++;
            ActiveLeases++;
            PeakActiveLeases = Math.Max(
                PeakActiveLeases,
                ActiveLeases);
            return new CountingBackdropLease(
                this,
                lease);
        }

        private void Release()
        {
            ActiveLeases--;
            ReleasedLeases++;
        }

        private sealed class CountingBackdropLease :
            IMonoGameBackdropFrameLease
        {
            private CountingBackdropSource? owner;
            private IMonoGameBackdropFrameLease? inner;

            public CountingBackdropLease(
                CountingBackdropSource owner,
                IMonoGameBackdropFrameLease inner)
            {
                this.owner = owner;
                this.inner = inner;
            }

            public Texture2D Texture =>
                (inner ??
                 throw new ObjectDisposedException(
                     nameof(CountingBackdropLease)))
                .Texture;

            public BackdropFrameMetadata Metadata =>
                (inner ??
                 throw new ObjectDisposedException(
                     nameof(CountingBackdropLease)))
                .Metadata;

            public void Dispose()
            {
                IMonoGameBackdropFrameLease? currentInner =
                    Interlocked.Exchange(
                        ref inner,
                        null);
                CountingBackdropSource? currentOwner =
                    Interlocked.Exchange(
                        ref owner,
                        null);
                if (currentInner is null ||
                    currentOwner is null)
                {
                    return;
                }

                try
                {
                    currentInner.Dispose();
                }
                finally
                {
                    currentOwner.Release();
                }
            }
        }
    }

    private sealed class ProviderSlot
    {
        private readonly MonoGameUiHost host;
        private bool completed;

        public ProviderSlot(
            MonoGameUiHost host,
            CountingBackdropSource current)
        {
            this.host = host;
            Current = current;
        }

        public CountingBackdropSource Current
        {
            get;
            private set;
        }

        public int TotalAcquireCalls { get; private set; }

        public int TotalReleasedLeases { get; private set; }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public WeakReference Replace(
            IBackdropFrameSource inner)
        {
            CountingBackdropSource previous = Current;
            Assert.Equal(0, previous.ActiveLeases);
            Add(previous);
            CountingBackdropSource replacement =
                new(inner);
            host.BackdropFrameSource = replacement;
            Current = replacement;
            return new WeakReference(previous);
        }

        public void Complete()
        {
            if (completed)
            {
                return;
            }

            Assert.Equal(0, Current.ActiveLeases);
            Add(Current);
            completed = true;
        }

        private void Add(
            CountingBackdropSource source)
        {
            TotalAcquireCalls += source.AcquireCalls;
            TotalReleasedLeases +=
                source.ReleasedLeases;
        }
    }

    private sealed class BackdropScene : IDisposable
    {
        private readonly IDisposable firstAttachment;
        private readonly IDisposable secondAttachment;

        public BackdropScene(
            UIRoot root,
            SceneElement firstControl,
            SceneElement secondControl,
            IDisposable firstAttachment,
            IDisposable secondAttachment)
        {
            Root = root;
            FirstControl = firstControl;
            SecondControl = secondControl;
            this.firstAttachment = firstAttachment;
            this.secondAttachment = secondAttachment;
        }

        public UIRoot Root { get; }

        public SceneElement FirstControl { get; }

        public SceneElement SecondControl { get; }

        public void Dispose()
        {
            secondAttachment.Dispose();
            firstAttachment.Dispose();
        }
    }

    private sealed class SceneElement : UIElement
    {
        private readonly LayoutSize size;
        private readonly SceneRectangle[] rectangles;

        public SceneElement(
            float width,
            float height,
            SceneRectangle[] rectangles)
        {
            size = new LayoutSize(width, height);
            this.rectangles = rectangles;
        }

        protected override LayoutSize MeasureCore(
            MeasureContext context) =>
            size;

        protected override LayoutRect ArrangeCore(
            ArrangeContext context) =>
            context.FinalRect;

        protected override void OnRender(
            RenderContext context)
        {
            foreach (SceneRectangle rectangle in rectangles)
            {
                DrawRect bounds = rectangle.Bounds;
                context.DrawingContext.FillRectangle(
                    new DrawRect(
                        context.Bounds.X + bounds.X,
                        context.Bounds.Y + bounds.Y,
                        bounds.Width,
                        bounds.Height),
                    rectangle.Color);
            }
        }
    }

    private readonly record struct SceneRectangle(
        DrawRect Bounds,
        CernealaColor Color);

    private readonly record struct BackdropCapture(
        byte[] Png,
        PrismExecutionCounters Counters);

    private readonly record struct LifecycleReferences(
        WeakReference Session,
        WeakReference[] ReplacedProviders);

    private sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new();
        private readonly IPlatformWindow window;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title =
                        $"Cerneala backdrop adapter {Guid.NewGuid():N}",
                    Width = Width,
                    Height = Height
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session =
                Assert.IsType<WindowsDxWindowGraphicsSession>(
                    window.GraphicsSession);
            Session.Resize(
                Width,
                Height,
                coordinateScale: 1f);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void HideAndShow()
        {
            window.Hide();
            platform.PumpEvents();
            window.Show();
            platform.PumpEvents();
        }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
    }

    private sealed class CallbackSink :
        IWindowPlatformCallbacks
    {
        public void RequestClose()
        {
        }

        public void ActivationChanged(bool active)
        {
        }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested()
        {
        }
    }
}
