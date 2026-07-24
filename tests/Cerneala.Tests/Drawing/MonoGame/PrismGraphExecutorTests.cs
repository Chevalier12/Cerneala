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
    public void RegistryValidatesCatalogAndRegistersGeneratedColorKernels()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

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
            Assert.Equal(
                isAdjustment
                    ? PrismKernelKind.AdjustmentFilter
                    : isNeighborhood
                        ? PrismKernelKind.NeighborhoodFilter
                        : isResampling
                            ? PrismKernelKind.ResamplingFilter
                            : PrismKernelKind.CatalogFilter,
                filterKernel.Kind);
            Assert.Equal(
                isAdjustment
                    ? "AdjustmentFilter"
                    : isNeighborhood
                        ? "NeighborhoodFilter"
                        : isResampling
                            ? "ResamplingFilter"
                            : "CatalogFilter",
                filterKernel.Technique.Name);
            Assert.Equal(
                isAdjustment
                    ? registry.AdjustmentFilter
                    : isNeighborhood
                        ? registry.NeighborhoodFilter
                        : isResampling
                            ? registry.ResamplingFilter
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
    public void NeighborhoodNoiseGpuIsDeterministicAndUsesPreparedSeed()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 32;
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

        Assert.Equal(first, repeated);
        Assert.False(first.SequenceEqual(changed));
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

        HalfVector4[] DrawNoise(int seed)
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
                    0,
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
        bool backgroundAvailable = true)
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
            BlendState blendState)
        {
            BeginBatch(effect, blendState);
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
            BlendState blendState)
        {
            if (batchActive)
            {
                throw new InvalidOperationException(
                    "The executor test SpriteBatch is already active.");
            }

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                blendState,
                SamplerState.LinearClamp,
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
