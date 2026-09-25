using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeSceneImagePresentationTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void ColdSpriteAndPrismSharePreparationAndReleaseImagesAcross32NativeCycles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-scene-image-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string atlasPath = Path.Combine(directory, "atlas.png"), framePath = Path.Combine(directory, "frame.png");
        using (SKBitmap bitmap = new(16, 16))
        {
            bitmap.Erase(SKColors.White);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }

        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        // Caller-owned fixture image, prepared before presentation begins. The
        // scene must withhold even this ready sibling while the atlas is cold.
        using SdlGpuImage borrowed = (SdlGpuImage)new SdlGpuImageLoader().Load(atlasPath);
        ResourceId<ImageResource> atlas = new("Atlas");
        Sprite2D npc = new()
        {
            X = 16, Y = 16, Width = 16, Height = 16, Tint = Color.Red,
            Collider = new BoxCollider2D { Width = 16, Height = 16, IsSimulated = true }
        };
        using IDisposable effect = GeneratedMarkup.AttachPrism(npc,
            () => new PrismInstance(new PrismCompositionDefinition("cold-atlas",
                [new PrismLayerDefinition(new PrismNodeId(1), "content",
                    filters: [new PrismFilterDefinition(PrismFilterId.Invert)],
                    mask: new(new PrismResourceId("Atlas")))])));
        int moves = 0;
        SceneItems2D actors = new() { ItemsSource = new[] { npc } };
        Scene2D scene = new();
        scene.Children.Add(actors);
        scene.Children.Add(new Sprite2D { X = 80, Y = 16, Width = 16, Height = 16, Image = new(borrowed), Tint = Color.Blue });
        Sprite2D wall = new() { X = 40, Y = 16, Collider = new BoxCollider2D { Width = 4, Height = 16 } };
        scene.Children.Add(wall);
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, ViewBox = new(1000, 0, 128, 96), Scene = scene,
            Content = overlay, ClearColor = Color.Black, RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        DelayedSdlImageLoader loader = new();
        AddButton("begin", 0, () =>
        {
            loader.Begin();
            npc.Image = new(atlas);
            surface.ViewBox = new(0, 0, 128, 96);
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
        AddButton("complete", 64, loader.Complete);
        AddButton("reset", 96, () =>
        {
            npc.Image = null;
            surface.ViewBox = new(1000, 0, 128, 96);
        });
        Window window = new() { Title = "Scene atlas preparation input", Width = 128, Height = 96, Content = surface };
        window.Resources.SetResource(atlas, new ImageResource(atlasPath));
        try
        {
            runtime.Show(window, modal: false);
            window.Root!.SetImageLoader(loader);
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(window.Root.ImageResourceCache);
            ServoApi servo = new(window);
            Wait(() => actors.RealizedItemCount == 1 && surface.PresentationState == RenderSurface2DPresentationState.Ready);
            byte[] empty = Capture(SKColors.Black, SKColors.Black);
            byte[]? reference = null;
            for (int cycle = 0; cycle < 32; cycle++)
            {
                Click("begin");
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Loading);
                Assert.Equal(empty, Capture(SKColors.Black, SKColors.Black));
                MoveTwice();
                Assert.Equal(RenderSurface2DPresentationState.Loading, surface.PresentationState);
                Assert.Equal(cycle + 1, loader.AsyncLoads);
                Assert.Equal(0, loader.SyncLoads);
                Click("complete");
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
                byte[] ready = Capture(SKColors.Cyan, SKColors.Blue);
                reference ??= ready;
                Assert.Equal(reference, ready);
                Assert.Equal(1, cache.ResidentCount);
                SdlGpuImage previous;
                using (ImageResourceLease inspection = cache.Acquire(new ImageResource(atlasPath)))
                {
                    previous = Assert.IsType<SdlGpuImage>(inspection.Image);
                }
                Click("reset");
                Wait(() => cache.ResidentCount == 0 && surface.PresentationState == RenderSurface2DPresentationState.Ready);
                Assert.Equal(empty, Capture(SKColors.Black, SKColors.Black));
                Assert.Throws<ObjectDisposedException>(() => previous.RgbaPixels);
                MoveTwice(); // Same NPC/collider while its entire region is off-camera.
                Assert.Equal(empty, Capture(SKColors.Black, SKColors.Black));
            }

            Click("begin");
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Loading);
            IOException failure = new("native scene atlas unavailable");
            loader.Fail(failure); // External completion, not simulated user input.
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Error);
            Assert.Same(failure, surface.PresentationError);
            Assert.Equal(empty, Capture(SKColors.Black, SKColors.Black));
            Click("reset");
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
            Assert.Null(surface.PresentationError);
            Assert.Equal(128, moves);
            Assert.Equal(33, loader.AsyncLoads);
            Assert.Equal(0, loader.SyncLoads);
            runtime.Close(window, force: true);
            Assert.False(npc.IsAttached);
            Assert.Equal(0, cache.ResidentCount);
            Assert.Equal(0, cache.PendingLoadCount);

            void Click(string id)
            {
                Task click = servo.ClickAsync(ServoTarget.ById(id));
                Wait(() => click.IsCompleted);
                Assert.True(click.IsCompletedSuccessfully, click.Exception?.ToString());
            }
            void MoveTwice()
            {
                Click("move");
                Click("move");
                Assert.Equal(16, npc.X);
                Assert.Same(npc, Assert.Single(actors.LogicalChildren));
                Assert.True(npc.IsAttached && npc.IsVisible && npc.Collider!.Enabled);
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            loader.Cancel();
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
        }, TimeSpan.FromSeconds(10)), "Native scene image did not reach the required state.");
        byte[] Capture(SKColor npcColor, SKColor siblingColor)
        {
            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(framePath);
            using SKBitmap bitmap = SKBitmap.Decode(framePath);
            Assert.Equal(npcColor, bitmap.GetPixel(24, 24));
            Assert.Equal(siblingColor, bitmap.GetPixel(84, 24));
            return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
        }
    }
}
