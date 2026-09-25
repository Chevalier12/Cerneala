using System.Diagnostics;
using System.Numerics;
using System.Runtime.ExceptionServices;
using Cerneala.SceneVillage;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using SkiaSharp;
using Xunit.Abstractions;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SceneVillage;

[Collection(VillageNativeTestCollection.Name)]
public sealed class NativeVillageWindowTests
{
    private readonly ITestOutputHelper output;

    public NativeVillageWindowTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void RealSdlWindowSupportsVillageMovementCollisionCameraAndEveryTenThousandItemPreset()
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        Exception? failure = null;
        bool started = false;
        string captureDirectory = CreateCaptureDirectory();

        int exitCode = GeneratedWindowApplication.Run(
            new GeneratedWindowStartupDescriptor(
                createApplication: () => new App(),
                configureServices: _ => { },
                createStartupWindow: _ =>
                {
                    var window = new MainWindow();
                    // Start at the declared minimum, not a test-side resize
                    // after layout, so every toolbar action must be reachable.
                    window.Width = window.MinWidth;
                    window.Height = window.MinHeight;
                    UIElement[] tree = DescendantsAndSelf(window).ToArray();
                    VillageGameSurface surface = Assert.Single(tree.OfType<VillageGameSurface>());
                    TextBlock status = Assert.Single(tree.OfType<TextBlock>().Where(item => item.Text?.Contains("requested") == true));
                    ServoApi.SetId(surface, "village-surface");
                    SetButtonId(tree, "0", "count-0");
                    SetButtonId(tree, "100", "count-100");
                    SetButtonId(tree, "1k", "count-1000");
                    SetButtonId(tree, "10k", "count-10000");
                    SetButtonId(tree, "Static", "mode-static");
                    SetButtonId(tree, "Animated", "mode-animated");
                    SetButtonId(tree, "Collision", "mode-collision");
                    SetButtonId(tree, "Stress field", "stress-field");
                    SetButtonId(tree, "Village", "village");

                    window.ContentRendered += async (_, _) =>
                    {
                        if (started)
                        {
                            return;
                        }

                        started = true;
                        try
                        {
                            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                            await ExerciseWindowAsync(window, surface, status, captureDirectory, deadline.Token);
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
                startupWindowTypeName: typeof(MainWindow).FullName),
            []);

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Assert.True(started, "The real SDL window never rendered its content.");
        Assert.Equal(0, exitCode);
        output.WriteLine($"App-owned captures: {captureDirectory}");
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void RealVillageShowsTheNativeTreeCanopyAboveItsLowerCellTrunk()
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        Exception? failure = null;
        bool started = false;
        string capturePath = Path.Combine(CreateCaptureDirectory(), "decorations.png");

        int exitCode = GeneratedWindowApplication.Run(
            new GeneratedWindowStartupDescriptor(
                createApplication: () => new App(),
                configureServices: _ => { },
                createStartupWindow: _ =>
                {
                    var window = new MainWindow();
                    window.Width = window.MinWidth;
                    window.Height = window.MinHeight;
                    VillageGameSurface surface = Assert.Single(DescendantsAndSelf(window).OfType<VillageGameSurface>());
                    ServoApi.SetId(surface, "decoration-surface");
                    window.ContentRendered += async (_, _) =>
                    {
                        if (started)
                        {
                            return;
                        }

                        started = true;
                        try
                        {
                            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                            await ExerciseDecorationAsync(window, surface, capturePath, deadline.Token);
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
                startupWindowTypeName: typeof(MainWindow).FullName),
            []);

        if (failure is not null)
        {
            ExceptionDispatchInfo.Capture(failure).Throw();
        }

        Assert.True(started, "The native Village decoration window never rendered.");
        Assert.Equal(0, exitCode);
    }

    private async Task ExerciseDecorationAsync(
        MainWindow window,
        VillageGameSurface surface,
        string capturePath,
        CancellationToken cancellationToken)
    {
        var servo = new ServoApi(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMinutes(2) });
        await servo.ClickAsync(ServoTarget.ById("decoration-surface"), cancellationToken);
        Vector2 start = surface.PlayerCenter;

        // Use the app's routed WASD path to bring a real tree and shrub into
        // the camera view. No test-side player or camera assignment is made.
        int rightTaps = 0;
        while (surface.PlayerCenter.X < 2100f && rightTaps < 120)
        {
            await servo.PressKeyAsync(InputKey.D, cancellationToken: cancellationToken);
            await Task.Delay(12, cancellationToken);
            rightTaps++;
        }

        int downTaps = 0;
        while (surface.PlayerCenter.Y < 2210f && downTaps < 150)
        {
            await servo.PressKeyAsync(InputKey.S, cancellationToken: cancellationToken);
            await Task.Delay(12, cancellationToken);
            downTaps++;
        }

        Assert.InRange(surface.PlayerCenter.X, 2100f, 2160f);
        Assert.InRange(surface.PlayerCenter.Y, 2210f, 2250f);
        Assert.True(surface.PlayerCenter.X > start.X && surface.PlayerCenter.Y > start.Y);
        await WaitForFramesAsync(window, 2, cancellationToken);

        // Servo queues the app-owned Window screenshot after frame commit.
        // The expected pixels come from the unchanged Tiny Town PNG.
        await servo.SaveScreenshotAsync(capturePath, cancellationToken);
        AssertScreenshot(capturePath);
        ServoElement game = await servo.FindAsync(ServoTarget.ById("decoration-surface"), cancellationToken);
        var view = Assert.IsType<Cerneala.Drawing.DrawRect>(surface.ViewBox);
        var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
        using SKBitmap? bitmap = SKBitmap.Decode(capturePath);
        Assert.NotNull(bitmap);
        float pixelScaleX = bitmap.Width / viewport.Width;
        float pixelScaleY = bitmap.Height / viewport.Height;
        int ScreenX(float worldX) => (int)MathF.Floor(
            (game.Bounds.X + (worldX - view.X) / view.Width * game.Bounds.Width) * pixelScaleX);
        int ScreenY(float worldY) => (int)MathF.Floor(
            (game.Bounds.Y + (worldY - view.Y) / view.Height * game.Bounds.Height) * pixelScaleY);
        SKColor Pixel(float worldX, float worldY)
        {
            int x = ScreenX(worldX);
            int y = ScreenY(worldY);
            Assert.InRange(x, 0, bitmap.Width - 1);
            Assert.InRange(y, 0, bitmap.Height - 1);
            return bitmap.GetPixel(x, y);
        }

        // Cell 5 is an unchanged shrub control. At tree site (2224,2192),
        // cells 4+16 contribute the top outline, canopy leaf and trunk.
        SKColor expectedLeaf = new(71, 159, 74);
        // Probe source-pixel centers. World-unit boundaries can floor into
        // the neighboring output pixel when the followed camera is fractional.
        SKColor shrub = Pixel(2183.5f, 2274.5f); // cell 5 local (7,2)
        SKColor topOutline = Pixel(2230.5f, 2178.5f); // source (70,2), inside the opaque outline
        SKColor canopy = Pixel(2232.5f, 2185.5f); // source (72,9)
        SKColor trunk = Pixel(2232.5f, 2204.5f); // source (72,28)
        output.WriteLine($"Decoration capture {capturePath}; routed D/S taps={rightTaps}/{downTaps}; " +
            $"player={surface.PlayerCenter}; ViewBox={view}; shrub={shrub}, " +
            $"tree top/leaf/trunk={topOutline}/{canopy}/{trunk}.");
        Assert.Equal(expectedLeaf, shrub);
        Assert.Equal(new SKColor(63, 38, 49), topOutline);
        Assert.Equal(expectedLeaf, canopy);
        Assert.Equal(new SKColor(234, 165, 108), trunk);
    }

    private async Task ExerciseWindowAsync(
        MainWindow window,
        VillageGameSurface surface,
        TextBlock status,
        string captureDirectory,
        CancellationToken cancellationToken)
    {
        var servo = new ServoApi(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMinutes(2) });
        await AssertToolbarFitsViewportAsync(servo, window, cancellationToken);
        await servo.ClickAsync(ServoTarget.ById("village-surface"), cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);

        Vector2 spawn = surface.PlayerPosition;
        var initialBox = Assert.IsType<Cerneala.Drawing.DrawRect>(surface.ViewBox);
        Assert.Equal(0, surface.RealizedStressCount);
        Assert.Contains("requested 0 / realized 0", status.Text);
        string villagePath = Path.Combine(captureDirectory, "village.png");
        await servo.SaveScreenshotAsync(villagePath, cancellationToken);
        AssertScreenshot(villagePath);
        await AssertVillagePathHasNoAtlasEdgeLinesAsync(servo, window, surface, villagePath, cancellationToken);

        await servo.PressKeyAsync(InputKey.D, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.True(surface.PlayerPosition.X > spawn.X, "A routed D tap must move the player right.");
        Assert.Equal(spawn.Y, surface.PlayerPosition.Y, 2);
        Assert.True(surface.ViewBox!.Value.X > initialBox.X, "The camera must follow routed movement.");
        Assert.Equal(surface.PlayerCenter.X, surface.ViewBox.Value.X + surface.ViewBox.Value.Width / 2f, 2);
        await CapturePlayerFacingAsync(servo, surface, "Right", captureDirectory, cancellationToken);
        await AssertVillagePathHasNoAtlasEdgeLinesAsync(
            servo, window, surface, Path.Combine(captureDirectory, "player-right.png"), cancellationToken);

        await servo.PressKeyAsync(InputKey.R, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.Equal(spawn, surface.PlayerPosition);
        await servo.PressKeyAsync(InputKey.W, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.True(surface.PlayerPosition.Y < spawn.Y, "A routed W tap must move the player up.");
        Assert.True(surface.ViewBox!.Value.Y < initialBox.Y, "The camera must follow routed upward movement.");
        await CapturePlayerFacingAsync(servo, surface, "Up", captureDirectory, cancellationToken);
        await AssertVillagePathHasNoAtlasEdgeLinesAsync(
            servo, window, surface, Path.Combine(captureDirectory, "player-up.png"), cancellationToken);

        await servo.PressKeyAsync(InputKey.R, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.Equal(spawn, surface.PlayerPosition);
        await servo.PressKeyAsync(InputKey.S, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.True(surface.PlayerPosition.Y > spawn.Y, "A routed S tap must move the player down.");
        await CapturePlayerFacingAsync(servo, surface, "Down", captureDirectory, cancellationToken);

        await servo.PressKeyAsync(InputKey.R, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.Equal(spawn, surface.PlayerPosition);

        // Servo's public key API is a discrete tap, not a configurable hold.
        // Repeated real routed taps approach the house; the separate model
        // test covers sustained delta-time speed and normalized diagonals.
        int taps = 0;
        while (surface.PlayerPosition.X > 1920.1f && taps < 300)
        {
            await servo.PressKeyAsync(InputKey.A, cancellationToken: cancellationToken);
            await Task.Delay(12, cancellationToken);
            taps++;
        }

        Assert.InRange(surface.PlayerPosition.X, 1920f, 1920.1f);
        for (int i = 0; i < 6; i++)
        {
            await servo.PressKeyAsync(InputKey.A, cancellationToken: cancellationToken);
        }

        Assert.InRange(surface.PlayerPosition.X, 1920f, 1920.1f);
        output.WriteLine($"Routed west-house contact after {taps} Servo A taps at player x={surface.PlayerPosition.X:F3}.");
        await WaitForFramesAsync(window, 2, cancellationToken);
        await CapturePlayerFacingAsync(servo, surface, "Left", captureDirectory, cancellationToken);

        // These are real routed keys after the left-facing capture. The
        // frame-level mirror must not stick when the player faces right or down.
        await servo.PressKeyAsync(InputKey.D, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        await CapturePlayerFacingAsync(servo, surface, "Right", captureDirectory, cancellationToken, "after-left");
        await servo.PressKeyAsync(InputKey.S, cancellationToken: cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        await CapturePlayerFacingAsync(servo, surface, "Down", captureDirectory, cancellationToken, "after-left");

        await servo.ClickAsync(ServoTarget.ById("village"), cancellationToken);
        await servo.ClickAsync(ServoTarget.ById("stress-field"), cancellationToken);
        await WaitForFramesAsync(window, 2, cancellationToken);
        Assert.Equal(VillageLayout.StressFieldWaypoint, surface.PlayerCenter);
        var fieldBox = Assert.IsType<Cerneala.Drawing.DrawRect>(surface.ViewBox);
        Assert.NotEqual(initialBox.X, fieldBox.X);

        await servo.ClickAsync(ServoTarget.ById("count-100"), cancellationToken);
        await WaitForRealizedAsync(surface, 100, cancellationToken);
        Assert.Contains("requested 100 / realized 100", status.Text);
        await servo.ClickAsync(ServoTarget.ById("count-1000"), cancellationToken);
        await WaitForRealizedAsync(surface, 1_000, cancellationToken);
        Assert.Contains("requested 1,000 / realized 1,000", status.Text);
        await servo.ClickAsync(ServoTarget.ById("count-10000"), cancellationToken);
        await WaitForRealizedAsync(surface, 10_000, cancellationToken);
        Assert.Equal(StressPreset.Static, surface.Preset);
        Assert.Contains("requested 10,000 / realized 10,000", status.Text);
        string staticPath = Path.Combine(captureDirectory, "static-10000.png");
        await WaitForFramesAsync(window, 2, cancellationToken);
        await servo.SaveScreenshotAsync(staticPath, cancellationToken);
        AssertScreenshot(staticPath);

        await servo.ClickAsync(ServoTarget.ById("mode-animated"), cancellationToken);
        await WaitForRealizedAsync(surface, 10_000, cancellationToken);
        Assert.Equal(StressPreset.Animated, surface.Preset);
        Assert.Contains("requested 10,000 / realized 10,000", status.Text);
        Assert.Equal(fieldBox, surface.ViewBox);
        string animatedPath = Path.Combine(captureDirectory, "animated-10000.png");
        await WaitForFramesAsync(window, 2, cancellationToken);
        await servo.SaveScreenshotAsync(animatedPath, cancellationToken);
        AssertScreenshot(animatedPath);
        int animatedPixelChanges = CountChangedGamePixels(staticPath, animatedPath);
        for (int attempt = 1; animatedPixelChanges < 50 && attempt <= 6; attempt++)
        {
            await Task.Delay(150, cancellationToken);
            await WaitForFramesAsync(window, 2, cancellationToken);
            string nextPath = Path.Combine(captureDirectory, $"animated-10000-{attempt}.png");
            await servo.SaveScreenshotAsync(nextPath, cancellationToken);
            AssertScreenshot(nextPath);
            animatedPixelChanges = CountChangedGamePixels(staticPath, nextPath);
        }

        Assert.True(animatedPixelChanges >= 50,
            $"The animated preset never presented a different walk pose in the game area; changed sampled pixels={animatedPixelChanges}.");
        output.WriteLine($"Animated preset changed {animatedPixelChanges} sampled game-area pixels relative to static idle.");

        await servo.ClickAsync(ServoTarget.ById("mode-collision"), cancellationToken);
        await WaitForRealizedAsync(surface, 10_000, cancellationToken);
        Assert.Equal(StressPreset.Collision, surface.Preset);
        Assert.Contains("requested 10,000 / realized 10,000", status.Text);
        Assert.Equal(fieldBox, surface.ViewBox);
        string collisionPath = Path.Combine(captureDirectory, "collision-10000.png");
        await WaitForFramesAsync(window, 2, cancellationToken);
        await servo.SaveScreenshotAsync(collisionPath, cancellationToken);
        AssertScreenshot(collisionPath);

        float firstBlockingActorX = VillageLayout.StressPositions
            .Where(position => position.Y == 2048f && position.X >= 2640f)
            .Min(position => position.X);
        float expectedContactX = firstBlockingActorX - 16f;
        int fieldTaps = 0;
        while (surface.PlayerPosition.X < expectedContactX - 0.1f && fieldTaps < 300)
        {
            await servo.PressKeyAsync(InputKey.D, cancellationToken: cancellationToken);
            await Task.Delay(12, cancellationToken);
            fieldTaps++;
        }

        Assert.InRange(surface.PlayerPosition.X, expectedContactX - 0.1f, expectedContactX);
        for (int i = 0; i < 6; i++)
        {
            await servo.PressKeyAsync(InputKey.D, cancellationToken: cancellationToken);
        }

        Assert.InRange(surface.PlayerPosition.X, expectedContactX - 0.1f, expectedContactX);
        output.WriteLine($"Collidable stress actor contact after {fieldTaps} routed D taps at x={surface.PlayerPosition.X:F3}.");

        await servo.ClickAsync(ServoTarget.ById("count-0"), cancellationToken);
        await WaitForRealizedAsync(surface, 0, cancellationToken);
        Assert.Contains("requested 0 / realized 0", status.Text);
        await servo.ClickAsync(ServoTarget.ById("mode-static"), cancellationToken);
        Assert.Equal(StressPreset.Static, surface.Preset);
        Assert.Contains("requested 0 / realized 0", status.Text);
        await WaitForFramesAsync(window, 2, cancellationToken);
        string clearedPath = Path.Combine(captureDirectory, "cleared-0.png");
        await servo.SaveScreenshotAsync(clearedPath, cancellationToken);
        AssertScreenshot(clearedPath);
        Assert.True(CountChangedGamePixels(collisionPath, clearedPath) >= 50,
            "Clicking 0 must retire the stress actors from the presented game area.");
        output.WriteLine("Servo clicked 0 after 10,000 and the app retired all 10,000 realized stress actors.");

        output.WriteLine("All three ten-thousand-item presets realized 10,000 SceneItems2D children at the same camera ViewBox.");
    }

    private static async Task AssertToolbarFitsViewportAsync(
        ServoApi servo,
        Window window,
        CancellationToken cancellationToken)
    {
        float viewportWidth = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport.Width;
        string[] buttonIds =
        [
            "mode-static", "mode-animated", "mode-collision",
            "count-0", "count-100", "count-1000", "count-10000",
            "stress-field", "village"
        ];
        foreach (string id in buttonIds)
        {
            ServoElement button = await servo.FindAsync(ServoTarget.ById(id), cancellationToken);
            Assert.True(button.IsVisible, $"Toolbar button {id} is not visible.");
            Assert.True(button.Bounds.Width > 0 && button.Bounds.Height > 0,
                $"Toolbar button {id} has empty bounds {button.Bounds}.");
            Assert.True(button.Bounds.X >= 0 && button.Bounds.X + button.Bounds.Width <= viewportWidth,
                $"Toolbar button {id} ends at {button.Bounds.X + button.Bounds.Width} beyond viewport width {viewportWidth}.");
        }
    }

    private static async Task CapturePlayerFacingAsync(
        ServoApi servo,
        VillageGameSurface surface,
        string direction,
        string captureDirectory,
        CancellationToken cancellationToken,
        string? suffix = null)
    {
        Sprite2D player = Assert.Single(surface.Scene!.Children.OfType<Sprite2D>()
            .Where(sprite => sprite.Collider?.IsSimulated == true));
        Assert.True(player.AnimationState?.EndsWith(direction, StringComparison.Ordinal) == true,
            $"Servo-routed movement should face {direction}, but state is {player.AnimationState}.");
        Assert.Equal(RenderSurface2DSpriteFlip.None, player.Flip);
        Assert.True(player.Animations!.TryGetClip(player.AnimationState!, out SpriteAnimationClip? activeClip));
        float expectedY = direction switch
        {
            "Down" => 0f,
            "Up" => 32f,
            "Left" or "Right" => 96f,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
        RenderSurface2DSpriteFlip expectedFlip = direction == "Left"
            ? RenderSurface2DSpriteFlip.Horizontal
            : RenderSurface2DSpriteFlip.None;
        Assert.All(activeClip!.Frames, frame =>
        {
            Assert.Equal(expectedY, frame.SourceRect.Y);
            Assert.Equal(expectedFlip, frame.Flip);
        });
        if (direction is "Left" or "Right")
        {
            string opposite = direction == "Left" ? "Right" : "Left";
            string oppositeState = player.AnimationState![..^direction.Length] + opposite;
            Assert.True(player.Animations.TryGetClip(oppositeState, out SpriteAnimationClip? oppositeClip));
            Assert.Equal(
                activeClip.Frames.Select(frame => frame.SourceRect).ToArray(),
                oppositeClip!.Frames.Select(frame => frame.SourceRect).ToArray());
        }

        string fileName = $"player-{direction.ToLowerInvariant()}" +
            (suffix is null ? string.Empty : $"-{suffix}") + ".png";
        string path = Path.Combine(captureDirectory, fileName);
        await servo.SaveScreenshotAsync(path, cancellationToken);
        AssertScreenshot(path);
    }

    private static async Task WaitForRealizedAsync(VillageGameSurface surface, int count, CancellationToken cancellationToken)
    {
        while (surface.RealizedStressCount != count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(20, cancellationToken);
        }

        Assert.Equal(count, surface.RequestedStressCount);
        Assert.Equal(count, surface.RealizedStressCount);
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

    private static void SetButtonId(IEnumerable<UIElement> tree, string content, string id)
    {
        Button button = Assert.Single(tree.OfType<Button>().Where(item => Equals(item.Content, content)));
        ServoApi.SetId(button, id);
    }

    private static IEnumerable<UIElement> DescendantsAndSelf(UIElement element)
    {
        HashSet<UIElement> visited = new(ReferenceEqualityComparer.Instance);
        return Visit(element);

        IEnumerable<UIElement> Visit(UIElement current)
        {
            if (!visited.Add(current))
            {
                yield break;
            }

            yield return current;
            foreach (UIElement child in current.LogicalChildren.Concat(current.VisualChildren))
            {
                foreach (UIElement descendant in Visit(child))
                {
                    yield return descendant;
                }
            }
        }
    }

    private static string CreateCaptureDirectory()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(
            directory.FullName,
            "artifacts", "ci", "scene-village", "screenshots",
            "native-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void AssertScreenshot(string path)
    {
        Assert.True(File.Exists(path), $"Missing app-owned screenshot: {path}");
        using SKBitmap? bitmap = SKBitmap.Decode(path);
        Assert.NotNull(bitmap);
        Assert.True(bitmap.Width >= 800 && bitmap.Height >= 600, "Screenshot must show the real window at game size.");
    }

    private async Task AssertVillagePathHasNoAtlasEdgeLinesAsync(
        ServoApi servo,
        Window window,
        VillageGameSurface surface,
        string screenshotPath,
        CancellationToken cancellationToken)
    {
        // The opaque grass border of Tiny Town cell 43 is the same #84C669
        // as neighboring cell 0. Its immediate atlas neighbors are different
        // art; a cropped path tile must not bring those neighbors into view.
        ServoElement game = await servo.FindAsync(ServoTarget.ById("village-surface"), cancellationToken);
        var view = Assert.IsType<Cerneala.Drawing.DrawRect>(surface.ViewBox);
        var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
        using SKBitmap? bitmap = SKBitmap.Decode(screenshotPath);
        Assert.NotNull(bitmap);

        float pixelScaleX = bitmap.Width / viewport.Width;
        float pixelScaleY = bitmap.Height / viewport.Height;
        float ScreenX(float worldX) =>
            (game.Bounds.X + (worldX - view.X) / view.Width * game.Bounds.Width) * pixelScaleX;
        float ScreenY(float worldY) =>
            (game.Bounds.Y + (worldY - view.Y) / view.Height * game.Bounds.Height) * pixelScaleY;

        // Vertical path begins at world x=2032. Sample its opaque first two
        // physical pixels at a grass-only y, and a safe pixel further inside.
        int verticalEdgeX = (int)MathF.Ceiling(ScreenX(2032f));
        int verticalSafeX = verticalEdgeX + 3;
        int verticalY = (int)MathF.Floor(ScreenY(1968f));
        // Horizontal path ends at world y=2064. Sample its last two physical
        // pixels and a safe pixel farther inside, away from the intersection.
        int horizontalEdgeY = (int)MathF.Floor(ScreenY(2064f)) - 1;
        int horizontalBoundaryY = (int)MathF.Floor(ScreenY(2064f));
        int horizontalSafeY = horizontalEdgeY - 3;
        int horizontalX = (int)MathF.Floor(ScreenX(2096f));

        Assert.InRange(verticalSafeX, 0, bitmap.Width - 1);
        Assert.InRange(verticalY, 0, bitmap.Height - 1);
        Assert.InRange(horizontalX, 0, bitmap.Width - 1);
        Assert.InRange(horizontalSafeY, 0, bitmap.Height - 1);
        Assert.InRange(horizontalBoundaryY, 0, bitmap.Height - 1);
        SKColor grass = new(132, 198, 105);
        SKColor verticalSafe = bitmap.GetPixel(verticalSafeX, verticalY);
        SKColor horizontalSafe = bitmap.GetPixel(horizontalX, horizontalSafeY);
        SKColor verticalEdge = bitmap.GetPixel(verticalEdgeX, verticalY);
        SKColor horizontalEdge = bitmap.GetPixel(horizontalX, horizontalEdgeY);
        SKColor horizontalBoundary = bitmap.GetPixel(horizontalX, horizontalBoundaryY);
        float verticalPhase = ScreenX(2032f) - MathF.Floor(ScreenX(2032f));
        float horizontalPhase = ScreenY(2064f) - MathF.Floor(ScreenY(2064f));
        output.WriteLine($"Path-edge crop probe: ViewBox={view}, gameBounds={game.Bounds}, screenshot={bitmap.Width}x{bitmap.Height}, edge phases x={verticalPhase:F3} y={horizontalPhase:F3}; vertical x={verticalEdgeX}/{verticalSafeX} y={verticalY} edge={verticalEdge} safe={verticalSafe}; horizontal x={horizontalX} y={horizontalEdgeY}/{horizontalBoundaryY}/{horizontalSafeY} edge={horizontalEdge} boundary={horizontalBoundary} safe={horizontalSafe}.");
        Assert.Equal(grass, verticalSafe);
        Assert.Equal(grass, horizontalSafe);
        Assert.True(verticalEdge == verticalSafe && horizontalEdge == horizontalSafe,
            $"Opaque path tile 43 must join grass without edge lines. Vertical {verticalEdge} vs {verticalSafe}; horizontal {horizontalEdge} vs {horizontalSafe}.");
        Assert.Equal(grass, horizontalBoundary);
    }

    private static int CountChangedGamePixels(string firstPath, string secondPath)
    {
        using SKBitmap? first = SKBitmap.Decode(firstPath);
        using SKBitmap? second = SKBitmap.Decode(secondPath);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first.Width, second.Width);
        Assert.Equal(first.Height, second.Height);

        // The central crop is wholly inside the render surface and excludes
        // the mode/status HUD, so text changes cannot satisfy this assertion.
        int changed = 0;
        for (int y = first.Height / 5; y < first.Height * 4 / 5; y += 3)
        {
            for (int x = first.Width / 4; x < first.Width * 3 / 4; x += 3)
            {
                if (first.GetPixel(x, y) != second.GetPixel(x, y))
                {
                    changed++;
                }
            }
        }

        return changed;
    }
}
