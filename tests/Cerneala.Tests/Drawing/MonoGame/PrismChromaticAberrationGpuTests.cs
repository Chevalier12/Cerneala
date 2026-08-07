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

public sealed class PrismChromaticAberrationGpuTests
{
    [Fact]
    public void GpuMatchesRadialLinearCpuFallback()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 8;
        const int height = 6;
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
            PrismFilterId.ChromaticAberration,
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
                (int)PrismFilterId.ChromaticAberration,
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
            Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.004);
            Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.004);
            Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.004);
            Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.004);
        }
    }

    private static HalfVector4[] CreateSource(int width, int height)
    {
        HalfVector4[] pixels = new HalfVector4[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.35f + (0.05f * x) + (0.025f * y);
                pixels[(y * width) + x] = new HalfVector4(
                    (0.05f + (0.08f * x)) * alpha,
                    (0.1f + (0.1f * y)) * alpha,
                    (0.85f - (0.07f * x)) * alpha,
                    alpha);
            }
        }
        return pixels;
    }

    private static PrismCatalogFilterPlan CreatePlan(
        int width,
        int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ChromaticAberration,
            [
                Number(0, 1.25f),
                VectorParameter(1, new System.Numerics.Vector2(0.4f, 0.55f)),
                VectorParameter(2, new System.Numerics.Vector2(0.8f, 0.6f)),
                Boolean(3, true),
                Symbol(4, "Linear")
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

    private static PrismGraphParameter VectorParameter(
        int slot,
        System.Numerics.Vector2 value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Vector,
            vectorValue: new System.Numerics.Vector4(value, 0, 0));

    private static PrismGraphParameter Boolean(int slot, bool value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Boolean,
            booleanValue: value);

    private static PrismGraphParameter Symbol(int slot, string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(
                "Sampling",
                value));

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
