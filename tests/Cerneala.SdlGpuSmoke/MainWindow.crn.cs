using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.SmokeTests;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Servo;
using System.Numerics;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.SdlGpuSmoke;

public partial class MainWindow : Window
{
    private static readonly PrismNodeId SmokeLayerId = new(1);

    private IDisposable? prismLifetime;
    private Window? secondaryWindow;
    private int mainFrames;
    private int secondaryFrames;
    private bool inputObserved;
    private bool servoActivated;
    private bool completed;
    private RenderSurface2D? tileMapSurface;
    private TileMapStage5ConformanceFixture? tileMapConformance;
    private CollisionStageFiveFixture? collisionFixture;
    private SpriteAnimationConformanceFixture? animationFixture;
    private SceneDebugOverlayConformanceFixture? debugFixture;
    private RenderSurface3DConformanceFixture? surface3DFixture;
    private bool surface3DInputComplete;
    private int surface3DCaptureFrame;
    private int surface3DCaptures;

    private void OnContentRendered(object? sender, EventArgs args)
    {
        SmokeOptions options = SmokeOptions.Current;
        StatusText.Text = $"mode: {options.Mode}";

        if (options.Mode == "rendersurface3d")
        {
            Width = 800;
            Height = 600;
            surface3DFixture = RenderSurface3DConformanceFixture.Create();
            Content = surface3DFixture.Surface;
            prismLifetime = GeneratedMarkup.AttachPrism(surface3DFixture.Surface, () =>
                new PrismInstance(new PrismCompositionDefinition("RenderSurface3DSmoke",
                    [new PrismLayerDefinition(new PrismNodeId(1), "SurfaceImage",
                        filters: [new PrismFilterDefinition(PrismFilterId.Invert)])])));
            _ = RunRenderSurface3DInputAsync(options);
        }

        if (options.Mode == "scene-debug")
        {
            Width = 640;
            Height = 420;
            debugFixture = new SceneDebugOverlayConformanceFixture(options.ArtifactDirectory);
            Content = debugFixture.Surface;
        }

        if (options.Mode == "sprite-animation")
        {
            Width = 640;
            Height = 420;
            animationFixture = new SpriteAnimationConformanceFixture(options.ArtifactDirectory);
            Content = animationFixture.Surface;
        }

        if (options.Mode == "tilemap")
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            tileMapSurface = TileMapStage3CaptureFixture.CreateSurface(options.ArtifactDirectory);
            Content = tileMapSurface;
        }

        if (options.Mode == "tilemap-conformance")
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            Width = 640;
            Height = 420;
            Background = new SolidColorBrush(new Color(8, 16, 24));
            tileMapConformance = TileMapStage5ConformanceFixture.Create(
                options.ArtifactDirectory);
            Content = tileMapConformance.Surface;
        }

        if (options.Mode == "collision")
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            Width = 640;
            Height = 400;
            Background = new SolidColorBrush(new Color(8, 16, 24));
            collisionFixture = CollisionStageFiveFixture.Create(
                options.ArtifactDirectory);
            Content = collisionFixture.Surface;
        }

        if (options.Mode == "prism")
        {
            PrismInstance prism = new(new PrismCompositionDefinition(
                "SdlGpuSmoke",
                [new PrismLayerDefinition(
                    SmokeLayerId,
                    "SmokeTarget",
                    styles: [new PrismStyleDefinition(PrismStyleId.OuterGlow)])]));
            prismLifetime = GeneratedMarkup.AttachPrism(PrismTarget, () => prism);
            PrismTarget.Invalidate(InvalidationFlags.Render, "SDL_GPU smoke Prism attachment");
        }

        if (options.Mode == "multi-window")
        {
            SmokeDrawingSurface secondarySurface = new();
            secondaryWindow = new Window
            {
                Title = "Cerneala SDL_GPU smoke secondary",
                Width = 420,
                Height = 300,
                Left = 80,
                Top = 80,
                Background = new SolidColorBrush(new Color(22, 41, 57)),
                Content = secondarySurface
            };
            secondaryWindow.FrameRendered += (_, _) => secondaryFrames++;
            secondaryWindow.Show();
        }

        if (options.Mode == "servo")
        {
            _ = RunServoAsync(options);
        }
    }

    private void OnServoClick(UiElementId sender, RoutedEventArgs args)
    {
        servoActivated = true;
        ServoButton.Content = "SERVO ACTIVE";
        StatusText.Text = "servo: activated";
    }

    private void OnPreviewKeyDown(UiElementId sender, RoutedEventArgs args)
    {
        inputObserved = true;
        StatusText.Text = args is KeyEventArgs key
            ? $"input: {key.Key}"
            : "input observed";
    }

    private void OnFrameRendered(object? sender, EventArgs args)
    {
        if (completed)
        {
            return;
        }

        mainFrames++;
        SmokeOptions options = SmokeOptions.Current;
        if (options.Mode == "rendersurface3d")
        {
            if (!surface3DInputComplete) return;
            surface3DCaptureFrame++;
            if (surface3DCaptureFrame < 3)
            {
                surface3DFixture!.Surface.InvalidateFrame();
                return;
            }
            surface3DCaptureFrame = 0;
            if (options.CaptureScreenshots)
            {
                Directory.CreateDirectory(options.ArtifactDirectory);
                SaveScreenshot(Path.Combine(options.ArtifactDirectory, $"rendersurface3d-direction-{surface3DCaptures:D2}.png"));
            }
            surface3DCaptures++;
            if (surface3DCaptures < 8)
            {
                surface3DFixture!.SetPresetDirection(surface3DCaptures);
                return;
            }
            completed = true;
            Console.WriteLine($"SDL_GPU_SMOKE_OK mode=rendersurface3d captures={surface3DCaptures} " +
                $"orbit={surface3DFixture!.OrbitCount} pan={surface3DFixture.PanCount} " +
                $"zoom={surface3DFixture.ZoomCount} selected={surface3DFixture.SelectedJoint} " +
                $"pose={surface3DFixture.PoseIndex} draws={surface3DFixture.DrawCount}");
            Close();
            return;
        }
        if (options.Mode == "scene-debug")
        {
            if (mainFrames < 4 || mainFrames % 4 != 0) { return; }
            int sample = mainFrames / 4 - 1;
            debugFixture!.VerifySample(sample);
            SaveScreenshot(Path.Combine(options.ArtifactDirectory, SceneDebugOverlayConformanceFixture.CaptureNames[sample]));
            if (sample + 1 < SceneDebugOverlayConformanceFixture.CaptureNames.Length) { debugFixture.SelectSample(sample + 1); }
            else
            {
                completed = true;
                debugFixture.VerifyCaptures("SDL_GPU");
                Console.WriteLine("SDL_GPU_SMOKE_OK mode=scene-debug flags=7 effects=Aspect,Motion,Prism");
                Close();
            }
            return;
        }
        if (options.Mode == "sprite-animation")
        {
            if (mainFrames == 3) animationFixture!.Prepare();
            if (mainFrames < 4 || mainFrames % 4 != 0) return;
            int sample = mainFrames / 4 - 1;
            animationFixture!.VerifySample(sample);
            SaveScreenshot(Path.Combine(options.ArtifactDirectory, SpriteAnimationConformanceFixture.CaptureNames[sample]));
            if (sample < 3) animationFixture.Advance(sample + 1);
            else
            {
                completed = true;
                SpriteAnimationConformanceFixture.VerifyCaptures(options.ArtifactDirectory, "SDL_GPU");
                Console.WriteLine("SDL_GPU_SMOKE_OK mode=sprite-animation samples=0,100,200,300");
                Close();
            }
            return;
        }
        if (options.Mode == "servo")
        {
            return;
        }

        if (options.Mode == "tilemap")
        {
            if (mainFrames < 3)
            {
                return;
            }
            if (mainFrames == 3)
            {
                if (options.CaptureScreenshots)
                {
                    SaveScreenshot(Path.Combine(options.ArtifactDirectory, "tilemap-before.png"));
                }
                tileMapSurface!.ViewBox = new DrawRect(16, 0, 64, 16);
                return;
            }
            if (mainFrames < 6)
            {
                return;
            }

            completed = true;
            if (options.CaptureScreenshots)
            {
                string beforePath = Path.Combine(options.ArtifactDirectory, "tilemap-before.png");
                string afterPath = Path.Combine(options.ArtifactDirectory, "tilemap-after.png");
                SaveScreenshot(afterPath);
                TileMapStage3CaptureFixture.VerifyPanCaptures(
                    beforePath,
                    afterPath,
                    "SDL_GPU");
            }
            Console.WriteLine($"SDL_GPU_SMOKE_OK mode=tilemap mainFrames={mainFrames}");
            Close();
            return;
        }

        if (options.Mode == "tilemap-conformance")
        {
            if (mainFrames < 4)
            {
                return;
            }
            if (mainFrames == 4)
            {
                if (options.CaptureScreenshots)
                {
                    SaveScreenshot(Path.Combine(
                        options.ArtifactDirectory,
                        TileMapStage5ConformanceFixture.InitialCaptureName));
                }
                tileMapConformance!.StartMotion();
                return;
            }
            if (mainFrames < 8)
            {
                return;
            }
            if (mainFrames == 8)
            {
                if (options.CaptureScreenshots)
                {
                    SaveScreenshot(Path.Combine(
                        options.ArtifactDirectory,
                        TileMapStage5ConformanceFixture.MotionCaptureName));
                }
                tileMapConformance!.PanAndZoom();
                return;
            }
            if (mainFrames < 12)
            {
                return;
            }

            completed = true;
            if (options.CaptureScreenshots)
            {
                SaveScreenshot(Path.Combine(
                    options.ArtifactDirectory,
                    TileMapStage5ConformanceFixture.PanZoomCaptureName));
                TileMapStage5ConformanceFixture.VerifyCaptures(
                    options.ArtifactDirectory,
                    "SDL_GPU");
            }
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=tilemap-conformance mainFrames={mainFrames}");
            Close();
            return;
        }

        if (options.Mode == "collision")
        {
            if (mainFrames < 4)
            {
                return;
            }
            if (mainFrames == 4)
            {
                collisionFixture!.VerifyClosedContract();
                collisionFixture.VerifyCoordinateRoundTrip();
                if (options.CaptureScreenshots)
                {
                    SaveScreenshot(Path.Combine(
                        options.ArtifactDirectory,
                        CollisionStageFiveFixture.ClosedCaptureName));
                }
                collisionFixture.OpenDoor();
                return;
            }
            if (mainFrames < 8)
            {
                return;
            }

            completed = true;
            collisionFixture!.VerifyOpenContract();
            if (options.CaptureScreenshots)
            {
                SaveScreenshot(Path.Combine(
                    options.ArtifactDirectory,
                    CollisionStageFiveFixture.OpenCaptureName));
                CollisionStageFiveFixture.VerifyCaptures(
                    options.ArtifactDirectory,
                    "SDL_GPU");
            }
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=collision mainFrames={mainFrames}");
            Close();
            return;
        }

        if (options.Mode == "resize" && mainFrames == 1)
        {
            Width = 720;
            Height = 460;
            return;
        }

        if (options.Mode == "multi-window" && secondaryFrames == 0)
        {
            return;
        }

        if (options.Mode == "input" && options.RequireInput && !inputObserved)
        {
            if (mainFrames < 600)
            {
                return;
            }

            Console.Error.WriteLine("SDL_GPU_SMOKE_FAIL input was required but no key event arrived.");
            completed = true;
            Application.Current?.Shutdown(2);
            return;
        }

        if (mainFrames < 2)
        {
            return;
        }

        completed = true;
        Directory.CreateDirectory(options.ArtifactDirectory);
        if (options.CaptureScreenshots)
        {
            SaveScreenshot(Path.Combine(options.ArtifactDirectory, $"{options.Mode}-main.png"));
            secondaryWindow?.SaveScreenshot(
                Path.Combine(options.ArtifactDirectory, $"{options.Mode}-secondary.png"));
        }

        Console.WriteLine(
            $"SDL_GPU_SMOKE_OK mode={options.Mode} mainFrames={mainFrames} " +
            $"secondaryFrames={secondaryFrames} inputObserved={inputObserved}");
        secondaryWindow?.Close();
        Close();
    }

    private async Task RunServoAsync(SmokeOptions options)
    {
        string fullPath = Path.Combine(options.ArtifactDirectory, "servo-main.png");
        string targetPath = Path.Combine(options.ArtifactDirectory, "servo-target.png");
        string errorPath = Path.Combine(options.ArtifactDirectory, "servo.error.txt");
        try
        {
            Directory.CreateDirectory(options.ArtifactDirectory);
            ServoApi servo = new(this);
            ServoTarget button = ServoTarget.ById("servo-target");
            _ = await servo.FindAsync(button);
            await servo.ClickAsync(button);
            await servo.WaitUntilAsync(async token =>
                (await servo.FindAsync(ServoTarget.ById("servo-status"), token)).Name == "servo: activated");
            if (!servoActivated)
            {
                throw new InvalidOperationException("Servo input completed without the routed click handler running.");
            }

            if (options.CaptureScreenshots)
            {
                await servo.SaveScreenshotAsync(fullPath);
                await servo.SaveScreenshotAsync(button, targetPath);
            }

            completed = true;
            Console.WriteLine(
                $"SDL_GPU_SMOKE_OK mode=servo state={StatusText.Text} " +
                $"full={fullPath} target={targetPath}");
            Close();
        }
        catch (Exception exception)
        {
            completed = true;
            Directory.CreateDirectory(options.ArtifactDirectory);
            await File.WriteAllTextAsync(errorPath, exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL servo: {exception}");
            Application.Current?.Shutdown(2);
        }
    }

    private async Task RunRenderSurface3DInputAsync(SmokeOptions options)
    {
        try
        {
            ServoApi servo = new(this);
            ServoTarget surfaceTarget = ServoTarget.ById("3d-surface");
            ServoElement element = await servo.FindAsync(surfaceTarget);
            float centerX = element.Bounds.X + element.Bounds.Width / 2;
            float centerY = element.Bounds.Y + element.Bounds.Height / 2;
            RenderSurface3DConformanceFixture fixture = surface3DFixture!;
            await servo.ClickAsync(surfaceTarget);
            if (fixture.SelectedJoint < 0) throw new InvalidOperationException("Surface click did not select a visible joint.");
            await servo.DragAsync(surfaceTarget, new ServoPoint(centerX + 72, centerY + 28));
            if (fixture.OrbitCount == 0) throw new InvalidOperationException("Orbit drag did not reach the controller.");
            int orbitBeforeOverlay = fixture.OrbitCount;
            await servo.ClickAsync(ServoTarget.ById("3d-pan"));
            if (fixture.OrbitCount != orbitBeforeOverlay) throw new InvalidOperationException("Overlay click started orbit.");
            Matrix4x4 beforePan = fixture.Surface.ViewMatrix;
            await servo.DragAsync(surfaceTarget, new ServoPoint(centerX + 40, centerY + 18));
            if (fixture.Surface.ViewMatrix == beforePan) throw new InvalidOperationException("Pan input did not move the camera.");
            fixture.SetPresetDirection(0);
            await servo.ClickAsync(ServoTarget.ById("3d-projection"));
            if (!fixture.Surface.TryWorldToRoot(fixture.JointPositions[6], out Vector2 leftBefore) ||
                !fixture.Surface.TryWorldToRoot(fixture.JointPositions[10], out Vector2 rightBefore))
                throw new InvalidOperationException("Orthographic sample joints were not visible before wheel input.");
            await servo.ScrollAsync(surfaceTarget, 120);
            if (!fixture.Surface.TryWorldToRoot(fixture.JointPositions[6], out Vector2 leftAfter) ||
                !fixture.Surface.TryWorldToRoot(fixture.JointPositions[10], out Vector2 rightAfter) ||
                Vector2.Distance(leftAfter, rightAfter) <= Vector2.Distance(leftBefore, rightBefore) + .1f)
                throw new InvalidOperationException("Orthographic wheel input did not enlarge the projected skeleton.");
            await servo.ClickAsync(ServoTarget.ById("3d-pose"));
            if (fixture.PanCount == 0 || fixture.ZoomCount == 0 || fixture.PoseIndex != 1 ||
                fixture.Surface.Projection.Kind != RenderProjection3DKind.Orthographic || fixture.SelectedJoint < 0)
                throw new InvalidOperationException($"3D input mismatch: pan={fixture.PanCount}, zoom={fixture.ZoomCount}, " +
                    $"projection={fixture.Surface.Projection.Kind}, pose={fixture.PoseIndex}, selected={fixture.SelectedJoint}.");
            fixture.SetPresetDirection(0);
            surface3DInputComplete = true;
        }
        catch (Exception exception)
        {
            completed = true;
            Directory.CreateDirectory(options.ArtifactDirectory);
            await File.WriteAllTextAsync(Path.Combine(options.ArtifactDirectory, "rendersurface3d.error.txt"), exception.ToString());
            Console.Error.WriteLine($"SDL_GPU_SMOKE_FAIL rendersurface3d: {exception}");
            Application.Current?.Shutdown(2);
        }
    }

    protected override void OnDetached()
    {
        debugFixture?.Dispose();
        debugFixture = null;
        animationFixture?.Dispose();
        animationFixture = null;
        tileMapConformance?.Dispose();
        tileMapConformance = null;
        prismLifetime?.Dispose();
        prismLifetime = null;
        base.OnDetached();
    }
}
