using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.UI.Servo;
using SkiaSharp;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeUiImagePresentationTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void SharedPendingUiAtlasKeepsInputResponsiveAndPreservesPixelsAcross32Cycles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-native-ui-image-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string atlasPath = Path.Combine(directory, "atlas.png"), framePath = Path.Combine(directory, "frame.png");
        using (SKBitmap bitmap = new(16, 8))
        {
            bitmap.Erase(SKColors.Red);
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }

        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        ResourceId<ImageResource> atlas = new("Atlas");
        Image fixedImage = new() { Width = 32, Height = 16 }, naturalImage = new();
        Canvas overlay = new();
        Canvas.SetLeft(fixedImage, 8);
        Canvas.SetTop(fixedImage, 8);
        Canvas.SetLeft(naturalImage, 64);
        Canvas.SetTop(naturalImage, 8);
        overlay.VisualChildren.Add(fixedImage);
        overlay.VisualChildren.Add(naturalImage);
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, Content = overlay, Scene = new Cerneala.UI.Controls.Scene2D(), ClearColor = Color.Black,
            RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        DelayedSdlImageLoader loader = new();
        int inputs = 0;
        AddButton("begin", 0, () =>
        {
            loader.Begin();
            fixedImage.SourceResourceId = atlas;
            naturalImage.SourceResourceId = atlas;
        });
        AddButton("complete", 32, loader.Complete);
        AddButton("reset", 64, () =>
        {
            fixedImage.SourceResourceId = null;
            naturalImage.SourceResourceId = null;
        });
        AddButton("ping", 96, () => inputs++);
        Window window = new() { Title = "Asynchronous UI image input", Width = 128, Height = 96, Content = surface };
        window.Resources.SetResource(atlas, new ImageResource(atlasPath));
        try
        {
            runtime.Show(window, modal: false);
            window.Root!.SetImageLoader(loader);
            ImageResourceCache cache = Assert.IsType<ImageResourceCache>(window.Root.ImageResourceCache);
            ServoApi servo = new(window);
            byte[] empty = Capture(SKColors.Black);
            byte[]? reference = null;
            for (int cycle = 0; cycle < 32; cycle++)
            {
                Click("begin");
                Wait(() => Both(ImageLoadingState.Loading));
                Assert.Equal(new LayoutSize(32, 16), fixedImage.DesiredSize);
                Assert.Equal(LayoutSize.Zero, naturalImage.DesiredSize);
                Assert.True(fixedImage.IsAttached && fixedImage.IsEnabled && fixedImage.IsVisible);
                Assert.Equal(empty, Capture(SKColors.Black));
                Click("ping");
                Assert.Equal(cycle * 2 + 1, inputs);
                Assert.True(Both(ImageLoadingState.Loading));
                Assert.Equal(cycle + 1, loader.AsyncLoads);
                Assert.Equal(0, loader.SyncLoads);

                Click("complete");
                Wait(() => Both(ImageLoadingState.Ready));
                Assert.Equal(new LayoutSize(16, 8), naturalImage.DesiredSize);
                byte[] ready = Capture(SKColors.Red);
                reference ??= ready;
                Assert.Equal(reference, ready);
                Assert.Equal(1, cache.ResidentCount);
                SdlGpuImage previous;
                using (ImageResourceLease inspection = cache.Acquire(new ImageResource(atlasPath)))
                {
                    previous = Assert.IsType<SdlGpuImage>(inspection.Image);
                }
                Click("ping");
                Assert.Equal((cycle + 1) * 2, inputs);
                Click("reset");
                Wait(() => cache.ResidentCount == 0 && Both(ImageLoadingState.Ready));
                Assert.Equal(empty, Capture(SKColors.Black));
                Assert.Throws<ObjectDisposedException>(() => previous.RgbaPixels);
            }

            Click("begin");
            Wait(() => Both(ImageLoadingState.Loading));
            IOException failure = new("native UI atlas unavailable");
            loader.Fail(failure); // An external loader result, not simulated user input.
            Wait(() => Both(ImageLoadingState.Error));
            Assert.Same(failure, fixedImage.LoadingError);
            Assert.Same(failure, naturalImage.LoadingError);
            Assert.Equal(empty, Capture(SKColors.Black));
            Click("ping");
            Assert.Equal(65, inputs);
            Click("reset");
            Wait(() => Both(ImageLoadingState.Ready));
            Assert.Null(fixedImage.LoadingError);
            Assert.Null(naturalImage.LoadingError);
            Assert.Equal(33, loader.AsyncLoads);
            Assert.Equal(0, loader.SyncLoads);
            runtime.Close(window, force: true);
            Assert.Equal(0, cache.ResidentCount);
            Assert.Equal(0, cache.PendingLoadCount);

            bool Both(ImageLoadingState state) => fixedImage.LoadingState == state && naturalImage.LoadingState == state;
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
        }, TimeSpan.FromSeconds(10)), "Native UI image did not reach the required state.");
        byte[] Capture(SKColor expected)
        {
            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(framePath);
            using SKBitmap bitmap = SKBitmap.Decode(framePath);
            Assert.Equal(expected, bitmap.GetPixel(16, 16));
            Assert.Equal(expected, bitmap.GetPixel(68, 12));
            return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
        }
    }

}
