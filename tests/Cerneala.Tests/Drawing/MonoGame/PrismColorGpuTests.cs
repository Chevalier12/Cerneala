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
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using NumericsVector4 = System.Numerics.Vector4;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismColorGpuTests
{
    [Fact]
    public void ScRgbGpuKernelsPreservePremultipliedExtendedRange()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const float alpha = 0.5f;
        Vector4 expected = new(-0.25f * alpha, 2f * alpha, 7f * alpha, alpha);
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            1,
            1,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData([new HalfVector4(expected)]);
        using RenderTarget2D target = new(
            graphicsDevice,
            1,
            1,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
        PrismKernelParameters parameters = new(
            source,
            1,
            Vector2.One,
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismColorProfile.ScRgb,
                (int)PrismColorProfile.ScRgb,
                0,
                0)
        };

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        registry.Bind(registry.BackdropColorConversion, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, 1, 1),
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] actual = new HalfVector4[1];
        target.GetData(actual);
        Vector4 pixel = actual[0].ToVector4();
        Assert.InRange(Math.Abs(pixel.X - expected.X), 0, 0.003f);
        Assert.InRange(Math.Abs(pixel.Y - expected.Y), 0, 0.003f);
        Assert.InRange(Math.Abs(pixel.Z - expected.Z), 0, 0.008f);
        Assert.InRange(Math.Abs(pixel.W - expected.W), 0, 0.003f);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GpuMatchesCpuForOklabCat16GradeAndClampMode(bool clamp)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const float alpha = 0.55f;
        PrismCatalogFilterPlan plan = CreatePlan(clamp);
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);

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
                0.72f * alpha,
                0.24f * alpha,
                0.08f * alpha,
                alpha)
        ]);

        PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
            PrismFilterId.Color,
            pass.Iteration);
        PrismKernelParameters parameters = new(
            sourceTexture,
            1,
            Vector2.One,
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Color,
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
                [PrismPremultipliedColor.FromStraight(0.72, 0.24, 0.08, alpha)],
                1,
                1,
                PrismColorProfile.LinearSrgb));

        Assert.InRange(Math.Abs(gpu.X - cpu.Red), 0, 0.006f);
        Assert.InRange(Math.Abs(gpu.Y - cpu.Green), 0, 0.006f);
        Assert.InRange(Math.Abs(gpu.Z - cpu.Blue), 0, 0.006f);
        Assert.InRange(Math.Abs(gpu.W - cpu.Alpha), 0, 0.003f);
    }

    private static PrismCatalogFilterPlan CreatePlan(bool clamp) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Color,
            [
                Number(0, 0.08f),
                Boolean(1, clamp),
                Number(2, 1.2f),
                Number(3, 0.35f),
                Number(4, 41),
                Symbol(5, "Matrix", "Identity"),
                Number(6, 1.7f),
                Number(7, 0.3f),
                ColorParameter(8, new Cerneala.Drawing.Color(24, 190, 220, 96))
            ],
            PrismBlendMode.Normal,
            1,
            NumericsMatrix3x2.Identity,
            new DrawRect(0, 0, 1, 1));

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter Boolean(int slot, bool value) =>
        new(slot, PrismGraphParameterValueKind.Boolean, booleanValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Color,
            colorValue: value);

    private static PrismGraphParameter Symbol(
        int slot,
        string property,
        string value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Symbol,
            integerValue: PrismCatalogRuntime.ResolveSymbol(property, value));

    private static Vector4 ToXna(NumericsVector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
