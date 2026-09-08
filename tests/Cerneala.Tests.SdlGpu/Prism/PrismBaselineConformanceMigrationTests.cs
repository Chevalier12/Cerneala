using System.Numerics;
using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.SdlGpu;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using SkiaSharp;
using CernealaColor = Cerneala.Drawing.Color;

namespace Cerneala.Tests.Drawing.SdlGpu;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismBaselineConformanceMigrationTests
{
    private const int Width = 96, Height = 64;
    private static readonly Color ClearColor = new(9, 13, 21);
    private static readonly DrawRect ScopeBounds = new(0, 0, Width, Height);

    public static IEnumerable<object[]> Scenes()
    {
        foreach (PrismScene scene in CreateScenes())
        {
            using (scene) { yield return [scene.Name]; }
        }
    }

    [SdlNativeTheory]
    [MemberData(nameof(Scenes))]
    public void BaselineScenesPreserveFrozenPixelsAndSemanticAnchors(string name)
    {
        PrismScene[] scenes = CreateScenes();
        try
        {
            PrismScene scene = Assert.Single(scenes, candidate => candidate.Name == name);
            using SdlDrawingFixture fixture = new(Width, Height);
            Color[] fresh = fixture.Render(scene.Commands, ClearColor);
            Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
            Assert.Equal(scene.Analysis.Scopes.Length, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
            PrismGraphExecutionPlan rasterPlan = new PrismRasterPlanner().Prepare(scene.Plan, Width, Height).GraphPlan;
            Assert.Equal(rasterPlan.ExecutionOrder.Length + scene.Analysis.Scopes.Length,
                fixture.Backend.PrismDiagnostics.Counters.PassCount);
            AssertFrozen(scene, fresh, fixture.Backend.PrismDiagnostics.DumpExecutedGraph());
            AssertSemanticImage(scene, fresh, 2);

            Color[] cached = fixture.Render(scene.Commands, ClearColor);
            Assert.Equal(fresh, cached);
            Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
            Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
            // Unversioned masks and frame backdrops deliberately prevent final hits;
            // the unchanged capture remains reusable, while those dependents rerender.
        }
        finally
        {
            foreach (PrismScene scene in scenes) { scene.Dispose(); }
        }
    }

    [SdlNativeFact]
    public void TransformedControlBoundsLimitCaptureBeforeFiltering()
    {
        PrismCompositionDefinition composition = new("Capture bounds", [Layer(1, "Blur")]);
        PrismDrawScope scope = PrismTestData.Scope(composition, ownerToken: 1901,
            bounds: new DrawRect(0, 0, 32, 24), transform: Matrix3x2.CreateTranslation(20, 20));
        using SdlDrawingFixture fixture = new(Width, Height);
        Color[] overflow = fixture.Render(Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(10, 20, 20, 20), Color.White),
            DrawCommand.EndPrism()), ClearColor);
        fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        Color[] bounded = fixture.Render(Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(new DrawRect(20, 20, 10, 20), Color.White),
            DrawCommand.EndPrism()), ClearColor);

        Assert.Equal(bounded, overflow);
    }

    [SdlNativeTheory]
    [InlineData(0, 1)]
    [InlineData(45, 1)]
    [InlineData(0, 1.5f)]
    [InlineData(45, 1.5f)]
    public void CaptureBoundsMatchAnExplicitClipAtTheSameTransform(float degrees, float scale)
    {
        DrawRect bounds = new(0, 0, 32, 24);
        Matrix3x2 transform = Matrix3x2.CreateRotation(degrees * MathF.PI / 180, new Vector2(16, 12)) *
            Matrix3x2.CreateTranslation(30, 20);
        PrismCompositionDefinition composition = new("Capture clip equivalence", [Layer(1, "Blur")]);
        PrismDrawScope scope = PrismTestData.Scope(composition, ownerToken: 1902,
            bounds: bounds, transform: transform, pixelScale: scale);
        using SdlDrawingFixture fixture = new(144, 96, coordinateScale: scale);
        DrawCommand fill = DrawCommand.FillRectangle(new DrawRect(0, 0, 96, 64), Color.White);
        Color[] implicitClip = fixture.Render(Commands(DrawCommand.BeginPrism(scope), fill,
            DrawCommand.EndPrism()), ClearColor);
        fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
        Assert.True(Matrix3x2.Invert(transform, out Matrix3x2 inverse));
        Color[] explicitClip = fixture.Render(Commands(DrawCommand.BeginPrism(scope),
            DrawCommand.PushTransform(transform), DrawCommand.PushClip(bounds), DrawCommand.PushTransform(inverse),
            fill, DrawCommand.PopTransform(), DrawCommand.PopClip(), DrawCommand.PopTransform(),
            DrawCommand.EndPrism()), ClearColor);

        Assert.Equal(explicitClip, implicitClip);
        Assert.Contains(implicitClip, pixel => pixel != ClearColor);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.ActiveSurfaceCount);
    }

    [SdlNativeFact]
    public void CatalogGalleryRendersNonblankFramesThroughNativeFrameExport()
    {
        Assert.NotEmpty(PrismFilterConformanceGallery.Entries);
        using SdlDrawingFixture fixture = new(Width, Height);
        foreach (PrismFilterConformanceGalleryEntry entry in PrismFilterConformanceGallery.Entries)
        {
            PrismDrawScope scope = PrismTestData.Scope(entry.Composition,
                ownerToken: 10_000L + (int)entry.Filter, bounds: ScopeBounds);
            Color[] pixels = fixture.Render(Commands(DrawCommand.BeginPrism(scope),
                RedRectangle(), BlueRectangle(), DrawCommand.EndPrism()), ClearColor);
            Assert.Equal(Width * Height, pixels.Length);
            Assert.Contains(pixels, pixel => pixel != ClearColor);
            Assert.Equal(1, fixture.Backend.PrismDiagnostics.Counters.CaptureCount);
            Assert.True(fixture.Backend.PrismDiagnostics.Counters.PassCount >= 1, entry.Symbol);
        }
    }

    [SdlNativeFact]
    public void FreshExecutedGraphDumpsCorrelateNestedAndTransformScopesDeterministically()
    {
        using SdlDrawingFixture fixture = new(Width, Height);
        using PrismScene nested = CreateNestedScene();
        using PrismScene transform = CreateTransformScene();
        string nestedDump = Dump(nested);
        Assert.Equal(nestedDump, Dump(nested));
        string transformDump = Dump(transform);
        Assert.Equal(transformDump, Dump(transform));
        Assert.StartsWith("prism-execution v2 runtime-identifiers=redacted", nestedDump);
        Assert.Contains("scope 1 commands=", nestedDump);
        Assert.Contains("depth=1 parent=0 owner=scope-1", nestedDump);
        Assert.Contains(" NestedPresent scope=1 ", nestedDump);
        Assert.Contains("transform=[1,0,0,1,12,8]", transformDump);
        Assert.DoesNotContain("Texture2D", nestedDump);
        Assert.DoesNotContain("RenderTarget2D", nestedDump);
        Assert.DoesNotContain("GraphicsDevice", nestedDump);

        string Dump(PrismScene scene)
        {
            fixture.Session.DrawingResources.PrismResources.Invalidate(PrismCacheInvalidation.All);
            fixture.Render(scene.Commands, ClearColor);
            Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
            return fixture.Backend.PrismDiagnostics.DumpExecutedGraph();
        }
    }

    [SdlNativeFact]
    public void ResizeRecreatesTheWindowTargetAndPrismRendersAtTheNewExtent()
    {
        using SdlDrawingFixture fixture = new(Width, Height);
        using PrismScene scene = CreateNormalBlendScene();
        Color[] before = fixture.Render(scene.Commands, ClearColor);
        fixture.Session.Resize(112, 72, 1);
        Color[] after = fixture.Render(scene.Commands, ClearColor);
        Assert.Equal(112, fixture.Session.PixelWidth);
        Assert.Equal(72, fixture.Session.PixelHeight);
        Assert.Equal(112 * 72, after.Length);
        Assert.Equal(before[18 * Width + 20], after[18 * 112 + 20]);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Count);
        Assert.Equal(0, fixture.Backend.PrismDiagnostics.Counters.ActiveSurfaceCount);
    }

    [SdlNativeFact]
    public void DisposedPrismSessionCannotRenderAndANewSessionPreservesPixels()
    {
        using PrismScene scene = CreateNormalBlendScene();
        SdlDrawingFixture first = new(Width, Height);
        Color[] before;
        try { before = first.Render(scene.Commands, ClearColor); }
        finally { first.Dispose(); }
        Assert.Throws<ObjectDisposedException>(() => first.Session.BeginFrame(ClearColor));
        Assert.Throws<ObjectDisposedException>(() => first.Session.CapturePresentedFrame());
        using SdlDrawingFixture second = new(Width, Height);
        Assert.Equal(before, second.Render(scene.Commands, ClearColor));
        Assert.Equal(0, second.Backend.PrismDiagnostics.Count);
    }

    [Fact]
    public void JfaPlusOneUsesVersionedDistanceAndContourShaderSources()
    {
        string root = FindRepositoryRoot();
        string shader = File.ReadAllText(Path.Combine(root, "Drawing", "Prism", "Shaders", "Hlsl", "Styles", "DistanceField.hlsl"));
        string contour = File.ReadAllText(Path.Combine(root, "Drawing", "Prism", "Shaders", "Hlsl", "Styles", "Common.hlsl"));
        Assert.Contains("StyleAntiAliasedEdgeDistance", shader);
        Assert.Contains("SelectNearestStyleDistanceSeed", shader);
        Assert.Contains("StyleDistanceSeedPixelShader", shader);
        Assert.Contains("StyleDistanceFloodPixelShader", shader);
        Assert.Contains("SampleStyleContourLut", contour);
        // The shared planner's typed topology tests own the JFA+1 sequence;
        // this check only verifies the versioned shader source contract.
    }

    [Fact]
    public void BevelUsesVersionedJfaAndSobelHeightProfileSources()
    {
        string root = FindRepositoryRoot();
        string shader = File.ReadAllText(Path.Combine(root, "Drawing", "Prism", "Shaders", "Hlsl", "Styles", "BevelEmboss.hlsl"));
        Assert.Contains("StyleSignedEuclideanDistance", shader);
        Assert.Contains("BevelHeightPixelShader", shader);
        Assert.Contains("SobelBevelNormal", shader);
        Assert.Contains("SampleBevelTextureHeight", shader);
    }

    private static void AssertFrozen(PrismScene scene, Color[] pixels, string executionDump)
    {
        string directory = Path.Combine(FindRepositoryRoot(), "tests", "Cerneala.Tests", "Golden", "Prism");
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, "conformance.json")));
        JsonElement metadata = manifest.RootElement;
        string imagePath = Path.Combine(directory, scene.Name + ".png");
        if (metadata.GetProperty("sceneRevisions").TryGetProperty(scene.Name, out JsonElement revision))
        {
            Assert.Equal("SDL_GPU", revision.GetProperty("platform").GetString());
            Assert.Equal("Window.SaveScreenshot", revision.GetProperty("captureApi").GetString());
            Assert.False(string.IsNullOrWhiteSpace(revision.GetProperty("reason").GetString()));
            Assert.Equal(revision.GetProperty("sha256").GetString(),
                Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(imagePath))).ToLowerInvariant());
        }
        else
        {
            Assert.Equal("WindowsDX", metadata.GetProperty("platform").GetString());
            Assert.Equal("IWindowScreenshotSource.RenderPng", metadata.GetProperty("captureApi").GetString());
            JsonElement hardware = metadata.GetProperty("supportedHardware");
            Assert.Equal("Direct3D 11", hardware.GetProperty("graphicsApi").GetString());
            Assert.Equal("10_0", hardware.GetProperty("minimumFeatureLevel").GetString());
            Assert.Equal("ps_4_0", hardware.GetProperty("shaderProfile").GetString());
            Assert.Equal("WHQL-certified or current vendor production driver", hardware.GetProperty("driverPolicy").GetString());
        }
        Assert.Equal("R8G8B8A8_UNorm", metadata.GetProperty("pixelFormat").GetString());
        Assert.Equal("sRGB IEC61966-2.1", metadata.GetProperty("colorProfile").GetString());
        Assert.Equal("LinearSrgb", metadata.GetProperty("workingColorProfile").GetString());
        Assert.Equal("straight RGBA PNG output; premultiplied linear-light compositor inputs",
            metadata.GetProperty("alphaConvention").GetString());
        Assert.Equal(12648430, metadata.GetProperty("seed").GetInt32());
        Assert.Equal(Width, metadata.GetProperty("width").GetInt32());
        Assert.Equal(Height, metadata.GetProperty("height").GetInt32());
        foreach (JsonProperty channel in metadata.GetProperty("channelTolerance").EnumerateObject())
            Assert.Equal(2, channel.Value.GetInt32());

        using SKBitmap expected = SKBitmap.Decode(imagePath);
        Assert.NotNull(expected);
        Assert.Equal((Width, Height), (expected.Width, expected.Height));
        using SKBitmap actual = Decode(pixels);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            Assert.True(IsWithinTolerance(expected.GetPixel(x, y), actual.GetPixel(x, y), 2),
                $"{scene.Name} differs at ({x},{y}): expected={expected.GetPixel(x, y)}, actual={actual.GetPixel(x, y)}, tolerance=2.\n{executionDump}");
    }

    private static void AssertSemanticImage(
        PrismScene scene,
        Color[] pixels,
        int tolerance)
    {
        using SKBitmap bitmap = Decode(pixels);
        Assert.Equal(Width, bitmap.Width);
        Assert.Equal(Height, bitmap.Height);

        SKColor background =
            new(ClearColor.R, ClearColor.G, ClearColor.B, ClearColor.A);
        SKColor backgroundPixel =
            bitmap.GetPixel(scene.BackgroundX, scene.BackgroundY);
        Assert.True(
            IsWithinTolerance(
                backgroundPixel,
                background,
                tolerance),
            $"{scene.Name} changed the known background anchor.");

        SKColor foregroundPixel =
            bitmap.GetPixel(scene.ForegroundX, scene.ForegroundY);
        Assert.False(
            IsWithinTolerance(
                foregroundPixel,
                background,
                tolerance),
            $"{scene.Name} produced no content at its foreground anchor.");

        HashSet<uint> distinctColors = [];
        int contentPixelCount = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                SKColor pixel = bitmap.GetPixel(x, y);
                distinctColors.Add(Pack(pixel));
                if (!IsWithinTolerance(pixel, background, tolerance))
                {
                    contentPixelCount++;
                }
            }
        }

        Assert.True(
            distinctColors.Count >= 3,
            $"{scene.Name} did not produce a meaningful color field.");
        Assert.True(
            contentPixelCount >= 300,
            $"{scene.Name} rendered too few content pixels.");
    }


    private static PrismScene[] CreateScenes()
    {
        return
        [
            CreateNormalBlendScene(),
            CreateOpacityScene(),
            CreateFillScene(),
            CreateMaskScene(),
            CreateClipScene(),
            CreateNestedScene(),
            CreateTransformScene(),
            CreateBlendCombinationScene(),
            CreateMaskTransformScene(),
            CreateClippingChainScene(),
            CreateNestedGroupsScene(),
            CreateStyleScene(
                                PrismStyleId.DropShadow,
                "style-drop-shadow"),
            CreateStyleScene(
                                PrismStyleId.InnerShadow,
                "style-inner-shadow"),
            CreateStyleScene(
                                PrismStyleId.OuterGlow,
                "style-outer-glow"),
            CreateStyleScene(
                                PrismStyleId.InnerGlow,
                "style-inner-glow"),
            CreateStyleScene(
                                PrismStyleId.BevelEmboss,
                "style-bevel-emboss"),
            CreateStyleScene(
                                PrismStyleId.Satin,
                "style-satin"),
            CreateStyleScene(
                                PrismStyleId.ColorOverlay,
                "style-color-overlay"),
            CreateStyleScene(
                                PrismStyleId.GradientOverlay,
                "style-gradient-overlay"),
            CreateStyleScene(
                                PrismStyleId.PatternOverlay,
                "style-pattern-overlay"),
            CreateStyleScene(
                                PrismStyleId.Stroke,
                "style-stroke")
        ];
    }

    private static PrismScene CreateNormalBlendScene()
    {
        PrismDrawScope scope = CreateScope(
            "Normal blend",
            ownerToken: 101,
            Layer(1, "Normal foreground"),
            Layer(2, "Normal background"));
        return BuildScene(
            "normal-blend",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateOpacityScene()
    {
        PrismDrawScope scope = CreateScope(
            "Opacity",
            ownerToken: 201,
            Layer(1, "Half opacity", opacity: 0.5f));
        return BuildScene(
            "opacity",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateFillScene()
    {
        PrismDrawScope scope = CreateScope(
            "Fill",
            ownerToken: 301,
            Layer(1, "Partial fill", fill: 0.35f));
        return BuildScene(
            "fill",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateMaskScene()
    {
        ImageResource resource = CreateImageResource(
                        "ConformanceMask",
            static (x, y) =>
            {
                float horizontal = x / (float)(Width - 1);
                float vertical = y / (float)(Height - 1);
                byte alpha = ToByte(
                    Math.Clamp(
                        (horizontal * 0.75f) +
                        (vertical * 0.25f),
                        0,
                        1));
                return new CernealaColor(alpha, alpha, alpha, alpha);
            });
        PrismMaskDefinition mask = new(
            resource.Id,
            density: 0.65f);
        PrismDrawScope scope = CreateScope(
            "Mask",
            ownerToken: 401,
            Matrix3x2.Identity,
            resource.Resources,
            Layer(1, "Masked layer", mask: mask));
        return BuildScene(
            "mask",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18,
            ownedResource: resource.Image);
    }

    private static PrismScene CreateClipScene()
    {
        PrismDrawScope scope = CreateScope(
            "Clip",
            ownerToken: 501,
            Layer(1, "Clipped foreground", clipToBelow: true),
            Layer(2, "Clip base"));
        return BuildScene(
            "clip",
            Commands(
                DrawCommand.BeginPrism(scope),
                RedRectangle(),
                BlueRectangle(),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 20,
            foregroundY: 18);
    }

    private static PrismScene CreateNestedScene()
    {
        PrismDrawScope outer = CreateScope(
            "Outer",
            ownerToken: 601,
            Layer(1, "Outer layer", opacity: 0.75f));
        PrismDrawScope inner = CreateScope(
            "Inner",
            ownerToken: 602,
            Layer(1, "Inner layer", fill: 0.55f));
        return BuildScene(
            "nested-prism",
            Commands(
                DrawCommand.BeginPrism(outer),
                DrawCommand.FillRectangle(
                    new DrawRect(10, 10, 72, 40),
                    new CernealaColor(210, 54, 74)),
                DrawCommand.BeginPrism(inner),
                DrawCommand.FillRectangle(
                    new DrawRect(28, 20, 40, 28),
                    new CernealaColor(37, 190, 126)),
                DrawCommand.EndPrism(),
                DrawCommand.FillRectangle(
                    new DrawRect(58, 38, 24, 14),
                    new CernealaColor(244, 191, 52)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 16,
            foregroundY: 16);
    }

    private static PrismScene CreateTransformScene()
    {
        PrismDrawScope scope = CreateScope(
            "Transform",
            ownerToken: 701,
            transform: Matrix3x2.CreateTranslation(12, 8),
            Layer(1, "Translated layer"));
        return BuildScene(
            "transform",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(22, 18, 50, 30),
                    new CernealaColor(222, 69, 83)),
                DrawCommand.FillRectangle(
                    new DrawRect(54, 32, 34, 22),
                    new CernealaColor(56, 129, 229)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 30,
            foregroundY: 26,
            backgroundX: 12,
            backgroundY: 12);
    }

    private static PrismScene CreateBlendCombinationScene()
    {
        ImageResource resource = CreateImageResource(
                        "BlendCombinationMask",
            static (x, y) =>
            {
                float centerX = Width * 0.52f;
                float centerY = Height * 0.48f;
                float dx = (x - centerX) / (Width * 0.55f);
                float dy = (y - centerY) / (Height * 0.7f);
                byte alpha = ToByte(
                    Math.Clamp(
                        1 - MathF.Sqrt((dx * dx) + (dy * dy)),
                        0,
                        1));
                return new CernealaColor(alpha, alpha, alpha, alpha);
            });
        PrismMaskDefinition mask = new(
            resource.Id,
            density: 0.72f,
            feather: 1.5f);
        PrismLayerDefinition clipped = Layer(
            11,
            "Masked vivid-light clip",
            opacity: 0.82f,
            fill: 0.68f,
            mask: mask,
            clipToBelow: true,
            blendMode: PrismBlendMode.VividLight,
            styles:
            [
                new PrismStyleDefinition(
                    PrismStyleId.DropShadow)
            ]);
        PrismGroupDefinition isolated = new(
            new PrismNodeId(10),
            "Isolated blend group",
            [
                clipped,
                Layer(
                    12,
                    "Multiply clip base",
                    blendMode: PrismBlendMode.Multiply)
            ],
            opacity: 0.88f,
            blendMode: PrismBlendMode.Normal);
        PrismDrawScope scope = CreateScope(
            "Blend combination",
            ownerToken: 801,
            Matrix3x2.Identity,
            resource.Resources,
            isolated,
            Layer(
                2,
                "Screen root base",
                blendMode: PrismBlendMode.Screen));
        return BuildScene(
            "blend-combination",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(8, 8, 58, 34),
                    new CernealaColor(224, 58, 92, 220)),
                DrawCommand.FillRectangle(
                    new DrawRect(34, 20, 50, 34),
                    new CernealaColor(43, 170, 224, 196)),
                DrawCommand.FillRectangle(
                    new DrawRect(20, 36, 58, 20),
                    new CernealaColor(238, 193, 51, 184)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 24,
            foregroundY: 18,
            ownedResource: resource.Image);
    }

    private static PrismScene CreateMaskTransformScene()
    {
        ImageResource resource = CreateImageResource(
                        "TransformedMask",
            static (x, y) =>
            {
                bool high =
                    ((x / 12) + (y / 10)) % 2 == 0;
                byte value = high ? (byte)224 : (byte)36;
                return new CernealaColor(
                    value,
                    value,
                    value,
                    byte.MaxValue);
            });
        PrismMaskDefinition mask = new(
            resource.Id,
            channel: PrismMaskChannel.Luminance,
            feather: 2,
            density: 0.8f,
            invert: true);
        PrismDrawScope scope = CreateScope(
            "Mask transform",
            ownerToken: 901,
            Matrix3x2.CreateTranslation(12, 8),
            resource.Resources,
            Layer(1, "Transformed mask layer", mask: mask));
        return BuildScene(
            "mask-transform",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    // The source is already in host drawing coordinates. Its
                    // left edge must respect the translated capture boundary.
                    new DrawRect(12, 10, 64, 38),
                    new CernealaColor(230, 72, 96)),
                DrawCommand.FillRectangle(
                    new DrawRect(38, 28, 46, 26),
                    new CernealaColor(48, 174, 219, 210)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 30,
            foregroundY: 24,
            ownedResource: resource.Image);
    }

    private static PrismScene CreateClippingChainScene()
    {
        PrismDrawScope scope = CreateScope(
            "Clipping chain",
            ownerToken: 1001,
            Layer(
                3,
                "Top clipped layer",
                opacity: 0.7f,
                clipToBelow: true,
                blendMode: PrismBlendMode.Screen),
            Layer(
                2,
                "Middle clipped layer",
                fill: 0.55f,
                clipToBelow: true,
                blendMode: PrismBlendMode.Multiply),
            Layer(
                1,
                "Partial alpha base",
                opacity: 0.62f));
        return BuildScene(
            "clipping-chain",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(8, 10, 58, 36),
                    new CernealaColor(232, 63, 95, 176)),
                DrawCommand.FillRectangle(
                    new DrawRect(34, 22, 50, 32),
                    new CernealaColor(47, 175, 225, 204)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 24,
            foregroundY: 20);
    }

    private static PrismScene CreateNestedGroupsScene()
    {
        PrismGroupDefinition inner = new(
            new PrismNodeId(20),
            "Inner isolated group",
            [
                Layer(
                    21,
                    "Inner top",
                    fill: 0.65f,
                    blendMode: PrismBlendMode.Overlay),
                Layer(
                    22,
                    "Inner base",
                    opacity: 0.8f,
                    blendMode: PrismBlendMode.Multiply)
            ],
            opacity: 0.82f,
            blendMode: PrismBlendMode.Normal);
        PrismGroupDefinition outer = new(
            new PrismNodeId(10),
            "Outer pass-through group",
            [
                inner,
                Layer(
                    11,
                    "Outer base",
                    blendMode: PrismBlendMode.Screen)
            ],
            opacity: 0.9f,
            blendMode: PrismBlendMode.PassThrough);
        PrismDrawScope scope = CreateScope(
            "Nested groups",
            ownerToken: 1101,
            outer);
        return BuildScene(
            "nested-groups",
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(8, 8, 64, 38),
                    new CernealaColor(226, 61, 91, 218)),
                DrawCommand.FillRectangle(
                    new DrawRect(30, 22, 54, 34),
                    new CernealaColor(46, 171, 222, 194)),
                DrawCommand.FillRectangle(
                    new DrawRect(18, 38, 62, 18),
                    new CernealaColor(239, 194, 53, 180)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 22,
            foregroundY: 18);
    }

    private static PrismScene CreateStyleScene(
        PrismStyleId style,
        string sceneName)
    {
        PrismLayerDefinition layer = Layer(
            1,
            style.ToString(),
            styles:
            [
                new PrismStyleDefinition(style)
            ]);
        ImageResource? resource = null;
        if (style == PrismStyleId.PatternOverlay)
        {
            resource = CreateImageResource(
                                "ConformanceStylePattern",
                static (x, y) =>
                {
                    bool first =
                        ((x / 8) + (y / 8)) % 2 == 0;
                    return first
                        ? new CernealaColor(35, 202, 157)
                        : new CernealaColor(244, 188, 52);
                });
        }
        PrismCompositionDefinition composition = new(
            style.ToString(),
            [layer],
            workingColorProfile: PrismColorProfile.LinearSrgb);
        PrismDrawScope scope = PrismTestData.Scope(
            composition,
            ownerToken: 1200 + (int)style,
            bounds: new DrawRect(20, 14, 66, 42),
            resources: resource?.Resources);

        PrismStyleState state = Assert.Single(
            scope.Instance
                .GetLayerState(layer.Id)
                .Styles);
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry((int)style);
        if (resource is not null)
        {
            PrismCatalogPropertyDescriptor pattern =
                entry.Properties.Single(property =>
                    property.Name == "Pattern");
            GeneratedMarkup.SetPrismStyleResource(
                state,
                entry.StableId,
                pattern.TypeSlot,
                resource.Id);
        }
        else if (style is PrismStyleId.DropShadow or
            PrismStyleId.Stroke)
        {
            PrismCatalogPropertyDescriptor color =
                entry.Properties.Single(property =>
                    property.Name == "Color");
            GeneratedMarkup.SetPrismStyleColor(
                state,
                entry.StableId,
                color.TypeSlot,
                style == PrismStyleId.DropShadow
                    ? new CernealaColor(74, 218, 188)
                    : new CernealaColor(255, 208, 62));
        }

        return BuildScene(
            sceneName,
            Commands(
                DrawCommand.BeginPrism(scope),
                DrawCommand.FillRectangle(
                    new DrawRect(20, 14, 52, 36),
                    new CernealaColor(222, 69, 83)),
                DrawCommand.FillRectangle(
                    new DrawRect(58, 32, 28, 24),
                    new CernealaColor(56, 129, 229, 210)),
                DrawCommand.EndPrism()),
            expectedFallbackCount: 0,
            foregroundX: 28,
            foregroundY: 22,
            ownedResource: resource?.Image);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        params PrismNodeDefinition[] layers)
    {
        return CreateScope(
            name,
            ownerToken,
            Matrix3x2.Identity,
            resources: null,
            layers);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        Matrix3x2 transform,
        params PrismNodeDefinition[] layers)
    {
        return CreateScope(
            name,
            ownerToken,
            transform,
            resources: null,
            layers);
    }

    private static PrismDrawScope CreateScope(
        string name,
        long ownerToken,
        Matrix3x2 transform,
        PrismDrawResources? resources,
        params PrismNodeDefinition[] layers)
    {
        PrismCompositionDefinition composition = new(
            name,
            layers,
            workingColorProfile: PrismColorProfile.LinearSrgb);
        return PrismTestData.Scope(
            composition,
            ownerToken,
            ScopeBounds,
            transform,
            resources: resources);
    }

    private static PrismLayerDefinition Layer(
        int id,
        string name,
        float opacity = 1,
        float fill = 1,
        PrismMaskDefinition? mask = null,
        bool clipToBelow = false,
        PrismBlendMode blendMode = PrismBlendMode.Normal,
        IEnumerable<PrismStyleDefinition>? styles = null)
    {
        return new PrismLayerDefinition(
            new PrismNodeId(id),
            name,
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.Blur)
            ],
            styles: styles,
            mask: mask,
            opacity: opacity,
            fill: fill,
            blendMode: blendMode,
            clipToBelow: clipToBelow);
    }

    private static PrismScene BuildScene(
        string name,
        DrawCommandList commands,
        int expectedFallbackCount,
        int foregroundX,
        int foregroundY,
        int backgroundX = 2,
        int backgroundY = 2,
        IDisposable? ownedResource = null)
    {
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraph graph =
            new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);
        return new PrismScene(
            name,
            commands,
            analysis,
            plan,
            expectedFallbackCount,
            foregroundX,
            foregroundY,
            backgroundX,
            backgroundY,
            ownedResource);
    }

    private static ImageResource CreateImageResource(string key, Func<int, int, Color> pixelFactory)
    {
        byte[] pixels = new byte[Width * Height * 4];
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Color pixel = pixelFactory(x, y);
            int offset = (y * Width + x) * 4;
            pixels[offset] = pixel.R;
            pixels[offset + 1] = pixel.G;
            pixels[offset + 2] = pixel.B;
            pixels[offset + 3] = pixel.A;
        }
        SdlGpuImage image = new(Width, Height, pixels);
        PrismResourceId id = new(key);
        return new(id, PrismDrawResources.Create([new PrismDrawImageResource(id, image)]), image);
    }

    private static byte ToByte(float value) => (byte)MathF.Round(Math.Clamp(value, 0, 1) * 255);
    private static DrawCommandList Commands(params DrawCommand[] commands) => PrismTestData.Commands(commands);
    private static DrawCommand RedRectangle() => DrawCommand.FillRectangle(new(10, 10, 50, 30), new(222, 69, 83));
    private static DrawCommand BlueRectangle() => DrawCommand.FillRectangle(new(42, 24, 42, 28), new(56, 129, 229));
    private static bool IsWithinTolerance(SKColor a, SKColor b, int tolerance) =>
        Math.Abs(a.Red - b.Red) <= tolerance && Math.Abs(a.Green - b.Green) <= tolerance &&
        Math.Abs(a.Blue - b.Blue) <= tolerance && Math.Abs(a.Alpha - b.Alpha) <= tolerance;
    private static uint Pack(SKColor color) => ((uint)color.Alpha << 24) | ((uint)color.Red << 16) | ((uint)color.Green << 8) | color.Blue;

    private static SKBitmap Decode(Color[] pixels)
    {
        Assert.Equal(Width * Height, pixels.Length);
        SKBitmap bitmap = new(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Color pixel = pixels[y * Width + x];
            bitmap.SetPixel(x, y, new SKColor(pixel.R, pixel.G, pixel.B, pixel.A));
        }
        return bitmap;
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }

    private sealed record ImageResource(PrismResourceId Id, PrismDrawResources Resources, SdlGpuImage Image);
    private sealed record PrismScene(string Name, DrawCommandList Commands, PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan, int ExpectedFallbackCount, int ForegroundX, int ForegroundY,
        int BackgroundX, int BackgroundY, IDisposable? OwnedResource) : IDisposable
    {
        public void Dispose() => OwnedResource?.Dispose();
    }
}
