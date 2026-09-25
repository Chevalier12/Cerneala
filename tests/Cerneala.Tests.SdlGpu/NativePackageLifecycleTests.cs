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
public sealed class NativePackageLifecycleTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void CameraCancelsParkedPackageChunkAndRetryLoadsItAgain()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-package-lifecycle-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        Exception? failure = null;
        try { Run(directory); }
        catch (Exception error) { failure = error; throw; }
        finally
        {
            try
            {
                string parent = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string resolved = Path.GetFullPath(directory);
                if (!resolved.StartsWith(parent, StringComparison.OrdinalIgnoreCase) ||
                    !Path.GetFileName(resolved).StartsWith("Cerneala-native-package-lifecycle-", StringComparison.Ordinal))
                { throw new InvalidOperationException("Unsafe native fixture cleanup target."); }
                Directory.Delete(resolved, recursive: true);
            }
            catch (Exception cleanupError)
            {
                if (failure is not null) { throw new AggregateException(failure, cleanupError); }
                throw;
            }
        }
    }

    private static void Run(string directory)
    {
        string packagePath = WritePackage(directory);
        TileMap2D packageMap = null!, map = null!;
        SdlGpuWindowGraphicsSessionFactory graphics = null!;
        SdlWindowPlatform platform = null!;
        WindowApplicationRuntime runtime = null!;
        SceneGraph2D scene = null!;
        Canvas overlay = null!;
        RenderSurface2D surface = null!;
        Window window = null!;
        TaskCompletionSource<bool> parked = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken firstToken = default;
        int attempts = 0, releases = 0;
        string screenshot = Path.Combine(directory, "frame.png");
        Exception? testFailure = null;
        Scene2DPackage package = Scene2DPackage.OpenAsync(packagePath).GetAwaiter().GetResult();
        try
        {
            packageMap = package.Levels[0].CreateTileMap(Assert.Single(package.Levels[0].TileMapIds));
            TileMapSource2D original = packageMap.Source!;
            Assert.Single(original.Catalog.Chunks);
            map = TileMap2D.FromSource(new TileMapSource2D(original.Catalog, async (_, info, token) =>
            {
                SceneSpatialLease2D<TileMapChunkData2D> packageLease = await original.LoadAsync(info.Spatial, token).ConfigureAwait(false);
                try
                {
                    if (Interlocked.Increment(ref attempts) == 1)
                    {
                        firstToken = token;
                        parked.TrySetResult(true);
                        // Model a package payload that completes after the camera has
                        // canceled its interest; residency must retire this late lease.
                        await releaseFirst.Task.ConfigureAwait(false);
                    }
                    return new SceneSpatialLease2D<TileMapChunkData2D>(packageLease.Value, _ =>
                    {
                        packageLease.Dispose();
                        Interlocked.Increment(ref releases);
                    });
                }
                catch
                {
                    packageLease.Dispose();
                    throw;
                }
            }));
            NativeSdlApi api = new();
            graphics = new(api, useMultisampling: false);
            platform = new(api, graphics, coordinateScaleOverride: 1);
            runtime = new(platform);
            scene = new();
            scene.Children.Add(map);
            overlay = new();
            surface = new()
            {
                Width = 128, Height = 96, ViewBox = new(10_000, 0, 128, 96), Scene = scene,
                Content = overlay, ClearColor = Color.Black, RedrawMode = RenderSurface2DRedrawMode.OnDemand
            };
            AddButton("near", 0, 0);
            AddButton("empty", 40, 10_000);
            window = new() { Title = "Package cancellation and retry", Width = 128, Height = 96, Content = surface };
            Scene2DAsset atlas = Assert.Single(package.Assets);
            window.Resources.SetResource(atlas.ResourceId, new ImageResource(package.GetFilePath(atlas.Path)));
            runtime.Show(window, modal: false);
            ServoApi servo = new(window);
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(window.Root!.ImageResourceCache);
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
            Capture(SKColors.Black);

            Click("near");
            Wait(() => parked.Task.IsCompleted && surface.PresentationState == RenderSurface2DPresentationState.Loading);
            Assert.Equal(1, Volatile.Read(ref attempts));
            Click("empty");
            Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready &&
                map.Preparation.IsCompletedSuccessfully && firstToken.IsCancellationRequested);
            Assert.False(releaseFirst.Task.IsCompleted);
            Capture(SKColors.Black);
            Assert.Equal(0, cache.ResidentCount);

            releaseFirst.TrySetResult(true);
            Wait(() => Volatile.Read(ref releases) == 1 && map.GetDiagnosticsSnapshot().PendingDataChunks == 0);
            Assert.Equal(0, map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Equal(0, map.GetDiagnosticsSnapshot().RetainedObjects);
            Assert.Equal(0, map.GetDiagnosticsSnapshot().WarmChargedBytes);
            Assert.Empty(map.LogicalChildren);
            Assert.Equal(0, cache.ResidentCount);
            Assert.Equal(0, cache.PendingLoadCount);

            Click("near");
            Wait(() => Volatile.Read(ref attempts) == 2 &&
                map.Preparation.IsCompletedSuccessfully && surface.PresentationState == RenderSurface2DPresentationState.Ready);
            Capture(SKColors.White);
            Assert.Equal(1, map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Equal(1, cache.ResidentCount);

            Click("empty");
            Wait(() => Volatile.Read(ref releases) == 2 && cache.ResidentCount == 0 && cache.PendingLoadCount == 0);
            Capture(SKColors.Black);
            Assert.Equal(0, map.GetDiagnosticsSnapshot().ResidentDataChunks);
            Assert.Equal(0, map.GetDiagnosticsSnapshot().RetainedObjects);
            Assert.Empty(map.LogicalChildren);

            void Click(string id)
            {
                Task click = servo.ClickAsync(ServoTarget.ById(id));
                Wait(() => click.IsCompleted);
                Assert.True(click.IsCompletedSuccessfully, click.Exception?.ToString());
            }
            void Capture(SKColor expected)
            {
                runtime.PumpOnce(TimeSpan.Zero);
                window.SaveScreenshot(screenshot);
                using SKBitmap bitmap = SKBitmap.Decode(screenshot);
                Assert.Equal(expected, bitmap.GetPixel(24, 24));
            }
        }
        catch (Exception error) { testFailure = error; throw; }
        finally
        {
            releaseFirst.TrySetResult(true);
            List<Exception> cleanupFailures = [];
            if (runtime is not null && window is not null) { Attempt(() => runtime.Close(window, force: true)); }
            if (scene is not null && map is not null) { Attempt(() => scene.Children.Remove(map)); }
            if (map is not null) { Attempt(() => map.DisposeAsync().AsTask().GetAwaiter().GetResult()); }
            if (packageMap is not null) { Attempt(() => packageMap.DisposeAsync().AsTask().GetAwaiter().GetResult()); }
            Attempt(() => package.DisposeAsync().AsTask().GetAwaiter().GetResult());
            if (runtime is not null) { Attempt(runtime.Dispose); }
            if (platform is not null) { Attempt(platform.Dispose); }
            if (graphics is not null) { Attempt(graphics.Dispose); }
            if (cleanupFailures.Count > 0)
            {
                if (testFailure is not null) { cleanupFailures.Insert(0, testFailure); }
                throw new AggregateException("Native package lifecycle teardown failed.", cleanupFailures);
            }

            void Attempt(Action cleanup)
            {
                try { cleanup(); }
                catch (Exception error) { cleanupFailures.Add(error); }
            }
        }

        void AddButton(string id, float x, float camera)
        {
            Button button = new() { Width = 36, Height = 24, Command = new ActionCommand(_ => surface.ViewBox = new(camera, 0, 128, 96)) };
            ServoApi.SetId(button, id);
            Canvas.SetLeft(button, x);
            Canvas.SetTop(button, 64);
            overlay.VisualChildren.Add(button);
        }
        void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            runtime.PumpOnce(TimeSpan.Zero);
            if (surface.PresentationError is { } error) { throw new InvalidOperationException("Package presentation failed.", error); }
            return done();
        }, TimeSpan.FromSeconds(15)), "Native package lifecycle did not reach the required state.");
    }

    private static string WritePackage(string directory)
    {
        string input = Path.Combine(directory, "input");
        Directory.CreateDirectory(input);
        string atlasPath = Path.Combine(input, "atlas.png");
        using (SKBitmap bitmap = new(16, 16))
        {
            bitmap.Erase(SKColors.White);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }
        ResourceId<ImageResource> id = new("atlas");
        TileSet2D set = new("set", id, [new TileDefinition2D(1, new(0, 0, 16, 16))]);
        TileMap2DModel model = new("map", new DrawSize(16, 16), [set],
            [new TileChunk2D(new(1, 1), 1, 1, [new TileCell2D(1)])]);
        Scene2DDocument document = new([new Scene2DLevel("level", [model])],
            [new Scene2DAsset(id, "atlas.png", new(16, 16))]);
        string destination = Path.Combine(directory, "package");
        Scene2DPackageWriter.WriteAsync(destination, document, input).GetAwaiter().GetResult();
        File.Delete(atlasPath); // The runtime must use the built package asset.
        return destination;
    }
}
