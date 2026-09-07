using Cerneala.Backends.SdlGpu;
using Cerneala.Tests.SdlGpu;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Rendering;
using SkiaSharp;
using CernealaColor = Cerneala.Drawing.Color;
using LayoutCanvas = Cerneala.UI.Layout.Panels.Canvas;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismBackdropHostingMigrationTests
{
    private const int Width = 96;
    private const int Height = 64;

    [Fact]
    public void PrismRuntimeDoesNotReadGpuSurfacesBackToTheCpu()
    {
        string runtimeDirectory = Path.Combine(
            FindRepositoryRoot(), "Cerneala.Backends.SdlGpu", "Prism");
        string[] sources = Directory.GetFiles(
            runtimeDirectory, "*.cs", SearchOption.AllDirectories);
        Assert.NotEmpty(sources);
        foreach (string path in sources)
        {
            string source = File.ReadAllText(path);
            foreach (string readback in new[]
            {
                "DownloadFromGpuTexture", "MapGpuTransferBuffer", "CapturePresentedFrame"
            })
            {
                Assert.False(source.Contains(readback, StringComparison.Ordinal),
                    $"Prism runtime source '{path}' contains CPU readback '{readback}'.");
            }
        }
    }

    [SdlNativeFact]
    public void SdlLeaseBorrowsTheActiveFrameTargetAndExpiresAtPresent()
    {
        using BackdropFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        nint originalTarget = session.FrameTexture;
        Assert.NotEqual(0, originalTarget);

        session.BeginFrame(CernealaColor.Black);
        BackdropFrameRequest request =
            CreateRequest(Width, Height, 1f);
        IBackdropFrameLease lease =
            ((IBackdropFrameSource)session).AcquireFrame(in request);
        ISdlGpuBackdropFrameLease sdlLease =
            Assert.IsAssignableFrom<ISdlGpuBackdropFrameLease>(lease);
        BackdropFrameMetadata metadata = lease.Metadata;

        Assert.Equal(originalTarget, sdlLease.Texture);
        Assert.Equal(Width, metadata.PixelWidth);
        Assert.Equal(Height, metadata.PixelHeight);
        Assert.Equal(1f, metadata.PixelScale);
        Assert.Equal(PrismColorProfile.Srgb, metadata.ColorProfile);
        Assert.Contains(metadata.PixelFormat, new[] { BackdropPixelFormat.Bgra8Unorm, BackdropPixelFormat.Rgba8Unorm });
        Assert.Equal(BackdropAlphaMode.Premultiplied, metadata.AlphaMode);
        Assert.Equal(Matrix3x2.Identity, metadata.CoordinateTransform);
        Assert.Equal(1, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);

        Assert.Throws<InvalidOperationException>(() => session.Present());
        Assert.False(session.IsFrameActive);
        Assert.Throws<InvalidOperationException>(
            () => _ = sdlLease.Texture);
        lease.Dispose();
        Assert.Equal(0, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);
        Assert.Throws<ObjectDisposedException>(
            () => _ = sdlLease.Texture);

        session.Resize(112, 72, coordinateScale: 1.5f);
        Assert.NotEqual(0, session.FrameTexture);
        Assert.Equal(112, session.PixelWidth);
        Assert.Equal(72, session.PixelHeight);

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
        Assert.Equal(0, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);
    }

    [SdlNativeFact]
    public void RenderPngKeepsItsDelegateContractAndExposesTheActiveTarget()
    {
        using BackdropFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        nint persistentTarget = session.FrameTexture;
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
                ISdlGpuBackdropFrameLease sdlLease =
                    Assert.IsAssignableFrom<ISdlGpuBackdropFrameLease>(
                        lease);
                Assert.Equal(persistentTarget, sdlLease.Texture);
                captureVersion = lease.Metadata.ContentVersion;
            });

        Assert.True(output.Length > 0);
        Assert.True(captureVersion > 0);
        Assert.False(session.IsFrameActive);
        Assert.Equal(0, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);
        Assert.Equal(persistentTarget, session.FrameTexture);
    }

    [SdlNativeFact]
    public void BackdropSamplesLowerUiOnGpuAndUpperUiRemainsUnaffected()
    {
        using BackdropFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
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
                    SdlGpuDrawingBackend backend =
                        Assert.IsType<SdlGpuDrawingBackend>(
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
                "Could not decode the SDL_GPU backdrop capture.");
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
        Assert.Equal(0, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);
    }

    [SdlNativeFact]
    public void UiHostReplacesProvidersTransactionallyWithoutTakingOwnership()
    {
        using BackdropFixture fixture = new();
        TrackingBackdropSource initial = new(compatible: true);
        TrackingBackdropSource replacement = new(compatible: true);
        TrackingBackdropSource incompatible =
            new(compatible: false);
        UiHost host = new(
            new UiHostOptions
            {
                Viewport = new UiViewport(Width, Height),
                Backend = new HostBackend(fixture.Session.DrawingBackend, initial)
            });

        Assert.Same(initial, host.Backend!.BackdropFrameSource);
        host.Backend = new HostBackend(fixture.Session.DrawingBackend, replacement);
        Assert.Same(replacement, host.Backend!.BackdropFrameSource);
        Assert.Throws<InvalidOperationException>(
            () => host.Backend = new HostBackend(fixture.Session.DrawingBackend, incompatible));
        Assert.Same(replacement, host.Backend!.BackdropFrameSource);
        host.Backend = null;

        Assert.False(initial.IsDisposed);
        Assert.False(replacement.IsDisposed);
        Assert.False(incompatible.IsDisposed);
    }

    [SdlNativeFact]
    public void ForeignBackdropLeaseFallsBackObservablyWithoutCpuReadback()
    {
        using BackdropFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        DrawCommandList commands = CreateBackdropCommands();
        PrismFrameAnalysis analysis = new PrismFrameAnalyzer().Analyze(commands);
        using ForeignBackdropLease lease = new(new BackdropFrameMetadata(
            Width, Height, 1, PrismColorProfile.Srgb, BackdropPixelFormat.Rgba8Unorm,
            BackdropAlphaMode.Premultiplied, Matrix3x2.Identity, ContentVersion: 7));
        session.BeginFrame(CernealaColor.Black);
        try
        {
            DrawingFrameContext frame = new(analysis, lease);
            session.DrawingBackend.Render(commands, in frame);
            PrismExecutionDiagnostics diagnostics = ((SdlGpuDrawingBackend)session.DrawingBackend).PrismDiagnostics;
            Assert.True(diagnostics.Counters.FallbackCount > 0);
            Assert.Contains("reason=UnsupportedCapability", diagnostics.DumpExecutedGraph(), StringComparison.Ordinal);
        }
        finally
        {
            session.CompleteFrame(present: false);
        }

        string sessionSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "Cerneala.Backends.SdlGpu", "Gpu", "SdlGpuWindowGraphicsSession.cs"));
        int start = sessionSource.IndexOf("public IBackdropFrameLease AcquireFrame(", StringComparison.Ordinal);
        int end = sessionSource.IndexOf("public void Resize(", start, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start);
        string acquire = sessionSource[start..end];
        string executorSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "Cerneala.Backends.SdlGpu", "Prism", "SdlGpuPrismExecutor.cs"));
        foreach (string readback in new[] { "DownloadFromGpuTexture", "MapGpuTransferBuffer", "CapturePresentedFrame" })
        {
            Assert.DoesNotContain(readback, acquire, StringComparison.Ordinal);
            Assert.DoesNotContain(readback, executorSource, StringComparison.Ordinal);
        }
    }

    [SdlNativeFact]
    public void UiHostBackdropSceneMatchesGoldensAndSharesOneLeaseAtEveryScale()
    {
        using BackdropFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        using BackdropScene scene = CreateBackdropHostingScene();
        CountingBackdropSource source = new(session);
        UiHost host = new(
            new UiHostOptions
            {
                Root = scene.Root,
                Viewport = new UiViewport(Width, Height),
                Backend = new HostBackend(session.DrawingBackend, source)
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
        Assert.Equal(0, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);
    }

    [SdlNativeFact]
    public void BackdropLifecycleStressReleasesResourcesAcrossVisibilityReplacementResetAndNavigation()
    {
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

        using BackdropFixture replacementFixture = new();
        SdlGpuWindowGraphicsSession replacementSession =
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
            ((IWindowGraphicsSession)replacementSession).ActiveBackdropLeaseCount);
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
                new PrismLayerDefinition(
                    new PrismNodeId(1),
                    "Invert lower UI",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.Invert)
                    ],
                    blendMode: PrismBlendMode.Multiply)
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
            DrawCommand.FillRectangle(
                new DrawRect(18, 10, 50, 36),
                new CernealaColor(96, 176, 232, 220)),
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
                        new PrismLayerDefinition(
                            new PrismNodeId(nodeId + 1),
                            "Blurred color backdrop",
                            filters:
                            [
                                new PrismFilterDefinition(
                                    PrismFilterId.GaussianBlur),
                                new PrismFilterDefinition(
                                    PrismFilterId.Invert)
                            ],
                            opacity: 0.76f,
                            blendMode: PrismBlendMode.Multiply)
                    ],
                    workingColorProfile:
                        PrismColorProfile.LinearSrgb)));
    }

    private static BackdropCapture CaptureHostedBackdrop(
        SdlGpuWindowGraphicsSession session,
        UiHost host,
        UiViewport viewport)
    {
        host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
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
                counters = ((SdlGpuDrawingBackend)session.DrawingBackend).PrismDiagnostics.Counters;
            });

        Assert.Equal(0, ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);
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
        Assert.True(
            File.Exists(goldenPath),
            $"Missing frozen backdrop golden '{goldenPath}'.");
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
        using BackdropFixture fixture = new();
        SdlGpuWindowGraphicsSession session = fixture.Session;
        WeakReference sessionReference = new(session);
        using BackdropScene scene = CreateBackdropHostingScene();
        CountingBackdropSource? initialSource = new(session);
        UiHost host = new(
            new UiHostOptions
            {
                Root = scene.Root,
                Viewport = new UiViewport(Width, Height),
                Backend = new HostBackend(session.DrawingBackend, initialSource)
            });
        ProviderSlot providers =
            new(host, initialSource);
        initialSource = null;
        int visibleFrames = 0;
        int hiddenFrames = 0;
        int pixelWidth = Width;
        int pixelHeight = Height;
        int freshPassBudget = 0;
        int freshCaptureBudget = 0;
        int transientSurfaceBudget = 0;
        long surfaceCreationBudget = 0;
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
                ((IWindowGraphicsSession)session).ActiveBackdropLeaseCount);

            if (visible)
            {
                visibleFrames++;
                if (freshPassBudget == 0)
                {
                    AssertResourceBudget(capture.Counters);
                    freshPassBudget =
                        capture.Counters.PassCount;
                    freshCaptureBudget =
                        capture.Counters.CaptureCount;
                    transientSurfaceBudget =
                        capture.Counters.PeakLiveSurfaceCount;
                    surfaceCreationBudget =
                        capture.Counters.CreatedSurfaceCount;
                }
                Assert.InRange(
                    capture.Counters.PassCount,
                    1,
                    freshPassBudget);
                Assert.InRange(
                    capture.Counters.CaptureCount,
                    0,
                    freshCaptureBudget);
                Assert.InRange(
                    capture.Counters.PeakLiveSurfaceCount,
                    0,
                    transientSurfaceBudget);
                Assert.InRange(
                    capture.Counters.CreatedSurfaceCount,
                    0,
                    surfaceCreationBudget);
                Assert.Equal(
                    0,
                    capture.Counters.FallbackCount);
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
        Assert.True(freshPassBudget > 0);
        Assert.True(freshCaptureBudget > 0);
        Assert.True(transientSurfaceBudget > 0);
        Assert.True(surfaceCreationBudget > 0);

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

    private sealed record ForeignBackdropLease(BackdropFrameMetadata Metadata) : IBackdropFrameLease
    {
        public void Dispose() { }
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
            drawingBackend is SdlGpuDrawingBackend;

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
            ISdlGpuBackdropFrameLease lease =
                Assert.IsAssignableFrom<
                    ISdlGpuBackdropFrameLease>(
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
            ISdlGpuBackdropFrameLease
        {
            private CountingBackdropSource? owner;
            private ISdlGpuBackdropFrameLease? inner;

            public CountingBackdropLease(
                CountingBackdropSource owner,
                ISdlGpuBackdropFrameLease inner)
            {
                this.owner = owner;
                this.inner = inner;
            }

            public nint Texture =>
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
                ISdlGpuBackdropFrameLease? currentInner =
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
        private readonly UiHost host;
        private bool completed;

        public ProviderSlot(
            UiHost host,
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
            host.Backend = new HostBackend(host.Backend!.DrawingBackend!, replacement);
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

    private sealed record HostBackend(
        IDrawingBackend DrawingBackend,
        IBackdropFrameSource BackdropFrameSource) : IUiBackend
    {
        public IInputSource? InputSource => null;
    }

    private sealed class BackdropFixture : IDisposable
    {
        private readonly NativeSdlApi api = new();
        private readonly SdlPlatformLifetime lifetime;
        private readonly SdlGpuWindowGraphicsSessionFactory factory;
        private readonly nint window;

        public BackdropFixture()
        {
            lifetime = new SdlPlatformLifetime(api);
            factory = new SdlGpuWindowGraphicsSessionFactory(api);
            window = api.CreateWindow("Cerneala backdrop contracts", Width, Height, SdlWindowOptions.Hidden);
            Assert.NotEqual(0, window);
            Session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
                new SdlWindowSurface(window, api.GetWindowId(window)), Width, Height, 1));
        }

        public SdlGpuWindowGraphicsSession Session { get; }

        public void HideAndShow()
        {
            api.HideWindow(window);
            api.ShowWindow(window);
        }

        public void Dispose()
        {
            Session.Dispose();
            factory.Dispose();
            api.DestroyWindow(window);
            lifetime.Dispose();
        }
    }
}
