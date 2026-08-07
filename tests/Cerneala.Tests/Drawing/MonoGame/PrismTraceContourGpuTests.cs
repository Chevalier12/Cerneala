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

public sealed class PrismTraceContourGpuTests
{
    [Fact]
    public void GpuMatchesCpuForLowerAndUpperLevelSetBoundaries()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 5;
        const int height = 3;
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

        HalfVector4[] sourcePixels = new HalfVector4[width * height];
        PrismPremultipliedColor[] cpuSource =
            new PrismPremultipliedColor[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = x < 2 ? 0.25f : 0.75f;
                int index = (y * width) + x;
                sourcePixels[index] = new HalfVector4(
                    value * alpha,
                    value * alpha,
                    value * alpha,
                    alpha);
                cpuSource[index] =
                    PrismPremultipliedColor.FromStraight(
                        value,
                        value,
                        value,
                        alpha);
            }
        }
        sourceTexture.SetData(sourcePixels);

        AssertMatches("Lower");
        AssertMatches("Upper");

        void AssertMatches(string edge)
        {
            PrismCatalogFilterPlan plan = CreatePlan(edge);
            PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
            PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
                PrismFilterId.TraceContour,
                pass.Iteration);
            PrismKernelParameters parameters = new(
                sourceTexture,
                1,
                new Vector2(1f / width, 1f / height),
                Vector2.One,
                Vector2.Zero)
            {
                FilterHeader = new Vector4(
                    (int)PrismFilterId.TraceContour,
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
            PrismPremultipliedColor[] expected =
                PrismCatalogFilterMath.Apply(
                    plan,
                    cpuSource,
                    width,
                    height,
                    PrismColorProfile.LinearSrgb);
            for (int index = 0; index < expected.Length; index++)
            {
                Vector4 actual = gpuPixels[index].ToVector4();
                Assert.InRange(
                    Math.Abs(actual.X - expected[index].Red),
                    0,
                    0.002f);
                Assert.InRange(
                    Math.Abs(actual.Y - expected[index].Green),
                    0,
                    0.002f);
                Assert.InRange(
                    Math.Abs(actual.Z - expected[index].Blue),
                    0,
                    0.002f);
                Assert.InRange(
                    Math.Abs(actual.W - expected[index].Alpha),
                    0,
                    0.002f);
            }
        }
    }

    private static PrismCatalogFilterPlan CreatePlan(string edge) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.TraceContour,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol("Edge", edge)),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 0.5f)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 5, 3));

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
