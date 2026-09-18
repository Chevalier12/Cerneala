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
using SkiaSharp;
using SceneGraph2D = Cerneala.UI.Controls.Scene2D;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeScenePrismStreamingTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void PointwiseSceneAndMapStreamingMatchEagerPixelsAcrossCameraAndFilterChanges()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-pointwise-streaming-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string atlasPath = Path.Combine(directory, "atlas.png");
        string framePath = Path.Combine(directory, "frame.png");
        using (SKBitmap bitmap = new(16, 16))
        {
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bitmap.SetPixel(x, y, new((byte)(x * 17), (byte)(y * 17), 120, 255));
                }
            }
            using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(atlasPath, encoded.ToArray());
        }

        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: 1);
        using WindowApplicationRuntime runtime = new(platform);
        using SdlGpuImage image = (SdlGpuImage)new SdlGpuImageLoader().Load(atlasPath);
        ImageReference picture = new(image);
        SceneSpatialEntry2D[] entries = [new("near", new(16, 16, 16, 16), null), new("far", new(1016, 16, 16, 16), null)];
        List<string> itemLoads = [], itemReleases = [], mapLoads = [], mapReleases = [];
        SceneItems2D items = new() { ItemsSource = new SceneSpatialSource2D<object>(entries, (entry, _) =>
        {
            itemLoads.Add(entry.Id);
            return ValueTask.FromResult(new SceneSpatialLease2D<object>(Sprite(entry), _ => itemReleases.Add(entry.Id)));
        }) };
        TileMapCatalog2D catalog = new("native-pointwise", entries.Select(entry => new TileMapChunkInfo2D(entry, 1, [picture])));
        TileMap2D map = new() { Source = new(catalog, (_, info, _) =>
        {
            SceneSpatialEntry2D entry = info.Spatial;
            mapLoads.Add(entry.Id);
            return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(
                new([new Tile(picture, x: entry.Bounds.X, y: entry.Bounds.Y, width: 16, height: 16)]),
                _ => mapReleases.Add(entry.Id)));
        }) };
        SceneGraph2D eager = new(), actors = new(), terrain = new();
        foreach (SceneSpatialEntry2D entry in entries) { eager.Children.Add(Sprite(entry)); }
        actors.Children.Add(items);
        terrain.Children.Add(map);
        SceneGraph2D[] scenes = [eager, actors, terrain];
        PrismFilterId[] filters = [PrismFilterId.Invert, PrismFilterId.Levels, PrismFilterId.Posterize];
        int sceneIndex = 0, filterIndex = 0;
        bool far = false;
        using IDisposable eagerEffect = Effect(eager);
        using IDisposable actorsEffect = Effect(actors);
        using IDisposable terrainEffect = Effect(terrain);
        Canvas overlay = new();
        RenderSurface2D surface = new()
        {
            Width = 128, Height = 96, ViewBox = new(0, 0, 128, 96), Scene = eager,
            Content = overlay, ClearColor = new(30, 40, 50), RedrawMode = RenderSurface2DRedrawMode.OnDemand
        };
        AddButton("mode", 0, () => { sceneIndex = (sceneIndex + 1) % scenes.Length; surface.Scene = scenes[sceneIndex]; });
        AddButton("camera", 40, () => { far = !far; surface.ViewBox = new(far ? 1000 : 0, 0, 128, 96); });
        AddButton("filter", 80, () =>
        {
            filterIndex = (filterIndex + 1) % filters.Length;
            surface.Scene = null;
            surface.Scene = scenes[sceneIndex];
        });
        Window window = new() { Title = "Pointwise scene streaming parity", Width = 128, Height = 96, Content = surface };
        try
        {
            runtime.Show(window, modal: false);
            ServoApi servo = new(window);
            WaitReady();
            for (int filter = 0; filter < filters.Length; filter++)
            {
                for (int camera = 0; camera < 2; camera++)
                {
                    byte[] reference = Capture();
                    string expected = far ? "far" : "near";
                    int beforeItems = itemLoads.Count, beforeMaps = mapLoads.Count;
                    Click("mode");
                    Assert.Equal(1, items.RealizedItemCount);
                    Assert.Equal(beforeItems + 1, itemLoads.Count);
                    Assert.Equal(expected, itemLoads[^1]);
                    Assert.Equal(reference, Capture());
                    Click("mode");
                    Assert.Equal(itemLoads.Count, itemReleases.Count);
                    Assert.Equal(beforeMaps + 1, mapLoads.Count);
                    Assert.Equal(expected, mapLoads[^1]);
                    Assert.Equal(reference, Capture());
                    Click("mode");
                    Assert.Equal(mapLoads.Count, mapReleases.Count);
                    Click("camera");
                }
                Click("filter");
            }
            Assert.Equal(6, itemLoads.Count);
            Assert.Equal(6, mapLoads.Count);
            Assert.Equal(itemLoads, itemReleases);
            Assert.Equal(mapLoads, mapReleases);

            void Click(string id)
            {
                Task click = servo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById(id));
                Wait(() => click.IsCompleted);
                Assert.True(click.IsCompletedSuccessfully, click.Exception?.ToString());
                WaitReady();
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            File.Delete(atlasPath);
            File.Delete(framePath);
            Directory.Delete(directory);
        }

        Sprite2D Sprite(SceneSpatialEntry2D entry) => new()
        { X = entry.Bounds.X, Y = entry.Bounds.Y, Width = 16, Height = 16, Image = picture };
        IDisposable Effect(SceneGraph2D scene) => GeneratedMarkup.AttachPrism(scene, () =>
        {
            PrismInstance instance = new(new("Pointwise", [new PrismLayerDefinition(new(1), "Adjustment",
                filters: [new(filters[filterIndex])])]));
            if (filters[filterIndex] == PrismFilterId.Levels)
            {
                instance.GetLayerState(new(1)).Filters[0].SetValue(
                    PrismCatalogGenerated.PrismFilterParameterKeys.Levels.GammaKey, 1.4f);
            }
            return instance;
        });
        void AddButton(string id, float x, Action action)
        {
            Button button = new() { Width = 36, Height = 24, Command = new ActionCommand(_ => action()) };
            ServoApi.SetId(button, id);
            Canvas.SetLeft(button, x);
            Canvas.SetTop(button, 64);
            overlay.VisualChildren.Add(button);
        }
        void WaitReady() => Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
        void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
        {
            runtime.PumpOnce(TimeSpan.Zero);
            return done();
        }, TimeSpan.FromSeconds(10)), "Native pointwise scene did not reach the required state.");
        byte[] Capture()
        {
            runtime.PumpOnce(TimeSpan.Zero);
            window.SaveScreenshot(framePath);
            using SKBitmap bitmap = SKBitmap.Decode(framePath);
            Assert.NotEqual(new SKColor(30, 40, 50), bitmap.GetPixel(24, 24));
            return bitmap.Bytes.AsSpan(0, bitmap.RowBytes * 64).ToArray();
        }
    }
}
