using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;
using Scene2D = global::Cerneala.UI.Controls.Scene2D;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeImageLeaseTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void GridCameraInputRetiresAtlasesAndKeepsNpcTerrainAcross32Cycles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-grid-residency-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string nearPath = Path.Combine(directory, "near.png"), farPath = Path.Combine(directory, "far.png");
        foreach ((string path, SKColor color) in new[] { (nearPath, SKColors.Red), (farPath, SKColors.Lime) })
        {
            using SKBitmap bitmap = new(1, 1);
            bitmap.SetPixel(0, 0, color);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, encoded.ToArray());
        }
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        ResourceId<ImageResource> nearId = new("Near"), farId = new("Far");
        TileColliderDescriptor2D collider = new(TileColliderShape2D.Box, width: 16, height: 16);
        TileMap2D map = new()
        {
            Source = TileMapSource2D.FromModel(new TileMap2DModel("terrain", new DrawSize(16, 16),
                [new TileSet2D("near", nearId, [new TileDefinition2D(1, new(0, 0, 1, 1), collider: collider),
                    new TileDefinition2D(3, new(0, 0, 1, 1))]),
                 new TileSet2D("far", farId, [new TileDefinition2D(2, new(0, 0, 1, 1), collider: collider)])],
                [new TileChunk2D(new(1, 1), 1, 1, [new TileCell2D(1)]),
                 new TileChunk2D(new(8, 1), 1, 1, [new TileCell2D(3)]),
                 new TileChunk2D(new(126, 1), 1, 1, [new TileCell2D(2)])]))
        };
        Sprite2D npc = new() { X = 1984, Y = 16, Width = 16, Height = 16, Collider = new BoxCollider2D { Width = 16, Height = 16 } };
        int npcLoads = 0, npcReleases = 0, moves = 0, camera = 0;
        SceneSpatialSource2D<object> source = new(Catalog(), (_, _) =>
        {
            npcLoads++;
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(npc, _ => npcReleases++));
        });
        SceneItems2D actors = new() { ItemsSource = source };
        Scene2D scene = new();
        scene.Children.Add(map);
        scene.Children.Add(actors);
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, ViewBox = new(0, 0, 128, 96), Scene = scene,
            Content = overlay, ClearColor = Color.Black, RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        AddButton("grid-camera", 0, () =>
        {
            camera = (camera + 1) % 3;
            surface.ViewBox = new(camera * 2000, 0, 128, 96);
        });
        AddButton("grid-step", 40, () =>
        {
            if (npc.X == 1984)
            {
                MoveCollisionResult2D blocked = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(100, 0));
                Assert.Same(map, blocked.Collision!.Entity);
                Assert.InRange(blocked.Travel.X, 15.9999f, 16);
            }
            MoveCollisionResult2D step = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(npc.X > 1984 ? -4 : 4, 0));
            Assert.Null(step.Collision);
            npc.X += step.Travel.X;
            source.SetEntries(Catalog());
            moves++;
        });
        AddButton("grid-warm", 80, () => surface.ViewBox = new(surface.ViewBox!.Value.X == 0 ? 16 : 0, 0, 128, 96));
        Window window = new() { Title = "Grid spatial residency input", Width = 128, Height = 96, Content = surface };
        window.Resources.SetResource(nearId, new ImageResource(nearPath));
        window.Resources.SetResource(farId, new ImageResource(farPath));
        try
        {
            runtime.Show(window, modal: false);
            byte[] reference = Capture(SKColors.Red);
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(window.Root!.ImageResourceCache);
            int initialLoads = cache.LoadCount;
            Assert.Equal(1, initialLoads);
            Assert.Equal(1, cache.ResidentCount);
            Assert.Equal(2, map.LogicalChildren.Count);
            ServoApi servo = new(window);
            for (int cycle = 0; cycle < 32; cycle++)
            {
                Assert.True(window.Root.Detective.CaptureTileMap(map).WarmChunks > 0);
                int beforeWarmPan = cache.LoadCount;
                Click("grid-warm");
                _ = Capture(SKColors.Black, SKColors.Red);
                Assert.Equal(0, window.Root.Detective.CaptureTileMap(map).BatchesRebuilt);
                Click("grid-warm");
                Assert.Equal(reference, Capture(SKColors.Red));
                Assert.Equal(0, window.Root.Detective.CaptureTileMap(map).BatchesRebuilt);
                Assert.Equal(beforeWarmPan, cache.LoadCount);
                Assert.InRange(window.Root.Detective.CaptureTileMap(map).WarmChargedBytes, 1, 1_048_576);
                SdlGpuImage previousNear = Inspect(nearPath);
                Click("grid-camera");
                _ = Capture(SKColors.Lime);
                Assert.Equal(1, cache.ResidentCount);
                Assert.Single(map.LogicalChildren);
                Assert.Throws<ObjectDisposedException>(() => previousNear.RgbaPixels);
                SdlGpuImage previousFar = Inspect(farPath);
                Click("grid-camera");
                _ = Capture(SKColors.Black);
                Assert.Equal(0, cache.ResidentCount);
                Assert.Single(map.LogicalChildren);
                Assert.Throws<ObjectDisposedException>(() => previousFar.RgbaPixels);
                int unloadedLoads = cache.LoadCount;
                Click("grid-step");
                Click("grid-step");
                Assert.Equal(1984, npc.X);
                Assert.Equal(unloadedLoads, cache.LoadCount);
                Assert.Equal(0, cache.ResidentCount);
                Assert.True(actors.TryGetRealizedNode("npc", out SceneNode2D? retainedNpc));
                Assert.Same(npc, retainedNpc);
                Assert.Equal(0, npcReleases);
                Click("grid-camera");
                Assert.Equal(reference, Capture(SKColors.Red));
                Assert.Equal(2, map.LogicalChildren.Count);
                Assert.Equal(1, cache.ResidentCount);
                Assert.Equal(initialLoads + (cycle + 1) * 2, cache.LoadCount);
            }
            Assert.Equal(1, npcLoads);
            Assert.Equal(64, moves);
            runtime.Close(window, force: true);
            Assert.Equal(1, npcReleases);
            Assert.Equal(0, cache.ResidentCount);
            Assert.Empty(map.LogicalChildren);

            SdlGpuImage Inspect(string path)
            {
                using ImageResourceLease inspection = cache.Acquire(new(path));
                return Assert.IsType<SdlGpuImage>(inspection.Image);
            }
            void Click(string id)
            {
                Task input = servo.ClickAsync(ServoTarget.ById(id));
                Assert.True(SpinWait.SpinUntil(() =>
                {
                    runtime.PumpOnce(TimeSpan.Zero);
                    return input.IsCompleted;
                }, TimeSpan.FromSeconds(10)), $"Input did not finish: {id}");
                Assert.True(input.IsCompletedSuccessfully, input.Exception?.ToString());
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            foreach (string name in new[] { "near.png", "far.png", "frame.png" }) { File.Delete(Path.Combine(directory, name)); }
            Directory.Delete(directory);
        }

        SceneSpatialEntry2D[] Catalog() =>
            [new("npc", new(npc.X, 16, 16, 16), new DrawRect(1980, 16, 64, 16), isSimulated: true)];
        void AddButton(string id, float left, Action action)
        {
            Button button = new() { Width = 32, Height = 24, Command = new ActionCommand(_ => action()) };
            ServoApi.SetId(button, id);
            Canvas.SetLeft(button, left);
            Canvas.SetTop(button, 64);
            overlay.VisualChildren.Add(button);
        }
        byte[] Capture(SKColor expected, SKColor? warmExpected = null)
        {
            Assert.True(SpinWait.SpinUntil(() =>
            {
                runtime.PumpOnce(TimeSpan.Zero);
                return surface.PresentationState == RenderSurface2DPresentationState.Ready;
            }, TimeSpan.FromSeconds(10)), surface.PresentationError?.ToString() ?? "Tile data/images did not become ready.");
            string path = Path.Combine(directory, "frame.png");
            SaveCommittedScreenshot(runtime, window, path);
            using SKBitmap bitmap = SKBitmap.Decode(path);
            Assert.Equal(expected, bitmap.GetPixel(20, 20));
            if (warmExpected.HasValue) { Assert.Equal(warmExpected.Value, bitmap.GetPixel(116, 20)); }
            return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
        }
    }

    [SdlNativeTheory]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Native")]
    public void SpatialCameraInputRetiresImagesAndStaticPayloadsButPreservesMovingNpcAcross32Cycles(bool invalidateBeforeCapture)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-spatial-leases-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string npcPath = Path.Combine(directory, "npc.png");
        string propPath = Path.Combine(directory, "prop.png");
        WriteImage(npcPath, SKColors.Red);
        WriteImage(propPath, SKColors.Lime);
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        ResourceId<ImageResource> npcImage = new("Npc");
        ResourceId<ImageResource> propImage = new("Prop");
        int npcLoads = 0, npcReleases = 0, propLoads = 0, propReleases = 0, moves = 0, invalidatedCaptures = 0;
        Sprite2D? npc = null;
        SceneSpatialSource2D<object> source = new(Catalog(), (entry, _) =>
        {
            if (entry.Id == "npc") { npcLoads++; } else { propLoads++; }
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(entry.Id, _ =>
            {
                if (entry.Id == "npc") { npcReleases++; } else { propReleases++; }
            }));
        });
        SceneItems2D items = new() { ItemsSource = source };
        items.Templates.Add(new ContentTemplate<string>("sprite", null, 0, context => new Sprite2D
        {
            X = context.Data == "npc" ? 16 : 48, Y = 16, Width = 16, Height = 16,
            Image = new ImageReference(context.Data == "npc" ? npcImage : propImage),
            Collider = context.Data == "npc" ? new BoxCollider2D { Width = 16, Height = 16 } : null
        }));
        Sprite2D wall = new() { X = 40, Y = 16, Collider = new BoxCollider2D { Width = 4, Height = 16 } };
        Scene2D scene = new();
        scene.Children.Add(items);
        scene.Children.Add(wall);
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, ViewBox = new DrawRect(0, 0, 128, 96),
            Scene = scene, Content = overlay, ClearColor = Color.Black,
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        bool away = false;
        AddButton("camera", 0, () =>
        {
            away = !away;
            surface.ViewBox = new DrawRect(away ? 1_000 : 0, 0, 128, 96);
        });
        AddButton("step", 40, () =>
        {
            Assert.NotNull(npc);
            bool returning = npc.X > 18;
            if (!returning)
            {
                MoveCollisionResult2D blocked = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(100, 0));
                Assert.NotNull(blocked.Collision);
                Assert.Same(wall.Collider, blocked.Collision.Collider);
                Assert.InRange(blocked.Travel.X, 7.99998f, 8);
            }
            // Move in free space. MoveAndCollide is a cast, not a contact-response solver.
            MoveCollisionResult2D move = scene.CollisionWorld.MoveAndCollide(npc.Collider!, new(returning ? -4 : 4, 0));
            Assert.Null(move.Collision);
            npc.X += move.Travel.X;
            source.SetEntries(Catalog());
            moves++;
        });
        Window window = new() { Title = "Spatial residency input", Width = 128, Height = 96, Content = surface };
        window.Resources.SetResource(npcImage, new ImageResource(npcPath));
        window.Resources.SetResource(propImage, new ImageResource(propPath));
        try
        {
            runtime.Show(window, modal: false);
            byte[] reference = Capture("visible.png", visible: true);
            Assert.True(items.TryGetRealizedNode("npc", out SceneNode2D? initialNpc));
            npc = Assert.IsType<Sprite2D>(initialNpc);
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(window.Root!.ImageResourceCache);
            Assert.Equal(2, cache.ResidentCount);
            int initialImageLoads = cache.LoadCount;
            ServoApi servo = new(window);
            for (int iteration = 0; iteration < 32; iteration++)
            {
                SdlGpuImage previous;
                using (ImageResourceLease inspected = cache.Acquire(new(npcPath)))
                {
                    previous = Assert.IsType<SdlGpuImage>(inspected.Image);
                }
                Click("camera");
                _ = Capture("away.png", visible: false);
                Assert.Equal(1, items.RealizedItemCount);
                Assert.True(items.TryGetRealizedNode("npc", out SceneNode2D? currentNpc));
                Assert.Same(initialNpc, currentNpc);
                Assert.True(npc.IsAttached);
                Assert.Equal(iteration + 1, propReleases);
                Assert.Equal(0, npcReleases);
                Assert.Equal(0, cache.ResidentCount);
                Assert.Throws<ObjectDisposedException>(() => previous.RgbaPixels);
                int awayLoads = cache.LoadCount;
                Click("step");
                Click("step");
                Assert.Equal((iteration + 1) * 2, moves);
                Assert.Equal(16, npc.X);
                Assert.Equal(awayLoads, cache.LoadCount);
                Assert.Equal(0, cache.ResidentCount);
                Click("camera");
                Assert.Equal(reference, Capture("visible.png", visible: true));
                Assert.Equal(2, items.RealizedItemCount);
                Assert.Equal(2, cache.ResidentCount);
                Assert.Equal(initialImageLoads + (iteration + 1) * 2, cache.LoadCount);
                Assert.Equal(iteration + 2, propLoads);
                Assert.Equal(1, npcLoads);
            }
            Assert.Equal(64, moves);
            Assert.Equal(invalidateBeforeCapture ? 65 : 0, invalidatedCaptures);
            runtime.Close(window, force: true);
            Assert.Equal(1, npcReleases);
            Assert.Equal(propLoads, propReleases);
            Assert.Equal(0, cache.ResidentCount);

            void Click(string id)
            {
                Task click = servo.ClickAsync(ServoTarget.ById(id));
                Assert.True(SpinWait.SpinUntil(() =>
                {
                    runtime.PumpOnce(TimeSpan.Zero);
                    return click.IsCompleted;
                }, TimeSpan.FromSeconds(10)), $"Input did not finish: {id}");
                Assert.True(click.IsCompletedSuccessfully, click.Exception?.ToString());
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            foreach (string name in new[] { "npc.png", "prop.png", "visible.png", "away.png" })
            {
                File.Delete(Path.Combine(directory, name));
            }
            Directory.Delete(directory);
        }

        SceneSpatialEntry2D[] Catalog() =>
        [
            new("npc", new DrawRect(npc?.X ?? 16, 16, 16, 16), isSimulated: true),
            new("prop", new DrawRect(48, 16, 16, 16), collisionBounds: null)
        ];

        void AddButton(string id, float left, Action action)
        {
            Button button = new() { Width = 32, Height = 24, Command = new ActionCommand(_ => action()) };
            ServoApi.SetId(button, id);
            Canvas.SetLeft(button, left);
            Canvas.SetTop(button, 64);
            overlay.VisualChildren.Add(button);
        }

        byte[] Capture(string name, bool visible)
        {
            Assert.True(SpinWait.SpinUntil(() =>
            {
                runtime.PumpOnce(TimeSpan.Zero);
                return surface.PresentationState == RenderSurface2DPresentationState.Ready;
            }, TimeSpan.FromSeconds(10)), surface.PresentationError?.ToString() ?? "Scene images did not become ready.");
            if (invalidateBeforeCapture)
            {
                // Frame-readiness fault injection, not simulated user input:
                // ready scene data does not imply a committed root command tree.
                window.Root!.Invalidate(InvalidationFlags.Render, "Capture boundary regression");
                Assert.Equal(RenderSurface2DPresentationState.Ready, surface.PresentationState);
                Assert.False(window.Root.RetainedRenderCache.IsRootValid);
                invalidatedCaptures++;
            }
            string path = Path.Combine(directory, name);
            SaveCommittedScreenshot(runtime, window, path);
            using SKBitmap bitmap = SKBitmap.Decode(path);
            Assert.Equal(visible ? SKColors.Red : SKColors.Black, bitmap.GetPixel(20, 20));
            Assert.Equal(visible ? SKColors.Lime : SKColors.Black, bitmap.GetPixel(52, 20));
            // Exclude the input overlay, whose hover/focus chrome legitimately changes.
            return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
        }

        static void WriteImage(string path, SKColor color)
        {
            using SKBitmap bitmap = new(1, 1);
            bitmap.SetPixel(0, 0, color);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(path, encoded.ToArray());
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void SharedAtlasSurvivesWindowCloseAnd32UnloadReloadCyclesPreservePixels()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-image-leases-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string imagePath = Path.Combine(directory, "atlas.png");
        using (SKBitmap bitmap = new(1, 1))
        {
            bitmap.SetPixel(0, 0, SKColors.Red);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(imagePath, encoded.ToArray());
        }
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        ResourceId<ImageResource> id = new("Atlas");
        Window first = CreateWindow("Image lease A");
        Window second = CreateWindow("Image lease B");
        try
        {
            runtime.Show(first, modal: false);
            runtime.Show(second, modal: false);
            _ = Capture(first, "first.png");
            byte[] reference = Capture(second, "before-close.png");
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(second.Root!.ImageResourceCache);
            Assert.Same(cache, first.Root!.ImageResourceCache);
            Assert.Equal(1, cache.ResidentCount);
            int initialLoads = cache.LoadCount;
            SdlGpuImage original;
            using (ImageResourceLease inspected = cache.Acquire(new(imagePath)))
            {
                original = Assert.IsType<SdlGpuImage>(inspected.Image);
            }

            runtime.Close(first, force: true);

            Assert.Equal(reference, Capture(second, "after-close.png"));
            Assert.Equal(1, cache.ResidentCount);
            Assert.Equal(initialLoads, cache.LoadCount);
            Assert.Equal(4, original.RgbaPixels.Length);
            for (int iteration = 0; iteration < 32; iteration++)
            {
                SdlGpuImage previous;
                using (ImageResourceLease inspected = cache.Acquire(new(imagePath)))
                {
                    previous = Assert.IsType<SdlGpuImage>(inspected.Image);
                }
                // This is resource-lifetime setup, not a claim of pointer/key interaction.
                second.Content = null;
                _ = Capture(second, "unloaded.png");
                Assert.Equal(0, cache.ResidentCount);
                Assert.Throws<ObjectDisposedException>(() => previous.RgbaPixels);

                second.Content = CreateImage();
                Assert.Equal(reference, Capture(second, "reloaded.png"));
                Assert.Equal(1, cache.ResidentCount);
                Assert.Equal(initialLoads + iteration + 1, cache.LoadCount);
            }
            runtime.Close(second, force: true);
            Assert.Equal(0, cache.ResidentCount);
        }
        finally
        {
            runtime.Close(first, force: true);
            runtime.Close(second, force: true);
            foreach (string name in new[] { "atlas.png", "first.png", "before-close.png", "after-close.png", "unloaded.png", "reloaded.png" })
            {
                File.Delete(Path.Combine(directory, name));
            }
            Directory.Delete(directory);
        }

        Image CreateImage() => new() { SourceResourceId = id, Width = 64, Height = 48 };

        Window CreateWindow(string title)
        {
            Window window = new() { Title = title, Width = 64, Height = 48, Content = CreateImage() };
            window.Resources.SetResource(id, new ImageResource(imagePath));
            return window;
        }

        byte[] Capture(Window window, string name)
        {
            runtime.PumpOnce(TimeSpan.Zero);
            if (window.Content is Image control)
            {
                Assert.True(SpinWait.SpinUntil(() =>
                {
                    runtime.PumpOnce(TimeSpan.Zero);
                    return control.LoadingState == ImageLoadingState.Ready;
                }, TimeSpan.FromSeconds(10)), control.LoadingError?.ToString() ?? "UI atlas did not become ready.");
            }
            string path = Path.Combine(directory, name);
            SaveCommittedScreenshot(runtime, window, path);
            using SKBitmap bitmap = SKBitmap.Decode(path);
            if (window.Content is Image)
            {
                Assert.Equal(SKColors.Red, bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2));
            }
            return bitmap.Bytes;
        }
    }
    private static void SaveCommittedScreenshot(WindowApplicationRuntime runtime, Window window, string path)
    {
        Task capture = new ServoApi(window).SaveScreenshotAsync(path);
        Assert.True(SpinWait.SpinUntil(() =>
        {
            if (!capture.IsCompleted) { runtime.PumpOnce(TimeSpan.Zero); }
            return capture.IsCompleted;
        }, TimeSpan.FromSeconds(10)), "The application-owned screenshot did not reach a committed frame.");
        capture.GetAwaiter().GetResult();
    }
}
