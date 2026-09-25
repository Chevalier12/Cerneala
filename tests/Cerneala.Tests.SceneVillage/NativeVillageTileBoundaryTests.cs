using System.Runtime.ExceptionServices;
using Cerneala.Drawing;
using Cerneala.SceneVillage;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using Xunit.Abstractions;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SceneVillage;

[Collection(VillageNativeTestCollection.Name)]
public sealed class NativeVillageTileBoundaryTests
{
    private readonly ITestOutputHelper output;

    public NativeVillageTileBoundaryTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void OpaquePointSampledAtlasTileJoinsOpaqueGroundAtFractionalCameraPosition()
    {
        // Calibrate against the runner's actual layout origin and physical
        // screenshot scale. Each matrix camera is then initial window setup;
        // none is changed after the scene starts rendering.
        BoundaryProbe calibration = RunBoundaryFixture(new DrawRect(-100f, -68f, 396f, 220f), null);
        float[] requestedPhases = [0.125f, 0.275f, 0.375f, 0.500f, 0.625f, 0.875f];
        var probes = new List<BoundaryProbe>(requestedPhases.Length);
        foreach (float requestedPhase in requestedPhases)
        {
            PixelMapping mapping = calibration.Mapping;
            var view = new DrawRect(
                -(mapping.AnchorX + requestedPhase - mapping.SurfaceLeftPixels) / mapping.PixelsPerWorldX,
                32f - (mapping.AnchorY + requestedPhase - mapping.SurfaceTopPixels) / mapping.PixelsPerWorldY,
                396f,
                220f);
            probes.Add(RunBoundaryFixture(view, requestedPhase));
        }

        SKColor grass = new(132, 198, 105);
        foreach (BoundaryProbe probe in probes)
        {
            float requestedPhase = Assert.IsType<float>(probe.RequestedPhase);
            Assert.InRange(probe.HorizontalPhase, requestedPhase - 0.002f, requestedPhase + 0.002f);
            Assert.InRange(probe.VerticalPhase, requestedPhase - 0.002f, requestedPhase + 0.002f);
            Assert.Equal(grass, probe.UniformBottom);
            Assert.Equal(grass, probe.UniformLeft);
            Assert.True(probe.LinearInteriorDifferences >= 10,
                $"Linear filtering must remain visibly distinct in the path interior at phase {requestedPhase:F3}.");
        }

        var violations = new List<string>();
        foreach (BoundaryProbe probe in probes)
        {
            if (probe.PointBottom != grass || probe.PointLeft != grass)
            {
                violations.Add($"phase {probe.HorizontalPhase:F3}/{probe.VerticalPhase:F3}: " +
                    $"Point bottom {probe.PointBottom}, left {probe.PointLeft}");
            }

            // The mirrored sprite's right edge maps to tile 43's opaque left
            // border at every phase, including the exact half-pixel boundary.
            if (probe.FlippedRight != grass)
            {
                violations.Add($"phase {probe.VerticalPhase:F3}: flipped right {probe.FlippedRight}");
            }
        }
        foreach (BoundaryProbe probe in probes.Where(probe =>
                     probe.RequestedPhase is 0.275f or 0.625f))
        {
            if (probe.RotatedLeft != grass)
            {
                violations.Add($"phase {probe.VerticalPhase:F3}: rotated left {probe.RotatedLeft}");
            }

            if (!IsClose(probe.AlphaReference, probe.AlphaBottom, 2))
            {
                violations.Add($"phase {probe.HorizontalPhase:F3}: alpha join {probe.AlphaBottom}, " +
                    $"safe reference {probe.AlphaReference}");
            }
        }

        Assert.True(violations.Count == 0,
            "Opaque and half-opacity Point-sampled tile joins must preserve their matching source borders. " +
            string.Join("; ", violations));
    }

    private BoundaryProbe RunBoundaryFixture(DrawRect initialViewBox, float? requestedPhase)
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        Exception? failure = null;
        bool started = false;
        BoundaryProbe? probe = null;
        string capturePath = CreateCapturePath(requestedPhase);

        int exitCode = GeneratedWindowApplication.Run(
            new GeneratedWindowStartupDescriptor(
                createApplication: () => new App(),
                configureServices: _ => { },
                createStartupWindow: _ =>
                {
                    // Initial scene/camera setup is a renderer conformance
                    // fixture, not a substitute for routed player input.
                    var window = new Window { Width = 820f, Height = 600f, Title = "Village tile boundary fixture" };
                    var surface = new RenderSurface2D
                    {
                        Width = 792f,
                        Height = 440f,
                        ClearColor = new Color(255, 0, 255),
                        Stretch = DrawBrushStretch.Fill,
                        ViewBox = initialViewBox
                    };
                    var imageId = new ResourceId<ImageResource>("TownBoundaryFixture");
                    surface.Resources.SetResource(imageId,
                        new ImageResource(Path.Combine(AppContext.BaseDirectory, "Assets", "tiny-town.png")));
                    var image = new ImageReference(imageId);
                    var scene = new Scene2D();

                    Sprite2D Tile(int index, float x, float y, DrawSamplingMode sampling) => new()
                    {
                        Image = image,
                        X = x,
                        Y = y,
                        Width = 32f,
                        Height = 32f,
                        SourceX = index % 12 * 16f,
                        SourceY = index / 12 * 16f,
                        SourceWidth = 16f,
                        SourceHeight = 16f,
                        Sampling = sampling
                    };

                    // Tile 43's bottom and left border texels and neighboring
                    // tile 0 are opaque #84C669. The background cells form
                    // actual contiguous geometry on both axes; no clear-color
                    // hole can satisfy the join assertions.
                    foreach ((float columnX, float rowY) in new[]
                    {
                        (0f, 0f), (96f, 0f), (192f, 0f),
                        (0f, 96f), (96f, 96f)
                    })
                    {
                        for (int row = -1; row <= 1; row++)
                        {
                            for (int column = -1; column <= 1; column++)
                            {
                                scene.Children.Add(Tile(0, columnX + column * 32f,
                                    rowY + row * 32f, DrawSamplingMode.Point));
                            }
                        }
                    }

                    scene.Children.Add(Tile(43, 0f, 0f, DrawSamplingMode.Point));
                    scene.Children.Add(Tile(43, 96f, 0f, DrawSamplingMode.Linear));
                    scene.Children.Add(Tile(0, 192f, 0f, DrawSamplingMode.Point));
                    Sprite2D flipped = Tile(43, 0f, 96f, DrawSamplingMode.Point);
                    flipped.Flip = RenderSurface2DSpriteFlip.Horizontal;
                    scene.Children.Add(flipped);
                    Sprite2D rotated = Tile(43, 128f, 96f, DrawSamplingMode.Point);
                    rotated.Rotation = MathF.PI / 2f;
                    scene.Children.Add(rotated);

                    // Unlike the opaque controls, these two adjacent sprites
                    // are half-opacity over contrasting magenta clear color.
                    // Their grass border has an observable blended reference.
                    Sprite2D alphaTop = Tile(43, 192f, 96f, DrawSamplingMode.Point);
                    alphaTop.Opacity = 0.5f;
                    scene.Children.Add(alphaTop);
                    Sprite2D alphaBottom = Tile(0, 192f, 128f, DrawSamplingMode.Point);
                    alphaBottom.Opacity = 0.5f;
                    scene.Children.Add(alphaBottom);
                    surface.Scene = scene;
                    ServoApi.SetId(surface, "boundary-surface");
                    window.Content = surface;
                    window.ContentRendered += async (_, _) =>
                    {
                        if (started)
                        {
                            return;
                        }

                        started = true;
                        try
                        {
                            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                            var servo = new ServoApi(window);
                            await WaitForFramesAsync(window, 2, deadline.Token);
                            await servo.SaveScreenshotAsync(capturePath, deadline.Token);
                            probe = await ProbeBoundaryPixelsAsync(
                                servo, window, surface, capturePath, requestedPhase, deadline.Token);
                        }
                        catch (Exception ex)
                        {
                            failure = ex;
                        }
                        finally
                        {
                            window.Close();
                        }
                    };
                    return window;
                },
                startupWindowTypeName: typeof(Window).FullName),
            []);

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Assert.True(started, "The native tile boundary window never rendered.");
        Assert.Equal(0, exitCode);
        Assert.NotNull(probe);
        output.WriteLine($"App-owned tile-boundary capture: {capturePath}");
        return probe.Value;
    }

    private async Task<BoundaryProbe> ProbeBoundaryPixelsAsync(
        ServoApi servo,
        Window window,
        RenderSurface2D surface,
        string screenshotPath,
        float? requestedPhase,
        CancellationToken cancellationToken)
    {
        ServoElement game = await servo.FindAsync(ServoTarget.ById("boundary-surface"), cancellationToken);
        DrawRect view = Assert.IsType<DrawRect>(surface.ViewBox);
        var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
        using SKBitmap? bitmap = SKBitmap.Decode(screenshotPath);
        Assert.NotNull(bitmap);

        float pixelScaleX = bitmap.Width / viewport.Width;
        float pixelScaleY = bitmap.Height / viewport.Height;
        float ScreenX(float worldX) =>
            (game.Bounds.X + (worldX - view.X) / view.Width * game.Bounds.Width) * pixelScaleX;
        float ScreenY(float worldY) =>
            (game.Bounds.Y + (worldY - view.Y) / view.Height * game.Bounds.Height) * pixelScaleY;

        int boundaryY = (int)MathF.Floor(ScreenY(32f));
        int boundaryX = (int)MathF.Floor(ScreenX(0f));
        float horizontalPhase = ScreenY(32f) - boundaryY;
        float verticalPhase = ScreenX(0f) - boundaryX;
        var mapping = new PixelMapping(
            game.Bounds.X * pixelScaleX,
            game.Bounds.Y * pixelScaleY,
            game.Bounds.Width * pixelScaleX / view.Width,
            game.Bounds.Height * pixelScaleY / view.Height,
            boundaryX,
            boundaryY);
        SKColor grass = new(132, 198, 105);
        SKColor Pixel(int x, int y)
        {
            Assert.InRange(x, 0, bitmap.Width - 1);
            Assert.InRange(y, 0, bitmap.Height - 1);
            return bitmap.GetPixel(x, y);
        }

        int PhysicalX(float worldX) => (int)MathF.Floor(ScreenX(worldX));
        int PhysicalY(float worldY) => (int)MathF.Floor(ScreenY(worldY));
        SKColor Sample(float worldX, float worldY) => Pixel(PhysicalX(worldX), PhysicalY(worldY));

        SKColor pointBottom = Sample(16f, 32f);
        SKColor linearBottom = Sample(112f, 32f);
        SKColor uniformBottom = Sample(208f, 32f);
        SKColor pointLeft = Sample(0f, 16f);
        SKColor linearLeft = Sample(96f, 16f);
        SKColor uniformLeft = Sample(192f, 16f);
        SKColor flippedRight = Sample(32f, 112f);
        SKColor rotatedLeft = Sample(96f, 112f);
        SKColor alphaBottom = Sample(208f, 128f);
        int alphaX = PhysicalX(208f);
        int alphaY = PhysicalY(128f);
        SKColor alphaAbove = Pixel(alphaX, alphaY - 3);
        SKColor alphaBelow = Pixel(alphaX, alphaY + 3);
        AssertClose(alphaAbove, alphaBelow, 2);
        // 50%-opaque #84C669 over the magenta clear color is approximately
        // #C163B4 after 8-bit source-over rounding. This rejects a fixture
        // that accidentally placed opaque grass beneath the alpha sprites.
        AssertClose(new SKColor(193, 99, 180), alphaAbove, 2);
        Assert.NotEqual(grass, alphaAbove);
        Assert.NotEqual(new SKColor(255, 0, 255), alphaAbove);

        foreach (int delta in new[] { -3, 3 })
        {
            Assert.Equal(grass, Pixel(PhysicalX(16f), boundaryY + delta));
            Assert.Equal(grass, Pixel(boundaryX + delta, PhysicalY(16f)));
            Assert.Equal(grass, Pixel(PhysicalX(32f) + delta, PhysicalY(112f)));
            Assert.Equal(grass, Pixel(PhysicalX(96f) + delta, PhysicalY(112f)));
        }

        SKColor pointInteriorGrass = Pixel(PhysicalX(2f), PhysicalY(2f));
        SKColor linearInteriorGrass = Pixel(PhysicalX(98f), PhysicalY(2f));
        Assert.Equal(grass, pointInteriorGrass);
        Assert.Equal(grass, linearInteriorGrass);

        int linearInteriorDifferences = 0;
        for (int y = 4; y <= 28; y += 2)
        {
            for (int x = 4; x <= 28; x += 2)
            {
                if (Pixel(PhysicalX(x), PhysicalY(y)) !=
                    Pixel(PhysicalX(x + 96f), PhysicalY(y)))
                {
                    linearInteriorDifferences++;
                }
            }
        }

        output.WriteLine($"Tile boundary {requestedPhase?.ToString("F3") ?? "calibration"}: ViewBox={view}, bounds={game.Bounds}, viewportScale={viewport.Scale}, screenshot={bitmap.Width}x{bitmap.Height}, physicalPerWorld={mapping.PixelsPerWorldX:F4}/{mapping.PixelsPerWorldY:F4}, origin={mapping.SurfaceLeftPixels:F3}/{mapping.SurfaceTopPixels:F3}, phaseX/Y={verticalPhase:F3}/{horizontalPhase:F3}, Point bottom/left={pointBottom}/{pointLeft}, Linear bottom/left={linearBottom}/{linearLeft}, Uniform bottom/left={uniformBottom}/{uniformLeft}, flippedRight={flippedRight}, rotatedLeft={rotatedLeft}, alpha reference/edge={alphaAbove}/{alphaBottom}, changedLinearInterior={linearInteriorDifferences}.");
        output.WriteLine($"Tile boundary raw coordinates: view.X={view.X:G9}, view.Y={view.Y:G9}, ScreenX(0)={ScreenX(0f):G9}, ScreenX(32)={ScreenX(32f):G9}, ScreenY(112)={ScreenY(112f):G9}.");

        return new BoundaryProbe(requestedPhase, horizontalPhase, verticalPhase,
            pointBottom, linearBottom, uniformBottom,
            pointLeft, linearLeft, uniformLeft,
            flippedRight, rotatedLeft, alphaAbove, alphaBottom,
            linearInteriorDifferences, mapping);
    }

    private static async Task WaitForFramesAsync(Window window, int count, CancellationToken cancellationToken)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler? handler = null;
        handler = (_, _) =>
        {
            if (--count == 0)
            {
                window.FrameRendered -= handler;
                completion.TrySetResult();
            }
        };
        window.FrameRendered += handler;
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        try
        {
            await completion.Task;
        }
        finally
        {
            window.FrameRendered -= handler;
        }
    }

    private static string CreateCapturePath(float? requestedPhase)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(directory.FullName, "artifacts", "ci", "scene-village", "screenshots",
            "tile-boundary-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"),
            requestedPhase is null ? "calibration.png" : $"phase-{requestedPhase:F3}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    private static void AssertClose(SKColor expected, SKColor actual, int tolerance)
    {
        Assert.True(IsClose(expected, actual, tolerance),
            $"Expected {actual} to be within {tolerance} per RGB channel of {expected}.");
    }

    private static bool IsClose(SKColor expected, SKColor actual, int tolerance) =>
        Math.Abs(expected.Red - actual.Red) <= tolerance &&
        Math.Abs(expected.Green - actual.Green) <= tolerance &&
        Math.Abs(expected.Blue - actual.Blue) <= tolerance;

    private readonly record struct PixelMapping(
        float SurfaceLeftPixels,
        float SurfaceTopPixels,
        float PixelsPerWorldX,
        float PixelsPerWorldY,
        int AnchorX,
        int AnchorY);

    private readonly record struct BoundaryProbe(
        float? RequestedPhase,
        float HorizontalPhase,
        float VerticalPhase,
        SKColor PointBottom,
        SKColor LinearBottom,
        SKColor UniformBottom,
        SKColor PointLeft,
        SKColor LinearLeft,
        SKColor UniformLeft,
        SKColor FlippedRight,
        SKColor RotatedLeft,
        SKColor AlphaReference,
        SKColor AlphaBottom,
        int LinearInteriorDifferences,
        PixelMapping Mapping);
}
