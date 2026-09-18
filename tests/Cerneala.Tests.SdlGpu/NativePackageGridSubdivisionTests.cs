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
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativePackageGridSubdivisionTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void PreparedGridMatchesAuthoredPixelsAcrossBoundariesFlipsAndZoom()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Cerneala-native-package-grid-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { Run(directory); }
        finally
        {
            string resolved = Path.GetFullPath(directory);
            Assert.Equal(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar), Path.GetDirectoryName(resolved));
            Assert.StartsWith("Cerneala-native-package-grid-", Path.GetFileName(resolved), StringComparison.Ordinal);
            Directory.Delete(resolved, recursive: true);
        }
    }

    private static void Run(string directory)
    {
        string atlasPath = Path.Combine(directory, "atlas.png");
        using (SKBitmap bitmap = new(32, 16))
        {
            for (int y = 0; y < bitmap.Height; y++)
            for (int x = 0; x < bitmap.Width; x++)
            {
                bitmap.SetPixel(x, y, new SKColor((byte)(x * 7), (byte)(y * 15), (byte)(255 - x * 5), (byte)(x % 4 == 0 ? 128 : 255)));
            }
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }
        TileSet2D set = new("set", new("atlas"), [new(1, new(0, 0, 16, 16)), new(2, new(16, 0, 16, 16))]);
        TileCell2D[] cells = Enumerable.Range(0, 33 * 19)
            .Select(index => new TileCell2D(index % 7 == 0 ? 0 : index % 2 + 1, (TileFlip2D)(index % 8))).ToArray();
        TileMap2DModel model = new("map", new(16, 16), [set], [new(new(-17, -9), 33, 19, cells)],
            offset: new(3, 5), opacity: .75f, tint: new Color(200, 180, 230, 220));
        Scene2DDocument document = new([new Scene2DLevel("level", [model])], [new(new("atlas"), "atlas.png", new(32, 16))]);
        string packageDirectory = Path.Combine(directory, "package");
        Scene2DPackageWriter.WriteAsync(packageDirectory, document, directory).GetAwaiter().GetResult();
        File.Delete(atlasPath); // Both references must use the autonomous package resource.
        using Scene2DPackage package = Scene2DPackage.OpenAsync(packageDirectory).GetAwaiter().GetResult();
        TileMapSource2D prepared = package.Levels[0].TileMaps[0], authored = TileMapSource2D.FromModel(model);
        Assert.Single(authored.Entries);
        Assert.Equal(6, prepared.Entries.Count);
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        TileMap2D map = new() { Source = authored };
        var scene = new global::Cerneala.UI.Controls.Scene2D();
        scene.Children.Add(map);
        DrawRect[] views = [new(-280, -150, 192, 160), new(-64, 64, 192, 160), new(-64, 64, 288, 240), new(210, 110, 288, 240), new(2000, 0, 192, 160)];
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 192, Height = 160, ViewBox = views[0], Scene = scene, Content = overlay,
            ClearColor = Color.Black, RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        int stage = 0;
        Button next = new() { Width = 80, Height = 24, Command = new ActionCommand(_ =>
        {
            stage++;
            map.Source = stage % 2 == 0 ? authored : prepared;
            surface.ViewBox = views[stage / 2];
        }) };
        ServoApi.SetId(next, "next-grid-view");
        // Keep the button's antialiased border outside the 192x128 scene crop.
        Canvas.SetTop(next, 136);
        overlay.VisualChildren.Add(next);
        Window window = new() { Title = "Grid subdivision conformance", Width = 192, Height = 160, Content = surface };
        ImageResource resource = new(package.GetFilePath("atlas.png"));
        window.Resources.SetResource(new ResourceId<ImageResource>("atlas"), resource);
        try
        {
            runtime.Show(window, modal: false);
            ServoApi servo = new(window);
            string screenshot = Path.Combine(directory, "frame.png");
            for (int view = 0; view < views.Length; view++)
            {
                byte[] reference = Capture(view == views.Length - 1);
                Advance();
                Assert.Equal(reference, Capture(view == views.Length - 1));
                if (view != views.Length - 1) { Advance(); }
            }
            Wait(() => window.Root!.ImageResourceCache!.ResidentCount == 0 && window.Root.ImageResourceCache.PendingLoadCount == 0);

            void Advance()
            {
                Task click = servo.ClickAsync(ServoTarget.ById("next-grid-view"));
                Wait(() => click.IsCompleted);
                click.GetAwaiter().GetResult();
            }
            byte[] Capture(bool empty)
            {
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
                Task capture = servo.SaveScreenshotAsync(screenshot);
                Wait(() => capture.IsCompleted);
                capture.GetAwaiter().GetResult();
                using SKBitmap bitmap = SKBitmap.Decode(screenshot);
                Assert.Equal(192, bitmap.Width);
                int colored = bitmap.Pixels.Take(bitmap.Width * 128).Count(pixel => pixel != SKColors.Black);
                if (empty) { Assert.Equal(0, colored); } else { Assert.True(colored > 10); }
                return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 128).ToArray();
            }
        }
        finally { runtime.Close(window, force: true); }

        void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            runtime.PumpOnce(TimeSpan.Zero);
            if (surface.PresentationError is { } error) { throw new InvalidOperationException("Grid package presentation failed.", error); }
            return done();
        }, TimeSpan.FromSeconds(10)), "Native grid package did not reach the required state.");
    }
}
