using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeScenePresentationTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void TileMapLoadingAndErrorWithholdTheWholeSceneWhileUiAndNpcMovementContinueAcross32Cycles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-scene-preparation-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string atlasPath = Path.Combine(directory, "atlas.png"), framePath = Path.Combine(directory, "frame.png");
        using (SKBitmap bitmap = new(1, 1))
        {
            bitmap.SetPixel(0, 0, SKColors.Red);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }

        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        ResourceId<ImageResource> atlas = new("Atlas");
        ImageReference picture = new(atlas);
        Sprite2D npc = new()
        {
            X = 16, Y = 16, Width = 16, Height = 16, Image = new(atlas),
            Collider = new BoxCollider2D { Width = 16, Height = 16, IsSimulated = true }
        };
        int coldReleases = 0, moves = 0;
        TaskCompletionSource<SceneSpatialLease2D<TileMapChunkData2D>>? pending = null;
        TileMapSource2D coldSource = new(new TileMapCatalog2D("cold", []), (_, _, _) => new(pending!.Task));
        SceneItems2D actors = new() { ItemsSource = new[] { npc } };
        TileMap2D cold = TileMap2D.FromSource(coldSource);
        Scene2D scene = new();
        scene.Children.Add(actors);
        scene.Children.Add(cold);
        Sprite2D wall = new() { X = 40, Y = 16, Collider = new BoxCollider2D { Width = 4, Height = 16 } };
        scene.Children.Add(wall);
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, ViewBox = new(0, 0, 128, 96), Scene = scene,
            Content = overlay, ClearColor = Color.Black, RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        AddButton("begin", 0, () =>
        {
            pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            coldSource.SetCatalog(new TileMapCatalog2D("cold", [new TileMapChunkInfo2D(
                new SceneSpatialEntry2D("cold", new(80, 16, 16, 16), collisionBounds: null), 1, [picture])]));
        });
        AddButton("move", 32, () =>
        {
            if (npc.X == 16)
            {
                MoveCollisionResult2D blocked = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(100, 0));
                Assert.Same(wall.Collider, blocked.Collision!.Collider);
                Assert.InRange(blocked.Travel.X, 7.9999f, 8);
            }
            MoveCollisionResult2D step = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(npc.X > 16 ? -4 : 4, 0));
            Assert.Null(step.Collision);
            npc.X += step.Travel.X;
            moves++;
        });
        AddButton("complete", 64, () => pending!.SetResult(new SceneSpatialLease2D<TileMapChunkData2D>(
            new TileMapChunkData2D([new Tile(picture, x: 80, y: 16, width: 16, height: 16)]), _ => coldReleases++)));
        AddButton("reset", 96, () => coldSource.SetCatalog(new TileMapCatalog2D("cold", [])));
        Window window = new() { Title = "Scene preparation input", Width = 128, Height = 96, Content = surface };
        window.Resources.SetResource(atlas, new ImageResource(atlasPath));
        try
        {
            runtime.Show(window, modal: false);
            ServoApi servo = new(window);
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready && actors.RealizedItemCount == 1);
            byte[] reference = Capture(SKColors.Red, SKColors.Black);
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(window.Root!.ImageResourceCache);
            for (int cycle = 0; cycle < 32; cycle++)
            {
                Click("begin");
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Loading);
                _ = Capture(SKColors.Black, SKColors.Black);
                Click("move");
                Click("move");
                Assert.Equal(16, npc.X);
                Assert.Same(npc, Assert.Single(actors.LogicalChildren));
                Assert.True(npc.IsAttached);
                Assert.True(npc.IsVisible);
                Assert.True(npc.Collider!.Enabled);
                _ = Capture(SKColors.Black, SKColors.Black);
                Click("complete");
                Wait(() => cold.Preparation.IsCompletedSuccessfully && surface.PresentationState == RenderSurface2DPresentationState.Ready);
                _ = Capture(SKColors.Red, SKColors.Red);
                Click("reset");
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready && cold.LogicalChildren.Count == 0);
                Assert.Equal(reference, Capture(SKColors.Red, SKColors.Black));
                Assert.Equal(cycle + 1, coldReleases);
            }
            Click("begin");
            IOException failure = new("native deterministic load failure");
            pending!.SetException(failure); // Loader completion is test setup, not simulated UI input.
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Error);
            Assert.Same(failure, surface.PresentationError);
            Assert.Same(failure, cold.PreparationError);
            _ = Capture(SKColors.Black, SKColors.Black);
            Click("reset");
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
            Assert.Equal(reference, Capture(SKColors.Red, SKColors.Black));
            Assert.Equal(64, moves);
            Assert.Equal(1, cache.LoadCount);
            runtime.Close(window, force: true);
            Assert.False(npc.IsAttached);
            Assert.Equal(0, cache.ResidentCount);

            void Click(string id)
            {
                Task click = servo.ClickAsync(ServoTarget.ById(id));
                Wait(() => click.IsCompleted);
                Assert.True(click.IsCompletedSuccessfully, click.Exception?.ToString());
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            pending?.TrySetCanceled();
            File.Delete(atlasPath);
            File.Delete(framePath);
            Directory.Delete(directory);
        }

        void AddButton(string id, float x, Action action)
        {
            Button button = new() { Width = 28, Height = 24, Command = new ActionCommand(_ => action()) };
            ServoApi.SetId(button, id);
            Canvas.SetLeft(button, x);
            Canvas.SetTop(button, 64);
            overlay.VisualChildren.Add(button);
        }
        void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            runtime.PumpOnce(TimeSpan.Zero);
            return done();
        }, TimeSpan.FromSeconds(10)), "Native scene presentation did not reach the required state.");
        byte[] Capture(SKColor npcColor, SKColor coldColor)
        {
            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(framePath);
            using SKBitmap bitmap = SKBitmap.Decode(framePath);
            Assert.Equal(npcColor, bitmap.GetPixel(24, 24));
            Assert.Equal(coldColor, bitmap.GetPixel(84, 24));
            return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
        }
    }
}
