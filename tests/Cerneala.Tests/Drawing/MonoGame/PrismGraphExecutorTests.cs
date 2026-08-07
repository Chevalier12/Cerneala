using System.Diagnostics;
using System.Globalization;
using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Cerneala.Drawing.MonoGame.Prism;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.MonoGame.Prism.Surfaces;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using CernealaColor = Cerneala.Drawing.Color;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismGraphExecutorTests
{
    internal const int SurfaceWidth = 16;
    internal const int SurfaceHeight = 16;
    private const int MeasuredFrameCount = 16;
    private const int StyleStressCount = 48;
    private const int AnimatedFrameCount = 2_048;

    [Fact]
    public void CurvesTextureCacheUploadsAndInvalidates1024SampleRgbLut()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismCurveTextureCache cache =
            new(fixture.Session.GraphicsDevice);
        PrismResourceId id = new("curves-cache");
        PrismCurvesResource firstResource = new(
            red:
            [
                new PrismCurvePoint(0, 0),
                new PrismCurvePoint(1, 0.5f)
            ]);

        Texture2D first = cache.GetOrCreate(
            id,
            firstResource,
            identity: 1,
            version: 1);
        Texture2D reused = cache.GetOrCreate(
            id,
            firstResource,
            identity: 1,
            version: 1);
        HalfVector4[] pixels =
            new HalfVector4[PrismCurveLut.SampleCount];
        first.GetData(pixels);

        Assert.Same(first, reused);
        Assert.Equal(PrismCurveLut.SampleCount, first.Width);
        Assert.Equal(1, first.Height);
        Assert.Equal(SurfaceFormat.HalfVector4, first.Format);
        Assert.InRange(
            pixels[PrismCurveLut.SampleCount / 2].ToVector4().X,
            0.249f,
            0.252f);

        PrismCurvesResource changedResource = new();
        Texture2D changed = cache.GetOrCreate(
            id,
            changedResource,
            identity: 2,
            version: 2);
        Assert.NotSame(first, changed);
        Assert.True(first.IsDisposed);
    }

    [Fact]
    public void RegistryValidatesCatalogAndRegistersGeneratedColorKernels()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

        Assert.Equal("LevelsCdf", registry.LevelsCdf.Technique.Name);
        Assert.Equal("LevelsRange", registry.LevelsRange.Technique.Name);

        foreach (PrismBlendMode blendMode in
            Enum.GetValues<PrismBlendMode>())
        {
            Assert.True(
                registry.IsFundamentalCatalogEntryRegistered(
                    "blend-mode",
                    blendMode.ToString()));
            Assert.True(
                registry.TryGetBlendKernel(
                    blendMode,
                    out PrismKernel blendKernel));
            Assert.Equal(PrismKernelKind.Blend, blendKernel.Kind);
            Assert.Equal(
                $"{blendMode}Blend",
                blendKernel.Technique.Name);
        }
        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            Assert.True(
                registry.IsFundamentalCatalogEntryRegistered(
                    "color-profile",
                    profile.ToString()));
            Assert.True(
                registry.TryGetColorConversionKernel(
                    profile,
                    out PrismKernel inputKernel));
            Assert.Equal(
                PrismKernelKind.InputColorConversion,
                inputKernel.Kind);
            Assert.Equal(
                $"InputTo{profile}",
                inputKernel.Technique.Name);
            Assert.True(
                registry.TryGetPresentKernel(
                    profile,
                    out PrismKernel outputKernel));
            Assert.Equal(
                PrismKernelKind.OutputColorConversion,
                outputKernel.Kind);
            Assert.Equal(
                $"{profile}ToOutput",
                outputKernel.Technique.Name);
        }
        Assert.True(
            registry.IsFundamentalCatalogEntryRegistered(
                "sampling",
                "Linear"));
        foreach (PrismCatalogEntryDescriptor entry in
            PrismCatalogGenerated.Entries.Where(candidate =>
                candidate.Kind == "filter" &&
                (PrismAdjustmentPlanner.IsSupported(
                    (PrismFilterId)candidate.StableId) ||
                PrismNeighborhoodPlanner.IsSupported(
                    (PrismFilterId)candidate.StableId) ||
                PrismResamplingPlanner.IsSupported(
                    (PrismFilterId)candidate.StableId) ||
                PrismCatalogFilterPlanner.IsSupported(
                    (PrismFilterId)candidate.StableId))))
        {
            PrismFilterId filter =
                (PrismFilterId)entry.StableId;
            Assert.True(
                registry.IsFundamentalCatalogEntryRegistered(
                    "filter",
                    entry.Symbol));
            Assert.True(
                registry.TryGetFilterKernel(
                    filter,
                    out PrismKernel filterKernel));
            bool isAdjustment =
                PrismAdjustmentPlanner.IsSupported(filter);
            bool isNeighborhood =
                PrismNeighborhoodPlanner.IsSupported(filter);
            bool isResampling =
                PrismResamplingPlanner.IsSupported(filter);
            bool isColorHalftone =
                filter == PrismFilterId.ColorHalftone;
            bool isColoredPencil =
                filter == PrismFilterId.ColoredPencil;
            bool isFresco =
                filter == PrismFilterId.Fresco;
            bool isCutout =
                filter == PrismFilterId.Cutout;
            bool isDryBrush =
                filter == PrismFilterId.DryBrush;
            bool isDeinterlace =
                filter == PrismFilterId.Deinterlace;
            bool isFacet =
                filter == PrismFilterId.Facet;
            bool isLightingEffects =
                filter == PrismFilterId.LightingEffects;
            bool isChalkCharcoal =
                filter == PrismFilterId.ChalkCharcoal;
            bool isCharcoal =
                filter == PrismFilterId.Charcoal;
            bool isConteCrayon =
                filter == PrismFilterId.ConteCrayon;
            bool isGraphicPen =
                filter == PrismFilterId.GraphicPen;
            bool usesAccentedEdgesKernel =
                filter is
                    PrismFilterId.AccentedEdges or
                    PrismFilterId.DarkStrokes or
                    PrismFilterId.InkOutlines;
            bool isGlowingEdges =
                filter == PrismFilterId.GlowingEdges;
            bool isTraceContour =
                filter == PrismFilterId.TraceContour;
            bool isBasRelief =
                filter == PrismFilterId.BasRelief;
            bool isPosterEdges =
                filter == PrismFilterId.PosterEdges;
            bool isUnderpainting =
                filter == PrismFilterId.Underpainting;
            bool isWatercolor =
                filter == PrismFilterId.Watercolor;
            bool isWaterPaper =
                filter == PrismFilterId.WaterPaper;
            bool isWind =
                filter == PrismFilterId.Wind;
            bool isSumiE =
                filter == PrismFilterId.SumiE;
            bool isChrome =
                filter == PrismFilterId.Chrome;
            bool isNotePaper =
                filter == PrismFilterId.NotePaper;
            bool isPlaster =
                filter == PrismFilterId.Plaster;
            bool usesPhotocopyKernel =
                filter is
                    PrismFilterId.Photocopy or
                    PrismFilterId.Stamp or
                    PrismFilterId.TornEdges;
            bool isCraquelure =
                filter == PrismFilterId.Craquelure;
            bool isTexturizer =
                filter == PrismFilterId.Texturizer;
            bool isGrain =
                filter == PrismFilterId.Grain;
            bool isMosaicTiles =
                filter == PrismFilterId.MosaicTiles;
            bool isPatchwork =
                filter == PrismFilterId.Patchwork;
            bool isReticulation =
                filter == PrismFilterId.Reticulation;
            bool isStainedGlass =
                filter == PrismFilterId.StainedGlass;
            bool isWaveNoise =
                filter is PrismFilterId.Clouds or
                    PrismFilterId.DifferenceClouds;
            bool isSpatter =
                filter == PrismFilterId.Spatter;
            bool isSprayedStrokes =
                filter == PrismFilterId.SprayedStrokes;
            Assert.Equal(
                isAdjustment
                    ? PrismKernelKind.AdjustmentFilter
                    : isNeighborhood
                        ? PrismKernelKind.NeighborhoodFilter
                        : isResampling
                            ? PrismKernelKind.ResamplingFilter
                            : isColoredPencil
                                ? PrismKernelKind.ColoredPencilFilter
                            : isFresco
                                ? PrismKernelKind.FrescoFilter
                            : isCutout
                                ? PrismKernelKind.CutoutFilter
                            : isColorHalftone
                                ? PrismKernelKind.ColorHalftoneFilter
                                : isFacet
                                    ? PrismKernelKind.FacetFilter
                                    : isLightingEffects
                                        ? PrismKernelKind.LightingEffectsFilter
                                    : PrismKernelKind.CatalogFilter,
                filterKernel.Kind);
            Assert.Equal(
                isAdjustment
                    ? "AdjustmentFilter"
                    : isNeighborhood
                        ? "NeighborhoodFilter"
                        : isResampling
                            ? "ResamplingFilter"
                            : isColoredPencil
                                ? "ColoredPencilFilter"
                            : isFresco
                                ? "FrescoFilter"
                            : isCutout
                                ? "CutoutFilter"
                            : isDryBrush
                                ? "DryBrushFilter"
                            : isDeinterlace
                                ? "DeinterlaceFilter"
                            : isColorHalftone
                            ? "ColorHalftoneFilter"
                                : isFacet
                                    ? "FacetFilter"
                                    : isLightingEffects
                                        ? "LightingEffectsFilter"
                                    : isWaveNoise
                                        ? "WaveNoiseFilter"
                                    : isSpatter
                                        ? "SpatterFilter"
                                    : isSprayedStrokes
                                        ? "SprayedStrokesFilter"
                                    : isChalkCharcoal
                                        ? "ChalkCharcoalFilter"
                                    : isCharcoal
                                        ? "CharcoalFilter"
                                    : isConteCrayon
                                        ? "ConteCrayonFilter"
                                    : isGraphicPen
                                        ? "GraphicPenFilter"
                                    : usesAccentedEdgesKernel
                                        ? "AccentedEdgesFilter"
                                    : isGlowingEdges
                                        ? "GlowingEdgesFilter"
                                    : isTraceContour
                                        ? "TraceContourFilter"
                                    : isBasRelief
                                        ? "BasReliefFilter"
                                    : isPosterEdges
                                        ? "PosterEdgesFilter"
                                    : isUnderpainting
                                        ? "UnderpaintingFilter"
                                    : isWatercolor
                                        ? "WatercolorFilter"
                                    : isWaterPaper
                                        ? "WaterPaperFilter"
                                    : isWind
                                        ? "WindFilter"
                                    : isSumiE
                                        ? "SumiEFilter"
                                    : isChrome
                                        ? "ChromeFilter"
                                    : isNotePaper
                                        ? "NotePaperFilter"
                                    : isPlaster
                                        ? "PlasterFilter"
                                    : usesPhotocopyKernel
                                        ? "PhotocopyFilter"
                                    : isCraquelure
                                        ? "CraquelureFilter"
                                    : isTexturizer
                                        ? "TexturizerFilter"
                                    : isGrain
                                        ? "GrainFilter"
                                    : isMosaicTiles
                                        ? "MosaicTilesFilter"
                                    : isPatchwork
                                        ? "PatchworkFilter"
                                    : isReticulation
                                        ? "ReticulationFilter"
                                    : isStainedGlass
                                        ? "StainedGlassFilter"
                                        : "CatalogFilter",
                filterKernel.Technique.Name);
            Assert.Equal(
                isAdjustment
                    ? registry.AdjustmentFilter
                    : isNeighborhood
                        ? registry.NeighborhoodFilter
                        : isResampling
                            ? registry.ResamplingFilter
                            : isColoredPencil
                                ? registry.ColoredPencilFilter
                            : isFresco
                                ? registry.FrescoFilter
                            : isCutout
                                ? registry.CutoutFilter
                            : isDryBrush
                                ? registry.DryBrushFilter
                            : isDeinterlace
                                ? registry.DeinterlaceFilter
                            : isColorHalftone
                            ? registry.ColorHalftoneFilter
                                : isFacet
                                    ? registry.FacetFilter
                                    : isLightingEffects
                                        ? registry.LightingEffectsFilter
                                    : isWaveNoise
                                        ? registry.WaveNoiseFilter
                                    : isSpatter
                                        ? registry.SpatterFilter
                                    : isSprayedStrokes
                                        ? registry.SprayedStrokesFilter
                                    : isChalkCharcoal
                                        ? registry.ChalkCharcoalFilter
                                    : isCharcoal
                                        ? registry.CharcoalFilter
                                    : isConteCrayon
                                        ? registry.ConteCrayonFilter
                                    : isGraphicPen
                                        ? registry.GraphicPenFilter
                                    : usesAccentedEdgesKernel
                                        ? registry.AccentedEdgesFilter
                                    : isGlowingEdges
                                        ? registry.GlowingEdgesFilter
                                    : isTraceContour
                                        ? registry.TraceContourFilter
                                    : isBasRelief
                                        ? registry.BasReliefFilter
                                    : isPosterEdges
                                        ? registry.PosterEdgesFilter
                                    : isUnderpainting
                                        ? registry.UnderpaintingFilter
                                    : isWatercolor
                                        ? registry.WatercolorFilter
                                    : isWaterPaper
                                        ? registry.WaterPaperFilter
                                    : isWind
                                        ? registry.WindFilter
                                    : isSumiE
                                        ? registry.SumiEFilter
                                    : isChrome
                                        ? registry.ChromeFilter
                                    : isNotePaper
                                        ? registry.NotePaperFilter
                                    : isPlaster
                                        ? registry.PlasterFilter
                                    : usesPhotocopyKernel
                                        ? registry.PhotocopyFilter
                                    : isCraquelure
                                        ? registry.CraquelureFilter
                                    : isTexturizer
                                        ? registry.TexturizerFilter
                                    : isGrain
                                        ? registry.GrainFilter
                                    : isMosaicTiles
                                        ? registry.MosaicTilesFilter
                                    : isPatchwork
                                        ? registry.PatchworkFilter
                                    : isReticulation
                                        ? registry.ReticulationFilter
                                    : isStainedGlass
                                        ? registry.StainedGlassFilter
                                        : registry.CatalogFilter,
                filterKernel);
        }
        foreach (PrismStyleId style in Enum.GetValues<PrismStyleId>())
        {
            Assert.True(
                registry.IsFundamentalCatalogEntryRegistered(
                    "style",
                    style.ToString()));
            Assert.True(
                registry.TryGetStyleKernel(
                    style,
                    out PrismKernel styleKernel));
            Assert.Equal(
                PrismKernelKind.LayerStyle,
                styleKernel.Kind);
            Assert.Equal(
                "LayerStyle",
                styleKernel.Technique.Name);
            Assert.Equal(registry.LayerStyle, styleKernel);
        }

        Assert.False(
            registry.TryGetColorConversionKernel(
                (PrismColorProfile)int.MaxValue,
                out _));
        Assert.False(
            registry.TryGetPresentKernel(
                (PrismColorProfile)int.MaxValue,
                out _));
        Assert.False(
            registry.TryGetBlendKernel(
                (PrismBlendMode)int.MaxValue,
                out _));
        Assert.False(
            registry.TryGetStyleKernel(
                (PrismStyleId)int.MaxValue,
                out _));
        Assert.False(
            registry.TryGetFilterKernel(
                (PrismFilterId)int.MaxValue,
                out _));
        Assert.Equal(
            PrismKernelKind.MaskExtract,
            registry.MaskExtract.Kind);
        Assert.Equal(
            "MaskExtract",
            registry.MaskExtract.Technique.Name);
        Assert.Equal(
            PrismKernelKind.MaskFeather,
            registry.MaskFeather.Kind);
        Assert.Equal(
            "MaskFeather",
            registry.MaskFeather.Technique.Name);
    }

    [Fact]
    public void HalftonePatternGpuPreservesDotAreaColorsAndAlpha()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 64;
        const float alpha = 0.4f;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(
            Enumerable.Repeat(
                    new HalfVector4(
                        new Vector4(
                            0.7f * alpha,
                            0.7f * alpha,
                            0.7f * alpha,
                            alpha)),
                    size * size)
                .ToArray());
        using RenderTarget2D target = new(
            graphicsDevice,
            size,
            size,
            mipMap: false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / size, 1f / size),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.HalftonePattern,
                (int)PrismColorProfile.LinearSrgb,
                (int)PrismCatalogFilterPrimitive.Procedural,
                0),
            FilterOptions0 = new Vector4(0, 0, 1, 1),
            FilterOptions1 = Vector4.Zero,
            FilterOptions2 = new Vector4(1, 0, 0, 1),
            FilterOptions3 = Vector4.Zero,
            FilterOptions4 = new Vector4(8, 0, 0, 0),
            FilterOptions9 = new Vector4(
                0,
                0,
                0,
                (int)PrismBlendMode.Normal)
        };
        registry.Bind(registry.CatalogFilter, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, size, size),
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] pixels = new HalfVector4[size * size];
        target.GetData(pixels);
        Vector4[] values = pixels
            .Select(pixel => pixel.ToVector4())
            .ToArray();
        double meanInk = values.Average(value => value.X / value.W);

        Assert.InRange(meanInk, 0.25, 0.35);
        Assert.True(
            values.Max(value => value.X) -
            values.Min(value => value.X) > 0.2f);
        Assert.All(
            values,
            value =>
            {
                Assert.InRange(value.W, alpha - 0.001f, alpha + 0.001f);
                Assert.InRange(
                    Math.Abs((value.X + value.Z) - value.W),
                    0,
                    0.002f);
            });
    }

    [Fact]
    public void WaveNoiseGpuMatchesTheCpuSpectralEvaluation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 8;
        const float scale = 1.75f;
        const uint seed = 2_000_000_007u;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using PrismWaveNoiseTextureCache cache =
            new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(
            Enumerable.Repeat(
                    new HalfVector4(Vector4.One),
                    size * size)
                .ToArray());
        using RenderTarget2D target = new(
            graphicsDevice,
            size,
            size,
            mipMap: false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);
        PrismWaveNoiseTable table = PrismWaveNoise.Precompute(
            unchecked((int)seed),
            new System.Numerics.Vector4(
                0.03125f,
                1,
                0,
                0),
            PrismWaveSpectrum.Brown);
        Texture2D tableTexture = cache.GetOrCreate(table);
        Vector4 foreground = new(0.8f, 0.1f, 0.2f, 1);
        Vector4 background = new(0.1f, 0.6f, 0.9f, 1);

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / size, 1f / size),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Clouds,
                (int)PrismColorProfile.LinearSrgb,
                (int)PrismCatalogFilterPrimitive.Procedural,
                0),
            FilterOptions0 = foreground,
            FilterOptions1 = background,
            FilterOptions2 = new Vector4(scale, 0, 0, 0),
            FilterOptions3 = new Vector4(
                seed & 0xffffu,
                seed >> 16,
                0,
                0),
            FilterOptions4 = new Vector4(20, 0, 0, 0),
            FilterOptions5 = new Vector4(4, 0, 0, 0),
            FilterOptions7 = new Vector4(0, 1, 0, 0),
            FilterOptions9 = new Vector4(
                table.Normalization,
                0,
                0,
                (int)PrismBlendMode.Normal),
            FilterAuxiliaryTexture = tableTexture
        };
        registry.Bind(registry.WaveNoiseFilter, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, size, size),
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] actual = new HalfVector4[size * size];
        target.GetData(actual);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = PrismWaveNoise.Sample(
                    table,
                    new System.Numerics.Vector2(
                        (x + 0.5f) / scale,
                        (y + 0.5f) / scale),
                    seed,
                    20,
                    4,
                    new System.Numerics.Vector4(0, 1, 0, 0));
                Vector3 expected = Vector3.Lerp(
                    new Vector3(
                        background.X,
                        background.Y,
                        background.Z),
                    new Vector3(
                        foreground.X,
                        foreground.Y,
                        foreground.Z),
                    noise);
                Vector4 pixel = actual[(y * size) + x].ToVector4();
                Assert.True(
                    MathF.Abs(pixel.X - expected.X) <= 0.006f,
                    $"Wave noise at ({x}, {y}) was {pixel}, " +
                    $"expected {expected} from noise {noise:R}.");
                Assert.InRange(
                    MathF.Abs(pixel.Y - expected.Y),
                    0,
                    0.006f);
                Assert.InRange(
                    MathF.Abs(pixel.Z - expected.Z),
                    0,
                    0.006f);
                Assert.InRange(pixel.W, 0.999f, 1);
            }
        }
    }

    [Fact]
    public void ColorHalftoneGpuProducesChromaticAngleSensitiveScreens()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 17;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(
            Enumerable.Repeat(
                    new HalfVector4(
                        new Vector4(0.15f, 0.45f, 0.3f, 0.75f)),
                    size * size)
                .ToArray());
        using RenderTarget2D firstTarget = CreateTarget();
        using RenderTarget2D changedTarget = CreateTarget();
        Vector4 angles = new(108, 162, 90, 45);

        HalfVector4[] first = Render(firstTarget, angles);
        HalfVector4[] changed = Render(
            changedTarget,
            new Vector4(139, angles.Y, angles.Z, angles.W));

        Assert.All(
            first,
            pixel => Assert.InRange(
                pixel.ToVector4().W,
                0.749f,
                0.751f));
        Assert.Contains(
            first,
            pixel =>
            {
                Vector4 value = pixel.ToVector4();
                return MathF.Abs(value.X - value.Y) > 0.02f ||
                    MathF.Abs(value.Y - value.Z) > 0.02f;
            });
        Assert.Contains(
            first.Zip(changed),
            pair =>
                Vector4.Distance(
                    pair.First.ToVector4(),
                    pair.Second.ToVector4()) > 0.02f);

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                size,
                size,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        HalfVector4[] Render(
            RenderTarget2D target,
            Vector4 screenAngles)
        {
            Vector4 radians = screenAngles * (MathF.PI / 180f);
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                source,
                1,
                new Vector2(1f / size, 1f / size),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.ColorHalftone,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Quantization,
                    0),
                FilterOptions2 = new Vector4(
                    MathF.Cos(radians.X),
                    MathF.Cos(radians.Y),
                    MathF.Cos(radians.Z),
                    MathF.Cos(radians.W)),
                FilterOptions3 = new Vector4(
                    MathF.Sin(radians.X),
                    MathF.Sin(radians.Y),
                    MathF.Sin(radians.Z),
                    MathF.Sin(radians.W)),
                FilterOptions9 = new Vector4(
                    4,
                    4,
                    0,
                    (int)PrismBlendMode.Normal)
            };
            registry.Bind(registry.ColorHalftoneFilter, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                source,
                new Rectangle(0, 0, size, size),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] pixels = new HalfVector4[size * size];
            target.GetData(pixels);
            return pixels;
        }
    }

    [Fact]
    public void ColoredPencilGpuRunsTensorBlurAndSwingBilateralComposite()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 17;
        const float alpha = 0.65f;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        HalfVector4[] sourcePixels =
            new HalfVector4[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool firstRegion = x + y < size;
                Vector4 straight = firstRegion
                    ? new Vector4(0.15f, 0.45f, 0.75f, 1)
                    : new Vector4(0.85f, 0.35f, 0.2f, 1);
                sourcePixels[(y * size) + x] =
                    new HalfVector4(
                        new Vector4(
                            straight.X * alpha,
                            straight.Y * alpha,
                            straight.Z * alpha,
                            alpha));
            }
        }
        source.SetData(sourcePixels);

        using RenderTarget2D tensor = CreateTarget();
        using RenderTarget2D blurX = CreateTarget();
        using RenderTarget2D blurY = CreateTarget();
        using RenderTarget2D result = CreateTarget();
        using RenderTarget2D softResult = CreateTarget();
        RenderPass(source, tensor, source, 0, 1, 1, 8);
        RenderPass(tensor, blurX, source, 5, 2, 0, 8);
        RenderPass(blurX, blurY, source, 10, 0, 2, 8);
        RenderPass(blurY, result, source, 15, 3, 3, 8);
        RenderPass(blurY, softResult, source, 15, 3, 3, 2);

        HalfVector4[] actual = new HalfVector4[size * size];
        HalfVector4[] soft = new HalfVector4[size * size];
        result.GetData(actual);
        softResult.GetData(soft);
        Assert.All(
            actual,
            pixel => Assert.InRange(
                pixel.ToVector4().W,
                alpha - 0.002f,
                alpha + 0.002f));
        Assert.Contains(
            actual.Zip(sourcePixels),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                pair.Second.ToVector4()) > 0.05f);
        Assert.Contains(
            actual.Zip(soft),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                pair.Second.ToVector4()) > 0.01f);

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                size,
                size,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        void RenderPass(
            Texture2D input,
            RenderTarget2D target,
            Texture2D original,
            float packedPass,
            float radiusX,
            float radiusY,
            float pressure)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                input,
                1,
                new Vector2(1f / size, 1f / size),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.ColoredPencil,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Artistic,
                    0),
                FilterOptions0 = new Vector4(3, 0, 0, 0),
                FilterOptions1 = new Vector4(pressure, 0, 0, 0),
                FilterOptions2 = new Vector4(0.25f, 0, 0, 0),
                FilterOptions3 = Vector4.One,
                FilterOptions9 = new Vector4(
                    radiusX,
                    radiusY,
                    packedPass,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = original
            };
            registry.Bind(
                registry.ColoredPencilFilter,
                in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, size, size),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }
    }

    [Fact]
    public void FrescoGpuRunsSmoothedTensorAndAnisotropicKuwaharaComposite()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 17;
        const float alpha = 0.65f;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        HalfVector4[] sourcePixels =
            new HalfVector4[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float value = x < size / 2 ? 0.15f : 0.85f;
                value += ((x + y) & 1) == 0 ? -0.08f : 0.08f;
                sourcePixels[(y * size) + x] =
                    new HalfVector4(
                        new Vector4(
                            value * alpha,
                            value * alpha,
                            value * alpha,
                            alpha));
            }
        }
        source.SetData(sourcePixels);

        using RenderTarget2D tensor = CreateTarget();
        using RenderTarget2D blurX = CreateTarget();
        using RenderTarget2D blurY = CreateTarget();
        using RenderTarget2D result = CreateTarget();
        using RenderTarget2D texturedResult = CreateTarget();
        RenderPass(source, tensor, source, 0, 1, 1, 0);
        RenderPass(tensor, blurX, source, 5, 2, 0, 0);
        RenderPass(blurX, blurY, source, 10, 0, 2, 0);
        RenderPass(blurY, result, source, 15, 3, 3, 0);
        RenderPass(blurY, texturedResult, source, 15, 3, 3, 8);

        HalfVector4[] actual = new HalfVector4[size * size];
        HalfVector4[] textured = new HalfVector4[size * size];
        result.GetData(actual);
        texturedResult.GetData(textured);
        Assert.All(
            actual,
            pixel => Assert.InRange(
                pixel.ToVector4().W,
                alpha - 0.002f,
                alpha + 0.002f));
        Assert.Contains(
            actual.Zip(sourcePixels),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                pair.Second.ToVector4()) > 0.02f);
        Assert.Contains(
            actual.Zip(textured),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                pair.Second.ToVector4()) > 0.005f);

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                size,
                size,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        void RenderPass(
            Texture2D input,
            RenderTarget2D target,
            Texture2D original,
            float packedPass,
            float radiusX,
            float radiusY,
            float texture)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                input,
                1,
                new Vector2(1f / size, 1f / size),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.Fresco,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Artistic,
                    0),
                FilterOptions0 = new Vector4(3, 0, 0, 0),
                FilterOptions1 = new Vector4(8, 0, 0, 0),
                FilterOptions2 = new Vector4(texture, 0, 0, 0),
                FilterOptions9 = new Vector4(
                    radiusX,
                    radiusY,
                    packedPass,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = original
            };
            registry.Bind(registry.FrescoFilter, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, size, size),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }
    }

    [Theory]
    [InlineData(PrismFilterId.AccentedEdges)]
    [InlineData(PrismFilterId.DarkStrokes)]
    [InlineData(PrismFilterId.InkOutlines)]
    public void XDogGpuMatchesTheCpuReference(PrismFilterId filter)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 31;
        const int height = 19;
        const float alpha = 0.7f;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        float option0 = filter switch
        {
            PrismFilterId.AccentedEdges => 0,
            PrismFilterId.InkOutlines => 20,
            _ => 5
        };
        float option1 = filter == PrismFilterId.InkOutlines
            ? 10
            : filter == PrismFilterId.AccentedEdges ? 2 : 6;
        float option2 = filter == PrismFilterId.InkOutlines
            ? 4
            : filter == PrismFilterId.AccentedEdges ? 5 : 2;
        PrismPremultipliedColor[] sourceColors =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double value = x < width / 2 ? 0.2 : 0.8;
                sourceColors[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value,
                        value,
                        alpha);
            }
        }

        using Texture2D source = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(sourceColors.Select(ToHalfVector).ToArray());
        using RenderTarget2D horizontal = CreateTarget();
        using RenderTarget2D vertical = CreateTarget();
        using RenderTarget2D result = CreateTarget();
        RenderPass(source, horizontal, 1);
        RenderPass(horizontal, vertical, 6);
        RenderPass(vertical, result, 8);

        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            filter,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Number,
                    numberValue: option0),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: option1),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: option2)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));
        PrismPremultipliedColor[] expected =
            PrismCatalogFilterMath.Apply(
                plan,
                sourceColors,
                width,
                height,
                PrismColorProfile.LinearSrgb);
        HalfVector4[] actual = new HalfVector4[width * height];
        result.GetData(actual);
        double meanDifference = actual
            .Select((pixel, index) =>
            {
                Vector4 value = pixel.ToVector4();
                PrismPremultipliedColor reference = expected[index];
                Assert.InRange(value.W, alpha - 0.002f, alpha + 0.002f);
                Assert.InRange(value.X, 0, value.W);
                Assert.InRange(value.Y, 0, value.W);
                Assert.InRange(value.Z, 0, value.W);
                return
                    Math.Abs(value.X - reference.Red) +
                    Math.Abs(value.Y - reference.Green) +
                    Math.Abs(value.Z - reference.Blue);
            })
            .Average();
        Assert.InRange(meanDifference, 0, 0.02);
        Assert.Contains(
            actual.Zip(sourceColors),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                new Vector4(
                    (float)pair.Second.Red,
                    (float)pair.Second.Green,
                    (float)pair.Second.Blue,
                    (float)pair.Second.Alpha)) > 0.05f);

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                width,
                height,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        void RenderPass(
            Texture2D input,
            RenderTarget2D target,
            float packedPass)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                input,
                1,
                new Vector2(1f / width, 1f / height),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)filter,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Artistic,
                    0),
                FilterOptions0 = new Vector4(option0, 0, 0, 0),
                FilterOptions1 = new Vector4(option1, 0, 0, 0),
                FilterOptions2 = new Vector4(option2, 0, 0, 0),
                FilterOptions3 = new Vector4(1, 1.6f, 2, 4),
                FilterOptions9 = new Vector4(
                    4,
                    4,
                    packedPass,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = source
            };
            registry.Bind(registry.AccentedEdgesFilter, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, width, height),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }
    }

    [Fact]
    public void PosterEdgesGpuRunsGuidedFilterQuantizationAndInkComposite()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 25;
        const int height = 17;
        const float alpha = 0.65f;
        const float radius = 2;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        HalfVector4[] sourcePixels =
            new HalfVector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = x < width / 2 ? 0.2f : 0.8f;
                value += ((x + y) & 1) == 0 ? -0.04f : 0.04f;
                sourcePixels[(y * width) + x] =
                    new HalfVector4(
                        new Vector4(
                            value * alpha,
                            value * alpha,
                            value * alpha,
                            alpha));
            }
        }
        source.SetData(sourcePixels);

        using RenderTarget2D momentsX = CreateTarget();
        using RenderTarget2D momentsY = CreateTarget();
        using RenderTarget2D coefficients = CreateTarget();
        using RenderTarget2D coefficientsX = CreateTarget();
        using RenderTarget2D guided = CreateTarget();
        using RenderTarget2D result = CreateTarget();
        using RenderTarget2D noInk = CreateTarget();
        RenderPass(source, momentsX, 1, radius, 0, 1);
        RenderPass(momentsX, momentsY, 6, 0, radius, 1);
        RenderPass(momentsY, coefficients, 8, 0, 0, 1);
        RenderPass(coefficients, coefficientsX, 13, radius, 0, 1);
        RenderPass(coefficientsX, guided, 18, 0, radius, 1);
        RenderPass(guided, result, 20, radius, radius, 1);
        RenderPass(guided, noInk, 20, radius, radius, 0);

        HalfVector4[] actual = new HalfVector4[width * height];
        HalfVector4[] noInkPixels = new HalfVector4[width * height];
        result.GetData(actual);
        noInk.GetData(noInkPixels);
        Assert.All(
            actual,
            pixel =>
            {
                Vector4 value = pixel.ToVector4();
                Assert.InRange(value.W, alpha - 0.002f, alpha + 0.002f);
                Assert.True(float.IsFinite(value.X));
                Assert.InRange(value.X, 0, value.W);
                Assert.InRange(value.Y, 0, value.W);
                Assert.InRange(value.Z, 0, value.W);
            });
        Assert.Contains(
            actual.Zip(sourcePixels),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                pair.Second.ToVector4()) > 0.05f);
        Assert.True(
            BoundaryMean(actual) <
            BoundaryMean(noInkPixels));

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                width,
                height,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        void RenderPass(
            Texture2D input,
            RenderTarget2D target,
            float packedPass,
            float radiusX,
            float radiusY,
            float edgeIntensity)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                input,
                1,
                new Vector2(1f / width, 1f / height),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.PosterEdges,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Artistic,
                    0),
                FilterOptions0 = new Vector4(
                    edgeIntensity,
                    0,
                    0,
                    0),
                FilterOptions1 = new Vector4(radius, 0, 0, 0),
                FilterOptions2 = new Vector4(4, 0, 0, 0),
                FilterOptions9 = new Vector4(
                    radiusX,
                    radiusY,
                    packedPass,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = source
            };
            registry.Bind(registry.PosterEdgesFilter, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, width, height),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }

        static float BoundaryMean(HalfVector4[] pixels) =>
            pixels
                .Where((_, index) =>
                    index % width is 11 or 12 or 13)
                .Average(pixel =>
                {
                    Vector4 value = pixel.ToVector4();
                    return value.W <= 0
                        ? 0
                        : value.X / value.W;
                });
    }

    [Fact]
    public void BasReliefGpuMatchesCpuAndReversesDirectionalShading()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 25;
        const int height = 17;
        const float alpha = 0.65f;
        const float radius = 3;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        PrismPremultipliedColor[] cpuSource =
            new PrismPremultipliedColor[width * height];
        HalfVector4[] sourcePixels =
            new HalfVector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = x < width / 2 ? 0.2f : 0.8f;
                value += ((x + y) & 1) == 0 ? -0.04f : 0.04f;
                int index = (y * width) + x;
                cpuSource[index] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value,
                        value,
                        alpha);
                sourcePixels[index] = new HalfVector4(
                    new Vector4(
                        value * alpha,
                        value * alpha,
                        value * alpha,
                        alpha));
            }
        }
        source.SetData(sourcePixels);

        HalfVector4[] left = RenderRelief(lightDirection: 6);
        HalfVector4[] right = RenderRelief(lightDirection: 2);
        PrismCatalogFilterPlan cpuPlan =
            PrismCatalogFilterPlanner.Create(
                PrismFilterId.BasRelief,
                [
                    new PrismGraphParameter(
                        0,
                        PrismGraphParameterValueKind.Color,
                        colorValue: CernealaColor.White),
                    new PrismGraphParameter(
                        1,
                        PrismGraphParameterValueKind.Number,
                        numberValue: 13),
                    new PrismGraphParameter(
                        2,
                        PrismGraphParameterValueKind.Color,
                        colorValue: CernealaColor.Black),
                    new PrismGraphParameter(
                        3,
                        PrismGraphParameterValueKind.Symbol,
                        integerValue: PrismCatalogRuntime.ResolveSymbol(
                            "LightDirection",
                            "Left")),
                    new PrismGraphParameter(
                        4,
                        PrismGraphParameterValueKind.Number,
                        numberValue: 3)
                ],
                PrismBlendMode.Normal,
                pixelScale: 1,
                System.Numerics.Matrix3x2.Identity,
                new DrawRect(0, 0, width, height));
        PrismPremultipliedColor[] expected =
            PrismCatalogFilterMath.Apply(
                cpuPlan,
                cpuSource,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.True(BoundaryMean(left) > BoundaryMean(right));
        for (int index = 0; index < expected.Length; index++)
        {
            AssertHalfVectorWithin(
                left[index],
                expected[index],
                tolerance: 0.025,
                context: $"BasRelief pixel {index}");
        }

        HalfVector4[] RenderRelief(float lightDirection)
        {
            using RenderTarget2D momentsX = CreateTarget();
            using RenderTarget2D momentsY = CreateTarget();
            using RenderTarget2D coefficients = CreateTarget();
            using RenderTarget2D coefficientsX = CreateTarget();
            using RenderTarget2D guided = CreateTarget();
            using RenderTarget2D result = CreateTarget();
            RenderPass(source, momentsX, 1, radius, 0, lightDirection);
            RenderPass(momentsX, momentsY, 6, 0, radius, lightDirection);
            RenderPass(momentsY, coefficients, 8, 0, 0, lightDirection);
            RenderPass(
                coefficients,
                coefficientsX,
                13,
                radius,
                0,
                lightDirection);
            RenderPass(
                coefficientsX,
                guided,
                18,
                0,
                radius,
                lightDirection);
            RenderPass(guided, result, 20, 1, 1, lightDirection);
            HalfVector4[] pixels = new HalfVector4[width * height];
            result.GetData(pixels);
            return pixels;
        }

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                width,
                height,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        void RenderPass(
            Texture2D input,
            RenderTarget2D target,
            float packedPass,
            float radiusX,
            float radiusY,
            float lightDirection)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                input,
                1,
                new Vector2(1f / width, 1f / height),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.BasRelief,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.EdgeDetection,
                    0),
                FilterOptions0 = Vector4.One,
                FilterOptions1 = new Vector4(13, 0, 0, 0),
                FilterOptions2 = new Vector4(0, 0, 0, 1),
                FilterOptions3 = new Vector4(lightDirection, 0, 0, 0),
                FilterOptions4 = new Vector4(3, 0, 0, 0),
                FilterOptions9 = new Vector4(
                    radiusX,
                    radiusY,
                    packedPass,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = source
            };
            registry.Bind(registry.BasReliefFilter, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, width, height),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }

        static float BoundaryMean(HalfVector4[] pixels) =>
            pixels
                .Where((_, index) =>
                    index % width is 11 or 12 or 13)
                .Average(pixel =>
                {
                    Vector4 value = pixel.ToVector4();
                    return value.W <= 0
                        ? 0
                        : value.X / value.W;
                });
    }

    [Fact]
    public void CutoutGpuRunsBoundedMeanShiftAndQuantizesOnce()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 17;
        const int height = 9;
        const float alpha = 0.6f;
        const float levels = 8;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        HalfVector4[] sourcePixels =
            new HalfVector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool leftRegion = x < width / 2;
                float noise =
                    ((((x * 3) + (y * 5)) % 7) - 3) * 0.018f;
                Vector3 straight = leftRegion
                    ? new Vector3(
                        0.18f + noise,
                        0.45f - (noise * 0.5f),
                        0.72f + (noise * 0.3f))
                    : new Vector3(
                        0.78f + noise,
                        0.28f - (noise * 0.3f),
                        0.14f - (noise * 0.5f));
                sourcePixels[(y * width) + x] =
                    new HalfVector4(
                        new Vector4(straight * alpha, alpha));
            }
        }
        source.SetData(sourcePixels);

        using RenderTarget2D shift0 = CreateTarget();
        using RenderTarget2D shift1 = CreateTarget();
        using RenderTarget2D result = CreateTarget();
        using RenderTarget2D originalResult = CreateTarget();
        RenderPass(source, shift0, packedPass: 3, opacity: 1);
        RenderPass(shift0, shift1, packedPass: 7, opacity: 1);
        RenderPass(shift1, result, packedPass: 8, opacity: 1);
        RenderPass(
            shift1,
            originalResult,
            packedPass: 8,
            opacity: 0);

        HalfVector4[] actual = new HalfVector4[width * height];
        HalfVector4[] opacityZero = new HalfVector4[width * height];
        result.GetData(actual);
        originalResult.GetData(opacityZero);
        Assert.All(
            actual,
            pixel =>
            {
                Vector4 color = pixel.ToVector4();
                Assert.InRange(
                    color.W,
                    alpha - 0.002f,
                    alpha + 0.002f);
                Vector3 straight =
                    new(color.X, color.Y, color.Z);
                straight /= color.W;
                Assert.InRange(
                    MathF.Abs(
                        (straight.X * (levels - 1)) -
                        MathF.Round(straight.X * (levels - 1))),
                    0,
                    0.01f);
                Assert.InRange(
                    MathF.Abs(
                        (straight.Y * (levels - 1)) -
                        MathF.Round(straight.Y * (levels - 1))),
                    0,
                    0.01f);
                Assert.InRange(
                    MathF.Abs(
                        (straight.Z * (levels - 1)) -
                        MathF.Round(straight.Z * (levels - 1))),
                    0,
                    0.01f);
            });
        Assert.Contains(
            actual.Zip(sourcePixels),
            pair => Vector4.Distance(
                pair.First.ToVector4(),
                pair.Second.ToVector4()) > 0.02f);
        Assert.All(
            opacityZero.Zip(sourcePixels),
            pair => Assert.True(
                Vector4.Distance(
                    pair.First.ToVector4(),
                    pair.Second.ToVector4()) < 0.003f));

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                width,
                height,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        void RenderPass(
            Texture2D input,
            RenderTarget2D target,
            float packedPass,
            float opacity)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                input,
                opacity,
                new Vector2(1f / width, 1f / height),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.Cutout,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Artistic,
                    0),
                FilterOptions0 =
                    new Vector4(levels, 0, 0, 0),
                FilterOptions1 = new Vector4(4, 0, 0, 0),
                FilterOptions2 = new Vector4(3, 0, 0, 0),
                FilterOptions9 = new Vector4(
                    4,
                    4,
                    packedPass,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = source
            };
            registry.Bind(registry.CutoutFilter, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, width, height),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
        }
    }

    [Fact]
    public void LightingEffectsGpuUsesPackedLightsHeightNormalsAndExposure()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 7;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(
            Enumerable.Repeat(
                    new HalfVector4(
                        new Vector4(0.28f, 0.12f, 0.04f, 0.4f)),
                    size * size)
                .ToArray());
        using Texture2D height = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        height.SetData(
            Enumerable.Range(0, size * size)
                .Select(index =>
                {
                    float value =
                        (index % size) / (float)(size - 1);
                    return new HalfVector4(
                        new Vector4(value, value, value, 1));
                })
                .ToArray());
        using RenderTarget2D flatTarget = CreateTarget();
        using RenderTarget2D reliefTarget = CreateTarget();
        using RenderTarget2D exposedTarget = CreateTarget();
        Vector4[] packedLights = new Vector4[24];
        packedLights[0] = new Vector4(0, 1.5f, 0, 0);
        packedLights[1] = Vector4.Normalize(
            new Vector4(0.6f, -0.2f, 1, 0));
        packedLights[2] = new Vector4(1, 0.8f, 0.6f, 0);

        HalfVector4[] flat = Render(flatTarget, 0, 0);
        HalfVector4[] relief = Render(reliefTarget, 8, 0);
        HalfVector4[] exposed = Render(exposedTarget, 8, 1);

        Assert.Contains(
            flat.Zip(relief),
            pair =>
                Vector4.Distance(
                    pair.First.ToVector4(),
                    pair.Second.ToVector4()) > 0.01f);
        int center = ((size / 2) * size) + (size / 2);
        Assert.True(
            exposed[center].ToVector4().X >
            relief[center].ToVector4().X);
        Assert.All(
            relief,
            pixel =>
            {
                Vector4 value = pixel.ToVector4();
                Assert.True(
                    float.IsFinite(value.X) &&
                    float.IsFinite(value.Y) &&
                    float.IsFinite(value.Z) &&
                    float.IsFinite(value.W));
                Assert.InRange(value.W, 0.399f, 0.401f);
                Assert.InRange(value.X, 0, value.W);
                Assert.InRange(value.Y, 0, value.W);
                Assert.InRange(value.Z, 0, value.W);
            });

        RenderTarget2D CreateTarget() =>
            new(
                graphicsDevice,
                size,
                size,
                mipMap: false,
                SurfaceFormat.HalfVector4,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);

        HalfVector4[] Render(
            RenderTarget2D target,
            float textureHeight,
            float exposure)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                source,
                1,
                new Vector2(1f / size, 1f / size),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.LightingEffects,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismCatalogFilterPrimitive.Procedural,
                    2),
                FilterOptions1 = new Vector4(0.05f, 0, 0, 0),
                FilterOptions2 = new Vector4(0.25f, 0, 0, 0),
                FilterOptions3 = new Vector4(0.8f, 0, 0, 0),
                FilterOptions4 = new Vector4(exposure, 0, 0, 0),
                FilterOptions6 = new Vector4(
                    textureHeight,
                    0,
                    0,
                    0),
                FilterOptions9 = new Vector4(
                    0,
                    0,
                    0,
                    (int)PrismBlendMode.Normal),
                FilterAuxiliaryTexture = height,
                FilterLightCount = 1,
                FilterLights = packedLights
            };
            registry.Bind(
                registry.LightingEffectsFilter,
                in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                source,
                new Rectangle(0, 0, size, size),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] pixels = new HalfVector4[size * size];
            target.GetData(pixels);
            return pixels;
        }
    }

    [Fact]
    public void StrokeGpuProducesSolidEuclideanOutsideBand()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 17;
        const int center = size / 2;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        HalfVector4[] sourcePixels = new HalfVector4[size * size];
        sourcePixels[(center * size) + center] =
            new HalfVector4(Vector4.One);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(sourcePixels);
        using RenderTarget2D output = new(
            graphicsDevice,
            size,
            size,
            mipMap: false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);
        using RenderTarget2D distanceA = new(
            graphicsDevice,
            size,
            size,
            mipMap: false,
            SurfaceFormat.Vector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);
        using RenderTarget2D distanceB = new(
            graphicsDevice,
            size,
            size,
            mipMap: false,
            SurfaceFormat.Vector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);

        DrawDistancePass(
            registry.StrokeDistanceSeed,
            source,
            distanceA,
            Vector2.Zero);
        RenderTarget2D read = distanceA;
        RenderTarget2D write = distanceB;
        for (int jump = 16; jump >= 1; jump >>= 1)
        {
            DrawDistancePass(
                registry.StrokeDistanceFlood,
                read,
                write,
                new Vector2(
                    jump / (float)size,
                    jump / (float)size));
            (read, write) = (write, read);
        }
        DrawDistancePass(
            registry.StrokeDistanceFlood,
            read,
            write,
            new Vector2(1f / size, 1f / size));
        read = write;

        graphicsDevice.SetRenderTarget(output);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / size, 1f / size),
            Vector2.One,
            Vector2.Zero)
        {
            StyleTexture = source,
            StyleMaskTexture = read,
            StyleColor = new Vector4(1, 0, 0, 1),
            StyleGeometry0 = new Vector4(0, 0, 3, 0),
            StyleOptions0 = new Vector4(1, 0, 0, 0),
            StyleModes0 = new Vector4(
                9,
                (int)PrismBlendMode.Normal,
                0,
                0),
            StyleModes1 = Vector4.Zero
        };
        registry.Bind(registry.LayerStyle, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, size, size),
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] pixels = new HalfVector4[size * size];
        output.GetData(pixels);

        Assert.InRange(AlphaAt(2, 0), 0.997f, 1f);
        Assert.InRange(AlphaAt(2, 2), 0.997f, 1f);
        Assert.InRange(AlphaAt(4, 1), 0f, 0.003f);

        float AlphaAt(int offsetX, int offsetY) =>
            pixels[
                ((center + offsetY) * size) +
                center + offsetX]
            .ToVector4()
            .W;

        void DrawDistancePass(
            PrismKernel kernel,
            Texture2D input,
            RenderTarget2D target,
            Vector2 jump)
        {
            graphicsDevice.SetRenderTarget(target);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters passParameters = new(
                input,
                1,
                new Vector2(1f / size, 1f / size),
                Vector2.One,
                Vector2.Zero)
            {
                MaskFeatherStep = jump
            };
            registry.Bind(kernel, in passParameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                input,
                new Rectangle(0, 0, size, size),
                XnaColor.White);
            spriteBatch.End();
        }
    }

    [Fact]
    public void StrokeExecutorPreparesAndConsumesSignedDistanceField()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 17;
        const int center = size / 2;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using TestPrismRenderer renderer = new(
            graphicsDevice,
            size,
            size);
        using PrismGraphExecutor executor = new(graphicsDevice);
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Stroke",
            styles: [new PrismStyleDefinition(PrismStyleId.Stroke)]);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition("Stroke", layer),
            bounds: new DrawRect(0, 0, size, size));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(center, center, 1, 1),
                CernealaColor.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));

        ExecuteFrame(
            renderer,
            executor,
            commands,
            analysis,
            plan,
            new Viewport(0, 0, size, size));
        XnaColor[] pixels = renderer.ReadPixels();

        Assert.InRange(AlphaAt(2, 0), 254, 255);
        Assert.InRange(AlphaAt(2, 2), 254, 255);
        Assert.InRange(AlphaAt(4, 1), 0, 1);
        Assert.Equal(0, executor.Diagnostics.Count);
        Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);

        byte AlphaAt(int offsetX, int offsetY) =>
            pixels[
                ((center + offsetY) * size) +
                center + offsetX]
            .A;
    }

    [Fact]
    public void TransformExecutionUsesMipmappedAnisotropicSampling()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using TestPrismRenderer renderer = new(
            graphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        using PrismGraphExecutor executor = new(graphicsDevice);
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Transform",
            filters: [new PrismFilterDefinition(PrismFilterId.Transform)]);
        PrismCompositionDefinition composition =
            PrismTestData.Composition("Transform", layer);
        PrismInstance instance = new(composition);
        instance.GetLayerState(new PrismNodeId(1))
            .Filters
            .Single()
            .SetValue(
                PrismCatalog.GetFilter(PrismFilterId.Transform)
                    .Parameters
                    .Single(parameter => parameter.Name == "Scale"),
                new System.Numerics.Vector4(0.25f, 0.25f, 0, 0));
        PrismDrawScope scope = new(
            instance,
            new PrismCacheOwnerToken(1),
            new DrawRect(0, 0, SurfaceWidth, SurfaceHeight),
            System.Numerics.Matrix3x2.Identity,
            1,
            1,
            PrismDrawResources.Empty);
        DrawCommandList commands = new();
        commands.Add(DrawCommand.BeginPrism(scope));
        for (int y = 0; y < SurfaceHeight; y++)
        {
            for (int x = 0; x < SurfaceWidth; x++)
            {
                commands.Add(DrawCommand.FillRectangle(
                    new DrawRect(x, y, 1, 1),
                    (x + y) % 2 == 0
                        ? CernealaColor.White
                        : new CernealaColor(0, 0, 0, 255)));
            }
        }
        commands.Add(DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));

        ExecuteFrame(
            renderer,
            executor,
            commands,
            analysis,
            plan,
            new Viewport(0, 0, SurfaceWidth, SurfaceHeight));

        Assert.True(renderer.UsedAnisotropicKernelSampler);
        Assert.Equal(0, executor.Diagnostics.Count);
        XnaColor center = renderer.ReadCenterPixel();
        Assert.InRange(center.R, 70, 200);
        Assert.Equal(center.R, center.G);
        Assert.Equal(center.R, center.B);
    }

    [Fact]
    public void ChannelMixerGpuMatchesCpuLinearRgbMatrixAcrossProfiles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismAdjustmentPlan matrix = new(
            PrismFilterId.ChannelMixer,
            PrismAdjustmentOperation.ChannelMixer,
            PrismBlendMode.Normal)
        {
            Parameters0 = new System.Numerics.Vector4(
                0.5f, 0.25f, 0.1f, 0),
            Parameters1 = new System.Numerics.Vector4(
                0.2f, 0.6f, 0.1f, 0),
            Parameters2 = new System.Numerics.Vector4(
                0.1f, 0.3f, 0.5f, 0),
            Parameters3 = new System.Numerics.Vector4(
                0.05f, 0.1f, 0, 0)
        };
        PrismPremultipliedColor[] input =
        [
            PrismPremultipliedColor.FromStraight(
                0.2, 0.4, 0.6, 0.5),
            PrismPremultipliedColor.FromStraight(
                0.7, 0.1, 0.3, 1),
            default
        ];
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);

        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.ChannelMixer,
                out PrismKernel kernel));
        Assert.Equal(PrismKernelKind.AdjustmentFilter, kernel.Kind);

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor[] working = input
                .Select(color => PrismColorPipeline.ConvertInputToWorking(
                    color,
                    profile))
                .ToArray();
            using Texture2D source = CreateHalfVectorTexture(
                graphicsDevice,
                working);
            using RenderTarget2D output = CreateTarget(
                graphicsDevice,
                working.Length,
                SurfaceFormat.HalfVector4);

            foreach (bool monochrome in new[] { false, true })
            {
                PrismAdjustmentPlan plan = matrix with
                {
                    Parameters4 = new System.Numerics.Vector4(
                        monochrome ? 1 : 0,
                        0,
                        0,
                        0)
                };
                graphicsDevice.SetRenderTarget(output);
                graphicsDevice.Clear(XnaColor.Transparent);
                PrismKernelParameters parameters = new(
                    source,
                    1,
                    new Vector2(1f / working.Length, 1),
                    Vector2.One,
                    Vector2.Zero)
                {
                    FilterHeader = new Microsoft.Xna.Framework.Vector4(
                        (int)PrismAdjustmentOperation.ChannelMixer,
                        (int)profile,
                        (int)PrismBlendMode.Normal,
                        0),
                    FilterOptions0 = ToXnaVector4(plan.Parameters0),
                    FilterOptions1 = ToXnaVector4(plan.Parameters1),
                    FilterOptions2 = ToXnaVector4(plan.Parameters2),
                    FilterOptions3 = ToXnaVector4(plan.Parameters3),
                    FilterOptions4 = ToXnaVector4(plan.Parameters4),
                    FilterTextureSize = new Vector2(
                        working.Length,
                        1)
                };
                registry.Bind(kernel, in parameters);
                spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    BlendState.Opaque,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    registry.Effect);
                spriteBatch.Draw(
                    source,
                    new Rectangle(0, 0, working.Length, 1),
                    XnaColor.White);
                spriteBatch.End();
                graphicsDevice.SetRenderTarget(null);
                HalfVector4[] actual = new HalfVector4[working.Length];
                output.GetData(actual);

                for (int index = 0; index < working.Length; index++)
                {
                    AssertHalfVectorWithin(
                        actual[index],
                        PrismAdjustmentMath.Apply(
                            plan,
                            working[index],
                            profile),
                        tolerance: 0.003,
                        $"{profile} monochrome={monochrome} " +
                            $"sample {index}");
                }
            }
        }
    }

    [Fact]
    public void PosterizeGpuMatchesCpuUniformLinearRgbQuantizationAcrossProfiles()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismAdjustmentPlan plan = new(
            PrismFilterId.Posterize,
            PrismAdjustmentOperation.Posterize,
            PrismBlendMode.Normal)
        {
            Parameters0 = new System.Numerics.Vector4(5, 0, 0, 0)
        };
        PrismPremultipliedColor[] input =
        [
            PrismPremultipliedColor.FromStraight(0, 0.376, 1, 0.4),
            PrismPremultipliedColor.FromStraight(0.12, 0.13, 0.62, 1),
            default
        ];
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        Assert.True(registry.TryGetFilterKernel(
            PrismFilterId.Posterize,
            out PrismKernel kernel));

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            PrismPremultipliedColor[] working = input
                .Select(color => PrismColorPipeline.ConvertInputToWorking(
                    color,
                    profile))
                .ToArray();
            using Texture2D source = CreateHalfVectorTexture(
                graphicsDevice,
                working);
            using RenderTarget2D output = CreateTarget(
                graphicsDevice,
                working.Length,
                SurfaceFormat.HalfVector4);
            graphicsDevice.SetRenderTarget(output);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                source,
                0.5f,
                new Vector2(1f / working.Length, 1),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismAdjustmentOperation.Posterize,
                    (int)profile,
                    (int)PrismBlendMode.Normal,
                    0),
                FilterOptions0 = ToXnaVector4(plan.Parameters0),
                FilterTextureSize = new Vector2(working.Length, 1)
            };
            registry.Bind(kernel, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                source,
                new Rectangle(0, 0, working.Length, 1),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] actual = new HalfVector4[working.Length];
            output.GetData(actual);

            for (int index = 0; index < working.Length; index++)
            {
                AssertHalfVectorWithin(
                    actual[index],
                    PrismAdjustmentMath.Apply(
                        plan,
                        working[index],
                        profile,
                        opacity: 0.5f),
                    tolerance: 0.003,
                    $"{profile} sample {index}");
            }
        }
    }

    [Fact]
    public void ResamplingTransformGpuMapsTranslationAndTransparentEdges()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        PrismPremultipliedColor red =
            PrismPremultipliedColor.FromStraight(
                1,
                0,
                0,
                1);
        PrismPremultipliedColor green =
            PrismPremultipliedColor.FromStraight(
                0,
                1,
                0,
                0.5);
        PrismPremultipliedColor blue =
            PrismPremultipliedColor.FromStraight(
                0,
                0,
                1,
                1);
        using Texture2D source = CreateHalfVectorTexture(
            graphicsDevice,
            [red, green, blue]);
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            3,
            SurfaceFormat.HalfVector4);
        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.Transform,
                out PrismKernel kernel));

        graphicsDevice.SetRenderTarget(output);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / 3, 1),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismResamplingOperation.Transform,
                (int)PrismColorProfile.LinearSrgb,
                (int)PrismResamplingPassKind.Direct,
                0),
            FilterOptions0 = new Vector4(
                1,
                0,
                1,
                1),
            FilterOptions1 = Vector4.Zero,
            FilterOptions2 = new Vector4(
                0.5f,
                0.5f,
                1,
                0),
            FilterOptions3 = new Vector4(
                3,
                1,
                0,
                0),
            FilterOptions9 = new Vector4(
                0,
                0,
                0,
                (int)PrismBlendMode.Normal),
            FilterTextureSize = new Vector2(3, 1)
        };
        registry.Bind(kernel, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, 3, 1),
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] pixels = new HalfVector4[3];
        output.GetData(pixels);

        AssertHalfVectorWithin(
            pixels[0],
            default,
            tolerance: 0.003,
            "translated transparent edge");
        AssertHalfVectorWithin(
            pixels[1],
            red,
            tolerance: 0.003,
            "translated red");
        AssertHalfVectorWithin(
            pixels[2],
            green,
            tolerance: 0.003,
            "translated green");
    }

    [Fact]
    public void SpherizeGpuMatchesCpuOrthographicProjection()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 17;
        const int height = 9;
        PrismPremultipliedColor[] sourcePixels =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                sourcePixels[(y * width) + x] =
                    PrismPremultipliedColor.FromStraight(
                        x / (double)(width - 1),
                        y / (double)(height - 1),
                        (x + y) /
                            (double)(width + height - 2),
                        1);
            }
        }

        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(
            sourcePixels
                .Select(ToHalfVector)
                .ToArray());
        using RenderTarget2D output = new(
            graphicsDevice,
            width,
            height,
            mipMap: false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);
        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.Spherize,
                out PrismKernel kernel));

        foreach (float amount in new[] { -1f, 1f })
        {
            for (int mode = 0; mode <= 2; mode++)
            {
                System.Numerics.Vector4 options =
                    new(amount, mode, 0.4f, 0.6f);
                PrismResamplingPlan plan = new(
                    PrismFilterId.Spherize,
                    PrismResamplingOperation.Spherize,
                    PrismBlendMode.Normal,
                    [
                        new PrismResamplingPass(
                            PrismResamplingPassKind.Direct,
                            IsNoOp: false)
                    ])
                {
                    Options0 = options
                };
                PrismPremultipliedColor[] expected =
                    PrismResamplingMath.Apply(
                        plan,
                        sourcePixels,
                        width,
                        height,
                        PrismColorProfile.LinearSrgb);

                graphicsDevice.SetRenderTarget(output);
                graphicsDevice.Clear(XnaColor.Transparent);
                PrismKernelParameters parameters = new(
                    source,
                    1,
                    new Vector2(1f / width, 1f / height),
                    Vector2.One,
                    Vector2.Zero)
                {
                    FilterHeader = new Vector4(
                        (int)PrismResamplingOperation.Spherize,
                        (int)PrismColorProfile.LinearSrgb,
                        (int)PrismResamplingPassKind.Direct,
                        0),
                    FilterOptions0 = ToXnaVector4(options),
                    FilterOptions9 = new Vector4(
                        0,
                        0,
                        0,
                        (int)PrismBlendMode.Normal),
                    FilterTextureSize = new Vector2(width, height)
                };
                registry.Bind(kernel, in parameters);
                spriteBatch.Begin(
                    SpriteSortMode.Immediate,
                    BlendState.Opaque,
                    SamplerState.LinearClamp,
                    DepthStencilState.None,
                    RasterizerState.CullNone,
                    registry.Effect);
                spriteBatch.Draw(
                    source,
                    new Rectangle(0, 0, width, height),
                    XnaColor.White);
                spriteBatch.End();
                graphicsDevice.SetRenderTarget(null);
                HalfVector4[] actual =
                    new HalfVector4[width * height];
                output.GetData(actual);

                for (int index = 0;
                    index < actual.Length;
                    index++)
                {
                    AssertHalfVectorWithin(
                        actual[index],
                        expected[index],
                        tolerance: 0.004,
                        $"amount={amount} mode={mode} sample={index}");
                }
            }
        }
    }

    [Fact]
    public void NeighborhoodNoiseGpuIsDeterministicAndUsesPreparedSeed()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 256;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D source = CreateHalfVectorTexture(
            graphicsDevice,
            Enumerable.Repeat(
                PrismPremultipliedColor.FromStraight(
                    0.5,
                    0.5,
                    0.5,
                    1),
                width)
            .ToArray());
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            width,
            SurfaceFormat.HalfVector4);
        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.AddNoise,
                out PrismKernel kernel));

        HalfVector4[] first = DrawNoise(seed: 41);
        HalfVector4[] repeated = DrawNoise(seed: 41);
        HalfVector4[] changed = DrawNoise(seed: 42);
        HalfVector4[] highSeedChanged = DrawNoise(seed: 41 + 65536);
        HalfVector4[] gaussian = DrawNoise(seed: 41, gaussian: true);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
        Assert.False(first.SequenceEqual(highSeedChanged));
        Assert.Contains(
            gaussian,
            pixel => MathF.Abs(pixel.ToVector4().X - 0.5f) > 0.4f);
        Assert.DoesNotContain(
            first,
            pixel => MathF.Abs(pixel.ToVector4().X - 0.5f) > 0.201f);
        Assert.All(
            first,
            pixel =>
            {
                Vector4 value = pixel.ToVector4();
                Assert.InRange(
                    MathF.Abs(value.X - value.Y),
                    0,
                    0.001f);
                Assert.InRange(
                    MathF.Abs(value.X - value.Z),
                    0,
                    0.001f);
            });

        HalfVector4[] DrawNoise(int seed, bool gaussian = false)
        {
            graphicsDevice.SetRenderTarget(output);
            graphicsDevice.Clear(XnaColor.Transparent);
            PrismKernelParameters parameters = new(
                source,
                1,
                new Vector2(1f / width, 1),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismNeighborhoodOperation.AddNoise,
                    (int)PrismColorProfile.LinearSrgb,
                    (int)PrismNeighborhoodPassKind.Direct,
                    0),
                FilterOptions0 = new Vector4(
                    0.2f,
                    gaussian ? 1 : 0,
                    1,
                    seed & 0xffff),
                FilterOptions1 = new Vector4(
                    (seed >> 16) & 0xffff,
                    0,
                    0,
                    0),
                FilterOptions9 = new Vector4(
                    0,
                    0,
                    9,
                    (int)PrismBlendMode.Normal),
                FilterTextureSize = new Vector2(
                    width,
                    1)
            };
            registry.Bind(kernel, in parameters);
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Opaque,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                registry.Effect);
            spriteBatch.Draw(
                source,
                new Rectangle(0, 0, width, 1),
                XnaColor.White);
            spriteBatch.End();
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] pixels =
                new HalfVector4[width];
            output.GetData(pixels);
            return pixels;
        }
    }

    [Fact]
    public void PrismMaskGpuHonorsChannelInvertDensityTransformAndFeather()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        PrismPremultipliedColor sample =
            PrismPremultipliedColor.FromStraight(
                0.8,
                0.2,
                0.1,
                0.4);
        using Texture2D constant = CreateHalfVectorTexture(
            graphicsDevice,
            Enumerable.Repeat(sample, 4).ToArray());
        using RenderTarget2D fourPixelTarget = CreateTarget(
            graphicsDevice,
            4,
            SurfaceFormat.HalfVector4);

        HalfVector4[] alphaDensity = DrawMaskKernel(
            graphicsDevice,
            spriteBatch,
            registry,
            registry.MaskExtract,
            constant,
            fourPixelTarget,
            channel: PrismMaskChannel.Alpha,
            density: 0.5f);
        HalfVector4[] luminance = DrawMaskKernel(
            graphicsDevice,
            spriteBatch,
            registry,
            registry.MaskExtract,
            constant,
            fourPixelTarget,
            channel: PrismMaskChannel.Luminance);
        HalfVector4[] inverted = DrawMaskKernel(
            graphicsDevice,
            spriteBatch,
            registry,
            registry.MaskExtract,
            constant,
            fourPixelTarget,
            channel: PrismMaskChannel.Alpha,
            invert: true);

        Assert.InRange(
            alphaDensity[0].ToVector4().W,
            0.697f,
            0.703f);
        Assert.InRange(
            luminance[0].ToVector4().W,
            0.317f,
            0.324f);
        Assert.InRange(
            inverted[0].ToVector4().W,
            0.597f,
            0.603f);

        using Texture2D transformed = CreateHalfVectorTexture(
            graphicsDevice,
            [
                default,
                default,
                new PrismPremultipliedColor(1, 1, 1, 1),
                new PrismPremultipliedColor(1, 1, 1, 1)
            ]);
        using RenderTarget2D eightPixelTarget = CreateTarget(
            graphicsDevice,
            8,
            SurfaceFormat.HalfVector4);
        HalfVector4[] mapped = DrawMaskKernel(
            graphicsDevice,
            spriteBatch,
            registry,
            registry.MaskExtract,
            transformed,
            eightPixelTarget,
            channel: PrismMaskChannel.Alpha,
            uvRowX: new Vector3(0.25f, 0, -0.5f));
        Assert.InRange(mapped[0].ToVector4().W, 0, 0.003f);
        Assert.InRange(mapped[3].ToVector4().W, 0, 0.003f);
        Assert.InRange(mapped[4].ToVector4().W, 0.997f, 1);
        Assert.InRange(mapped[5].ToVector4().W, 0.997f, 1);
        Assert.InRange(mapped[7].ToVector4().W, 0, 0.003f);

        using Texture2D featherInput = CreateHalfVectorTexture(
            graphicsDevice,
            Enumerable.Repeat(
                new PrismPremultipliedColor(
                    0.25,
                    0.25,
                    0.25,
                    0.25),
                4).ToArray());
        HalfVector4[] feathered = DrawMaskKernel(
            graphicsDevice,
            spriteBatch,
            registry,
            registry.MaskFeather,
            featherInput,
            fourPixelTarget,
            channel: PrismMaskChannel.Alpha,
            density: 0.5f,
            featherStep: new Vector2(0.5f, 0));
        Assert.InRange(
            feathered[0].ToVector4().W,
            0.622f,
            0.628f);
    }

    [Fact]
    public void PrismBlendGpuMatchesAnalyticReferenceForEveryModeAndAlphaCase()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        (
            string Name,
            PrismPremultipliedColor Source,
            PrismPremultipliedColor Backdrop)[] samples =
        [
            (
                "opaque",
                Premultiply(0.82, 0.21, 0.43, 1),
                Premultiply(0.27, 0.71, 0.54, 1)),
            (
                "transparent-source",
                default,
                Premultiply(0.18, 0.63, 0.91, 0.74)),
            (
                "transparent-backdrop",
                Premultiply(0.76, 0.34, 0.12, 0.62),
                default),
            (
                "partial",
                Premultiply(0.87, 0.16, 0.38, 0.43),
                Premultiply(0.22, 0.78, 0.49, 0.61))
        ];
        PrismBlendOptions options = PrismBlendOptions.Default;
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D source = CreateHalfVectorTexture(
            graphicsDevice,
            samples.Select(item => item.Source).ToArray());
        using Texture2D backdrop = CreateHalfVectorTexture(
            graphicsDevice,
            samples.Select(item => item.Backdrop).ToArray());
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            samples.Length,
            SurfaceFormat.HalfVector4);

        foreach (PrismBlendMode blendMode in
            Enum.GetValues<PrismBlendMode>())
        {
            Assert.True(
                registry.TryGetBlendKernel(
                    blendMode,
                    out PrismKernel kernel));
            DrawKernel(
                graphicsDevice,
                spriteBatch,
                registry,
                kernel,
                source,
                backdrop,
                output,
                1f,
                options);
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] actual =
                new HalfVector4[samples.Length];
            output.GetData(actual);

            for (int index = 0; index < samples.Length; index++)
            {
                PrismPremultipliedColor expected =
                    PrismBlendMath.Composite(
                        blendMode,
                        samples[index].Source,
                        samples[index].Backdrop,
                        options,
                        pixelX: index,
                        pixelY: 0);
                AssertHalfVectorWithin(
                    actual[index],
                    expected,
                    tolerance: 0.003,
                    $"{blendMode} {samples[index].Name}");
            }
        }
    }

    [Fact]
    public void PrismBlendGpuHonorsChannelsBlendIfAndKnockout()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismPremultipliedColor sourceColor =
            Premultiply(0.82, 0.31, 0.30, 0.65);
        PrismPremultipliedColor backdropColor =
            Premultiply(0.18, 0.72, 0.55, 0.78);
        (
            string Name,
            PrismBlendMode Mode,
            PrismBlendOptions Options)[] cases =
        [
            (
                "channels",
                PrismBlendMode.Screen,
                PrismBlendOptions.Default with
                {
                    BlendChannels =
                        PrismBlendChannels.Red |
                        PrismBlendChannels.Alpha
                }),
            (
                "blend-if",
                PrismBlendMode.Multiply,
                PrismBlendOptions.Default with
                {
                    BlendIfChannel = PrismBlendIfChannel.Blue,
                    ThisLayerRange =
                        new PrismBlendRange(0.2f, 0.4f, 0.6f, 0.8f)
                }),
            (
                "knockout",
                PrismBlendMode.Overlay,
                PrismBlendOptions.Default with
                {
                    Knockout = PrismKnockout.Deep
                })
        ];
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D source = CreateHalfVectorTexture(
            graphicsDevice,
            [sourceColor]);
        using Texture2D backdrop = CreateHalfVectorTexture(
            graphicsDevice,
            [backdropColor]);
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            1,
            SurfaceFormat.HalfVector4);

        foreach (var item in cases)
        {
            Assert.True(
                registry.TryGetBlendKernel(
                    item.Mode,
                    out PrismKernel kernel));
            DrawKernel(
                graphicsDevice,
                spriteBatch,
                registry,
                kernel,
                source,
                backdrop,
                output,
                1f,
                item.Options);
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] actual = new HalfVector4[1];
            output.GetData(actual);
            PrismPremultipliedColor expected =
                PrismBlendMath.Composite(
                    item.Mode,
                    sourceColor,
                    backdropColor,
                    item.Options);
            AssertHalfVectorWithin(
                actual[0],
                expected,
                tolerance: 0.003,
                item.Name);
        }
    }

    [Fact]
    public void PrismBlendGpuMatchesDualBackdropKnockoutRecurrence()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismPremultipliedColor originalBackdrop =
            Premultiply(0.1, 0.2, 0.3, 0.4);
        PrismPremultipliedColor currentBackdrop = new(
            0.42,
            0.09,
            0.16,
            0.7);
        PrismPremultipliedColor sourceColor =
            Premultiply(0.2, 0.9, 0.5, 0.4);
        PrismPremultipliedColor shapeColor = new(0, 0, 0, 0.8);
        PrismBlendOptions options = PrismBlendOptions.Default with
        {
            Knockout = PrismKnockout.Deep
        };
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = CreateHalfVectorTexture(
            graphicsDevice,
            [sourceColor]);
        using Texture2D current = CreateHalfVectorTexture(
            graphicsDevice,
            [currentBackdrop]);
        using Texture2D original = CreateHalfVectorTexture(
            graphicsDevice,
            [originalBackdrop]);
        using Texture2D shape = CreateHalfVectorTexture(
            graphicsDevice,
            [shapeColor]);
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            1,
            SurfaceFormat.HalfVector4);
        Assert.True(
            registry.TryGetBlendKernel(
                PrismBlendMode.Multiply,
                out PrismKernel kernel));

        DrawKernel(
            graphicsDevice,
            spriteBatch,
            registry,
            kernel,
            source,
            current,
            output,
            1,
            options,
            knockoutBackdrop: original,
            knockoutShape: shape);
        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] actual = new HalfVector4[1];
        output.GetData(actual);
        PrismPremultipliedColor expected =
            PrismBlendMath.CompositeKnockout(
                PrismBlendMode.Multiply,
                sourceColor,
                currentBackdrop,
                originalBackdrop,
                sourceShape: 0.8);

        AssertHalfVectorWithin(
            actual[0],
            expected,
            tolerance: 0.003,
            "dual-backdrop knockout");
    }

    [Fact]
    public void PrismDissolveIsDeterministicAndSeededPerLayer()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 64;
        PrismPremultipliedColor sourceColor =
            Premultiply(0.91, 0.24, 0.12, 0.45);
        PrismPremultipliedColor backdropColor =
            Premultiply(0.12, 0.38, 0.83, 1);
        PrismBlendOptions options =
            PrismBlendOptions.Default with
            {
                DissolveSeed = 17,
                LayerIdentity = 42
            };
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D source = CreateHalfVectorTexture(
            graphicsDevice,
            Enumerable.Repeat(sourceColor, width).ToArray());
        using Texture2D backdrop = CreateHalfVectorTexture(
            graphicsDevice,
            Enumerable.Repeat(backdropColor, width).ToArray());
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            width,
            SurfaceFormat.HalfVector4);
        Assert.True(
            registry.TryGetBlendKernel(
                PrismBlendMode.Dissolve,
                out PrismKernel kernel));

        HalfVector4[] first = DrawAndRead(options);
        HalfVector4[] repeated = DrawAndRead(options);
        PrismBlendOptions changedOptions = options with
        {
            DissolveSeed = options.DissolveSeed + 1
        };
        HalfVector4[] changed = DrawAndRead(changedOptions);

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
        for (int index = 0; index < width; index++)
        {
            AssertHalfVectorWithin(
                first[index],
                PrismBlendMath.Composite(
                    PrismBlendMode.Dissolve,
                    sourceColor,
                    backdropColor,
                    options,
                    pixelX: index,
                    pixelY: 0),
                tolerance: 0.003,
                $"Dissolve pixel {index}");
        }

        HalfVector4[] DrawAndRead(PrismBlendOptions blendOptions)
        {
            DrawKernel(
                graphicsDevice,
                spriteBatch,
                registry,
                kernel,
                source,
                backdrop,
                output,
                1f,
                blendOptions);
            graphicsDevice.SetRenderTarget(null);
            HalfVector4[] pixels = new HalfVector4[width];
            output.GetData(pixels);
            return pixels;
        }
    }

    [Theory]
    [InlineData(0.2, 0)]
    [InlineData(0.3, 0.5)]
    [InlineData(0.5, 1)]
    [InlineData(0.7, 0.5)]
    [InlineData(0.8, 0)]
    public void BlendIfUsesLinearSplitFeathers(
        double value,
        double expected)
    {
        double actual = PrismBlendMath.EvaluateBlendRange(
            value,
            new PrismBlendRange(0.2f, 0.4f, 0.6f, 0.8f));

        Assert.Equal(expected, actual, precision: 6);
    }

    [Fact]
    public void OpaqueBlendSentinelsMatchKnownChannelEquations()
    {
        PrismPremultipliedColor source =
            Premultiply(0.8, 0.4, 0.2, 1);
        PrismPremultipliedColor backdrop =
            Premultiply(0.25, 0.5, 0.75, 1);
        (
            PrismBlendMode Mode,
            PrismPremultipliedColor Expected)[] cases =
        [
            (
                PrismBlendMode.Multiply,
                new PrismPremultipliedColor(0.2, 0.2, 0.15, 1)),
            (
                PrismBlendMode.Screen,
                new PrismPremultipliedColor(0.85, 0.7, 0.8, 1)),
            (
                PrismBlendMode.Difference,
                new PrismPremultipliedColor(0.55, 0.1, 0.55, 1))
        ];

        foreach (var item in cases)
        {
            AssertPremultipliedWithin(
                PrismBlendMath.Composite(
                    item.Mode,
                    source,
                    backdrop,
                    PrismBlendOptions.Default),
                item.Expected,
                tolerance: 0.0000001,
                item.Mode.ToString());
        }
    }

    [Fact]
    public void PrismColorGpuRoundTripsEveryProfileWithinTheGoldenTolerance()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        XnaColor[] samples =
        [
            XnaColor.Transparent,
            new XnaColor(255, 127, 63, 0),
            new XnaColor(13, 6, 2, 17),
            new XnaColor(32, 64, 96, 128),
            new XnaColor(250, 125, 5, 255),
            new XnaColor(0, 255, 64, 255)
        ];
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            samples.Length,
            1,
            false,
            SurfaceFormat.Color);
        source.SetData(samples);

        foreach (PrismColorProfile profile in
            Enum.GetValues<PrismColorProfile>())
        {
            using RenderTarget2D working = CreateTarget(
                graphicsDevice,
                samples.Length,
                SurfaceFormat.HalfVector4);
            using RenderTarget2D output = CreateTarget(
                graphicsDevice,
                samples.Length,
                SurfaceFormat.Color);
            Assert.True(
                registry.TryGetColorConversionKernel(
                    profile,
                    out PrismKernel inputKernel));
            Assert.True(
                registry.TryGetPresentKernel(
                    profile,
                    out PrismKernel outputKernel));

            DrawKernel(
                graphicsDevice,
                spriteBatch,
                registry,
                inputKernel,
                source,
                source,
                working,
                1f);
            DrawKernel(
                graphicsDevice,
                spriteBatch,
                registry,
                outputKernel,
                working,
                working,
                output,
                1f);
            graphicsDevice.SetRenderTarget(null);

            XnaColor[] actual = new XnaColor[samples.Length];
            HalfVector4[] actualWorking =
                new HalfVector4[samples.Length];
            output.GetData(actual);
            working.GetData(actualWorking);
            for (int index = 0; index < samples.Length; index++)
            {
                PrismPremultipliedColor input =
                    ToPremultipliedColor(samples[index]);
                PrismPremultipliedColor expectedWorking =
                    PrismColorPipeline.ConvertInputToWorking(
                        input,
                        profile);
                PrismPremultipliedColor expected =
                    PrismColorPipeline.ConvertWorkingToOutput(
                        expectedWorking,
                        profile);
                AssertHalfVectorWithin(
                    actualWorking[index],
                    expectedWorking,
                    tolerance: 0.001,
                    $"{profile} working sample {index}");
                AssertColorWithin(
                    actual[index],
                    expected,
                    tolerance: 2,
                    $"{profile} sample {index}");
            }

            Assert.Equal(XnaColor.Transparent, actual[1]);
        }
    }

    [Fact]
    public void PrismColorFundamentalKernelsPreservePremultipliedAlpha()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        XnaColor foreground = new(64, 32, 16, 128);
        XnaColor secondary = new(20, 40, 60, 96);
        using WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice =
            fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry =
            new(graphicsDevice);
        using SpriteBatch spriteBatch =
            new(graphicsDevice);
        using Texture2D sourceTexture = new(
            graphicsDevice,
            1,
            1,
            false,
            SurfaceFormat.Color);
        using Texture2D secondaryTexture = new(
            graphicsDevice,
            1,
            1,
            false,
            SurfaceFormat.Color);
        using RenderTarget2D output = CreateTarget(
            graphicsDevice,
            1,
            SurfaceFormat.Color);
        sourceTexture.SetData([foreground]);
        secondaryTexture.SetData([secondary]);
        PrismPremultipliedColor source =
            ToPremultipliedColor(foreground);
        PrismPremultipliedColor backdrop =
            ToPremultipliedColor(secondary);
        Assert.True(
            registry.TryGetBlendKernel(
                PrismBlendMode.Normal,
                out PrismKernel normal));

        (
            PrismKernel Kernel,
            float Opacity,
            PrismPremultipliedColor Expected)[] cases =
        [
            (
                registry.Copy,
                0.5f,
                Scale(source, 0.5)),
            (
                registry.MaskAlpha,
                1f,
                Scale(source, backdrop.Alpha)),
            (
                registry.ClipAlpha,
                1f,
                Scale(source, backdrop.Alpha)),
            (
                normal,
                1f,
                Over(source, backdrop))
        ];

        foreach (var item in cases)
        {
            DrawKernel(
                graphicsDevice,
                spriteBatch,
                registry,
                item.Kernel,
                sourceTexture,
                secondaryTexture,
                output,
                item.Opacity);
            graphicsDevice.SetRenderTarget(null);
            XnaColor[] actual = new XnaColor[1];
            output.GetData(actual);
            AssertColorWithin(
                actual[0],
                item.Expected,
                tolerance: 1,
                item.Kernel.Kind.ToString());
        }
    }

    [Fact]
    public void RetainedCacheMissFinalHitAndIntermediateHitMatchFreshPixels()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        PrismRetainedScenario[] scenarios =
        [
            CreateAlphaRetainedScenario(),
            CreateComplexRetainedScenario(
                fixture.Session.GraphicsDevice),
            CreateNestedRetainedScenario(),
            CreateBackdropRetainedScenario(
                fixture.Session.GraphicsDevice)
        ];

        try
        {
            foreach (PrismRetainedScenario scenario in scenarios)
            {
                using TestPrismRenderer renderer = new(
                    fixture.Session.GraphicsDevice,
                    SurfaceWidth,
                    SurfaceHeight);
                PrismExecutionDiagnostics diagnostics = new();
                using PrismGraphExecutor executor = new(
                    fixture.Session.GraphicsDevice,
                    diagnostics);
                Viewport viewport =
                    new(0, 0, SurfaceWidth, SurfaceHeight);
                PrismRetainedRasterContext rasterContext =
                    CreateRetainedRasterContext(
                        scenario.Analysis,
                        viewport);

                ExpectedCacheWork missWork =
                    CalculateExpectedCacheWork(
                        scenario.Plan,
                        executor.RetainedSurfaceCache,
                        rasterContext);
                renderer.ResetRenderedCommandCount();
                ExecuteFrame(
                    renderer,
                    executor,
                    scenario.Commands,
                    scenario.Analysis,
                    scenario.Plan,
                    viewport,
                    scenario.BackdropLease);
                XnaColor[] freshPixels = renderer.ReadPixels();

                Assert.Equal(
                    missWork.PassCount,
                    diagnostics.Counters.PassCount);
                Assert.Equal(
                    missWork.CaptureCount,
                    diagnostics.Counters.CaptureCount);
                Assert.Equal(
                    scenario.Plan.ExecutionOrder.Length,
                    missWork.GraphPassCount);
                Assert.True(renderer.RenderedCommandCount > 0);
                Assert.True(
                    executor.RetainedSurfaceCache.PromotionCount > 0);

                ExpectedCacheWork finalHitWork =
                    CalculateExpectedCacheWork(
                        scenario.Plan,
                        executor.RetainedSurfaceCache,
                        rasterContext);
                renderer.ResetRenderedCommandCount();
                ExecuteFrame(
                    renderer,
                    executor,
                    scenario.Commands,
                    scenario.Analysis,
                    scenario.Plan,
                    viewport,
                    scenario.BackdropLease);
                XnaColor[] finalHitPixels =
                    renderer.ReadPixels();

                AssertPixelsWithin(
                    finalHitPixels,
                    freshPixels,
                    tolerance: 1,
                    $"{scenario.Name} final hit");
                Assert.Equal(0, finalHitWork.GraphPassCount);
                Assert.Equal(
                    finalHitWork.PassCount,
                    diagnostics.Counters.PassCount);
                Assert.Equal(0, diagnostics.Counters.CaptureCount);
                Assert.Equal(0, renderer.RenderedCommandCount);

                Assert.True(
                    RemoveFinalEntries(
                        scenario.Plan,
                        executor.RetainedSurfaceCache,
                        rasterContext) > 0);
                ExpectedCacheWork intermediateHitWork =
                    CalculateExpectedCacheWork(
                        scenario.Plan,
                        executor.RetainedSurfaceCache,
                        rasterContext);
                Assert.InRange(
                    intermediateHitWork.GraphPassCount,
                    1,
                    missWork.GraphPassCount - 1);
                Assert.Equal(0, intermediateHitWork.CaptureCount);

                renderer.ResetRenderedCommandCount();
                ExecuteFrame(
                    renderer,
                    executor,
                    scenario.Commands,
                    scenario.Analysis,
                    scenario.Plan,
                    viewport,
                    scenario.BackdropLease);
                XnaColor[] intermediateHitPixels =
                    renderer.ReadPixels();

                AssertPixelsWithin(
                    intermediateHitPixels,
                    freshPixels,
                    tolerance: 1,
                    $"{scenario.Name} intermediate hit");
                Assert.Equal(
                    intermediateHitWork.PassCount,
                    diagnostics.Counters.PassCount);
                Assert.Equal(
                    intermediateHitWork.CaptureCount,
                    diagnostics.Counters.CaptureCount);
                Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);
                Assert.Equal(
                    0,
                    executor.RetainedSurfaceCache.ActiveLeaseCount);
                Assert.Equal(0, diagnostics.Count);

                AssertScenarioCoverage(scenario);
                if (scenario.BackdropLease is
                    TestBackdropLease backdrop)
                {
                    Assert.False(backdrop.Texture.IsDisposed);
                    executor.RetainedSurfaceCache.Clear();
                    Assert.False(backdrop.Texture.IsDisposed);
                }
            }
        }
        finally
        {
            foreach (PrismRetainedScenario scenario in scenarios)
            {
                scenario.Dispose();
            }
        }
    }

    [Fact]
    public void RendererDiagnosticsExposeCacheWorkAndConfiguredBudgets()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismRendererOptions options = new()
        {
            SurfaceHardByteLimit = 1024 * 1024,
            RetainedCacheSoftByteLimit = 512 * 1024,
            RetainedCacheEntryLimit = 16
        };
        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics executionDiagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            executionDiagnostics,
            options,
            retainedCacheEnabled: true);
        using PrismRetainedScenario scenario =
            CreateAlphaRetainedScenario();
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);
        PrismRetainedRasterContext rasterContext =
            CreateRetainedRasterContext(
                scenario.Analysis,
                viewport);

        ExecuteFrame(
            renderer,
            executor,
            scenario.Commands,
            scenario.Analysis,
            scenario.Plan,
            viewport);
        PrismRendererDiagnostics cold =
            executor.RendererDiagnostics;

        Assert.True(cold.RetainedCacheEnabled);
        Assert.True(cold.MissCount > 0);
        Assert.True(
            cold.GetMissCount(
                PrismCacheMissReason.NotFound) > 0);
        Assert.True(cold.PromotionCount > 0);
        Assert.Equal(
            executor.RetainedSurfaceCache.PromotionCount,
            cold.PromotionCount);
        Assert.True(cold.RetainedEntryCount > 0);
        Assert.Equal(0, cold.PinnedEntryCount);
        Assert.True(cold.RetainedByteCount > 0);
        Assert.Equal(
            cold.TransientByteCount + cold.RetainedByteCount,
            cold.TotalByteCount);
        Assert.True(cold.PeakTotalByteCount >= cold.TotalByteCount);
        Assert.Equal(
            options.SurfaceHardByteLimit,
            executor.SurfacePool.MemoryAccountant.Budget.HardByteLimit);
        Assert.Equal(
            options.RetainedCacheSoftByteLimit,
            executor.SurfacePool.MemoryAccountant.Budget
                .RetainedSoftByteLimit);
        Assert.Equal(
            options.RetainedCacheEntryLimit,
            executor.SurfacePool.MemoryAccountant.Budget
                .RetainedEntryLimit);

        ExecuteFrame(
            renderer,
            executor,
            scenario.Commands,
            scenario.Analysis,
            scenario.Plan,
            viewport);
        PrismRendererDiagnostics finalHit =
            executor.RendererDiagnostics;

        Assert.True(finalHit.FinalHitCount > 0);
        Assert.True(finalHit.LookupCount > 0);
        Assert.True(finalHit.SavedCaptureCount > 0);
        Assert.True(finalHit.SavedPassCount > 0);

        Assert.True(
            RemoveFinalEntries(
                scenario.Plan,
                executor.RetainedSurfaceCache,
                rasterContext) > 0);
        ExecuteFrame(
            renderer,
            executor,
            scenario.Commands,
            scenario.Analysis,
            scenario.Plan,
            viewport);
        PrismRendererDiagnostics intermediateHit =
            executor.RendererDiagnostics;

        Assert.True(intermediateHit.IntermediateHitCount > 0);
        Assert.True(intermediateHit.EvictionCount > 0);
        Assert.Equal(
            PrismCacheEvictionReason.ExplicitRemoval,
            intermediateHit.LastEvictionReason);
        Assert.True(
            intermediateHit.GetEvictionCount(
                PrismCacheEvictionReason.ExplicitRemoval) > 0);
    }

    [Fact]
    public void DevelopmentDiagnosticsReportDependencyDiffOnlyWhenEnabled()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer enabledRenderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        using TestPrismRenderer disabledRenderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        var low = CreateSimpleComposition(
            opacity: 0.25f,
            ownerToken: 7_151);
        var high = CreateSimpleComposition(
            opacity: 0.75f,
            ownerToken: 7_151);
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);
        using PrismGraphExecutor enabled = new(
            fixture.Session.GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions
            {
                EnableDevelopmentDiagnostics = true
            },
            retainedCacheEnabled: true);
        using PrismGraphExecutor disabled = new(
            fixture.Session.GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: true);

        ExecuteFrame(
            enabledRenderer,
            enabled,
            low.Commands,
            low.Analysis,
            low.Plan,
            viewport);
        ExecuteFrame(
            enabledRenderer,
            enabled,
            high.Commands,
            high.Analysis,
            high.Plan,
            viewport);
        ExecuteFrame(
            disabledRenderer,
            disabled,
            low.Commands,
            low.Analysis,
            low.Plan,
            viewport);
        ExecuteFrame(
            disabledRenderer,
            disabled,
            high.Commands,
            high.Analysis,
            high.Plan,
            viewport);

        Assert.NotEqual(
            PrismDependencyChange.None,
            enabled.RendererDiagnostics.LastDependencyChange);
        Assert.True(
            enabled.RendererDiagnostics.LastDependencyChange.HasFlag(
                PrismDependencyChange.Values));
        Assert.Equal(
            PrismCacheMissReason.DependencyChanged,
            enabled.RendererDiagnostics.LastMissReason);
        Assert.True(
            enabled.RendererDiagnostics.GetMissCount(
                PrismCacheMissReason.DependencyChanged) > 0);
        Assert.Equal(
            PrismDependencyChange.None,
            disabled.RendererDiagnostics.LastDependencyChange);
    }

    [Fact]
    public void InternalCacheOffMatchesFreshPixelsAndDoesNoCacheWork()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer cachedRenderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        using TestPrismRenderer freshRenderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        using PrismGraphExecutor cached = new(
            fixture.Session.GraphicsDevice,
            diagnostics: null,
            new PrismRendererOptions(),
            retainedCacheEnabled: true);
        PrismExecutionDiagnostics freshExecution = new();
        using PrismGraphExecutor fresh = new(
            fixture.Session.GraphicsDevice,
            freshExecution,
            new PrismRendererOptions(),
            retainedCacheEnabled: false);
        var scene = CreateSimpleComposition();
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);

        ExecuteFrame(
            cachedRenderer,
            cached,
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            viewport);
        XnaColor[] cachedPixels =
            cachedRenderer.ReadPixels();
        ExecuteFrame(
            freshRenderer,
            fresh,
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            viewport);
        ExecuteFrame(
            freshRenderer,
            fresh,
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            viewport);
        XnaColor[] freshPixels =
            freshRenderer.ReadPixels();

        AssertPixelsWithin(
            freshPixels,
            cachedPixels,
            tolerance: 1,
            "internal cache-off");
        Assert.False(
            fresh.RendererDiagnostics.RetainedCacheEnabled);
        Assert.Equal(0, fresh.RendererDiagnostics.FinalHitCount);
        Assert.Equal(
            0,
            fresh.RendererDiagnostics.IntermediateHitCount);
        Assert.Equal(0, fresh.RetainedSurfaceCache.LookupCount);
        Assert.Equal(0, fresh.RetainedSurfaceCache.PromotionCount);
        Assert.Equal(0, fresh.RetainedSurfaceCache.EntryCount);
        Assert.True(
            fresh.RendererDiagnostics.GetMissCount(
                PrismCacheMissReason.Disabled) > 0);
        Assert.Equal(0, fresh.RendererDiagnostics.SavedCaptureCount);
        Assert.Equal(0, fresh.RendererDiagnostics.SavedPassCount);
        Assert.True(freshExecution.Counters.CaptureCount > 0);
        Assert.True(freshExecution.Counters.PassCount > 0);
    }

    [Fact]
    public void RetainedCacheNeverSharesControlPixelsAcrossOwners()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics);
        var first = CreateSimpleComposition(ownerToken: 7_101);
        var second = CreateSimpleComposition(ownerToken: 7_102);
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);
        PrismRetainedRasterContext secondContext =
            CreateRetainedRasterContext(
                second.Analysis,
                viewport);

        ExecuteFrame(
            renderer,
            executor,
            first.Commands,
            first.Analysis,
            first.Plan,
            viewport);
        XnaColor[] firstPixels = renderer.ReadPixels();
        ExpectedCacheWork secondWork =
            CalculateExpectedCacheWork(
                second.Plan,
                executor.RetainedSurfaceCache,
                secondContext);

        renderer.ResetRenderedCommandCount();
        ExecuteFrame(
            renderer,
            executor,
            second.Commands,
            second.Analysis,
            second.Plan,
            viewport);
        XnaColor[] secondPixels = renderer.ReadPixels();

        AssertPixelsWithin(
            secondPixels,
            firstPixels,
            tolerance: 1,
            "cross-owner full miss");
        Assert.Equal(
            second.Plan.ExecutionOrder.Length,
            secondWork.GraphPassCount);
        Assert.Equal(
            secondWork.PassCount,
            diagnostics.Counters.PassCount);
        Assert.Equal(
            secondWork.CaptureCount,
            diagnostics.Counters.CaptureCount);
        Assert.True(renderer.RenderedCommandCount > 0);
    }

    [Fact]
    public void RetainedCacheInvalidatesChangedOwnerContextAndDeviceReset()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics);
        var low = CreateSimpleComposition(
            opacity: 0.25f,
            ownerToken: 7_201);
        var high = CreateSimpleComposition(
            opacity: 0.75f,
            ownerToken: 7_201);
        var scaled = CreateSimpleComposition(
            opacity: 0.75f,
            ownerToken: 7_201,
            pixelScale: 1.5f);
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);

        ExecuteFrame(
            renderer,
            executor,
            low.Commands,
            low.Analysis,
            low.Plan,
            viewport);
        Assert.True(
            executor.RetainedSurfaceCache.EntryCount > 0);
        Assert.Equal(
            1,
            executor.RetainedSurfaceCache.OwnerIndexCount);

        renderer.ThrowOnNextRenderCommand = true;
        Assert.Throws<InvalidOperationException>(
            () => ExecuteFrame(
                renderer,
                executor,
                high.Commands,
                high.Analysis,
                high.Plan,
                viewport));
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.EntryCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.OwnerIndexCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.RetainedByteCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.ActiveLeaseCount);

        ExecuteFrame(
            renderer,
            executor,
            high.Commands,
            high.Analysis,
            high.Plan,
            viewport);
        int highEntryCount =
            executor.RetainedSurfaceCache.EntryCount;
        long evictionsBeforeScale =
            executor.RetainedSurfaceCache.EvictionCount;

        ExecuteFrame(
            renderer,
            executor,
            scaled.Commands,
            scaled.Analysis,
            scaled.Plan,
            viewport);

        Assert.True(
            executor.RetainedSurfaceCache.EvictionCount >=
            evictionsBeforeScale + highEntryCount);
        int scaledEntryCount =
            executor.RetainedSurfaceCache.EntryCount;
        long evictionsBeforeResize =
            executor.RetainedSurfaceCache.EvictionCount;
        Viewport resized =
            new(0, 0, SurfaceWidth - 1, SurfaceHeight);

        ExecuteFrame(
            renderer,
            executor,
            scaled.Commands,
            scaled.Analysis,
            scaled.Plan,
            resized);

        Assert.True(
            executor.RetainedSurfaceCache.EvictionCount >=
            evictionsBeforeResize + scaledEntryCount);
        Assert.Equal(
            1,
            executor.RetainedSurfaceCache.OwnerIndexCount);

        PrismColorProfile alternateOutputProfile =
            Enum.GetValues<PrismColorProfile>()
                .First(profile =>
                    profile != PrismColorProfile.Srgb);
        executor.EnsureRasterContext(
            CreateRetainedRasterContext(
                scaled.Analysis,
                resized,
                outputColorProfile:
                    alternateOutputProfile));

        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.EntryCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.OwnerIndexCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.RetainedByteCount);

        ExecuteFrame(
            renderer,
            executor,
            scaled.Commands,
            scaled.Analysis,
            scaled.Plan,
            resized);
        executor.EnsureRasterContext(
            CreateRetainedRasterContext(
                scaled.Analysis,
                resized,
                shaderPackageVersion:
                    PrismKernelRegistry.ShaderPackageVersion + 1));

        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.EntryCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.OwnerIndexCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.RetainedByteCount);

        ExecuteFrame(
            renderer,
            executor,
            scaled.Commands,
            scaled.Analysis,
            scaled.Plan,
            resized);
        Assert.True(
            executor.RetainedSurfaceCache.EntryCount > 0);

        executor.Reset();

        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.EntryCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.OwnerIndexCount);
        Assert.Equal(
            0,
            executor.RetainedSurfaceCache.RetainedByteCount);
        Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);

        ExecuteFrame(
            renderer,
            executor,
            scaled.Commands,
            scaled.Analysis,
            scaled.Plan,
            resized);
        ExecuteFrame(
            renderer,
            executor,
            scaled.Commands,
            scaled.Analysis,
            scaled.Plan,
            resized);

        Assert.Equal(0, diagnostics.Counters.CaptureCount);
        Assert.Equal(0, diagnostics.Count);
    }

    [Fact]
    public void BackendConsumesHiddenOwnerInvalidationWithoutCacheWork()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        MonoGameDrawingBackend backend =
            Assert.IsType<MonoGameDrawingBackend>(
                fixture.Session.DrawingBackend);
        var scene = CreateSimpleComposition(
            width: 96,
            height: 64,
            ownerToken: 7_301);
        DrawingFrameContext frameContext =
            new(scene.Analysis);

        fixture.Session.BeginFrame(
            CernealaColor.Transparent);
        backend.Render(
            scene.Commands,
            in frameContext);
        fixture.Session.Present();

        PrismRetainedSurfaceCache cache =
            Assert.IsType<PrismRetainedSurfaceCache>(
                backend.PrismRetainedCacheForDiagnostics);
        Assert.True(cache.EntryCount > 0);
        long lookupsBefore = cache.LookupCount;
        long promotionsBefore = cache.PromotionCount;
        PrismCacheInvalidationQueue invalidations = new();
        invalidations.EnqueueOwner(
            new PrismCacheOwnerToken(7_301));
        DrawCommandList hiddenCommands =
            PrismTestData.Commands();
        PrismFrameAnalysis hiddenAnalysis =
            new PrismFrameAnalyzer().Analyze(
                hiddenCommands);
        DrawingFrameContext hiddenContext = new(
            hiddenAnalysis,
            backdropLease: null,
            backdropSourceToken: default,
            prismCacheInvalidations: invalidations);

        fixture.Session.BeginFrame(
            CernealaColor.Transparent);
        backend.Render(
            hiddenCommands,
            in hiddenContext);
        fixture.Session.Present();

        Assert.Equal(0, invalidations.Count);
        Assert.Equal(0, cache.EntryCount);
        Assert.Equal(0, cache.OwnerIndexCount);
        Assert.Equal(0, cache.RetainedByteCount);
        Assert.Equal(lookupsBefore, cache.LookupCount);
        Assert.Equal(promotionsBefore, cache.PromotionCount);
        Assert.Equal(0, cache.ActiveLeaseCount);
    }

    [Fact]
    public void SimpleCompositionCapturesOnceAndAllocatesNothingAfterWarmup()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics);
        (
            DrawCommandList commands,
            PrismFrameAnalysis analysis,
            PrismGraphExecutionPlan plan) =
            CreateSimpleComposition();
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);

        for (int frame = 0; frame < 8; frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                commands,
                analysis,
                plan,
                viewport);
        }

        long createdAfterWarmup =
            executor.SurfacePool.CreatedSurfaceCount;
        long reusedAfterWarmup =
            executor.SurfacePool.ReusedSurfaceCount;
        renderer.ResetRenderedCommandCount();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        ExecuteFrame(
            renderer,
            executor,
            commands,
            analysis,
            plan,
            viewport);
        renderer.ResetRenderedCommandCount();

        long allocationStart =
            GC.GetAllocatedBytesForCurrentThread();
        long allocatedBytes = 0;
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            renderer.BeginFrame();
            try
            {
                executor.Execute(
                    commands,
                    analysis,
                    plan,
                    renderer,
                    viewport,
                    backdropLease: null);
            }
            finally
            {
                renderer.EndBatch();
            }
        }
        allocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() -
            allocationStart;

        Assert.Equal(0, allocatedBytes);
        Assert.Equal(0, renderer.RenderedCommandCount);
        Assert.Equal(
            createdAfterWarmup,
            executor.SurfacePool.CreatedSurfaceCount);
        Assert.Equal(
            reusedAfterWarmup,
            executor.SurfacePool.ReusedSurfaceCount);
        Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);
        Assert.Equal(0, diagnostics.Count);

        XnaColor pixel = renderer.ReadCenterPixel();
        Assert.InRange(pixel.R, 126, 129);
        Assert.InRange(pixel.G, 126, 129);
        Assert.InRange(pixel.B, 126, 129);
        Assert.InRange(pixel.A, 126, 129);
    }

    [Fact]
    public void PrismStyleStressReusesSurfacesAndAllocatesNothingAfterWarmup()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        PrismStyleDefinition[] styles = Enumerable
            .Range(0, StyleStressCount)
            .Select(
                _ => new PrismStyleDefinition(
                    PrismStyleId.ColorOverlay))
            .ToArray();
        PrismLayerDefinition layer = new(
            new PrismNodeId(1),
            "Style stress",
            styles: styles);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Style stress",
                layer),
            bounds: new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(
                    0,
                    0,
                    SurfaceWidth,
                    SurfaceHeight),
                CernealaColor.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        Assert.Equal(
            StyleStressCount,
            plan.OptimizedGraph.Nodes.Count(
                node => node.Kind == PrismGraphNodeKind.Style));
        Assert.InRange(
            plan.PeakLiveSurfaces,
            1,
            StyleStressCount - 1);

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics);
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);

        for (int frame = 0; frame < 8; frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                commands,
                analysis,
                plan,
                viewport);
        }

        long createdAfterWarmup =
            executor.SurfacePool.CreatedSurfaceCount;
        long reusedAfterWarmup =
            executor.SurfacePool.ReusedSurfaceCount;
        Assert.True(createdAfterWarmup > 0);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        ExecuteFrame(
            renderer,
            executor,
            commands,
            analysis,
            plan,
            viewport);
        renderer.ResetRenderedCommandCount();

        long allocationStart =
            GC.GetAllocatedBytesForCurrentThread();
        for (int frame = 0; frame < MeasuredFrameCount; frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                commands,
                analysis,
                plan,
                viewport);
        }
        long allocatedBytes =
            GC.GetAllocatedBytesForCurrentThread() -
            allocationStart;

        Assert.Equal(0, allocatedBytes);
        Assert.Equal(
            createdAfterWarmup,
            executor.SurfacePool.CreatedSurfaceCount);
        Assert.Equal(
            reusedAfterWarmup,
            executor.SurfacePool.ReusedSurfaceCount);
        Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);
        Assert.Equal(0, diagnostics.Count);
        Assert.Equal(0, renderer.RenderedCommandCount);
    }

    [Fact]
    public void RepresentativeScenesStayWithinMeasuredExecutionBudgets()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        foreach (PrismProfileScenario scenario in
            CreateRepresentativeScenarios())
        {
            using TestPrismRenderer renderer = new(
                fixture.Session.GraphicsDevice,
                SurfaceWidth,
                SurfaceHeight);
            PrismExecutionDiagnostics diagnostics = new();
            using PrismGraphExecutor executor = new(
                fixture.Session.GraphicsDevice,
                diagnostics);
            Viewport viewport =
                new(0, 0, SurfaceWidth, SurfaceHeight);

            for (int frame = 0; frame < 8; frame++)
            {
                ExecuteFrame(
                    renderer,
                    executor,
                    scenario.Commands,
                    scenario.Analysis,
                    scenario.Plan,
                    viewport);
            }

            long createdAfterWarmup =
                executor.SurfacePool.CreatedSurfaceCount;
            long reusedAfterWarmup =
                executor.SurfacePool.ReusedSurfaceCount;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            ExecuteFrame(
                renderer,
                executor,
                scenario.Commands,
                scenario.Analysis,
                scenario.Plan,
                viewport);
            renderer.ResetRenderedCommandCount();

            long allocationStart =
                GC.GetAllocatedBytesForCurrentThread();
            long cpuSubmitTicks = 0;
            for (int frame = 0;
                frame < MeasuredFrameCount;
                frame++)
            {
                ExecuteFrame(
                    renderer,
                    executor,
                    scenario.Commands,
                    scenario.Analysis,
                    scenario.Plan,
                    viewport);
                cpuSubmitTicks +=
                    diagnostics.Counters.CpuSubmitTime.Ticks;
            }
            long allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() -
                allocationStart;
            PrismExecutionCounters counters =
                diagnostics.Counters;

            long completionStarted =
                Stopwatch.GetTimestamp();
            ExecuteFrame(
                renderer,
                executor,
                scenario.Commands,
                scenario.Analysis,
                scenario.Plan,
                viewport);
            _ = renderer.ReadCenterPixel();
            TimeSpan gpuCompletionUpperBound =
                Stopwatch.GetElapsedTime(completionStarted);

            int expectedPasses =
                scenario.Plan.OptimizedGraph.Scopes.Count(
                    scope =>
                        scope.Depth == 0 &&
                        scope.Output.HasValue);
            Assert.Equal(expectedPasses, counters.PassCount);
            Assert.Equal(0, allocatedBytes);
            Assert.Equal(
                createdAfterWarmup,
                executor.SurfacePool.CreatedSurfaceCount);
            Assert.Equal(
                reusedAfterWarmup,
                executor.SurfacePool.ReusedSurfaceCount);
            Assert.Equal(0, counters.PeakLiveSurfaceCount);
            Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);
            Assert.Equal(0, diagnostics.Count);
            Assert.True(cpuSubmitTicks > 0);
            Assert.True(gpuCompletionUpperBound > TimeSpan.Zero);

            Console.WriteLine(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"PRISM_PROFILE name={scenario.Name} " +
                    $"passes={counters.PassCount} " +
                    $"captures={counters.CaptureCount} " +
                    $"peak={counters.PeakLiveSurfaceCount} " +
                    $"created={counters.CreatedSurfaceCount} " +
                    $"reused={counters.ReusedSurfaceCount} " +
                    $"cpu-submit-us=" +
                    $"{TimeSpan.FromTicks(cpuSubmitTicks / MeasuredFrameCount).TotalMicroseconds:F3} " +
                    $"gpu-completion-upper-bound-us=" +
                    $"{gpuCompletionUpperBound.TotalMicroseconds:F3} " +
                    $"allocated-bytes={allocatedBytes}"));
        }
    }

    [Fact]
    public void ThousandsOfAnimatedFramesReuseSurfacesAndCompiledShader()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using TestPrismRenderer renderer = new(
            fixture.Session.GraphicsDevice,
            SurfaceWidth,
            SurfaceHeight);
        PrismExecutionDiagnostics diagnostics = new();
        using PrismGraphExecutor executor = new(
            fixture.Session.GraphicsDevice,
            diagnostics);
        (
            DrawCommandList lowCommands,
            PrismFrameAnalysis lowAnalysis,
            PrismGraphExecutionPlan lowPlan) =
            CreateSimpleComposition(opacity: 0.25f);
        (
            DrawCommandList highCommands,
            PrismFrameAnalysis highAnalysis,
            PrismGraphExecutionPlan highPlan) =
            CreateSimpleComposition(opacity: 0.75f);
        Viewport viewport =
            new(0, 0, SurfaceWidth, SurfaceHeight);
        Effect compiledEffect = executor.Kernels.Effect;

        for (int frame = 0; frame < 8; frame++)
        {
            ExecuteFrame(
                renderer,
                executor,
                frame % 2 == 0 ? lowCommands : highCommands,
                frame % 2 == 0 ? lowAnalysis : highAnalysis,
                frame % 2 == 0 ? lowPlan : highPlan,
                viewport);
        }

        long createdAfterWarmup =
            executor.SurfacePool.CreatedSurfaceCount;
        long reusedAfterWarmup =
            executor.SurfacePool.ReusedSurfaceCount;
        renderer.ResetRenderedCommandCount();
        for (int frame = 0; frame < AnimatedFrameCount; frame++)
        {
            bool low = frame % 2 == 0;
            PrismGraphExecutionPlan plan =
                low ? lowPlan : highPlan;
            ExecuteFrame(
                renderer,
                executor,
                low ? lowCommands : highCommands,
                low ? lowAnalysis : highAnalysis,
                plan,
                viewport);
            Assert.InRange(
                diagnostics.Counters.PeakLiveSurfaceCount,
                0,
                plan.PeakLiveSurfaces);
        }

        Assert.Equal(
            createdAfterWarmup,
            executor.SurfacePool.CreatedSurfaceCount);
        Assert.True(
            executor.SurfacePool.ReusedSurfaceCount >
            reusedAfterWarmup);
        Assert.Equal(0, executor.SurfacePool.ActiveLeaseCount);
        Assert.Same(compiledEffect, executor.Kernels.Effect);
        Assert.True(renderer.RenderedCommandCount > 0);
        Assert.Equal(0, diagnostics.Count);
    }

    [Fact]
    public void PrismStyleGpuPathHasNoCpuReadbackCalls()
    {
        string repositoryRoot = FindRepositoryRoot();
        string prismRuntime = Path.Combine(
            repositoryRoot,
            "Drawing",
            "MonoGame",
            "Prism");
        string[] forbiddenCalls =
        [
            "GetData(",
            "GetBackBufferData("
        ];
        List<string> violations = [];
        foreach (string file in Directory.EnumerateFiles(
            prismRuntime,
            "*.cs",
            SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            foreach (string forbiddenCall in forbiddenCalls)
            {
                if (source.Contains(
                    forbiddenCall,
                    StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(repositoryRoot, file)}:" +
                        forbiddenCall);
                }
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void BackendRoutesPrismFramesThroughTheExecutor()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        MonoGameDrawingBackend backend =
            Assert.IsType<MonoGameDrawingBackend>(
                fixture.Session.DrawingBackend);
        (DrawCommandList commands, _, _) =
            CreateSimpleComposition(96, 64);
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        DrawingFrameContext frameContext = new(analysis);

        fixture.Session.BeginFrame(CernealaColor.Transparent);
        backend.Render(commands, in frameContext);
        fixture.Session.Present();

        PresentationParameters parameters =
            fixture.Session.GraphicsDevice.PresentationParameters;
        XnaColor[] pixels =
            new XnaColor[
                parameters.BackBufferWidth *
                parameters.BackBufferHeight];
        fixture.Session.GraphicsDevice.GetBackBufferData(pixels);
        XnaColor pixel = pixels[
            ((parameters.BackBufferHeight / 2) *
                parameters.BackBufferWidth) +
            (parameters.BackBufferWidth / 2)];

        Assert.InRange(pixel.R, 126, 129);
        Assert.InRange(pixel.G, 126, 129);
        Assert.InRange(pixel.B, 126, 129);
        Assert.InRange(pixel.A, 126, 129);
        Assert.Equal(0, backend.PrismDiagnostics.Count);
    }

    internal static (
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan)
        CreateSimpleComposition(
            int width = SurfaceWidth,
            int height = SurfaceHeight,
            float opacity = 0.5f,
            long ownerToken = 1,
            float pixelScale = 1)
    {
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Executor gate",
                PrismTestData.Layer(
                    1,
                    "Half opacity",
                    opacity: opacity)),
            ownerToken: ownerToken,
            bounds: new DrawRect(0, 0, width, height),
            pixelScale: pixelScale);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(0, 0, width, height),
                CernealaColor.White),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraph graph =
            new PrismGraphBuilder().Build(analysis);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(graph);
        return (commands, analysis, plan);
    }

    internal static PrismRetainedScenario
        CreateAlphaRetainedScenario()
    {
        var scene = CreateSimpleComposition(
            ownerToken: 8_101);
        return new PrismRetainedScenario(
            "alpha",
            scene.Commands,
            scene.Analysis,
            scene.Plan,
            BackdropLease: null,
            OwnedResource: null);
    }

    internal static PrismRetainedScenario
        CreateComplexRetainedScenario(
            GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(
            graphicsDevice,
            SurfaceWidth,
            SurfaceHeight,
            false,
            SurfaceFormat.Color);
        XnaColor[] maskPixels =
            new XnaColor[SurfaceWidth * SurfaceHeight];
        for (int y = 0; y < SurfaceHeight; y++)
        {
            for (int x = 0; x < SurfaceWidth; x++)
            {
                byte alpha = (byte)Math.Clamp(
                    32 + (x * 11) + (y * 5),
                    0,
                    byte.MaxValue);
                maskPixels[(y * SurfaceWidth) + x] =
                    new XnaColor(alpha, alpha, alpha, alpha);
            }
        }
        texture.SetData(maskPixels);
        MonoGameImage image = new(texture);
        PrismResourceId maskId =
            new("RetainedMatrixMask");
        PrismDrawResources resources =
            PrismDrawResources.Create(
            [
                new PrismDrawImageResource(
                    maskId,
                    image,
                    Version: 3,
                    Identity: 81_031)
            ]);
        PrismMaskDefinition mask = new(
            maskId,
            density: 0.72f,
            feather: 1.25f);
        PrismLayerDefinition clipped = new(
            new PrismNodeId(11),
            "Clipped masked screen",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.GaussianBlur)
            ],
            styles:
            [
                new PrismStyleDefinition(
                    PrismStyleId.ColorOverlay)
            ],
            mask: mask,
            opacity: 0.82f,
            fill: 0.68f,
            blendMode: PrismBlendMode.Screen,
            clipToBelow: true);
        PrismLayerDefinition clipBase = new(
            new PrismNodeId(12),
            "Multiply clip base",
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Invert)
            ],
            blendMode: PrismBlendMode.Multiply);
        PrismGroupDefinition group = new(
            new PrismNodeId(10),
            "Isolated group",
            [clipped, clipBase],
            filters:
            [
                new PrismFilterDefinition(
                    PrismFilterId.Threshold)
            ],
            opacity: 0.88f);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Complex retained matrix",
                group),
            ownerToken: 8_103,
            bounds: new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight),
            resources: resources);
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(1, 1, 12, 10),
                new CernealaColor(224, 58, 92, 220)),
            DrawCommand.FillRectangle(
                new DrawRect(5, 4, 10, 10),
                new CernealaColor(43, 170, 224, 196)),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new PrismRetainedScenario(
            "complex",
            commands,
            analysis,
            plan,
            BackdropLease: null,
            OwnedResource: image);
    }

    internal static PrismRetainedScenario
        CreateNestedRetainedScenario()
    {
        PrismDrawScope outer = PrismTestData.Scope(
            PrismTestData.Composition(
                "Retained outer",
                new PrismLayerDefinition(
                    new PrismNodeId(20),
                    "Outer",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.Maximum)
                    ])),
            ownerToken: 8_201,
            bounds: new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight));
        PrismDrawScope inner = PrismTestData.Scope(
            PrismTestData.Composition(
                "Retained inner",
                new PrismLayerDefinition(
                    new PrismNodeId(21),
                    "Inner",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur),
                        new PrismFilterDefinition(
                            PrismFilterId.Invert)
                    ])),
            ownerToken: 8_202,
            bounds: new DrawRect(
                2,
                2,
                SurfaceWidth - 4,
                SurfaceHeight - 4));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(outer),
            DrawCommand.FillRectangle(
                new DrawRect(
                    0,
                    0,
                    SurfaceWidth,
                    SurfaceHeight),
                new CernealaColor(220, 48, 80, 210)),
            DrawCommand.BeginPrism(inner),
            DrawCommand.FillRectangle(
                new DrawRect(
                    2,
                    2,
                    SurfaceWidth - 4,
                    SurfaceHeight - 4),
                new CernealaColor(42, 188, 126, 190)),
            DrawCommand.EndPrism(),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new PrismRetainedScenario(
            "nested",
            commands,
            analysis,
            plan,
            BackdropLease: null,
            OwnedResource: null);
    }

    internal static PrismRetainedScenario
        CreateBackdropRetainedScenario(
            GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(
            graphicsDevice,
            SurfaceWidth,
            SurfaceHeight,
            false,
            SurfaceFormat.Color);
        texture.SetData(
            Enumerable.Repeat(
                    new XnaColor(38, 112, 210, 255),
                    SurfaceWidth * SurfaceHeight)
                .ToArray());
        BackdropFrameMetadata metadata = new(
            SurfaceWidth,
            SurfaceHeight,
            1,
            PrismColorProfile.Srgb,
            BackdropPixelFormat.Rgba8Unorm,
            BackdropAlphaMode.Opaque,
            System.Numerics.Matrix3x2.Identity,
            8_301);
        TestBackdropLease lease = new(texture, metadata);
        PrismDrawScope scope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Retained backdrop",
                PrismTestData.Layer(
                    1,
                    "Foreground",
                    opacity: 0.65f),
                PrismTestData.Backdrop(
                    2,
                    "Versioned host backdrop")),
            ownerToken: 8_301,
            bounds: new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight));
        DrawCommandList commands = PrismTestData.Commands(
            DrawCommand.BeginPrism(scope),
            DrawCommand.FillRectangle(
                new DrawRect(3, 3, 10, 10),
                new CernealaColor(230, 70, 86, 208)),
            DrawCommand.EndPrism());
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(
                    analysis,
                    metadata,
                    PrismBackdropSourceToken.CreateUnique()));
        return new PrismRetainedScenario(
            "backdrop",
            commands,
            analysis,
            plan,
            lease,
            OwnedResource: null);
    }

    internal static PrismRetainedRasterContext
        CreateRetainedRasterContext(
            PrismFrameAnalysis analysis,
            Viewport viewport,
            PrismColorProfile outputColorProfile =
                PrismColorProfile.Srgb,
            long shaderPackageVersion =
                PrismKernelRegistry.ShaderPackageVersion) =>
        new(
            viewport.Width,
            viewport.Height,
            outputColorProfile,
            BackdropPixelFormat.Rgba16Float,
            PrismSampling.Linear,
            analysis.RequiredCapabilities,
            shaderPackageVersion);

    private static ExpectedCacheWork
        CalculateExpectedCacheWork(
            PrismGraphExecutionPlan plan,
            PrismRetainedSurfaceCache cache,
            PrismRetainedRasterContext rasterContext)
    {
        int nodeCount = plan.ExecutionOrder.Length;
        bool[] hits = new bool[nodeCount];
        bool[] required = new bool[nodeCount];
        int[] pending = new int[nodeCount];
        for (int index = 0; index < nodeCount; index++)
        {
            hits[index] =
                PrismRetainedCacheKey.TryCreate(
                    plan,
                    plan.ExecutionOrder[index],
                    rasterContext,
                    out PrismRetainedCacheKey key) &&
                cache.Contains(key);
        }

        int pendingCount = 0;
        foreach (int rootIndex in
            plan.RootOutputExecutionIndices)
        {
            required[rootIndex] = true;
            pending[pendingCount++] = rootIndex;
        }
        while (pendingCount > 0)
        {
            int index = pending[--pendingCount];
            if (hits[index])
            {
                continue;
            }

            foreach (int inputIndex in
                plan.CacheInputExecutionIndices[index])
            {
                if (required[inputIndex])
                {
                    continue;
                }

                required[inputIndex] = true;
                pending[pendingCount++] = inputIndex;
            }
        }

        int graphPassCount = 0;
        int captureCount = 0;
        for (int index = 0; index < nodeCount; index++)
        {
            if (!required[index] || hits[index])
            {
                continue;
            }

            graphPassCount++;
            if (plan.OptimizedGraph
                    .GetNode(plan.ExecutionOrder[index])
                    .Kind ==
                PrismGraphNodeKind.ControlCapture)
            {
                captureCount++;
            }
        }

        int presentationCount = 0;
        foreach (PrismGraphScope scope in
            plan.OptimizedGraph.Scopes)
        {
            if (scope.Output is PrismGraphNodeId output &&
                required[plan.GetExecutionIndex(output)])
            {
                presentationCount++;
            }
        }

        return new ExpectedCacheWork(
            graphPassCount + presentationCount,
            graphPassCount,
            captureCount);
    }

    internal static int RemoveFinalEntries(
        PrismGraphExecutionPlan plan,
        PrismRetainedSurfaceCache cache,
        PrismRetainedRasterContext rasterContext)
    {
        int removed = 0;
        for (int index = 0;
            index < plan.ExecutionOrder.Length;
            index++)
        {
            if (plan.NodePlans[index].CacheCandidateKind !=
                    PrismRetainedCacheCandidateKind.Final ||
                !PrismRetainedCacheKey.TryCreate(
                    plan,
                    plan.ExecutionOrder[index],
                    rasterContext,
                    out PrismRetainedCacheKey key))
            {
                continue;
            }

            if (cache.Remove(key))
            {
                removed++;
            }
        }

        return removed;
    }

    private static void AssertScenarioCoverage(
        PrismRetainedScenario scenario)
    {
        PrismGraph graph = scenario.Plan.OptimizedGraph;
        switch (scenario.Name)
        {
            case "alpha":
                Assert.Contains(
                    graph.Nodes,
                    node =>
                        node.Kind ==
                        PrismGraphNodeKind.Opacity);
                break;
            case "complex":
                Assert.Contains(
                    graph.Nodes,
                    node => node.Kind == PrismGraphNodeKind.Mask);
                Assert.Contains(
                    graph.Nodes,
                    node => node.Kind == PrismGraphNodeKind.ClipToBelow);
                Assert.Contains(
                    graph.Nodes,
                    node => node.Kind == PrismGraphNodeKind.Group);
                Assert.Contains(
                    graph.Nodes,
                    node => node.Kind == PrismGraphNodeKind.Style);
                Assert.Contains(
                    graph.Nodes,
                    node => node.Kind == PrismGraphNodeKind.Filter);
                Assert.Contains(
                    graph.Nodes,
                    node =>
                        node.BlendMode is
                            PrismBlendMode.Screen or
                            PrismBlendMode.Multiply);
                break;
            case "nested":
                Assert.True(graph.Scopes.Length > 1);
                break;
            case "backdrop":
                Assert.Contains(
                    graph.Nodes,
                    node =>
                        node.Kind ==
                        PrismGraphNodeKind.BackdropInput);
                Assert.Contains(
                    graph.Nodes,
                    node =>
                        node.Kind ==
                        PrismGraphNodeKind.BackdropCrop);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unknown retained scenario '{scenario.Name}'.");
        }
    }

    private static void AssertPixelsWithin(
        XnaColor[] actual,
        XnaColor[] expected,
        int tolerance,
        string context)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int index = 0; index < actual.Length; index++)
        {
            AssertByteWithin(
                actual[index].R,
                expected[index].R,
                tolerance,
                $"{context} pixel {index}",
                "red");
            AssertByteWithin(
                actual[index].G,
                expected[index].G,
                tolerance,
                $"{context} pixel {index}",
                "green");
            AssertByteWithin(
                actual[index].B,
                expected[index].B,
                tolerance,
                $"{context} pixel {index}",
                "blue");
            AssertByteWithin(
                actual[index].A,
                expected[index].A,
                tolerance,
                $"{context} pixel {index}",
                "alpha");
        }
    }

    private static PrismProfileScenario[] CreateRepresentativeScenarios()
    {
        (
            DrawCommandList simpleCommands,
            PrismFrameAnalysis simpleAnalysis,
            PrismGraphExecutionPlan simplePlan) =
            CreateSimpleComposition();
        PrismLayerDefinition chainedLayer = new(
            new PrismNodeId(10),
            "Chained",
            filters:
            [
                new PrismFilterDefinition(PrismFilterId.GaussianBlur),
                new PrismFilterDefinition(PrismFilterId.Threshold),
                new PrismFilterDefinition(PrismFilterId.Invert)
            ]);
        PrismDrawScope chainedScope = PrismTestData.Scope(
            PrismTestData.Composition(
                "Chained profile",
                chainedLayer),
            ownerToken: 10,
            bounds: new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight));
        PrismProfileScenario chained = CreateProfileScenario(
            "chained",
            PrismTestData.Commands(
                DrawCommand.BeginPrism(chainedScope),
                DrawCommand.FillRectangle(
                    new DrawRect(
                        0,
                        0,
                        SurfaceWidth,
                        SurfaceHeight),
                    CernealaColor.White),
                DrawCommand.EndPrism()));

        PrismDrawScope outer = PrismTestData.Scope(
            PrismTestData.Composition(
                "Nested outer profile",
                new PrismLayerDefinition(
                    new PrismNodeId(20),
                    "Outer",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.Maximum)
                    ])),
            ownerToken: 20,
            bounds: new DrawRect(
                0,
                0,
                SurfaceWidth,
                SurfaceHeight));
        PrismDrawScope inner = PrismTestData.Scope(
            PrismTestData.Composition(
                "Nested inner profile",
                new PrismLayerDefinition(
                    new PrismNodeId(21),
                    "Inner",
                    filters:
                    [
                        new PrismFilterDefinition(
                            PrismFilterId.GaussianBlur),
                        new PrismFilterDefinition(
                            PrismFilterId.Invert)
                    ])),
            ownerToken: 21,
            bounds: new DrawRect(
                2,
                2,
                SurfaceWidth - 4,
                SurfaceHeight - 4));
        PrismProfileScenario nested = CreateProfileScenario(
            "nested",
            PrismTestData.Commands(
                DrawCommand.BeginPrism(outer),
                DrawCommand.FillRectangle(
                    new DrawRect(
                        0,
                        0,
                        SurfaceWidth,
                        SurfaceHeight),
                    CernealaColor.White),
                DrawCommand.BeginPrism(inner),
                DrawCommand.FillRectangle(
                    new DrawRect(
                        2,
                        2,
                        SurfaceWidth - 4,
                        SurfaceHeight - 4),
                    CernealaColor.White),
                DrawCommand.EndPrism(),
                DrawCommand.EndPrism()));

        return
        [
            new PrismProfileScenario(
                "simple",
                simpleCommands,
                simpleAnalysis,
                simplePlan),
            chained,
            nested
        ];
    }

    private static PrismProfileScenario CreateProfileScenario(
        string name,
        DrawCommandList commands)
    {
        PrismFrameAnalysis analysis =
            new PrismFrameAnalyzer().Analyze(commands);
        PrismGraphExecutionPlan plan =
            new PrismGraphOptimizer().Optimize(
                new PrismGraphBuilder().Build(analysis));
        return new PrismProfileScenario(
            name,
            commands,
            analysis,
            plan);
    }

    internal static void ExecuteFrame(
        TestPrismRenderer renderer,
        PrismGraphExecutor executor,
        DrawCommandList commands,
        PrismFrameAnalysis analysis,
        PrismGraphExecutionPlan plan,
        Viewport viewport,
        IBackdropFrameLease? backdropLease = null)
    {
        renderer.BeginFrame();
        try
        {
            executor.Execute(
                commands,
                analysis,
                plan,
                renderer,
                viewport,
                backdropLease);
        }
        finally
        {
            renderer.EndBatch();
        }
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(
                Path.Combine(current.FullName, "Cerneala.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the Cerneala repository root.");
    }

    private static RenderTarget2D CreateTarget(
        GraphicsDevice graphicsDevice,
        int width,
        SurfaceFormat format)
    {
        return new RenderTarget2D(
            graphicsDevice,
            width,
            1,
            mipMap: false,
            format,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);
    }

    private static void DrawKernel(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        PrismKernel kernel,
        Texture2D source,
        Texture2D secondary,
        RenderTarget2D target,
        float opacity,
        PrismBlendOptions? blendOptions = null,
        bool backgroundAvailable = true,
        Texture2D? knockoutBackdrop = null,
        Texture2D? knockoutShape = null)
    {
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismBlendOptions options =
            blendOptions ?? PrismBlendOptions.Default;
        PrismKernelParameters parameters = new(
            secondary,
            opacity,
            new Vector2(
                1f / target.Width,
                1f / target.Height),
            Vector2.One,
            Vector2.Zero)
        {
            BlendChannels = ToBlendChannels(options.BlendChannels),
            KnockoutMode = (float)options.Knockout,
            KnockoutBackdropTexture =
                knockoutBackdrop ?? secondary,
            KnockoutShapeTexture =
                knockoutShape ?? source,
            KnockoutBackdropAvailable = 1,
            BlendIfChannel = (float)options.BlendIfChannel,
            ThisLayerRange = ToBlendRange(options.ThisLayerRange),
            UnderlyingRange =
                ToBlendRange(options.UnderlyingRange),
            DissolveSeed =
                PrismBlendMath.NormalizeDissolveSeed(
                    options.DissolveSeed,
                    options.LayerIdentity),
            BackgroundAvailable =
                backgroundAvailable ? 1 : 0
        };
        registry.Bind(kernel, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, target.Width, target.Height),
            XnaColor.White);
        spriteBatch.End();
    }

    private static HalfVector4[] DrawMaskKernel(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        PrismKernel kernel,
        Texture2D source,
        RenderTarget2D target,
        PrismMaskChannel channel,
        float density = 1,
        bool invert = false,
        Vector3? uvRowX = null,
        Vector2 featherStep = default)
    {
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(
                1f / target.Width,
                1f / target.Height),
            Vector2.One,
            Vector2.Zero)
        {
            MaskChannel = (float)channel,
            MaskDensity = density,
            MaskInvert = invert ? 1 : 0,
            MaskUvRowX =
                uvRowX ?? new Vector3(1f / target.Width, 0, 0),
            MaskUvRowY = new Vector3(0, 0, 0.5f),
            MaskFeatherStep = featherStep
        };
        registry.Bind(kernel, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, target.Width, target.Height),
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] pixels =
            new HalfVector4[target.Width * target.Height];
        target.GetData(pixels);
        return pixels;
    }

    private static Texture2D CreateHalfVectorTexture(
        GraphicsDevice graphicsDevice,
        PrismPremultipliedColor[] colors)
    {
        Texture2D texture = new(
            graphicsDevice,
            colors.Length,
            1,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(
            colors
                .Select(ToHalfVector)
                .ToArray());
        return texture;
    }

    private static HalfVector4 ToHalfVector(
        PrismPremultipliedColor color)
    {
        return new HalfVector4(
            new Vector4(
                (float)color.Red,
                (float)color.Green,
                (float)color.Blue,
                (float)color.Alpha));
    }

    private static Microsoft.Xna.Framework.Vector4 ToXnaVector4(
        System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);

    private static Vector4 ToBlendChannels(
        PrismBlendChannels channels)
    {
        return new Vector4(
            (channels & PrismBlendChannels.Red) != 0 ? 1 : 0,
            (channels & PrismBlendChannels.Green) != 0 ? 1 : 0,
            (channels & PrismBlendChannels.Blue) != 0 ? 1 : 0,
            (channels & PrismBlendChannels.Alpha) != 0 ? 1 : 0);
    }

    private static Vector4 ToBlendRange(PrismBlendRange range)
    {
        return new Vector4(
            range.BlackStart,
            range.BlackEnd,
            range.WhiteStart,
            range.WhiteEnd);
    }

    private static PrismPremultipliedColor Premultiply(
        double red,
        double green,
        double blue,
        double alpha)
    {
        return new PrismPremultipliedColor(
            red * alpha,
            green * alpha,
            blue * alpha,
            alpha);
    }

    private static PrismPremultipliedColor ToPremultipliedColor(
        XnaColor color)
    {
        const double scale = 1d / byte.MaxValue;
        return new PrismPremultipliedColor(
            color.R * scale,
            color.G * scale,
            color.B * scale,
            color.A * scale);
    }

    private static PrismPremultipliedColor Scale(
        PrismPremultipliedColor color,
        double amount)
    {
        return new PrismPremultipliedColor(
            color.Red * amount,
            color.Green * amount,
            color.Blue * amount,
            color.Alpha * amount);
    }

    private static PrismPremultipliedColor Over(
        PrismPremultipliedColor foreground,
        PrismPremultipliedColor background)
    {
        double backgroundAmount = 1 - foreground.Alpha;
        return new PrismPremultipliedColor(
            foreground.Red + (background.Red * backgroundAmount),
            foreground.Green + (background.Green * backgroundAmount),
            foreground.Blue + (background.Blue * backgroundAmount),
            foreground.Alpha + (background.Alpha * backgroundAmount));
    }

    private static void AssertColorWithin(
        XnaColor actual,
        PrismPremultipliedColor expected,
        int tolerance,
        string context)
    {
        AssertByteWithin(
            actual.R,
            ToByte(expected.Red),
            tolerance,
            context,
            "red");
        AssertByteWithin(
            actual.G,
            ToByte(expected.Green),
            tolerance,
            context,
            "green");
        AssertByteWithin(
            actual.B,
            ToByte(expected.Blue),
            tolerance,
            context,
            "blue");
        AssertByteWithin(
            actual.A,
            ToByte(expected.Alpha),
            tolerance,
            context,
            "alpha");
    }

    private static void AssertHalfVectorWithin(
        HalfVector4 actual,
        PrismPremultipliedColor expected,
        double tolerance,
        string context)
    {
        Vector4 value = actual.ToVector4();
        Assert.True(
            Math.Abs(value.X - expected.Red) <= tolerance,
            $"{context} red was {value.X:R}, expected {expected.Red:R}.");
        Assert.True(
            Math.Abs(value.Y - expected.Green) <= tolerance,
            $"{context} green was {value.Y:R}, expected {expected.Green:R}.");
        Assert.True(
            Math.Abs(value.Z - expected.Blue) <= tolerance,
            $"{context} blue was {value.Z:R}, expected {expected.Blue:R}.");
        Assert.True(
            Math.Abs(value.W - expected.Alpha) <= tolerance,
            $"{context} alpha was {value.W:R}, expected {expected.Alpha:R}.");
    }

    private static void AssertPremultipliedWithin(
        PrismPremultipliedColor actual,
        PrismPremultipliedColor expected,
        double tolerance,
        string context)
    {
        Assert.True(
            Math.Abs(actual.Red - expected.Red) <= tolerance,
            $"{context} red was {actual.Red:R}, expected {expected.Red:R}.");
        Assert.True(
            Math.Abs(actual.Green - expected.Green) <= tolerance,
            $"{context} green was {actual.Green:R}, expected {expected.Green:R}.");
        Assert.True(
            Math.Abs(actual.Blue - expected.Blue) <= tolerance,
            $"{context} blue was {actual.Blue:R}, expected {expected.Blue:R}.");
        Assert.True(
            Math.Abs(actual.Alpha - expected.Alpha) <= tolerance,
            $"{context} alpha was {actual.Alpha:R}, expected {expected.Alpha:R}.");
    }

    private static void AssertByteWithin(
        byte actual,
        byte expected,
        int tolerance,
        string context,
        string channel)
    {
        Assert.True(
            Math.Abs(actual - expected) <= tolerance,
            $"{context} {channel} was {actual}, expected {expected} " +
            $"within {tolerance}.");
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Round(
            Math.Clamp(value, 0, 1) * byte.MaxValue,
            MidpointRounding.AwayFromZero);
    }

    internal sealed class TestPrismRenderer :
        IPrismCommandRenderer,
        IDisposable
    {
        private readonly SpriteBatch spriteBatch;
        private readonly Texture2D whitePixel;
        private readonly RenderTarget2D hostTarget;
        private bool batchActive;

        public TestPrismRenderer(
            GraphicsDevice graphicsDevice,
            int width,
            int height)
        {
            GraphicsDevice = graphicsDevice;
            spriteBatch = new SpriteBatch(graphicsDevice);
            whitePixel = new Texture2D(graphicsDevice, 1, 1);
            whitePixel.SetData([XnaColor.White]);
            hostTarget = new RenderTarget2D(
                graphicsDevice,
                width,
                height,
                mipMap: false,
                SurfaceFormat.Color,
                DepthFormat.None,
                preferredMultiSampleCount: 0,
                RenderTargetUsage.PreserveContents);
        }

        public GraphicsDevice GraphicsDevice { get; }

        public int RenderedCommandCount { get; private set; }

        public bool UsedAnisotropicKernelSampler { get; private set; }

        public bool ThrowOnNextRenderCommand { get; set; }

        public void BeginFrame()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(hostTarget);
            GraphicsDevice.Clear(XnaColor.Transparent);
            BeginCommandBatch();
        }

        public void ResetRenderedCommandCount()
        {
            RenderedCommandCount = 0;
        }

        public XnaColor ReadCenterPixel()
        {
            XnaColor[] pixels = ReadPixels();
            return pixels[
                ((hostTarget.Height / 2) * hostTarget.Width) +
                (hostTarget.Width / 2)];
        }

        public XnaColor[] ReadPixels()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(null);
            XnaColor[] pixels =
                new XnaColor[hostTarget.Width * hostTarget.Height];
            hostTarget.GetData(pixels);
            return pixels;
        }

        public void BeginCommandBatch()
        {
            BeginBatch(effect: null, BlendState.AlphaBlend);
        }

        public void BeginKernelBatch(
            Effect effect,
            BlendState blendState,
            SamplerState samplerState)
        {
            UsedAnisotropicKernelSampler |=
                ReferenceEquals(
                    samplerState,
                    SamplerState.AnisotropicClamp);
            BeginBatch(effect, blendState, samplerState);
        }

        public void EndBatch()
        {
            if (!batchActive)
            {
                return;
            }

            try
            {
                spriteBatch.End();
            }
            finally
            {
                batchActive = false;
            }
        }

        public void RenderCommand(DrawCommand command)
        {
            if (ThrowOnNextRenderCommand)
            {
                ThrowOnNextRenderCommand = false;
                throw new InvalidOperationException(
                    "Injected retained-cache execution failure.");
            }

            if (command.Kind != DrawCommandKind.FillRectangle ||
                command.Brush is not null)
            {
                throw new InvalidOperationException(
                    $"Unsupported executor test command '{command.Kind}'.");
            }

            RenderedCommandCount++;
            Rectangle destination = new(
                (int)MathF.Round(command.Rect.X),
                (int)MathF.Round(command.Rect.Y),
                (int)MathF.Round(command.Rect.Width),
                (int)MathF.Round(command.Rect.Height));
            spriteBatch.Draw(
                whitePixel,
                destination,
                new XnaColor(
                    command.Color.R,
                    command.Color.G,
                    command.Color.B,
                    command.Color.A));
        }

        public void DrawFullscreen(
            Texture2D texture,
            Rectangle destination)
        {
            spriteBatch.Draw(
                texture,
                destination,
                XnaColor.White);
        }

        public void RestoreHostTarget()
        {
            GraphicsDevice.SetRenderTarget(hostTarget);
            GraphicsDevice.Viewport =
                new Viewport(
                    0,
                    0,
                    hostTarget.Width,
                    hostTarget.Height);
        }

        public void Dispose()
        {
            EndBatch();
            GraphicsDevice.SetRenderTarget(null);
            hostTarget.Dispose();
            whitePixel.Dispose();
            spriteBatch.Dispose();
        }

        private void BeginBatch(
            Effect? effect,
            BlendState blendState,
            SamplerState? samplerState = null)
        {
            if (batchActive)
            {
                throw new InvalidOperationException(
                    "The executor test SpriteBatch is already active.");
            }

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                blendState,
                samplerState ?? SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                effect);
            batchActive = true;
        }
    }

    private readonly record struct PrismProfileScenario(
        string Name,
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan);

    private readonly record struct ExpectedCacheWork(
        int PassCount,
        int GraphPassCount,
        int CaptureCount);

    internal sealed record PrismRetainedScenario(
        string Name,
        DrawCommandList Commands,
        PrismFrameAnalysis Analysis,
        PrismGraphExecutionPlan Plan,
        IBackdropFrameLease? BackdropLease,
        IDisposable? OwnedResource) :
        IDisposable
    {
        public void Dispose()
        {
            BackdropLease?.Dispose();
            OwnedResource?.Dispose();
        }
    }

    private sealed class TestBackdropLease :
        IMonoGameBackdropFrameLease
    {
        private Texture2D? texture;

        public TestBackdropLease(
            Texture2D texture,
            BackdropFrameMetadata metadata)
        {
            this.texture = texture;
            Metadata = metadata;
        }

        public BackdropFrameMetadata Metadata { get; }

        public Texture2D Texture =>
            texture ??
            throw new ObjectDisposedException(
                nameof(TestBackdropLease));

        public void Dispose()
        {
            Texture2D? ownedTexture = texture;
            if (ownedTexture is null)
            {
                return;
            }

            texture = null;
            ownedTexture.Dispose();
        }
    }

    internal sealed class WindowsDxFixture : IDisposable
    {
        private readonly Win32WindowPlatform platform = new();
        private readonly IPlatformWindow window;

        public WindowsDxFixture()
        {
            window = platform.CreateWindow(
                new Window
                {
                    Title =
                        $"Cerneala Prism executor {Guid.NewGuid():N}",
                    Width = 96,
                    Height = 64
                },
                new CallbackSink());
            window.Show();
            platform.PumpEvents();
            Session =
                Assert.IsType<WindowsDxWindowGraphicsSession>(
                    window.GraphicsSession);
        }

        public WindowsDxWindowGraphicsSession Session { get; }

        public void Dispose()
        {
            window.Dispose();
            platform.Dispose();
        }
    }

    private sealed class CallbackSink : IWindowPlatformCallbacks
    {
        public void RequestClose() { }

        public void ActivationChanged(bool active) { }

        public void BoundsChanged(
            UiViewport viewport,
            float left,
            float top,
            WindowState state)
        {
        }

        public void RenderRequested() { }
    }
}
