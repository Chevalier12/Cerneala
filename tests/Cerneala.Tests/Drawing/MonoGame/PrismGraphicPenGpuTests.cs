using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismGraphicPenGpuTests
{
    [Fact]
    public void FlowXDogPipelineRendersWithDedicatedGraphicPenTechnique()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 32;
        const int height = 24;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D original = CreateSubject(
            graphicsDevice,
            width,
            height);
        using RenderTarget2D first = CreateTarget(
            graphicsDevice,
            width,
            height);
        using RenderTarget2D second = CreateTarget(
            graphicsDevice,
            width,
            height);
        PrismCatalogFilterPlan plan = CreatePlan();

        Texture2D current = original;
        RenderTarget2D target = first;
        HalfVector4[]? flowPixels = null;
        foreach (PrismCatalogFilterPass pass in plan.Passes)
        {
            PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
                PrismFilterId.GraphicPen,
                pass.Iteration);
            DrawPass(
                graphicsDevice,
                spriteBatch,
                registry,
                kernel,
                current,
                original,
                target,
                plan,
                pass);
            current = target;
            target = ReferenceEquals(target, first) ? second : first;
            if (pass.Iteration == 5)
            {
                graphicsDevice.SetRenderTarget(null);
                flowPixels = new HalfVector4[width * height];
                current.GetData(flowPixels);
            }
        }

        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] sourcePixels = new HalfVector4[width * height];
        HalfVector4[] resultPixels = new HalfVector4[width * height];
        original.GetData(sourcePixels);
        current.GetData(resultPixels);

        Assert.Equal("GraphicPenFilter", registry.Effect.CurrentTechnique.Name);
        Assert.Equal(
            Vector4.One,
            registry.Effect.Parameters["FilterOptions0"].GetValueVector4());
        Assert.Equal(
            68,
            registry.Effect.Parameters["FilterOptions2"].GetValueVector4().X);
        Assert.Equal(
            12,
            registry.Effect.Parameters["FilterOptions4"].GetValueVector4().X);
        Assert.NotNull(flowPixels);
        double meanFlowResponse = flowPixels
            .Select(pixel => pixel.ToVector4())
            .Average(pixel => Math.Abs((pixel.Z * 2) - 1));
        Assert.InRange(meanFlowResponse, 0, 0.35);
        double difference = 0;
        PrismPremultipliedColor[] cpuSource = sourcePixels
            .Select(pixel => pixel.ToVector4())
            .Select(pixel => new PrismPremultipliedColor(
                pixel.X,
                pixel.Y,
                pixel.Z,
                pixel.W))
            .ToArray();
        PrismPremultipliedColor[] cpuResult = PrismCatalogFilterMath.Apply(
            plan,
            cpuSource,
            width,
            height,
            PrismColorProfile.LinearSrgb);
        double cpuInk = 0;
        double gpuInk = 0;
        for (int index = 0; index < resultPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index].ToVector4();
            Vector4 result = resultPixels[index].ToVector4();
            difference +=
                Math.Abs(source.X - result.X) +
                Math.Abs(source.Y - result.Y) +
                Math.Abs(source.Z - result.Z);
            if (result.W > 0.0001f)
            {
                cpuInk += 1 -
                    (((cpuResult[index].Red * 0.2126) +
                        (cpuResult[index].Green * 0.7152) +
                        (cpuResult[index].Blue * 0.0722)) /
                    cpuResult[index].Alpha);
                gpuInk += 1 -
                    (((result.X * 0.2126) +
                        (result.Y * 0.7152) +
                        (result.Z * 0.0722)) /
                    result.W);
            }
            Assert.InRange(Math.Abs(source.W - result.W), 0, 0.01f);
            Assert.InRange(result.X, 0, result.W + 0.01f);
            Assert.InRange(result.Y, 0, result.W + 0.01f);
            Assert.InRange(result.Z, 0, result.W + 0.01f);
        }

        Assert.True(difference / resultPixels.Length > 0.04);
        double cpuInkMean = cpuInk / resultPixels.Length;
        double gpuInkMean = gpuInk / resultPixels.Length;
        Assert.True(
            Math.Abs(cpuInkMean - gpuInkMean) <= 0.12,
            $"CPU ink {cpuInkMean:F4}, GPU ink {gpuInkMean:F4}, " +
            $"flow response {meanFlowResponse:F4}.");
    }

    private static void DrawPass(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        PrismKernel kernel,
        Texture2D source,
        Texture2D original,
        RenderTarget2D target,
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass)
    {
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        float packedPass = (int)pass.Kind + (pass.Iteration * 4);
        PrismKernelParameters parameters = new(
            original,
            1,
            new Vector2(1f / target.Width, 1f / target.Height),
            Vector2.One,
            Vector2.Zero)
        {
            SourceTexture = source,
            FilterHeader = new Vector4(
                (int)PrismFilterId.GraphicPen,
                (int)PrismColorProfile.LinearSrgb,
                (int)plan.Primitive,
                0),
            FilterOptions0 = ToXna(plan.Options0),
            FilterOptions1 = ToXna(plan.Options1),
            FilterOptions2 = ToXna(plan.Options2),
            FilterOptions3 = ToXna(plan.Options3),
            FilterOptions4 = ToXna(plan.Options4),
            FilterOptions5 = ToXna(plan.Options5),
            FilterOptions6 = ToXna(plan.Options6),
            FilterOptions7 = ToXna(plan.Options7),
            FilterOptions8 = ToXna(plan.Options8),
            FilterOptions9 = new Vector4(
                pass.RadiusX,
                pass.RadiusY,
                packedPass,
                (int)plan.BlendMode),
            FilterTextureSize = new Vector2(original.Width, original.Height),
            FilterAuxiliaryTexture = original
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
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.GraphicPen,
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                ColorParameter(1, Cerneala.Drawing.Color.Black),
                Number(2, 68),
                Symbol(3, "StrokeDirection", "LeftDiagonal"),
                Number(4, 12)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 24));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static Texture2D CreateSubject(
        GraphicsDevice graphicsDevice,
        int width,
        int height)
    {
        HalfVector4[] pixels = new HalfVector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float edge = x < width / 2 ? 0.12f : 0.8f;
                float grain = ((x * 7) + (y * 11)) % 17 / 120f;
                float value = Math.Clamp(edge + grain, 0, 1);
                float alpha = (x + y) % 9 == 0 ? 0.45f : 0.86f;
                pixels[(y * width) + x] = new HalfVector4(
                    value * alpha,
                    value * 0.92f * alpha,
                    value * 0.74f * alpha,
                    alpha);
            }
        }

        Texture2D texture = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(pixels);
        return texture;
    }

    private static RenderTarget2D CreateTarget(
        GraphicsDevice graphicsDevice,
        int width,
        int height) =>
        new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
