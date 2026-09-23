using System.Numerics;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Rendering;
using SkiaSharp;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeRenderSurface3DTests
{
    private static readonly string EvidenceDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
            "../../../../../artifacts/rendersurface3d/control-stage2/native"));
    private static readonly string Stage3EvidenceDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
            "../../../../../artifacts/rendersurface3d/control-stage3/native"));
    private static readonly string Stage5EvidenceDirectory = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory,
            "../../../../../artifacts/rendersurface3d/control-stage5/native"));

    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    [Trait("Category", "Native")]
    public void Dpi_matrix_preserves_asymmetric_projection_and_disc_coverage(float dpi)
    {
        using NativeFixture fixture = new($"stage5-dpi-{dpi}", 320, 180,
            dpi: dpi, evidenceDirectory: Stage5EvidenceDirectory);
        fixture.Surface.ViewMatrix = RenderSurface3DStageZeroOracle.View;
        Vector3 world = RenderSurface3DStageZeroOracle.ProjectionCases[2].World;
        Matrix4x4 model = Matrix4x4.CreateTranslation(world);
        (int Width, int Height, float Scale) snapshot = default;
        fixture.Surface.Draw += (_, frame) =>
        {
            snapshot = (frame.PixelWidth, frame.PixelHeight, frame.RasterScale);
            frame.DrawMarker(Vector3.Zero, Color.CornflowerBlue, model, 10);
        };

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(dpi, snapshot.Scale);
        Assert.Equal(bitmap.Width, snapshot.Width);
        Assert.Equal(bitmap.Height, snapshot.Height);
        Assert.Equal((int)MathF.Ceiling(320 * dpi), bitmap.Width);
        Assert.Equal((int)MathF.Ceiling(180 * dpi), bitmap.Height);
        Vector2 expected = RenderSurface3DStageZeroOracle.ProjectionCases[2].ExpectedPixel * dpi;
        RenderSurface3DStageZeroOracle.MaskCoverageResult coverage =
            EvaluateDiscCoverage(bitmap, expected, 10 * dpi,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.FractionalPhaseUnoccluded);
        File.WriteAllText(Path.Combine(Stage5EvidenceDirectory, $"disc-dpi-{dpi}-mask.json"),
            JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(coverage.Passes, coverage.ToString());
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Resize_recreates_physical_raster_and_preserves_center_projection()
    {
        using NativeFixture fixture = new("stage5-resize", evidenceDirectory: Stage5EvidenceDirectory);
        List<(int Width, int Height)> recorded = [];
        fixture.Surface.Draw += (_, frame) =>
        {
            recorded.Add((frame.PixelWidth, frame.PixelHeight));
            frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 20);
        };
        using (SKBitmap before = fixture.Capture("before"))
        {
            Assert.Equal((160, 120), (before.Width, before.Height));
            Assert.True(EvaluateDiscCoverage(before, new(80, 60), 20,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.PixelAlignedUnoccluded).Passes);
        }

        fixture.Resize(240, 160);
        using SKBitmap after = fixture.Capture("after");
        UIRoot resizedRoot = Assert.IsType<UIRoot>(fixture.Surface.Root);
        DrawCommand[] resizedCommands = resizedRoot.RetainedRenderCache.RootCommands
            .Where(command => command.Kind == DrawCommandKind.RenderSurface3D).ToArray();
        File.WriteAllText(Path.Combine(Stage5EvidenceDirectory, "resize-observation.json"),
            JsonSerializer.Serialize(new
            {
                Screenshot = new { after.Width, after.Height },
                Root = new
                {
                    resizedRoot.ViewportWidth,
                    resizedRoot.ViewportHeight,
                    resizedRoot.Scale,
                    resizedRoot.ViewportVersion,
                    resizedRoot.ArrangedBounds
                },
                Window = new
                {
                    fixture.Window.Width,
                    fixture.Window.Height,
                    fixture.Window.ArrangedBounds,
                    fixture.Window.DesiredSize
                },
                Arranged = fixture.Surface.ArrangedBounds,
                Generation = ((IRenderSurface3DSource)fixture.Surface).FrameVersion,
                Recorded = recorded.Select(pair => new { pair.Width, pair.Height }),
                Commands = resizedCommands.Select(command => new { command.Rect, command.RetainedVersion })
            }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal((240, 160), (after.Width, after.Height));
        Assert.Equal((240, 160), recorded[^1]);
        Assert.True(EvaluateDiscCoverage(after, new(120, 80), 20,
            RenderSurface3DStageZeroOracle.MaskCoverageExpectations.PixelAlignedUnoccluded).Passes);

        fixture.Resize(120, 80);
        using SKBitmap shrunk = fixture.Capture("shrunk");
        Assert.Equal((120, 80), (shrunk.Width, shrunk.Height));
        Assert.Equal((120, 80), recorded[^1]);
        Assert.True(EvaluateDiscCoverage(shrunk, new(60, 40), 20,
            RenderSurface3DStageZeroOracle.MaskCoverageExpectations.PixelAlignedUnoccluded).Passes);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Ui_transform_and_opacity_apply_to_composed_3d_before_later_2d_overlay()
    {
        SKColor expected;
        using (NativeFixture reference = new("stage5-transform-reference",
            contentFactory: surface => new SurfaceScopeHost(surface, true, drawReference: true, useTransform: true),
            evidenceDirectory: Stage5EvidenceDirectory))
        using (SKBitmap bitmap = reference.Capture())
        {
            expected = bitmap.GetPixel(102, 67);
        }
        using NativeFixture fixture = new("stage5-transform-3d",
            contentFactory: surface => new SurfaceScopeHost(surface, true, useTransform: true),
            evidenceDirectory: Stage5EvidenceDirectory);
        fixture.Surface.Draw += (_, frame) => frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 40);
        using SKBitmap actual = fixture.Capture();
        AssertColorNear(expected, actual.GetPixel(102, 67));
        AssertColorNear(SKColors.LimeGreen, actual.GetPixel(90, 67));
        AssertColorNear(SKColors.HotPink, actual.GetPixel(10, 10));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Callback_failure_under_prism_recovers_same_generation_and_pixels()
    {
        Color[] expectedPixels;
        using (NativeFixture reference = new("stage3-prism-reference",
            contentFactory: surface => new SurfaceScopeHost(surface, false),
            evidenceDirectory: Stage3EvidenceDirectory))
        using (IDisposable attachment = GeneratedMarkup.AttachPrism(reference.Content, () =>
            new PrismInstance(new PrismCompositionDefinition("Stage3Reference",
                [new PrismLayerDefinition(new PrismNodeId(1), "Invert",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])]))))
        {
            reference.Surface.Draw += (_, frame) =>
                frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 40);
            using SKBitmap bitmap = reference.Capture();
            expectedPixels = Pixels(bitmap);
        }

        using NativeFixture fixture = new("stage3-prism-recovery",
            contentFactory: surface => new SurfaceScopeHost(surface, false),
            evidenceDirectory: Stage3EvidenceDirectory);
        using IDisposable prism = GeneratedMarkup.AttachPrism(fixture.Content, () =>
            new PrismInstance(new PrismCompositionDefinition("Stage3Recovery",
                [new PrismLayerDefinition(new PrismNodeId(1), "Invert",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])])));
        int callbacks = 0;
        fixture.Surface.Draw += (_, frame) =>
        {
            callbacks++;
            if (callbacks == 1) throw new InvalidOperationException("Stage 3 callback sentinel");
            frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 40);
        };
        long generation = ((IRenderSurface3DSource)fixture.Surface).FrameVersion;
        Exception failure = Assert.ThrowsAny<Exception>(() => fixture.Capture());
        Assert.Contains("Stage 3 callback sentinel", failure.ToString(), StringComparison.Ordinal);
        Assert.Equal(generation, ((IRenderSurface3DSource)fixture.Surface).FrameVersion);
        using SKBitmap recovered = fixture.Capture();
        Assert.Equal(2, callbacks);
        Assert.Equal(expectedPixels, Pixels(recovered));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Retained_UI_preserves_valid_prism_pixels_then_reacquires_after_visible_surface_change()
    {
        using NativeFixture fixture = new("stage3-prism-root-epoch",
            background: Color.White, evidenceDirectory: Stage3EvidenceDirectory);
        using IDisposable prism = GeneratedMarkup.AttachPrism(fixture.Surface, () =>
            new PrismInstance(new PrismCompositionDefinition("Stage3RootEpoch",
                [new PrismLayerDefinition(new PrismNodeId(1), "Invert",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])])));
        int callbacks = 0;
        Color markerColor = Color.CornflowerBlue;
        fixture.Surface.Draw += (_, frame) =>
        {
            callbacks++;
            frame.DrawMarker(new(0, 0, -2), markerColor, 40);
        };
        IRenderSurface3DSource source = fixture.Surface;
        long requestedGeneration = source.FrameVersion;
        Color[] expectedPixels;
        using (SKBitmap before = fixture.Capture("before-cleanup"))
        {
            expectedPixels = Pixels(before);
            Assert.NotEqual(Color.White, expectedPixels[60 * before.Width + 80]);
        }
        Assert.Equal(1, callbacks);

        UIRoot root = Assert.IsType<UIRoot>(fixture.Surface.Root);
        DrawCommand beforeCommand = Assert.Single(root.RetainedRenderCache.RootCommands,
            command => command.Kind == DrawCommandKind.RenderSurface3D);
        DrawCommand beforePrism = Assert.Single(root.RetainedRenderCache.RootCommands,
            command => command.Kind == DrawCommandKind.BeginPrism);
        Assert.NotNull(root.ImageResourceCache);
        // The real root resource replacement performs retirement and schedules
        // retained-UI recapture; the test never writes a Prism scope version.
        root.SetImageResourceCache(root.ImageLoader, null);
        Assert.Equal(requestedGeneration, source.FrameVersion);
        using SKBitmap after = fixture.Capture("after-cleanup");
        DrawCommand afterCommand = Assert.Single(root.RetainedRenderCache.RootCommands,
            command => command.Kind == DrawCommandKind.RenderSurface3D);
        DrawCommand afterPrism = Assert.Single(root.RetainedRenderCache.RootCommands,
            command => command.Kind == DrawCommandKind.BeginPrism);
        Assert.Equal(1, callbacks);
        Assert.NotEqual(beforeCommand.RenderSurface3DResourceEpoch, afterCommand.RenderSurface3DResourceEpoch);
        Assert.Equal(beforeCommand.RetainedVersion, afterCommand.RetainedVersion);
        Assert.Equal(beforePrism.PrismScope?.VisualContentVersion,
            afterPrism.PrismScope?.VisualContentVersion);
        Assert.Equal(expectedPixels, Pixels(after));

        markerColor = Color.Red;
        fixture.Surface.InvalidateFrame();
        using SKBitmap changed = fixture.Capture("after-visible-change");
        DrawCommand changedCommand = Assert.Single(root.RetainedRenderCache.RootCommands,
            command => command.Kind == DrawCommandKind.RenderSurface3D);
        DrawCommand changedPrism = Assert.Single(root.RetainedRenderCache.RootCommands,
            command => command.Kind == DrawCommandKind.BeginPrism);
        File.WriteAllText(Path.Combine(Stage3EvidenceDirectory, "prism-root-epoch-probe.json"),
            JsonSerializer.Serialize(new
            {
                Callbacks = callbacks,
                BeforeEpoch = beforeCommand.RenderSurface3DResourceEpoch,
                AfterEpoch = afterCommand.RenderSurface3DResourceEpoch,
                ChangedEpoch = changedCommand.RenderSurface3DResourceEpoch,
                BeforeRetainedVersion = beforeCommand.RetainedVersion,
                AfterRetainedVersion = afterCommand.RetainedVersion,
                ChangedRetainedVersion = changedCommand.RetainedVersion,
                BeforePrismVersion = beforePrism.PrismScope?.VisualContentVersion,
                AfterPrismVersion = afterPrism.PrismScope?.VisualContentVersion,
                ChangedPrismVersion = changedPrism.PrismScope?.VisualContentVersion,
                DifferingPixels = expectedPixels.Zip(Pixels(changed)).Count(pair => pair.First != pair.Second),
                BeforeCenter = expectedPixels[60 * changed.Width + 80],
                ChangedCenter = changed.GetPixel(80, 60)
            }, new JsonSerializerOptions { WriteIndented = true }));
        Assert.Equal(2, callbacks);
        Assert.NotEqual(afterCommand.RetainedVersion, changedCommand.RetainedVersion);
        Assert.NotEqual(afterPrism.PrismScope?.VisualContentVersion,
            changedPrism.PrismScope?.VisualContentVersion);
        Assert.True(expectedPixels.Zip(Pixels(changed)).Count(pair => pair.First != pair.Second) > 1000);
        AssertColorNear(SKColors.Cyan, changed.GetPixel(80, 60));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Shared_owner_two_windows_isolate_cameras_colors_and_targets_through_move_and_close()
    {
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        TrackingGraphicsFactory tracked = new(graphics);
        using SdlWindowPlatform platform = new(api, tracked, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        RenderSurface3D red = NewNativeSurface(Color.Red, Matrix4x4.Identity);
        RenderSurface3D blue = NewNativeSurface(Color.Blue, Matrix4x4.CreateTranslation(.5f, 0, 0));
        Window first = new()
        {
            Title = "Stage3 shared owner A",
            Width = 160,
            Height = 120,
            Content = red,
            Background = new SolidColorBrush(Color.White)
        };
        Window second = new()
        {
            Title = "Stage3 shared owner B",
            Width = 160,
            Height = 120,
            Content = blue,
            Background = new SolidColorBrush(Color.White)
        };
        Directory.CreateDirectory(Stage3EvidenceDirectory);
        try
        {
            runtime.Show(first, modal: false);
            runtime.Show(second, modal: false);
            Assert.Equal(2, tracked.Sessions.Count);
            using (SKBitmap firstImage = Capture(first, "shared-owner-first"))
            using (SKBitmap secondImage = Capture(second, "shared-owner-second"))
            {
                AssertColorNear(SKColors.Red, firstImage.GetPixel(80, 60));
                AssertColorNear(SKColors.White, firstImage.GetPixel(106, 60));
                AssertColorNear(SKColors.White, secondImage.GetPixel(80, 60));
                AssertColorNear(SKColors.Blue, secondImage.GetPixel(106, 60));
            }
            Assert.Equal(1, tracked.Sessions[0].DrawingBackendAs3D()
                .LastFrameRenderSurface3DCounters.LiveTargetCount);
            Assert.Equal(1, tracked.Sessions[1].DrawingBackendAs3D()
                .LastFrameRenderSurface3DCounters.LiveTargetCount);

            using (SKBitmap secondRepeat = Capture(second, "shared-owner-second-repeat"))
                AssertColorNear(SKColors.Blue, secondRepeat.GetPixel(106, 60));
            using (SKBitmap firstRepeat = Capture(first, "shared-owner-first-repeat"))
                AssertColorNear(SKColors.Red, firstRepeat.GetPixel(80, 60));

            first.Content = null;
            second.Content = red;
            using (SKBitmap moved = Capture(second, "shared-owner-moved"))
            {
                AssertColorNear(SKColors.Red, moved.GetPixel(80, 60));
                AssertColorNear(SKColors.White, moved.GetPixel(106, 60));
            }
            Assert.Equal(0, tracked.Sessions[0].DrawingBackendAs3D()
                .LastFrameRenderSurface3DCounters.LiveTargetCount);
            Assert.Equal(1, tracked.Sessions[1].DrawingBackendAs3D()
                .LastFrameRenderSurface3DCounters.LiveTargetCount);
            runtime.Close(first, force: true);
            using (SKBitmap afterClose = Capture(second, "shared-owner-after-close"))
            {
                AssertColorNear(SKColors.Red, afterClose.GetPixel(80, 60));
                AssertColorNear(SKColors.White, afterClose.GetPixel(106, 60));
            }
            Assert.Equal(1, tracked.Sessions[1].DrawingBackendAs3D()
                .LastFrameRenderSurface3DCounters.LiveTargetCount);
        }
        finally
        {
            runtime.Close(first, force: true);
            runtime.Close(second, force: true);
        }

        RenderSurface3D NewNativeSurface(Color color, Matrix4x4 view)
        {
            RenderSurface3D surface = new()
            {
                ViewMatrix = view,
                Projection = RenderProjection3D.Perspective(MathF.PI / 3, .01f, 100),
                ClearColor = Color.Transparent
            };
            surface.Draw += (_, frame) => frame.DrawMarker(new(0, 0, -2), color, 20);
            return surface;
        }

        SKBitmap Capture(Window window, string name)
        {
            string path = Path.Combine(Stage3EvidenceDirectory, name + ".png");
            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(path);
            return SKBitmap.Decode(path) ?? throw new InvalidOperationException("Screenshot decode failed.");
        }
    }

    private sealed class TrackingGraphicsFactory(SdlGpuWindowGraphicsSessionFactory inner)
        : IWindowGraphicsSessionFactory
    {
        internal List<SdlGpuWindowGraphicsSession> Sessions { get; } = [];

        public IWindowGraphicsSession Create(IWindowSurface windowSurface,
            int pixelWidth, int pixelHeight, float coordinateScale)
        {
            SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(
                inner.Create(windowSurface, pixelWidth, pixelHeight, coordinateScale));
            Sessions.Add(session);
            return session;
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Fully_clipped_and_singular_UI_composition_leave_parent_pixels_and_following_2d_intact()
    {
        foreach (bool singular in new[] { false, true })
        {
            using NativeFixture fixture = new($"stage3-hidden-{singular}",
                contentFactory: surface => new Stage3HiddenHost(surface, singular),
                evidenceDirectory: Stage3EvidenceDirectory);
            fixture.Surface.Draw += (_, frame) =>
                frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 40);
            using SKBitmap bitmap = fixture.Capture();
            Assert.Equal(new SKColor(255, 105, 180), bitmap.GetPixel(80, 60));
            Assert.Equal(new SKColor(50, 205, 50), bitmap.GetPixel(10, 10));
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Marker_is_GPU_projected_and_composed_over_the_window()
    {
        using NativeFixture fixture = new("marker");
        fixture.Surface.Draw += (_, frame) =>
            frame.DrawMarker(new Vector3(0, 0, -2), Color.CornflowerBlue, 24);

        using SKBitmap bitmap = fixture.Capture();

        Assert.InRange(bitmap.Width, 160, 160);
        Assert.InRange(bitmap.Height, 120, 120);
        SKColor center = bitmap.GetPixel(80, 60);
        Assert.InRange(center.Red, 99, 101);
        Assert.InRange(center.Green, 148, 150);
        Assert.InRange(center.Blue, 236, 238);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Asymmetric_model_view_and_projection_put_a_marker_at_the_frozen_oracle_pixel()
    {
        using NativeFixture fixture = new("asymmetric-matrix", 320, 180);
        fixture.Surface.ViewMatrix = RenderSurface3DStageZeroOracle.View;
        Vector3 world = RenderSurface3DStageZeroOracle.ProjectionCases[2].World;
        Matrix4x4 model = Matrix4x4.CreateTranslation(world);
        fixture.Surface.Draw += (_, frame) =>
            frame.DrawMarker(Vector3.Zero, Color.CornflowerBlue, model, 10);

        using SKBitmap bitmap = fixture.Capture();

        Vector2 expected = RenderSurface3DStageZeroOracle.ProjectionCases[2].ExpectedPixel;
        Assert.True(IsBlue(bitmap.GetPixel((int)MathF.Round(expected.X), (int)MathF.Round(expected.Y))));
        Assert.Equal(SKColors.White, bitmap.GetPixel(160, 90));
        List<Vector2> interior = [];
        int minX = bitmap.Width, minY = bitmap.Height, maxX = -1, maxY = -1;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y) == SKColors.White) continue;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
                if (IsBlue(bitmap.GetPixel(x, y))) interior.Add(new Vector2(x + .5f, y + .5f));
            }
        }
        Assert.NotEmpty(interior);
        Vector2 centroid = new(interior.Average(pixel => pixel.X), interior.Average(pixel => pixel.Y));
        Assert.InRange(Vector2.Distance(centroid, expected), 0, 1);
        Assert.InRange(MathF.Abs((minX + .5f) - (expected.X - 5)), 0, 1);
        Assert.InRange(MathF.Abs((maxX + .5f) - (expected.X + 5)), 0, 1);
        Assert.InRange(MathF.Abs((minY + .5f) - (expected.Y - 5)), 0, 1);
        Assert.InRange(MathF.Abs((maxY + .5f) - (expected.Y + 5)), 0, 1);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Nearer_line_wins_at_crossing_regardless_of_recording_order()
    {
        RenderSurface3DStageZeroOracle.CrossingLinesScenario crossing =
            RenderSurface3DStageZeroOracle.CrossingLines;
        foreach (bool nearFirst in new[] { true, false })
        {
            using NativeFixture fixture = new($"depth-{nearFirst}", 320, 180);
            fixture.Surface.ViewMatrix = RenderSurface3DStageZeroOracle.View;
            fixture.Surface.Draw += (_, frame) =>
            {
                if (nearFirst) DrawNear(frame);
                DrawFar(frame);
                if (!nearFirst) DrawNear(frame);
            };
            using SKBitmap bitmap = fixture.Capture();
            SKColor center = bitmap.GetPixel(160, 90);
            Assert.InRange(center.Red, 254, 255);
            Assert.InRange(center.Green, 0, 1);
            Assert.InRange(center.Blue, 0, 1);
        }

        void DrawNear(RenderSurface3DFrame frame) =>
            frame.DrawLine(crossing.HorizontalStart, crossing.HorizontalEnd, Color.Red, 8);
        void DrawFar(RenderSurface3DFrame frame) =>
            frame.DrawLine(crossing.VerticalStart, crossing.VerticalEnd, Color.Blue, 8);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Equal_depth_last_covered_primitive_wins()
    {
        using NativeFixture fixture = new("equal-depth-last");
        fixture.Surface.Draw += (_, frame) =>
        {
            frame.DrawMarker(new(0, 0, -2), Color.Blue, 20);
            frame.DrawMarker(new(0, 0, -2), Color.Red, 10);
        };

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Red, bitmap.GetPixel(80, 60));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(87, 60));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Fractional_near_disc_edge_composites_over_opaque_far_geometry_independent_of_order()
    {
        SKBitmap nearFirst;
        using (NativeFixture fixture = new("fractional-depth-near-first"))
        {
            fixture.Surface.Draw += (_, frame) =>
            {
                frame.DrawMarker(new(.1f, .07f, -2), Color.Red, 18);
                frame.DrawMarker(new(.15f, .105f, -3), Color.Blue, 50);
            };
            nearFirst = fixture.Capture();
        }

        using (nearFirst)
        using (NativeFixture fixture = new("fractional-depth-far-first"))
        {
            fixture.Surface.Draw += (_, frame) =>
            {
                frame.DrawMarker(new(.15f, .105f, -3), Color.Blue, 50);
                frame.DrawMarker(new(.1f, .07f, -2), Color.Red, 18);
            };
            using SKBitmap farFirst = fixture.Capture();
            int differingSamples = 0;
            int mixedBoundarySamples = 0;
            int maxColorResidual = 0;
            int maxGreen = 0;
            for (int y = 40; y < 75; y++)
            {
                for (int x = 65; x < 105; x++)
                {
                    SKColor a = nearFirst.GetPixel(x, y);
                    SKColor b = farFirst.GetPixel(x, y);
                    if (a != b) differingSamples++;
                    if (b.Red > 1 && b.Red < 254 && b.Blue > 1 && b.Blue < 254)
                    {
                        mixedBoundarySamples++;
                        maxColorResidual = Math.Max(maxColorResidual, Math.Abs(b.Red + b.Blue - 255));
                        maxGreen = Math.Max(maxGreen, b.Green);
                        Assert.InRange(Math.Abs(b.Red + b.Blue - 255), 0, 3);
                        Assert.InRange(b.Green, 0, 1);
                        Assert.Equal((byte)255, b.Alpha);
                    }
                }
            }
            File.WriteAllText(Path.Combine(EvidenceDirectory, "fractional-depth-order.json"),
                JsonSerializer.Serialize(new
                {
                    VisibleDomain = new { Left = 65, Top = 40, RightExclusive = 105, BottomExclusive = 75 },
                    DifferingSamples = differingSamples,
                    MixedBoundarySamples = mixedBoundarySamples,
                    MaxColorResidual = maxColorResidual,
                    MaxGreen = maxGreen
                }, new JsonSerializerOptions { WriteIndented = true }));
            Assert.True(mixedBoundarySamples > 0, "The fractional near edge had no mixed-color boundary samples.");
            Assert.Equal(0, differingSamples);
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Discarded_marker_quad_corners_do_not_hide_a_farther_line()
    {
        using NativeFixture fixture = new("disc-depth-corner");
        fixture.Surface.Draw += (_, frame) =>
        {
            // The line crosses the marker's square bounding quad outside the disc.
            frame.DrawLine(new(-0.5f, 0.231f, -3), new(0.5f, 0.231f, -3), Color.Blue, 3);
            frame.DrawMarker(new(0, 0, -2), Color.Red, 20);
        };

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Red, bitmap.GetPixel(80, 60));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(88, 52));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Fractional_disc_matches_the_frozen_interor_boundary_and_exterior_mask()
    {
        using NativeFixture fixture = new("fractional-disc");
        Vector3 center = new(0.1f, 0.07f, -2);
        fixture.Surface.Draw += (_, frame) => frame.DrawMarker(center, Color.CornflowerBlue, 18);

        using SKBitmap bitmap = fixture.Capture();

        Vector2 projected = ProjectIdentityPerspective(center, bitmap.Width, bitmap.Height);
        RenderSurface3DStageZeroOracle.PrimitivePixelMask mask =
            RenderSurface3DStageZeroOracle.CreateDiscMask(bitmap.Width, bitmap.Height, projected, 18);
        RenderSurface3DStageZeroOracle.MaskCoverageResult result =
            RenderSurface3DStageZeroOracle.EvaluateCoverage(mask, Pixels(bitmap),
                Color.CornflowerBlue, Color.White,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.FractionalPhaseUnoccluded);
        File.WriteAllText(Path.Combine(EvidenceDirectory, "fractional-disc-mask.json"),
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(result.Passes, result.ToString());
    }

    [SdlNativeTheory]
    [InlineData(1f)]
    [InlineData(1.25f)]
    [InlineData(2f)]
    [Trait("Category", "Native")]
    public void Fractional_butt_line_matches_the_frozen_interor_boundary_and_exterior_mask(float dpi)
    {
        using NativeFixture fixture = new($"stage5-fractional-butt-line-dpi-{dpi}",
            dpi: dpi, evidenceDirectory: Stage5EvidenceDirectory);
        Vector3 start = new(-0.6f, 0.1f, -2);
        Vector3 end = new(0.6f, 0.1f, -2);
        fixture.Surface.Draw += (_, frame) => frame.DrawLine(start, end, Color.CornflowerBlue, 6);

        using SKBitmap bitmap = fixture.Capture();

        RenderSurface3DStageZeroOracle.PrimitivePixelMask mask =
            RenderSurface3DStageZeroOracle.CreateButtLineMask(bitmap.Width, bitmap.Height,
                ProjectIdentityPerspective(start, bitmap.Width, bitmap.Height),
                ProjectIdentityPerspective(end, bitmap.Width, bitmap.Height), 6 * dpi);
        RenderSurface3DStageZeroOracle.MaskCoverageResult result =
            RenderSurface3DStageZeroOracle.EvaluateCoverage(mask, Pixels(bitmap),
                Color.CornflowerBlue, Color.White,
                RenderSurface3DStageZeroOracle.MaskCoverageExpectations.FractionalPhaseUnoccluded);
        File.WriteAllText(Path.Combine(Stage5EvidenceDirectory, $"line-dpi-{dpi}-mask.json"),
            JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(result.Passes, result.ToString());
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Near_crossing_is_clipped_before_division_and_far_and_side_outliers_do_not_draw()
    {
        using NativeFixture fixture = new("near-far-side-clip", 320, 180);
        fixture.Surface.Draw += (_, frame) =>
        {
            frame.DrawLine(new(-.4f, 0, -.005f), new(.4f, 0, -1), Color.Red, 6);
            frame.DrawLine(new(-.5f, .2f, -200), new(.5f, .2f, -200), Color.Blue, 8);
            frame.DrawLine(new(100, -.5f, -1), new(100, .5f, -1), Color.Green, 8);
        };

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Red, bitmap.GetPixel(160, 90));
        Assert.Equal(SKColors.White, bitmap.GetPixel(240, 90));
        Assert.DoesNotContain(Pixels(bitmap), pixel => pixel == Color.Blue || pixel == Color.Green);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Near_view_parallel_line_remains_bounded_after_side_clipping()
    {
        using NativeFixture fixture = new("near-view-parallel", 320, 180);
        fixture.Surface.Draw += (_, frame) =>
            frame.DrawLine(new(.05f, 0, -.02f), new(.05f, 0, -20), Color.Blue, 6);

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Blue, bitmap.GetPixel(161, 90));
        Assert.Equal(SKColors.White, bitmap.GetPixel(160, 60));
        Assert.DoesNotContain(Pixels(bitmap), pixel => pixel == Color.Red);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Far_and_side_crossing_lines_cover_only_their_visible_domains()
    {
        using NativeFixture fixture = new("far-side-crossings", 320, 180);
        fixture.Surface.Projection = RenderProjection3D.Orthographic(2, .01f, 10);
        fixture.Surface.Draw += (_, frame) =>
        {
            frame.DrawLine(new(-.6f, 0, -5), new(.6f, 0, -15), Color.Red, 6);
            frame.DrawLine(new(1, .4f, -2), new(3, .4f, -2), Color.Blue, 6);
        };

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Red, bitmap.GetPixel(120, 90));
        Assert.Equal(SKColors.White, bitmap.GetPixel(190, 90));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(280, 54));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(318, 54));
        Assert.Equal(SKColors.White, bitmap.GetPixel(220, 54));
        for (int x = 112; x <= 150; x++)
            Assert.Equal(SKColors.Red, bitmap.GetPixel(x, 90));
        for (int x = 175; x <= 220; x++)
            Assert.Equal(SKColors.White, bitmap.GetPixel(x, 90));
        for (int x = 255; x <= 315; x++)
            Assert.Equal(SKColors.Blue, bitmap.GetPixel(x, 54));
        for (int x = 185; x <= 240; x++)
            Assert.Equal(SKColors.White, bitmap.GetPixel(x, 54));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Expanded_disc_and_line_remain_visible_when_their_centers_are_just_outside_side_plane()
    {
        using NativeFixture fixture = new("expanded-side-clip", 320, 180);
        fixture.Surface.Projection = RenderProjection3D.Orthographic(2, .01f, 10);
        fixture.Surface.Draw += (_, frame) =>
        {
            frame.DrawMarker(new(1.82f, .4f, -2), Color.Red, 20);
            frame.DrawLine(new(1.82f, -.5f, -2), new(1.82f, 0, -2), Color.Blue, 20);
        };

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Red, bitmap.GetPixel(317, 54));
        Assert.Equal(SKColors.Blue, bitmap.GetPixel(317, 110));
        Assert.Equal(SKColors.White, bitmap.GetPixel(305, 54));
        Assert.Equal(SKColors.White, bitmap.GetPixel(305, 110));
        for (int y = 50; y <= 58; y++)
        {
            Assert.Equal(SKColors.Red, bitmap.GetPixel(317, y));
            Assert.Equal(SKColors.White, bitmap.GetPixel(305, y));
        }
        for (int y = 95; y <= 130; y++)
        {
            Assert.Equal(SKColors.Blue, bitmap.GetPixel(317, y));
            Assert.Equal(SKColors.White, bitmap.GetPixel(305, y));
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Orthographic_projection_keeps_marker_size_and_changes_its_position()
    {
        using NativeFixture fixture = new("orthographic", 320, 180);
        fixture.Surface.Projection = RenderProjection3D.Orthographic(2, .01f, 100);
        fixture.Surface.Draw += (_, frame) =>
            frame.DrawMarker(new(.5f, .25f, -2), Color.Blue, 12);

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(SKColors.Blue, bitmap.GetPixel(205, 68));
        Assert.Equal(SKColors.White, bitmap.GetPixel(160, 90));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Transparent_surface_exterior_preserves_parent_background_without_halo()
    {
        using NativeFixture fixture = new("transparent-composite", background: Color.HotPink);
        fixture.Surface.Draw += (_, frame) =>
            frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 18);

        using SKBitmap bitmap = fixture.Capture();

        Assert.Equal(new SKColor(255, 105, 180), bitmap.GetPixel(40, 60));
        Assert.True(IsBlue(bitmap.GetPixel(80, 60)));
        Assert.Equal(new SKColor(255, 105, 180), bitmap.GetPixel(95, 60));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Parent_2d_before_nested_clip_layers_and_2d_after_keep_their_order()
    {
        SKColor[] expected;
        using (NativeFixture reference = new("nested-2d-reference",
            contentFactory: surface => new SurfaceScopeHost(surface, true, drawReference: true)))
        using (SKBitmap bitmap = reference.Capture())
        {
            expected = [bitmap.GetPixel(20, 60), bitmap.GetPixel(60, 60),
                bitmap.GetPixel(70, 60), bitmap.GetPixel(80, 60)];
        }

        using NativeFixture fixture = new("nested-2d-order",
            contentFactory: surface => new SurfaceScopeHost(surface, true));
        fixture.Surface.Draw += (_, frame) =>
            frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 60);
        using SKBitmap actual = fixture.Capture();

        Assert.Equal(new SKColor(255, 105, 180), expected[0]);
        Assert.Equal(new SKColor(50, 205, 50), expected[3]);
        AssertColorNear(expected[0], actual.GetPixel(20, 60));
        AssertColorNear(expected[1], actual.GetPixel(60, 60));
        AssertColorNear(expected[2], actual.GetPixel(70, 60));
        AssertColorNear(expected[3], actual.GetPixel(80, 60));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void Prism_filter_processes_the_composed_3d_surface_not_a_CPU_projection()
    {
        SKColor baselineCenter;
        using (NativeFixture ordinary = new("prism-source", contentFactory: surface => new SurfaceScopeHost(surface, false)))
        {
            ordinary.Surface.Draw += (_, frame) =>
                frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 40);
            using SKBitmap baseline = ordinary.Capture();
            baselineCenter = baseline.GetPixel(80, 60);
        }

        SKColor filteredCenter;
        using (NativeFixture filtered = new("prism-filtered",
            contentFactory: surface => new SurfaceScopeHost(surface, false)))
        {
            filtered.Surface.Draw += (_, frame) =>
                frame.DrawMarker(new(0, 0, -2), Color.CornflowerBlue, 40);
            using IDisposable attachment = GeneratedMarkup.AttachPrism(filtered.Content, () =>
                new PrismInstance(new PrismCompositionDefinition("Surface3DInvert",
                    [new PrismLayerDefinition(new PrismNodeId(1), "Invert",
                        filters: [new PrismFilterDefinition(PrismFilterId.Invert)])])));
            using SKBitmap result = filtered.Capture();
            filteredCenter = result.GetPixel(80, 60);
        }
        Assert.NotEqual(baselineCenter, filteredCenter);

        SKColor referenceCenter;
        using (NativeFixture reference = new("prism-2d-reference",
            contentFactory: surface => new SurfaceScopeHost(surface, false, drawReference: true)))
        using (IDisposable referenceAttachment = GeneratedMarkup.AttachPrism(reference.Content, () =>
            new PrismInstance(new PrismCompositionDefinition("Surface3DInvertReference",
                [new PrismLayerDefinition(new PrismNodeId(1), "Invert",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)])]))))
        using (SKBitmap referenceBitmap = reference.Capture())
        {
            referenceCenter = referenceBitmap.GetPixel(80, 60);
        }
        AssertColorNear(referenceCenter, filteredCenter);
    }

    private static Vector2 ProjectIdentityPerspective(Vector3 value, int width, int height)
    {
        float focal = 1 / MathF.Tan(MathF.PI / 6);
        return new(
            (1 + (value.X * focal * height / width / -value.Z)) * width * 0.5f,
            (1 - (value.Y * focal / -value.Z)) * height * 0.5f);
    }

    private static RenderSurface3DStageZeroOracle.MaskCoverageResult EvaluateDiscCoverage(
        SKBitmap bitmap,
        Vector2 expectedCenter,
        float physicalDiameter,
        RenderSurface3DStageZeroOracle.MaskCoverageExpectations expectations)
    {
        RenderSurface3DStageZeroOracle.PrimitivePixelMask mask =
            RenderSurface3DStageZeroOracle.CreateDiscMask(
                bitmap.Width, bitmap.Height, expectedCenter, physicalDiameter);
        return RenderSurface3DStageZeroOracle.EvaluateCoverage(
            mask, Pixels(bitmap), Color.CornflowerBlue, Color.White, expectations);
    }

    private static Color[] Pixels(SKBitmap bitmap)
    {
        Color[] result = new Color[checked(bitmap.Width * bitmap.Height)];
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                result[y * bitmap.Width + x] = new Color(pixel.Red, pixel.Green, pixel.Blue, pixel.Alpha);
            }
        }
        return result;
    }

    private static bool IsBlue(SKColor pixel) =>
        pixel.Red is >= 99 and <= 101 &&
        pixel.Green is >= 148 and <= 150 &&
        pixel.Blue is >= 236 and <= 238;

    private static void AssertColorNear(SKColor expected, SKColor actual)
    {
        Assert.InRange(Math.Abs(expected.Red - actual.Red), 0, 3);
        Assert.InRange(Math.Abs(expected.Green - actual.Green), 0, 3);
        Assert.InRange(Math.Abs(expected.Blue - actual.Blue), 0, 3);
        Assert.InRange(Math.Abs(expected.Alpha - actual.Alpha), 0, 3);
    }

    private sealed class NativeFixture : IDisposable
    {
        private readonly SdlGpuWindowGraphicsSessionFactory graphics;
        private readonly SdlWindowPlatform platform;
        private readonly WindowApplicationRuntime runtime;
        private readonly Window window;
        private readonly string name;

        private readonly string evidenceDirectory;

        internal NativeFixture(string name, int width = 160, int height = 120,
            Color? background = null, Func<RenderSurface3D, UIElement>? contentFactory = null,
            string? evidenceDirectory = null, float dpi = 1)
        {
            this.name = name;
            this.evidenceDirectory = evidenceDirectory ?? EvidenceDirectory;
            NativeSdlApi api = new();
            graphics = new(api, useMultisampling: true);
            platform = new(api, graphics, coordinateScaleOverride: dpi);
            runtime = new(platform);
            Surface = new RenderSurface3D
            {
                ViewMatrix = Matrix4x4.Identity,
                Projection = RenderProjection3D.Perspective(MathF.PI / 3, 0.01f, 100),
                ClearColor = Color.Transparent
            };
            Content = contentFactory?.Invoke(Surface) ?? Surface;
            window = new Window
            {
                Title = $"RenderSurface3D {name}",
                Width = width,
                Height = height,
                Content = Content,
                Background = background is Color color ? new SolidColorBrush(color) : null
            };
            runtime.Show(window, modal: false);
        }

        internal RenderSurface3D Surface { get; }

        internal UIElement Content { get; }

        internal Window Window => window;

        internal void Resize(int width, int height)
        {
            window.Width = width;
            window.Height = height;
            runtime.ApplyProperties(window);
            runtime.PumpOnce(TimeSpan.Zero);
        }

        internal SKBitmap Capture(string? suffix = null)
        {
            Directory.CreateDirectory(evidenceDirectory);
            string path = Path.Combine(evidenceDirectory,
                suffix is null ? $"{name}.png" : $"{name}-{suffix}.png");
            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(path);
            return SKBitmap.Decode(path) ?? throw new InvalidOperationException("Screenshot decode failed.");
        }

        public void Dispose()
        {
            runtime.Close(window, force: true);
            runtime.Dispose();
            graphics.Dispose();
            platform.Dispose();
        }
    }

    private sealed class SurfaceScopeHost(RenderSurface3D surface, bool useScopes,
        bool drawReference = false, bool useTransform = false) : Control
    {
        protected override void OnRender(RenderContext context)
        {
            DrawingContext drawing = context.DrawingContext;
            DrawRect bounds = new(context.Bounds.X, context.Bounds.Y,
                context.Bounds.Width, context.Bounds.Height);
            drawing.FillRectangle(bounds, Color.HotPink);
            if (useTransform) drawing.PushTransform(System.Numerics.Matrix3x2.CreateTranslation(12, 7));
            if (useScopes)
            {
                drawing.PushClip(new DrawRect(40, 30, 80, 60));
                drawing.PushOpacity(.5f);
                drawing.PushLayer(new DrawLayerOptions(.8f));
            }
            if (drawReference)
            {
                drawing.FillRectangle(new DrawRect(50, 35, 60, 50), Color.CornflowerBlue);
            }
            else
            {
                IRenderSurface3DSource source = surface;
                drawing.DrawRenderSurface3D(source, bounds, Color.White, source.FrameVersion);
            }
            if (useScopes)
            {
                drawing.PopLayer();
                drawing.PopOpacity();
                drawing.PopClip();
                drawing.FillRectangle(new DrawRect(75, 55, 10, 10), Color.LimeGreen);
            }
            if (useTransform) drawing.PopTransform();
        }
    }

    private sealed class Stage3HiddenHost(RenderSurface3D surface, bool singular) : Control
    {
        protected override void OnRender(RenderContext context)
        {
            DrawingContext drawing = context.DrawingContext;
            DrawRect bounds = new(context.Bounds.X, context.Bounds.Y,
                context.Bounds.Width, context.Bounds.Height);
            drawing.FillRectangle(bounds, Color.HotPink);
            if (singular)
                drawing.PushTransform(System.Numerics.Matrix3x2.CreateScale(0, 1));
            else
                drawing.PushClip(new DrawRect(180, 140, 10, 10));
            IRenderSurface3DSource source = surface;
            drawing.DrawRenderSurface3D(source, bounds, Color.White, source.FrameVersion);
            if (singular) drawing.PopTransform();
            else drawing.PopClip();
            drawing.FillRectangle(new DrawRect(5, 5, 10, 10), Color.LimeGreen);
        }
    }
}
