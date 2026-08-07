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

public sealed class PrismEmbossGpuTests
{
    [Fact]
    public void GpuReliefMatchesAnalyticCpuAtCenterAndPreservesAlpha()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 5;
        const int height = 5;
        const float alpha = 0.6f;
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

        HalfVector4[] sourcePixels = Enumerable.Repeat(
            new HalfVector4(0, 0, 0, alpha),
            width * height)
            .ToArray();
        sourcePixels[(1 * width) + 3] =
            new HalfVector4(alpha, alpha, alpha, alpha);
        sourceTexture.SetData(sourcePixels);

        PrismCatalogFilterPlan plan = CreatePlan();
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
            PrismFilterId.Emboss,
            pass.Iteration);
        PrismKernelParameters parameters = new(
            sourceTexture,
            1,
            new Vector2(1f / width, 1f / height),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Emboss,
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
            FilterTextureSize = new Vector2(width, height)
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
        Vector4 gpu = gpuPixels[(2 * width) + 2].ToVector4();
        PrismPremultipliedColor[] cpuSource = sourcePixels
            .Select(pixel => pixel.ToVector4())
            .Select(pixel => new PrismPremultipliedColor(
                pixel.X,
                pixel.Y,
                pixel.Z,
                pixel.W))
            .ToArray();
        PrismPremultipliedColor cpu = PrismCatalogFilterMath.Apply(
            plan,
            cpuSource,
            width,
            height,
            PrismColorProfile.LinearSrgb)[(2 * width) + 2];

        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.W - alpha), 0, 0.002f);
        Assert.InRange(
            Math.Abs(gpu.X - (alpha * (0.5f + (3f / 16)))),
            0,
            0.002f);
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Emboss,
            [
                Number(0, 1),
                Number(1, 0),
                Number(2, 1)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 5, 5));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
