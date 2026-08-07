using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Runtime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismTexturizerGpuTests
{
    [Fact]
    public void RegistryRoutesTexturizerToDedicatedShaderTechnique()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

        Assert.True(registry.TryGetFilterKernel(
            PrismFilterId.Texturizer,
            out PrismKernel kernel));
        Assert.Equal("TexturizerFilter", kernel.Technique.Name);
    }

    [Fact]
    public void ScharrHeightLightingMatchesCpuAndPreservesAssociatedAlpha()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 36;
        const int height = 28;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = CreateSubject(graphicsDevice, width, height);
        using RenderTarget2D target = CreateTarget(graphicsDevice, width, height);
        PrismCatalogFilterPlan plan = CreatePlan();
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

        DrawPass(graphicsDevice, spriteBatch, registry, source, target, plan, pass);
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

        Assert.Equal("TexturizerFilter", registry.Effect.CurrentTechnique.Name);
        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index].ToVector4();
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.04f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.04f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.04f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.012f);
            Assert.InRange(gpu.X, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Y, 0, gpu.W + 0.012f);
            Assert.InRange(gpu.Z, 0, gpu.W + 0.012f);
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
        Assert.True(registry.TryGetFilterKernel(
            PrismFilterId.Texturizer,
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
                (int)PrismFilterId.Texturizer,
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

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Texturizer,
            [
                new(0, PrismGraphParameterValueKind.Boolean, booleanValue: false),
                Symbol(1, "LightDirection", "TopRight"),
                Number(2, 0.24f),
                Number(3, 1.35f),
                Symbol(4, "Texture", "Burlap"),
                new(5, PrismGraphParameterValueKind.Resource)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 36, 28));

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
                float alpha = 0.3f + (0.65f * y / (height - 1f));
                pixels[(y * width) + x] = new HalfVector4(
                    (0.35f + (0.3f * x / (width - 1f))) * alpha,
                    (0.3f + (0.2f * y / (height - 1f))) * alpha,
                    0.28f * alpha,
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
        return new PrismPremultipliedColor(color.X, color.Y, color.Z, color.W);
    }

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
