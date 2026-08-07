using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using NumericsMatrix4x4 = System.Numerics.Matrix4x4;
using NumericsVector4 = System.Numerics.Vector4;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismColorMatrixGpuTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GpuMatchesCpuForAffineRgbaAndClampMode(bool clamp)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const float alpha = 0.5f;
        PrismColorMatrixResource matrix = new(
            new NumericsMatrix4x4(
                0, 1, 0, 0,
                1, 0, 0, 0,
                0, 0, 1, 0.1f,
                0, 0, 0, 0.5f),
            new NumericsVector4(1.1f, -0.4f, 0, 0.1f));
        PrismCatalogFilterPlan plan = CreatePlan(clamp);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismColorMatrixFilter.Pack(
            matrix,
            out NumericsVector4 rowRed,
            out NumericsVector4 rowGreen,
            out NumericsVector4 rowBlue,
            out NumericsVector4 rowAlpha,
            out NumericsVector4 offset);

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
                0.2f * alpha,
                0.4f * alpha,
                0.6f * alpha,
                alpha)
        ]);

        PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
            PrismFilterId.ColorMatrix,
            pass.Iteration);
        PrismKernelParameters parameters = new(
            sourceTexture,
            1,
            Vector2.One,
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.ColorMatrix,
                (int)PrismColorProfile.LinearSrgb,
                (int)plan.Primitive,
                1),
            FilterOptions0 = ToXna(plan.Options0),
            FilterOptions1 = ToXna(plan.Options1),
            FilterOptions2 = ToXna(rowRed),
            FilterOptions3 = ToXna(rowGreen),
            FilterOptions4 = ToXna(rowBlue),
            FilterOptions5 = ToXna(rowAlpha),
            FilterOptions6 = ToXna(offset),
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
                        0.2,
                        0.4,
                        0.6,
                        alpha)
                ],
                1,
                1,
                PrismColorProfile.LinearSrgb,
                colorMatrixResource: matrix));

        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.003f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.003f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.003f);
        Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.003f);
    }

    private static PrismCatalogFilterPlan CreatePlan(bool clamp) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.ColorMatrix,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: clamp),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: new PrismResourceId("color-matrix"))
            ],
            PrismBlendMode.Normal,
            1,
            NumericsMatrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));

    private static Vector4 ToXna(NumericsVector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
