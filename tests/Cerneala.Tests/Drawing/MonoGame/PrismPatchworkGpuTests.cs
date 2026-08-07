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

public sealed class PrismPatchworkGpuTests
{
    [Fact]
    public void RegistryRoutesPatchworkToDedicatedShaderTechnique()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.Patchwork,
                out PrismKernel kernel));
        Assert.Equal("PatchworkFilter", kernel.Technique.Name);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(32)]
    public void PatchworkShaderMatchesCpuAndPreservesAssociatedAlpha(
        float relief)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 12;
        const int height = 8;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = CreateSubject(graphicsDevice, width, height);
        using RenderTarget2D target = CreateTarget(graphicsDevice, width, height);
        PrismCatalogFilterPlan plan = CreatePlan(relief);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        DrawPass(
            graphicsDevice,
            spriteBatch,
            registry,
            source,
            target,
            plan,
            pass);
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] sourcePixels = new HalfVector4[width * height];
        HalfVector4[] gpuPixels = new HalfVector4[width * height];
        source.GetData(sourcePixels);
        target.GetData(gpuPixels);
        PrismPremultipliedColor[] cpuPixels = PrismCatalogFilterMath.Apply(
            plan,
            sourcePixels.Select(ToPrismColor).ToArray(),
            width,
            height,
            PrismColorProfile.LinearSrgb);

        Assert.Equal("PatchworkFilter", registry.Effect.CurrentTechnique.Name);
        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index].ToVector4();
            PrismPremultipliedColor cpu = cpuPixels[index];
            AssertClose(index, "red", gpu.X, (float)cpu.Red);
            AssertClose(index, "green", gpu.Y, (float)cpu.Green);
            AssertClose(index, "blue", gpu.Z, (float)cpu.Blue);
            AssertClose(index, "alpha", gpu.W, (float)cpu.Alpha);
            Assert.InRange(gpu.X, 0, gpu.W + 0.006f);
            Assert.InRange(gpu.Y, 0, gpu.W + 0.006f);
            Assert.InRange(gpu.Z, 0, gpu.W + 0.006f);
        }
    }

    private static void DrawPass(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        Texture2D source,
        RenderTarget2D target,
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass)
    {
        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.Patchwork,
                out PrismKernel kernel));
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        float packedPass = (int)pass.Kind + (pass.Iteration * 4);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / target.Width, 1f / target.Height),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Patchwork,
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
            FilterTextureSize = new Vector2(source.Width, source.Height)
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

    private static PrismCatalogFilterPlan CreatePlan(float relief) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Patchwork,
            [
                Number(0, relief),
                Integer(1, 0x12345678),
                Number(2, 4)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 12, 8));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Integer(int slot, int value) =>
        new(slot, PrismGraphParameterValueKind.Integer, integerValue: value);

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
                float alpha = 0.25f + (0.7f * y / (height - 1f));
                float red = 0.1f + (0.75f * x / (width - 1f));
                float green = 0.15f + (0.7f * y / (height - 1f));
                float blue = 0.2f + (0.5f * (x + y) / (width + height - 2f));
                pixels[(y * width) + x] = new HalfVector4(
                    red * alpha,
                    green * alpha,
                    blue * alpha,
                    alpha);
            }
        }
        pixels[0] = default;
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

    private static PrismPremultipliedColor ToPrismColor(HalfVector4 value)
    {
        Vector4 color = value.ToVector4();
        return new PrismPremultipliedColor(
            color.X,
            color.Y,
            color.Z,
            color.W);
    }

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);

    private static void AssertClose(
        int index,
        string channel,
        float actual,
        float expected)
    {
        const float tolerance = 0.006f;
        Assert.True(
            Math.Abs(actual - expected) <= tolerance,
            $"Pixel {index} {channel}: GPU {actual}, CPU {expected}.");
    }
}
