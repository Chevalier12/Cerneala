using System.Collections.Concurrent;
using System.Numerics;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.Scene2D.Packages;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using SceneGraph2D = Cerneala.UI.Controls.Scene2D;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativePackageWarmStreamingTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void GridPackagePreloadsWithinBudgetAndReleasesDataAndImages() => Run(free: false, oversized: false);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void PlacementPackagePreloadsWithinBudgetAndReleasesDataAndImages() => Run(free: true, oversized: false);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void GridPackageBudgetRejectsOptionalButNotRequiredData() => Run(free: false, oversized: true);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void PlacementPackageBudgetRejectsOptionalButNotRequiredData() => Run(free: true, oversized: true);

    private static void Run(bool free, bool oversized)
    {
        string directory = Path.Combine(Path.GetTempPath(), "Cerneala-native-package-warm-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        Exception? runFailure = null;
        try { RunOwned(directory, free, oversized); }
        catch (Exception error) { runFailure = error; throw; }
        finally
        {
            try
            {
                string parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string resolved = Path.GetFullPath(directory);
                if (!resolved.StartsWith(parent, StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(resolved).StartsWith("Cerneala-native-package-warm-", StringComparison.Ordinal))
                { throw new InvalidOperationException("Unsafe native fixture cleanup target."); }
                Directory.Delete(resolved, recursive: true);
            }
            catch (Exception cleanupFailure)
            {
                if (runFailure is not null) { throw new AggregateException(runFailure, cleanupFailure); }
                throw;
            }
        }
    }

    private static void RunOwned(string directory, bool free, bool oversized)
    {
        string packagePath = WritePackage(directory, free, oversized);
        using Scene2DPackage package = Scene2DPackage.OpenAsync(packagePath).GetAwaiter().GetResult();
        TileMap2D packageMap = package.Levels[0].CreateTileMap(Assert.Single(package.Levels[0].TileMapIds));
        TileMapSource2D original = packageMap.Source!;
        TileMapChunkInfo2D near = original.Catalog.Chunks[0], far = original.Catalog.Chunks[1];
        Assert.Equal(2, original.Catalog.Chunks.Count);
        Assert.Equal(oversized, far.DataResidencyBytes > TileMap2D.WarmCacheBudgetBytes);
        ConcurrentDictionary<string, int> loads = new(StringComparer.Ordinal);
        ConcurrentQueue<WeakReference> payloads = new();
        int released = 0;
        TileMap2D map = TileMap2D.FromSource(new TileMapSource2D(original.Catalog, async (_, info, cancellation) =>
            {
                var lease = await original.LoadAsync(info.Spatial, cancellation).ConfigureAwait(false);
                loads.AddOrUpdate(info.Spatial.Id, 1, static (_, count) => count + 1);
                payloads.Enqueue(new(lease.Value));
                return new SceneSpatialLease2D<TileMapChunkData2D>(lease.Value, _ =>
                {
                    lease.Dispose();
                    Interlocked.Increment(ref released);
                });
            }));
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        SceneGraph2D scene = new();
        scene.Children.Add(map);
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, ViewBox = new(10_000, 0, 128, 96), Scene = scene,
            Content = overlay, ClearColor = Color.Black, RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        AddButton("near", 0, 0);
        AddButton("far", 40, 128);
        AddButton("empty", 80, 10_000);
        Window window = new() { Title = "Disk package warm streaming", Width = 128, Height = 96, Content = surface };
        Scene2DAsset atlas = Assert.Single(package.Assets);
        ImageResource resource = new(package.GetFilePath(atlas.Path));
        window.Resources.SetResource(atlas.ResourceId, resource);
        CountingLoader loader = new();
        string screenshot = Path.Combine(directory, "frame.png");
        Exception? testFailure = null;
        try
        {
            runtime.Show(window, modal: false);
            window.Root!.SetImageLoader(loader);
            ImageResourceCache cache = window.Root.ImageResourceCache!;
            ServoApi servo = new(window);
            WaitReady();
            byte[] empty = Capture(SKColors.Black);
            byte[]? reference = null;
            for (int cycle = 0; cycle < 8; cycle++)
            {
                Click("near");
                if (!oversized) { Wait(() => map.GetDiagnosticsSnapshot().WarmChunks == 1); }
                else { for (int frame = 0; frame < 20; frame++) { runtime.PumpOnce(TimeSpan.Zero); } }
                Assert.Equal(cycle + 1, Count(near));
                Assert.Equal(oversized ? cycle : cycle + 1, Count(far));
                var state = window.Root.Detective.CaptureTileMap(map);
                Assert.Equal(free ? 256 : 1, state.DrawnTiles);
                Assert.Equal(oversized ? 0 : far.DataResidencyBytes, map.GetDiagnosticsSnapshot().WarmDataBytes);
                Assert.InRange(state.WarmChargedBytes, 0, TileMap2D.WarmCacheBudgetBytes);
                byte[] first = Capture(SKColors.White);
                reference ??= first;
                Assert.Equal(reference, first);
                // Optional render preparation must not claim collision coverage.
                Assert.Throws<SceneCollisionRegionNotReadyException>(() => scene.CollisionWorld.Raycast(new(148, 0), Vector2.UnitY, 40));

                Click("far");
                Assert.Equal(cycle + 1, Count(far));
                Assert.Equal(reference, Capture(SKColors.White));
                Assert.NotEmpty(scene.CollisionWorld.Raycast(new(148, 0), Vector2.UnitY, 40));
                Assert.InRange(map.GetDiagnosticsSnapshot().WarmChargedBytes, 0, TileMap2D.WarmCacheBudgetBytes);
                Assert.Equal(1, cache.ResidentCount);
                SdlGpuImage retired;
                using (var inspection = cache.Acquire(resource)) { retired = Assert.IsType<SdlGpuImage>(inspection.Image); }
                Click("empty");
                Wait(() => cache.ResidentCount == 0 && cache.PendingLoadCount == 0 && released == loads.Values.Sum());
                Assert.Equal(empty, Capture(SKColors.Black));
                Assert.Throws<ObjectDisposedException>(() => retired.RgbaPixels);
                Assert.Equal(0, map.GetDiagnosticsSnapshot().WarmChargedBytes);
                Assert.Equal(0, map.GetDiagnosticsSnapshot().RetainedObjects);
                Assert.Empty(map.LogicalChildren);
            }
            Assert.Equal(8, loader.AsyncLoads);
            Assert.Equal(0, loader.SyncLoads);

            Task<SceneCollisionRegion2D> request = scene.CollisionWorld.PrepareRegionAsync(new(140, 0, 40, 40)).AsTask();
            Wait(() => request.IsCompleted);
            using (SceneCollisionRegion2D region = request.GetAwaiter().GetResult())
            {
                Assert.NotEmpty(scene.CollisionWorld.Raycast(new(148, 0), Vector2.UnitY, 40));
                Assert.Equal(0, cache.ResidentCount);
                Assert.Equal(8, loader.AsyncLoads);
                Assert.Equal(empty, Capture(SKColors.Black));
            }
            Wait(() => released == loads.Values.Sum() && map.LogicalChildren.Count == 0);
            for (int collect = 0; collect < 3; collect++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
            Assert.All(payloads, weak => Assert.False(weak.IsAlive));
            GC.KeepAlive(original);
            GC.KeepAlive(package);

            void Click(string id)
            {
                Task click = servo.ClickAsync(ServoTarget.ById(id));
                Wait(() => click.IsCompleted);
                click.GetAwaiter().GetResult();
                WaitReady();
            }

            byte[] Capture(SKColor expected)
            {
                // Scene readiness is not a committed root frame. Use the
                // window-backed capture operation to synchronize that boundary.
                Task capture = servo.SaveScreenshotAsync(screenshot);
                Wait(() => capture.IsCompleted);
                capture.GetAwaiter().GetResult();
                using SKBitmap bitmap = SKBitmap.Decode(screenshot);
                Assert.Equal(expected, bitmap.GetPixel(24, 24));
                return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
            }
        }
        catch (Exception error) { testFailure = error; throw; }
        finally
        {
            List<Exception> cleanupFailures = [];
            Attempt(() => runtime.Close(window, force: true));
            // Close does not remove this map from the scene's logical collection.
            // Retire that parent relationship on the owner thread before terminal disposal.
            Attempt(() => scene.Children.Remove(map));
            Attempt(() => map.DisposeAsync().AsTask().GetAwaiter().GetResult());
            Attempt(() => packageMap.DisposeAsync().AsTask().GetAwaiter().GetResult());
            Attempt(() => package.DisposeAsync().AsTask().GetAwaiter().GetResult());
            if (cleanupFailures.Count > 0)
            {
                if (testFailure is not null) { cleanupFailures.Insert(0, testFailure); }
                throw new AggregateException("Native package teardown failed.", cleanupFailures);
            }

            void Attempt(Action cleanup)
            {
                try { cleanup(); }
                catch (Exception error) { cleanupFailures.Add(error); }
            }
        }

        int Count(TileMapChunkInfo2D info) => loads.GetValueOrDefault(info.Spatial.Id);
        void AddButton(string id, float x, float camera)
        {
            Button button = new() { Width = 36, Height = 24, Command = new ActionCommand(_ => surface.ViewBox = new(camera, 0, 128, 96)) };
            ServoApi.SetId(button, id);
            Canvas.SetLeft(button, x);
            Canvas.SetTop(button, 64);
            overlay.VisualChildren.Add(button);
        }
        void WaitReady() => Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
        void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            runtime.PumpOnce(TimeSpan.Zero);
            if (surface.PresentationError is { } error) { throw new InvalidOperationException("Package presentation failed.", error); }
            return done();
        }, TimeSpan.FromSeconds(10)), "Native package did not reach the required state.");
    }

    private static string WritePackage(string directory, bool free, bool oversized)
    {
        string input = Path.Combine(directory, "input");
        Directory.CreateDirectory(input);
        string atlas = Path.Combine(input, "atlas.png");
        using (SKBitmap bitmap = new(16, 16))
        {
            bitmap.Erase(SKColors.White);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlas, encoded.ToArray());
        }
        TileColliderDescriptor2D Collider(int bytes) => new(TileColliderShape2D.Box, width: 16, height: 16,
            properties: new Dictionary<string, object?> { ["bulk"] = new byte[bytes] });
        TileColliderDescriptor2D near = Collider(65_536), far = Collider(oversized ? 2_097_152 : 65_536);
        TileMap2DModel model;
        if (free)
        {
            ImageReference image = new(new ResourceId<ImageResource>("atlas"));
            Tile[] tiles = Enumerable.Range(0, 256).Select(_ => new Tile(image, near, 16, 16, 16, 16))
                .Append(new(image, far, 144, 16, 16, 16)).ToArray();
            model = new(tiles);
        }
        else
        {
            TileSet2D set = new("set", new("atlas"), [new(1, new(0, 0, 16, 16), collider: near), new(2, new(0, 0, 16, 16), collider: far)]);
            model = new("map", new(16, 16), [set], [new(new(1, 1), 1, 1, [new(1)]), new(new(9, 1), 1, 1, [new(2)])]);
        }
        string destination = Path.Combine(directory, "package");
        Scene2DDocument document = new([new Scene2DLevel("level", [model])], [new(new("atlas"), "atlas.png", new(16, 16))]);
        Scene2DPackageWriter.WriteAsync(destination, document, input).GetAwaiter().GetResult();
        File.Delete(atlas); // Runtime cannot fall back to the import-side file.
        return destination;
    }

    private sealed class CountingLoader : IAsyncImageLoader
    {
        private readonly SdlGpuImageLoader inner = new();
        internal int AsyncLoads;
        internal int SyncLoads;
        public ValueTask<IDrawImage> LoadAsync(string path, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref AsyncLoads);
            return inner.LoadAsync(path, cancellationToken);
        }
        public IDrawImage Load(string path)
        {
            Interlocked.Increment(ref SyncLoads);
            return inner.Load(path);
        }
    }
}
