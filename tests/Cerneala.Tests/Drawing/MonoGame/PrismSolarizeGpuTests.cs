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

public sealed class PrismSolarizeGpuTests
{
    [Fact]
    public void GpuMatchesClassicPerChannelHardThresholdAndCpuFallback()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const float alpha = 0.5f;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D sourceTexture = new(
            graphicsDevice,
            1,
            1,
            false,
            SurfaceFormat.HalfVector4);
        using RenderTarget2D target = new(
            graphicsDevice,
            1,
            1,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);

        sourceTexture.SetData(
        [
            new HalfVector4(
                0.125f * alpha,
                0.25f * alpha,
                0.75f * alpha,
                alpha)
        ]);
        PrismCatalogFilterPlan plan = CreatePlan(0.25f);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
            PrismFilterId.Solarize,
            pass.Iteration);
        PrismKernelParameters parameters = new(
            sourceTexture,
            1,
            Vector2.One,
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Solarize,
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
            FilterTextureSize = Vector2.One
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
            new Rectangle(0, 0, 1, 1),
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] gpuPixels = new HalfVector4[1];
        target.GetData(gpuPixels);
        Vector4 gpu = gpuPixels[0].ToVector4();
        PrismPremultipliedColor cpu = Assert.Single(
            PrismCatalogFilterMath.Apply(
                plan,
                [
                    PrismPremultipliedColor.FromStraight(
                        0.125,
                        0.25,
                        0.75,
                        alpha)
                ],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.InRange(Math.Abs(gpu.X - 0.0625f), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Y - 0.375f), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Z - 0.125f), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.W - alpha), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.002f);
        Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.002f);
    }

    private static PrismCatalogFilterPlan CreatePlan(float threshold) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Solarize,
            [Number(0, threshold)],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
