using System.Numerics;
using System.Runtime.ExceptionServices;
using Cerneala.Drawing;
using Cerneala.SceneVillage;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using Xunit.Abstractions;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SceneVillage;

[Collection(VillageNativeTestCollection.Name)]
public sealed class NativeCharacterAtlasTests
{
    private const float SourceCellSize = 32f;
    private const float VariantOffset = 36f;
    private const float GroupStep = 80f;
    private const float PixelsPerWorld = 2.5f;
    private static readonly SKColor Background = new(130, 190, 101);
    private static readonly (string Name, int SourceRow, RenderSurface2DSpriteFlip Flip)[] Directions =
    [
        ("Down", 0, RenderSurface2DSpriteFlip.None),
        ("Up", 1, RenderSurface2DSpriteFlip.None),
        ("Left", 3, RenderSurface2DSpriteFlip.Horizontal),
        ("Right", 3, RenderSurface2DSpriteFlip.None)
    ];

    private readonly ITestOutputHelper output;

    public NativeCharacterAtlasTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void RoutedRightAndUpKeyTapsMoveAndCaptureTheResultingIdlePose()
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        string captureDirectory = CreateCaptureDirectory("routed");
        Exception? failure = null;
        bool started = false;
        int walkRightFrames = 0;
        int walkUpFrames = 0;

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
                    Sprite2D player = Assert.Single(surface.Scene!.Children.OfType<Sprite2D>()
                        .Where(sprite => sprite.Collider?.IsSimulated == true));
                    ServoApi.SetId(surface, "character-game");
                    window.FrameRendered += (_, _) =>
                    {
                        if (player.AnimationState == "WalkRight")
                        {
                            walkRightFrames++;
                        }
                        else if (player.AnimationState == "WalkUp")
                        {
                            walkUpFrames++;
                        }
                    };
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
                            var servo = new ServoApi(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMinutes(2) });
                            await servo.ClickAsync(ServoTarget.ById("character-game"), deadline.Token);
                            Vector2 spawn = surface.PlayerPosition;
                            await servo.PressKeyAsync(InputKey.D, cancellationToken: deadline.Token);
                            await WaitForFramesAsync(window, 2, deadline.Token);
                            Assert.True(surface.PlayerPosition.X > spawn.X);
                            Assert.EndsWith("Right", player.AnimationState);
                            string rightPath = Path.Combine(captureDirectory, "right.png");
                            await servo.SaveScreenshotAsync(rightPath, deadline.Token);
                            await ReportSpriteCaptureAsync(servo, window, surface, player, rightPath, "Right", deadline.Token);

                            await servo.PressKeyAsync(InputKey.R, cancellationToken: deadline.Token);
                            await WaitForFramesAsync(window, 2, deadline.Token);
                            Assert.Equal(spawn, surface.PlayerPosition);
                            await servo.PressKeyAsync(InputKey.W, cancellationToken: deadline.Token);
                            await WaitForFramesAsync(window, 2, deadline.Token);
                            Assert.True(surface.PlayerPosition.Y < spawn.Y);
                            Assert.EndsWith("Up", player.AnimationState);
                            string upPath = Path.Combine(captureDirectory, "up.png");
                            await servo.SaveScreenshotAsync(upPath, deadline.Token);
                            await ReportSpriteCaptureAsync(servo, window, surface, player, upPath, "Up", deadline.Token);
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

        Assert.True(started);
        Assert.Equal(0, exitCode);
        output.WriteLine($"Routed WalkRight/WalkUp frame observations: {walkRightFrames}/{walkUpFrames}.");
        output.WriteLine($"App-owned routed captures: {captureDirectory}");
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void CharacterAtlasFilteringMatrixReportsAllDirectionsAndWalkingFrames()
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        string captureDirectory = CreateCaptureDirectory("matrix");
        string originalPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ch003.png");
        string isolatedPath = Path.Combine(captureDirectory, "isolated-frames.png");
        WriteIsolatedFrameAtlas(originalPath, isolatedPath);
        string capturePath = Path.Combine(captureDirectory, "matrix.png");
        Exception? failure = null;
        bool started = false;

        int exitCode = GeneratedWindowApplication.Run(
            new GeneratedWindowStartupDescriptor(
                createApplication: () => new App(),
                configureServices: _ => { },
                createStartupWindow: _ =>
                {
                    var window = new Window
                    {
                        Title = "Scene Village character atlas diagnosis",
                        Width = 1000f,
                        Height = 850f
                    };
                    ResourceId<ImageResource> atlasId = new("CharacterMatrixAtlas");
                    ResourceId<ImageResource> isolatedId = new("CharacterMatrixIsolated");
                    var atlas = new ImageReference(atlasId);
                    var isolated = new ImageReference(isolatedId);
                    var scene = new Scene2D();
                    for (int direction = 0; direction < Directions.Length; direction++)
                    {
                        (string _, int sourceRow, RenderSurface2DSpriteFlip flip) = Directions[direction];
                        for (int frame = 0; frame < 4; frame++)
                        {
                            float groupX = frame * GroupStep;
                            float groupY = direction * GroupStep;
                            float sourceX = frame * SourceCellSize;
                            float sourceY = sourceRow * SourceCellSize;
                            float isolatedX = 16f + frame * 64f;
                            float isolatedY = 16f + sourceRow * 64f;
                            scene.Children.Add(FrameSprite(atlas, sourceX, sourceY,
                                groupX, groupY, DrawSamplingMode.Linear, flip));
                            scene.Children.Add(FrameSprite(isolated, isolatedX, isolatedY,
                                groupX + VariantOffset, groupY, DrawSamplingMode.Linear, flip));
                            scene.Children.Add(FrameSprite(atlas, sourceX, sourceY,
                                groupX, groupY + VariantOffset, DrawSamplingMode.Point, flip));
                            scene.Children.Add(FrameSprite(isolated, isolatedX, isolatedY,
                                groupX + VariantOffset, groupY + VariantOffset, DrawSamplingMode.Point, flip));
                        }
                    }

                    var surface = new RenderSurface2D
                    {
                        Scene = scene,
                        ViewBox = new DrawRect(-16f, -16f, 400f, 340f),
                        ClearColor = new Color(Background.Red, Background.Green, Background.Blue),
                        Stretch = DrawBrushStretch.Fill,
                        RedrawMode = RenderSurface2DRedrawMode.Continuous
                    };
                    surface.Resources.SetResource(atlasId, new ImageResource(originalPath));
                    surface.Resources.SetResource(isolatedId, new ImageResource(isolatedPath));
                    ServoApi.SetId(surface, "character-matrix");
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
                            var servo = new ServoApi(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMinutes(2) });
                            ServoElement game = await servo.FindAsync(ServoTarget.ById("character-matrix"), deadline.Token);
                            var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
                            // Initial renderer-fixture calibration, not user input:
                            // all paired sprites land at the same physical phase.
                            surface.ViewBox = new DrawRect(-16f, -16f,
                                game.Bounds.Width * viewport.Scale / PixelsPerWorld,
                                game.Bounds.Height * viewport.Scale / PixelsPerWorld);
                            await WaitForFramesAsync(window, 3, deadline.Token);
                            await servo.SaveScreenshotAsync(capturePath, deadline.Token);
                            AnalyzeMatrix(window, surface, game, capturePath);
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

        Assert.True(started);
        Assert.Equal(0, exitCode);
        output.WriteLine($"App-owned character matrix capture: {capturePath}");
        output.WriteLine($"Exact source-cell control atlas: {isolatedPath}");
    }

    [VillageNativeFact]
    [Trait("Category", "Native")]
    public void ProductionCharacterFramesHaveNoForeignMarksOnTransparentTopRows()
    {
        Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend.EnsureRegistered();
        string captureDirectory = CreateCaptureDirectory("production-frames");
        string originalPath = Path.Combine(AppContext.BaseDirectory, "Assets", "ch003.png");
        using SKBitmap? source = SKBitmap.Decode(originalPath);
        Assert.NotNull(source);
        Assert.Equal(128, source.Width);
        Assert.Equal(128, source.Height);
        string capturePath = Path.Combine(captureDirectory, "production-frames.png");
        Exception? failure = null;
        bool started = false;

        int exitCode = GeneratedWindowApplication.Run(
            new GeneratedWindowStartupDescriptor(
                createApplication: () => new App(),
                configureServices: _ => { },
                createStartupWindow: _ =>
                {
                    var window = new Window
                    {
                        Title = "Scene Village production character frames",
                        Width = 1000f,
                        Height = 850f
                    };
                    ResourceId<ImageResource> atlasId = new("ProductionCharacterAtlas");
                    var scene = new Scene2D();
                    var poses = new Sprite2D[Directions.Length, 4];
                    for (int direction = 0; direction < Directions.Length; direction++)
                    {
                        for (int frame = 0; frame < 4; frame++)
                        {
                            Sprite2D player = VillageArt.Player(new ImageReference(atlasId),
                                frame * GroupStep, direction * GroupStep);
                            player.AnimationPlaybackRate = 0d;
                            poses[direction, frame] = player;
                            scene.Children.Add(player);
                        }
                    }

                    var surface = new RenderSurface2D
                    {
                        Scene = scene,
                        ViewBox = new DrawRect(-16f, -16f, 400f, 340f),
                        ClearColor = new Color(Background.Red, Background.Green, Background.Blue),
                        Stretch = DrawBrushStretch.Fill,
                        RedrawMode = RenderSurface2DRedrawMode.Continuous
                    };
                    surface.Resources.SetResource(atlasId, new ImageResource(originalPath));
                    ServoApi.SetId(surface, "production-character-frames");
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
                            var servo = new ServoApi(window, new ServoOptions { DefaultTimeout = TimeSpan.FromMinutes(2) });
                            ServoElement game = await servo.FindAsync(
                                ServoTarget.ById("production-character-frames"), deadline.Token);
                            var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
                            // This is a native rendering fixture, not simulated input. Its
                            // world-to-physical scale is calibrated from the arranged viewport.
                            surface.ViewBox = new DrawRect(-16f, -16f,
                                game.Bounds.Width * viewport.Scale / PixelsPerWorld,
                                game.Bounds.Height * viewport.Scale / PixelsPerWorld);
                            for (int direction = 0; direction < Directions.Length; direction++)
                            {
                                for (int frame = 0; frame < 4; frame++)
                                {
                                    Sprite2D player = poses[direction, frame];
                                    player.AnimationState = "Walk" + Directions[direction].Name;
                                    player.AnimationPlaybackRate = 1d;
                                    ((ITimeSensitiveRenderElement)surface).UpdateRenderTime(
                                        TimeSpan.FromMilliseconds(frame * 120));
                                    player.AnimationPlaybackRate = 0d;
                                }
                            }

                            await WaitForFramesAsync(window, 3, deadline.Token);
                            await servo.SaveScreenshotAsync(capturePath, deadline.Token);
                            AnalyzeProductionCharacterFrames(window, surface, game, poses, source, capturePath);
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

        Assert.True(started);
        Assert.Equal(0, exitCode);
        output.WriteLine($"App-owned production-frame capture: {capturePath}");
    }

    private void AnalyzeProductionCharacterFrames(
        Window window,
        RenderSurface2D surface,
        ServoElement game,
        Sprite2D[,] poses,
        SKBitmap source,
        string capturePath)
    {
        using SKBitmap? bitmap = SKBitmap.Decode(capturePath);
        Assert.NotNull(bitmap);
        var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
        DrawRect view = Assert.IsType<DrawRect>(surface.ViewBox);
        float scaleX = bitmap.Width / viewport.Width;
        float scaleY = bitmap.Height / viewport.Height;
        float ScreenX(float worldX) =>
            (game.Bounds.X + (worldX - view.X) / view.Width * game.Bounds.Width) * scaleX;
        float ScreenY(float worldY) =>
            (game.Bounds.Y + (worldY - view.Y) / view.Height * game.Bounds.Height) * scaleY;
        float physicalPerWorldX = ScreenX(1f) - ScreenX(0f);
        float physicalPerWorldY = ScreenY(1f) - ScreenY(0f);
        Assert.InRange(physicalPerWorldX, PixelsPerWorld - 0.01f, PixelsPerWorld + 0.01f);
        Assert.InRange(physicalPerWorldY, PixelsPerWorld - 0.01f, PixelsPerWorld + 0.01f);
        int pixelSize = (int)(SourceCellSize * PixelsPerWorld);
        int foreignTopPixels = 0;
        int renderedPoses = 0;

        for (int direction = 0; direction < Directions.Length; direction++)
        {
            for (int frame = 0; frame < 4; frame++)
            {
                Sprite2D player = poses[direction, frame];
                Assert.Equal("Walk" + Directions[direction].Name, player.AnimationState);
                float worldX = frame * GroupStep;
                float worldY = direction * GroupStep;
                float physicalX = ScreenX(worldX);
                float physicalY = ScreenY(worldY);
                int left = (int)MathF.Floor(physicalX);
                int top = (int)MathF.Floor(physicalY);
                Assert.InRange(left, 0, bitmap.Width - pixelSize);
                Assert.InRange(top, 0, bitmap.Height - pixelSize);
                Assert.InRange(physicalX - left, 0f, 0.01f);
                Assert.InRange(physicalY - top, 0f, 0.01f);

                int topSourceOpaque = 0;
                for (int sourceX = 0; sourceX < 32; sourceX++)
                {
                    if (source.GetPixel(frame * 32 + sourceX, Directions[direction].SourceRow * 32).Alpha > 0)
                    {
                        topSourceOpaque++;
                    }
                }

                Assert.Equal(frame is 0 or 2 ? 0 : 7, topSourceOpaque);
                int visiblePixels = 0;
                for (int y = 0; y < pixelSize; y++)
                {
                    for (int x = 0; x < pixelSize; x++)
                    {
                        if (!Close(bitmap.GetPixel(left + x, top + y), Background, 8))
                        {
                            visiblePixels++;
                        }
                    }
                }
                Assert.True(visiblePixels > 100,
                    $"Production {Directions[direction].Name} frame {frame} did not render source art.");
                renderedPoses++;

                int topDarkPixels = 0;
                for (int x = 0; x < pixelSize; x++)
                {
                    SKColor pixel = bitmap.GetPixel(left + x, top);
                    if (!Close(pixel, Background, 5))
                    {
                        topDarkPixels++;
                    }
                }

                if (topSourceOpaque == 0)
                {
                    foreignTopPixels += topDarkPixels;
                }
                else
                {
                    Assert.True(topDarkPixels >= 5,
                        $"Authored top-row hair disappeared from {Directions[direction].Name} frame {frame}.");
                }

                output.WriteLine($"Production {Directions[direction].Name} frame {frame}: " +
                    $"sourceTopOpaque={topSourceOpaque}, rendered={visiblePixels}, " +
                    $"topNonBackground={topDarkPixels}, sampling={player.Sampling}, " +
                    $"physicalOrigin=({physicalX:R},{physicalY:R}).");
            }
        }

        Assert.Equal(16, renderedPoses);
        output.WriteLine($"Production frame grid: foreign transparent-top pixels={foreignTopPixels}, " +
            $"physicalPerWorld={physicalPerWorldX:R}/{physicalPerWorldY:R}, " +
            $"screenshot={bitmap.Width}x{bitmap.Height}, ViewBox={view}.");
        Assert.True(foreignTopPixels == 0,
            $"Production character art includes {foreignTopPixels} foreign pixels on source-transparent frame top rows.");
    }

    private static Sprite2D FrameSprite(
        ImageReference image,
        float sourceX,
        float sourceY,
        float x,
        float y,
        DrawSamplingMode sampling,
        RenderSurface2DSpriteFlip flip) => new()
        {
            Image = image,
            X = x,
            Y = y,
            Width = SourceCellSize,
            Height = SourceCellSize,
            SourceX = sourceX,
            SourceY = sourceY,
            SourceWidth = SourceCellSize,
            SourceHeight = SourceCellSize,
            Sampling = sampling,
            Flip = flip
        };

    private static void WriteIsolatedFrameAtlas(string originalPath, string isolatedPath)
    {
        using SKBitmap? source = SKBitmap.Decode(originalPath);
        Assert.NotNull(source);
        Assert.Equal(128, source.Width);
        Assert.Equal(128, source.Height);
        using SKBitmap isolated = new(256, 256);
        isolated.Erase(SKColors.Transparent);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        isolated.SetPixel(16 + column * 64 + x, 16 + row * 64 + y,
                            source.GetPixel(column * 32 + x, row * 32 + y));
                    }
                }
            }
        }

        using SKImage image = SKImage.FromBitmap(isolated);
        using SKData data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(isolatedPath, data.ToArray());
        using SKBitmap? decoded = SKBitmap.Decode(isolatedPath);
        Assert.NotNull(decoded);
        for (int row = 0; row < 4; row++)
        {
            for (int column = 0; column < 4; column++)
            {
                for (int y = 0; y < 32; y++)
                {
                    for (int x = 0; x < 32; x++)
                    {
                        SKColor expected = source.GetPixel(column * 32 + x, row * 32 + y);
                        SKColor actual = decoded.GetPixel(16 + column * 64 + x, 16 + row * 64 + y);
                        Assert.Equal(expected.Alpha, actual.Alpha);
                        if (expected.Alpha > 0)
                        {
                            Assert.Equal(expected, actual);
                        }
                    }
                }
            }
        }
    }

    private void AnalyzeMatrix(Window window, RenderSurface2D surface, ServoElement game, string capturePath)
    {
        using SKBitmap? bitmap = SKBitmap.Decode(capturePath);
        Assert.NotNull(bitmap);
        var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
        DrawRect view = Assert.IsType<DrawRect>(surface.ViewBox);
        float scaleX = bitmap.Width / viewport.Width;
        float scaleY = bitmap.Height / viewport.Height;
        float ScreenX(float worldX) =>
            (game.Bounds.X + (worldX - view.X) / view.Width * game.Bounds.Width) * scaleX;
        float ScreenY(float worldY) =>
            (game.Bounds.Y + (worldY - view.Y) / view.Height * game.Bounds.Height) * scaleY;
        float physicalPerWorldX = ScreenX(1f) - ScreenX(0f);
        float physicalPerWorldY = ScreenY(1f) - ScreenY(0f);
        Assert.InRange(physicalPerWorldX, PixelsPerWorld - 0.01f, PixelsPerWorld + 0.01f);
        Assert.InRange(physicalPerWorldY, PixelsPerWorld - 0.01f, PixelsPerWorld + 0.01f);
        int pixelSize = (int)(SourceCellSize * PixelsPerWorld);
        int topBleedTotal = 0;
        int linearDifferenceTotal = 0;
        int pointDifferenceTotal = 0;

        for (int direction = 0; direction < Directions.Length; direction++)
        {
            for (int frame = 0; frame < 4; frame++)
            {
                float groupX = frame * GroupStep;
                float groupY = direction * GroupStep;
                int atlasX = (int)MathF.Floor(ScreenX(groupX));
                int atlasY = (int)MathF.Floor(ScreenY(groupY));
                int isolatedX = (int)MathF.Floor(ScreenX(groupX + VariantOffset));
                int pointY = (int)MathF.Floor(ScreenY(groupY + VariantOffset));
                Assert.Equal(90, isolatedX - atlasX);
                Assert.Equal(90, pointY - atlasY);
                Assert.InRange(atlasX, 0, bitmap.Width - pixelSize);
                Assert.InRange(atlasY, 0, bitmap.Height - pixelSize);
                Assert.InRange(isolatedX, 0, bitmap.Width - pixelSize);
                Assert.InRange(pointY, 0, bitmap.Height - pixelSize);

                int linearDifferences = 0;
                int pointDifferences = 0;
                int topBleed = 0;
                int visibleControlPixels = 0;
                for (int y = 0; y < pixelSize; y++)
                {
                    for (int x = 0; x < pixelSize; x++)
                    {
                        SKColor atlasLinear = bitmap.GetPixel(atlasX + x, atlasY + y);
                        SKColor isolatedLinear = bitmap.GetPixel(isolatedX + x, atlasY + y);
                        SKColor atlasPoint = bitmap.GetPixel(atlasX + x, pointY + y);
                        SKColor isolatedPoint = bitmap.GetPixel(isolatedX + x, pointY + y);
                        if (!Close(atlasLinear, isolatedLinear, 2))
                        {
                            linearDifferences++;
                        }
                        if (!Close(atlasPoint, isolatedPoint, 2))
                        {
                            pointDifferences++;
                        }
                        if (!Close(isolatedPoint, Background, 8))
                        {
                            visibleControlPixels++;
                        }
                        if (y < 5 && Close(isolatedLinear, Background, 5) &&
                            isolatedLinear.Red - atlasLinear.Red > 10 &&
                            isolatedLinear.Green - atlasLinear.Green > 10)
                        {
                            topBleed++;
                        }
                    }
                }

                Assert.True(visibleControlPixels > 100,
                    $"The isolated Point control for {Directions[direction].Name} frame {frame} did not render.");
                linearDifferenceTotal += linearDifferences;
                pointDifferenceTotal += pointDifferences;
                topBleedTotal += topBleed;
                output.WriteLine($"{Directions[direction].Name} frame {frame}: " +
                    $"atlas/extracted Linear diff={linearDifferences}, Point diff={pointDifferences}, " +
                    $"dark top bleed={topBleed}, opaque-control={visibleControlPixels}.");
            }
        }

        output.WriteLine($"Matrix totals: Linear diff={linearDifferenceTotal}, " +
            $"Point diff={pointDifferenceTotal}, dark top bleed={topBleedTotal}; " +
            $"physicalPerWorld={physicalPerWorldX:F4}/{physicalPerWorldY:F4}, " +
            $"screenshot={bitmap.Width}x{bitmap.Height}, ViewBox={view}.");
    }

    private async Task ReportSpriteCaptureAsync(
        ServoApi servo,
        MainWindow window,
        VillageGameSurface surface,
        Sprite2D player,
        string path,
        string direction,
        CancellationToken cancellationToken)
    {
        Assert.True(File.Exists(path));
        using SKBitmap? bitmap = SKBitmap.Decode(path);
        Assert.NotNull(bitmap);
        ServoElement game = await servo.FindAsync(ServoTarget.ById("character-game"), cancellationToken);
        var viewport = Assert.IsType<Cerneala.UI.Hosting.UiFrame>(window.LastFrame).Viewport;
        DrawRect view = Assert.IsType<DrawRect>(surface.ViewBox);
        float screenX = (game.Bounds.X + (player.X - view.X) / view.Width * game.Bounds.Width) * bitmap.Width / viewport.Width;
        float screenY = (game.Bounds.Y + (player.Y - view.Y) / view.Height * game.Bounds.Height) * bitmap.Height / viewport.Height;
        output.WriteLine($"Real Servo {direction}: state={player.AnimationState}, player=({player.X:F3},{player.Y:F3}), " +
            $"spritePhysicalOrigin=({screenX:F3},{screenY:F3}), ViewBox={view}, " +
            $"screenshot={bitmap.Width}x{bitmap.Height} at {path}.");
    }

    private static bool Close(SKColor a, SKColor b, int tolerance) =>
        Math.Abs(a.Red - b.Red) <= tolerance &&
        Math.Abs(a.Green - b.Green) <= tolerance &&
        Math.Abs(a.Blue - b.Blue) <= tolerance;

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

    private static string CreateCaptureDirectory(string name)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        string path = Path.Combine(directory.FullName, "artifacts", "ci", "scene-village", "screenshots",
            $"character-{name}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}");
        Directory.CreateDirectory(path);
        return path;
    }
}
