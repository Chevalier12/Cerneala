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
public sealed class NativeScenePrismDomainTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void GlobalInputMatchesFullDomainRenderingCroppedAfterTheEffect() => Run(nestedSprites: false);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NestedSpriteInputMatchesFullDomainRenderingCroppedAfterTheEffect() => Run(nestedSprites: true);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void LocalNeighborhoodInputMatchesCompleteDomainRendering() => Run(nestedSprites: false, localNeighborhood: true);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NestedLocalNeighborhoodInputMatchesCompleteDomainRendering() => Run(nestedSprites: true, localNeighborhood: true);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void HighDensityLocalNeighborhoodInputMatchesCompleteDomainRendering() =>
        Run(nestedSprites: true, localNeighborhood: true, density: 2);

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void NestedBlurInputMatchesCompleteDomainRendering() =>
        Run(nestedSprites: true, localNeighborhood: true, nestedFilter: PrismFilterId.Blur);

    public static TheoryData<PrismStyleId, float> DeclaredStyleCases
    {
        get
        {
            TheoryData<PrismStyleId, float> cases = new();
            PrismStyleId[] styles = [PrismStyleId.DropShadow, PrismStyleId.InnerShadow, PrismStyleId.OuterGlow,
                PrismStyleId.InnerGlow, PrismStyleId.BevelEmboss, PrismStyleId.Satin, PrismStyleId.Stroke];
            foreach (PrismStyleId style in styles) { cases.Add(style, 1); cases.Add(style, 2); }
            return cases;
        }
    }

    [SdlNativeTheory]
    [MemberData(nameof(DeclaredStyleCases))]
    [Trait("Category", "Native")]
    public void DeclaredStyleInputMatchesFullDomainRenderingCroppedAfterTheEffect(PrismStyleId style, float density) =>
        Run(nestedSprites: density == 2, density: density, paintStyle: style);

    [SdlNativeTheory]
    [InlineData("Wrap", 1)]
    [InlineData("Mirror", 1)]
    [InlineData("Wrap", 2)]
    [InlineData("Mirror", 2)]
    [Trait("Category", "Native")]
    public void DeclaredWrappedInputMatchesFullDomainRenderingCroppedAfterTheEffect(string edgeMode, float density) =>
        Run(nestedSprites: density == 2, density: density, finiteDomainFilter: PrismFilterId.GaussianBlur, edgeMode: edgeMode);

    public static TheoryData<PrismStyleId?, PrismMaskChannel?, float> PointwisePaintCases => new()
    {
        { PrismStyleId.ColorOverlay, null, 0 },
        { PrismStyleId.GradientOverlay, null, 0 },
        { PrismStyleId.PatternOverlay, null, 0 },
        { null, PrismMaskChannel.Alpha, 0 },
        { null, PrismMaskChannel.Alpha, 6 },
        { null, PrismMaskChannel.Luminance, 6 },
        { PrismStyleId.GradientOverlay, PrismMaskChannel.Alpha, 6 }
    };

    [SdlNativeTheory]
    [MemberData(nameof(PointwisePaintCases))]
    [Trait("Category", "Native")]
    public void PointwisePaintAndMaskInputMatchesCompleteDomainRendering(
        PrismStyleId? style, PrismMaskChannel? channel, float feather) => RunPointwisePaintCase(false, 1, style, channel, feather);

    [SdlNativeTheory]
    [MemberData(nameof(PointwisePaintCases))]
    [Trait("Category", "Native")]
    public void NestedPointwisePaintAndMaskInputMatchesCompleteDomainRendering(
        PrismStyleId? style, PrismMaskChannel? channel, float feather) => RunPointwisePaintCase(true, 1, style, channel, feather);

    [SdlNativeTheory]
    [MemberData(nameof(PointwisePaintCases))]
    [Trait("Category", "Native")]
    public void HighDensityPointwisePaintAndMaskInputMatchesCompleteDomainRendering(
        PrismStyleId? style, PrismMaskChannel? channel, float feather) => RunPointwisePaintCase(true, 2, style, channel, feather);

    private static void RunPointwisePaintCase(bool nestedSprites, float density,
        PrismStyleId? style, PrismMaskChannel? channel, float feather) =>
        Run(nestedSprites, density: density, paintStyle: style,
            imageMask: channel is { } value ? new(new("PaintResource"), channel: value, feather: feather) : null);

    private static void Run(bool nestedSprites, bool localNeighborhood = false, float density = 1,
        PrismFilterId nestedFilter = PrismFilterId.Invert, PrismStyleId? paintStyle = null, PrismMaskDefinition? imageMask = null,
        PrismFilterId? finiteDomainFilter = null, string? edgeMode = null)
    {
        bool pointwisePaint = paintStyle is PrismStyleId.ColorOverlay or PrismStyleId.GradientOverlay or PrismStyleId.PatternOverlay || imageMask is not null;
        string directory = Path.Combine(Path.GetTempPath(), $"Cerneala-prism-domain-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string screenshot = Path.Combine(directory, "frame.png");
        List<string> files = [];
        List<SdlGpuImage> images = [];
        List<IDisposable> nestedEffects = [];
        NativeSdlApi api = new();
        using SdlGpuWindowGraphicsSessionFactory graphics = new(api, useMultisampling: true);
        using SdlWindowPlatform platform = new(api, graphics, coordinateScaleOverride: density);
        using WindowApplicationRuntime runtime = new(platform);
        Window window = new() { Title = "Finite Prism input parity", Width = 256 * density, Height = 160 * density };
        PrismFilterId[] filters = finiteDomainFilter is { } operation ? [operation] :
            paintStyle is not null && !pointwisePaint ? [PrismFilterId.Invert] :
            pointwisePaint ? [PrismFilterId.Invert, PrismFilterId.Invert] : localNeighborhood
            ? [PrismFilterId.Average, PrismFilterId.Blur, PrismFilterId.BlurMore, PrismFilterId.BoxBlur,
                PrismFilterId.GaussianBlur, PrismFilterId.MotionBlur, PrismFilterId.SmartBlur, PrismFilterId.SurfaceBlur,
                PrismFilterId.Sharpen, PrismFilterId.SharpenMore, PrismFilterId.SharpenEdges, PrismFilterId.UnsharpMask,
                PrismFilterId.HighPass, PrismFilterId.DustScratches, PrismFilterId.Median]
            : [PrismFilterId.Levels, PrismFilterId.Threshold];
        try
        {
            byte[] values = [12, 24, 60, 80, 100, 130, 210, 240, 255];
            for (int index = 0; index < values.Length; index++)
            {
                string path = Path.Combine(directory, $"bar-{index}.png");
                files.Add(path);
                using SKBitmap bitmap = new(1, 1);
                bitmap.SetPixel(0, 0, new(values[index], values[index], values[index], 255));
                using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(path, encoded.ToArray());
                images.Add((SdlGpuImage)new SdlGpuImageLoader().Load(path));
            }
            if (pointwisePaint)
            {
                string path = Path.Combine(directory, "paint.png");
                files.Add(path);
                // Several mask texels must lie inside the viewport even though
                // the distant catalog entry stretches the logical paint bounds.
                using SKBitmap bitmap = new(64, 8);
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        bitmap.SetPixel(x, y, new((byte)(32 + (x % 8) * 24), (byte)(24 + y * 25),
                            (byte)(30 + ((x + y) % 2) * 180), (byte)(64 + ((x + 2 * y) % 8) * 24)));
                    }
                }
                using SKData encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(path, encoded.ToArray());
                window.Resources.SetResource(new ResourceId<ImageResource>("PaintResource"), new ImageResource(path));
            }
            SceneSpatialEntry2D[] entries = Enumerable.Range(0, 9)
                .Select(index => new SceneSpatialEntry2D(index.ToString(),
                    new(index == 8 ? 4096 : index * 32, 0, 32, 64), null)).ToArray();
            List<string> mapLoads = [];
            List<Sprite2D> sprites = [];
            foreach (SceneSpatialEntry2D entry in entries)
            {
                Sprite2D sprite = new()
                {
                    X = entry.Bounds.X, Y = entry.Bounds.Y, Width = 32, Height = 64,
                    Image = new(images[int.Parse(entry.Id)])
                };
                if (nestedSprites)
                {
                    nestedEffects.Add(GeneratedMarkup.AttachPrism(sprite, () => new PrismInstance(
                        new("Nested sprite", [new PrismLayerDefinition(new(1), "Local", filters: [new(nestedFilter)])]))));
                }
                sprites.Add(sprite);
            }
            SceneItems2D items = new() { ItemsSource = sprites };
            TileMapCatalog2D catalog = new("native-domain", entries.Select(entry =>
                new TileMapChunkInfo2D(entry, 1, [new ImageReference(images[int.Parse(entry.Id)])])));
            TileMap2D map = TileMap2D.FromSource(new TileMapSource2D(catalog, (_, info, _) =>
            {
                SceneSpatialEntry2D entry = info.Spatial;
                mapLoads.Add(entry.Id);
                return ValueTask.FromResult(new SceneSpatialLease2D<TileMapChunkData2D>(new([
                    new Tile(new(images[int.Parse(entry.Id)]), x: entry.Bounds.X, width: 32, height: 64)])));
            }));
            SceneGraph2D actors = new() { PrismInputDomain = new(0, 0, 256, 64) };
            SceneGraph2D terrain = new() { PrismInputDomain = new(0, 0, 256, 64) };
            actors.Children.Add(items);
            terrain.Children.Add(map);
            SceneGraph2D[] scenes = [actors, terrain];
            int sceneIndex = 0, filterIndex = 0;
            bool narrow = false;
            using IDisposable actorsEffect = Effect(actors);
            using IDisposable terrainEffect = Effect(terrain);
            Canvas root = new();
            RenderSurface2D surface = new()
            {
                Width = 256, Height = 64, ViewBox = new(0, 0, 256, 64), Scene = actors,
                ClearColor = new(30, 40, 50), RedrawMode = RenderSurface2DRedrawMode.OnDemand
            };
            root.VisualChildren.Add(surface);
            AddButton("camera", 0, () =>
            {
                narrow = !narrow;
                surface.Width = narrow ? 128 : 256;
                surface.ViewBox = narrow ? new(64, 0, 128, 64) : new(0, 0, 256, 64);
                if (localNeighborhood || pointwisePaint)
                {
                    foreach (SceneGraph2D scene in scenes) { scene.PrismInputDomain = narrow ? null : new(0, 0, 256, 64); }
                }
            });
            AddButton("mode", 40, () => { sceneIndex = 1 - sceneIndex; surface.Scene = scenes[sceneIndex]; });
            AddButton("filter", 80, () =>
            {
                filterIndex = (filterIndex + 1) % filters.Length;
                surface.Scene = null;
                surface.Scene = scenes[sceneIndex];
            });
            window.Content = root;
            runtime.Show(window, modal: false);
            ServoApi servo = new(window);
            WaitReady();
            List<string> differences = [];
            for (int filter = 0; filter < filters.Length; filter++)
            {
                for (int source = 0; source < 2; source++)
                {
                    byte[] reference = Capture(64);
                    Click("camera");
                    byte[] actual = Capture(0);
                    if (localNeighborhood || pointwisePaint)
                    {
                        Assert.Same(sprites, items.ItemsSource);
                        if (source == 0)
                        {
                            // A cropped active collection keeps every eager logical occurrence.
                            Assert.Equal(9, items.RealizedItemCount);
                        }
                        else
                        {
                            // Switching the surface to terrain detaches the actor scene.
                            Assert.Equal(0, items.RealizedItemCount);
                            int drawn = map.GetDiagnosticsSnapshot().DrawnTiles;
                            // Map recording excludes chunks with no positive-area intersection.
                            if (pointwisePaint) { Assert.Equal(4, drawn); }
                            else { Assert.True(drawn >= 4 && drawn < 8, $"{filters[filter]} source={source}, drawn={drawn}"); }
                        }
                    }
                    int first = -1, last = -1, different = 0;
                    for (int index = 0; index < reference.Length; index++)
                    {
                        if (reference[index] == actual[index]) { continue; }
                        if (first < 0) { first = index; }
                        last = index;
                        different++;
                    }
                    if (different != 0) { differences.Add($"filter={filters[filter]}, style={paintStyle}, mask={imageMask?.Channel}/{imageMask?.Feather}, variant={filter}, source={source}, bytes={different}, first={first}, last={last}"); }
                    Click("camera");
                    Click("mode");
                }
                Click("filter");
            }
            Assert.Same(sprites[8], items.LogicalChildren.Last());
            Assert.DoesNotContain("8", mapLoads);
            Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));

            IDisposable Effect(SceneGraph2D scene) => GeneratedMarkup.AttachPrism(scene, () =>
            {
                PrismInstance instance = new(new("Global input", [new PrismLayerDefinition(new(1), "Analysis",
                    filters: [new(filters[filterIndex])], styles: paintStyle is { } style ? [new(style)] : null, mask: imageMask)]));
                PrismLayerState layer = instance.GetLayerState(new(1));
                if (paintStyle is PrismStyleId.ColorOverlay)
                {
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.ColorOverlay.OpacityKey, 0.65f);
                }
                else if (paintStyle is PrismStyleId.GradientOverlay)
                {
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.GradientOverlay.OpacityKey, 0.65f);
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.GradientOverlay.AngleKey, 0f);
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.GradientOverlay.AlignWithLayerKey, filterIndex == 0);
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.GradientOverlay.DitherKey, true);
                }
                else if (paintStyle is PrismStyleId.PatternOverlay)
                {
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.PatternOverlay.PatternKey, new PrismResourceId("PaintResource"));
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.PatternOverlay.OpacityKey, 0.65f);
                    layer.Styles[0].SetValue(PrismCatalogGenerated.PrismStyleParameterKeys.PatternOverlay.LinkWithLayerKey, filterIndex == 0);
                }
                if (layer.Mask is { } mask) { mask.Invert = filterIndex != 0; }
                if (edgeMode is not null)
                {
                    layer.Filters[0].SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.EdgeModeKey,
                        PrismCatalogRuntime.ResolveSymbol("EdgeMode", edgeMode));
                    layer.Filters[0].SetValue(PrismCatalogGenerated.PrismFilterParameterKeys.GaussianBlur.RadiusKey, 80f);
                }
                if (filters[filterIndex] == PrismFilterId.Levels)
                {
                    instance.GetLayerState(new(1)).Filters[0].SetValue(
                        PrismCatalogGenerated.PrismFilterParameterKeys.Levels.AutoKey, true);
                }
                return instance;
            });
            void AddButton(string id, float x, Action action)
            {
                Button button = new() { Width = 36, Height = 24, Command = new ActionCommand(_ => action()) };
                ServoApi.SetId(button, id);
                Canvas.SetLeft(button, x);
                Canvas.SetTop(button, 128);
                root.VisualChildren.Add(button);
            }
            void Click(string id)
            {
                Task click = servo.ClickAsync(Cerneala.UI.Servo.ServoTarget.ById(id));
                Wait(() => click.IsCompleted);
                Assert.True(click.IsCompletedSuccessfully, click.Exception?.ToString());
                WaitReady();
            }
            void WaitReady()
            {
                Wait(() => surface.PresentationState == RenderSurface2DPresentationState.Ready);
                Assert.True(PrismAttachment.TryGetInstance(scenes[sceneIndex], out PrismInstance? active));
                Assert.Equal(filters[filterIndex], active!.GetLayerState(new(1)).Filters[0].Filter);
                if (filters[filterIndex] == PrismFilterId.Levels)
                {
                    Assert.True(active!.GetLayerState(new(1)).Filters[0].GetValue(
                        PrismCatalogGenerated.PrismFilterParameterKeys.Levels.AutoKey));
                }
                Assert.Equal(narrow ? 128 : 256, surface.ArrangedBounds.Width);
            }
            void Wait(Func<bool> done) => Assert.True(SpinWait.SpinUntil(() =>
            {
                runtime.PumpOnce(TimeSpan.Zero);
                return done();
            }, TimeSpan.FromSeconds(15)), $"State={surface.PresentationState}; error={surface.PresentationError}; " +
                $"loads={string.Join(',', mapLoads)}; realized={items.RealizedItemCount}; bounds={surface.ArrangedBounds}; " +
                $"domain={actors.PrismInputDomain}; attachments={string.Join(',', sprites.Select((sprite, index) =>
                    $"{index}:{sprite.IsAttached}/{sprite.Root is not null}"))}");
            byte[] Capture(int x)
            {
                runtime.PumpOnce(TimeSpan.Zero);
                window.SaveScreenshot(screenshot);
                using SKBitmap bitmap = SKBitmap.Decode(screenshot);
                int sourceX = (int)(x * density), width = (int)(128 * density), height = (int)(64 * density);
                Assert.True(bitmap.Width >= sourceX + width && bitmap.Height >= height);
                Assert.NotEqual(new SKColor(30, 40, 50), bitmap.GetPixel(sourceX + (int)(16 * density), (int)(32 * density)));
                byte[] pixels = new byte[width * height * 4];
                for (int y = 0; y < height; y++)
                {
                    bitmap.Bytes.AsSpan(y * bitmap.RowBytes + sourceX * 4, width * 4).CopyTo(pixels.AsSpan(y * width * 4));
                }
                return pixels;
            }
        }
        finally
        {
            runtime.Close(window, force: true);
            foreach (IDisposable effect in nestedEffects) { effect.Dispose(); }
            foreach (SdlGpuImage image in images) { image.Dispose(); }
            foreach (string file in files) { File.Delete(file); }
            File.Delete(screenshot);
            Directory.Delete(directory);
        }
    }
}
