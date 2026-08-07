using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismScanlinesGpuTests
{
    [Fact]
    public void GpuMatchesGeneralizedGaussianCpuFallback()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 12;
        const int height = 20;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D sourceTexture = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        using RenderTarget2D target = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);

        HalfVector4[] sourcePixels = CreateSource(width, height);
        sourceTexture.SetData(sourcePixels);
        PrismCatalogFilterPlan plan = CreatePlan(width, height);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
            PrismFilterId.Scanlines,
            pass.Iteration);
        Vector2 textureSize = new(width, height);
        PrismKernelParameters parameters = new(
            sourceTexture,
            1,
            Vector2.One / textureSize,
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Scanlines,
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
                (int)pass.Kind,
                (int)plan.BlendMode),
            FilterTextureSize = textureSize
        };

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        registry.Bind(kernel, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            sourceTexture,
            new Rectangle(0, 0, width, height),
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] gpuPixels = new HalfVector4[width * height];
        target.GetData(gpuPixels);
        PrismPremultipliedColor[] cpuPixels = PrismCatalogFilterMath.Apply(
            plan,
            sourcePixels.Select(ToPrismColor).ToArray(),
            width,
            height,
            PrismColorProfile.LinearSrgb);

        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index].ToVector4();
            PrismPremultipliedColor cpu = cpuPixels[index];
            AssertClose(index, "R", gpu.X, cpu.Red);
            AssertClose(index, "G", gpu.Y, cpu.Green);
            AssertClose(index, "B", gpu.Z, cpu.Blue);
            AssertClose(index, "A", gpu.W, cpu.Alpha);
        }
    }

    private static void AssertClose(
        int index,
        string channel,
        double gpu,
        double cpu) =>
        Assert.True(
            Math.Abs(gpu - cpu) <= 0.004,
            $"Pixel {index} {channel}: GPU={gpu:F6}, CPU={cpu:F6}");

    private static HalfVector4[] CreateSource(int width, int height)
    {
        HalfVector4[] pixels = new HalfVector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.3f + (0.04f * x) + (0.01f * y);
                pixels[(y * width) + x] = new HalfVector4(
                    (0.15f + (0.05f * x)) * alpha,
                    (0.2f + (0.025f * y)) * alpha,
                    (0.8f - (0.04f * x)) * alpha,
                    alpha);
            }
        }
        return pixels;
    }

    private static PrismCatalogFilterPlan CreatePlan(
        int width,
        int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Scanlines,
            [
                ColorParameter(0, new Cerneala.Drawing.Color(32, 96, 224, 180)),
                Number(1, 7.5f),
                Number(2, 0.72f),
                Number(3, 0.17f),
                Number(4, 0.65f),
                Number(5, 0.42f)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static PrismPremultipliedColor ToPrismColor(HalfVector4 value)
    {
        Vector4 color = value.ToVector4();
        return new PrismPremultipliedColor(
            color.X,
            color.Y,
            color.Z,
            color.W);
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
