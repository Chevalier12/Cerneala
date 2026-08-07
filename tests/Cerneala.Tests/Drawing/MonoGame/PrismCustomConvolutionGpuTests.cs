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

public sealed class PrismCustomConvolutionGpuTests
{
    [Fact]
    public void GpuMatchesCpuForNegativeUnnormalizedKernelAndWrapEdges()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 3;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = CreateSource(graphicsDevice);
        using Texture2D kernelTexture = CreateKernel(graphicsDevice);
        using RenderTarget2D target = new(
            graphicsDevice,
            width,
            1,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);
        PrismCatalogFilterPlan plan = CreatePlan();
        PrismCatalogFilterPass pass = Assert.Single(plan.Passes);
        PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
            PrismFilterId.CustomConvolution,
            pass.Iteration);
        PrismKernelParameters parameters = new(
            kernelTexture,
            1,
            new Vector2(1f / width, 1),
            Vector2.One,
            Vector2.Zero)
        {
            SourceTexture = source,
            FilterHeader = new Vector4(
                (int)PrismFilterId.CustomConvolution,
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
            FilterTextureSize = new Vector2(width, 1)
        };

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        registry.Bind(kernel, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, width, 1),
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] gpuPixels = new HalfVector4[width];
        target.GetData(gpuPixels);
        PrismPremultipliedColor[] cpuPixels =
            PrismCatalogFilterMath.Apply(
                plan,
                SourcePixels(),
                width,
                1,
                PrismColorProfile.LinearSrgb,
                primaryResource: DifferenceKernel);

        Assert.Equal("CatalogFilter", kernel.Technique.Name);
        for (int index = 0; index < width; index++)
        {
            Vector4 gpu = gpuPixels[index].ToVector4();
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(Math.Abs(gpu.X - (float)cpu.Red), 0, 0.003f);
            Assert.InRange(Math.Abs(gpu.Y - (float)cpu.Green), 0, 0.003f);
            Assert.InRange(Math.Abs(gpu.Z - (float)cpu.Blue), 0, 0.003f);
            Assert.InRange(Math.Abs(gpu.W - (float)cpu.Alpha), 0, 0.003f);
        }
    }

    private static PrismCatalogFilterPlan CreatePlan() =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.CustomConvolution,
            [
                new(
                    0,
                    PrismGraphParameterValueKind.Boolean,
                    booleanValue: false),
                new(
                    1,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "EdgeMode",
                        "Wrap")),
                new(
                    2,
                    PrismGraphParameterValueKind.Resource,
                    resourceValue: new PrismResourceId("custom-kernel")),
                Number(3, 0.01f),
                Number(4, 0.5f)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 3, 1));

    private static PrismGraphParameter Number(
        int slot,
        float value) =>
        new(
            slot,
            PrismGraphParameterValueKind.Number,
            numberValue: value);

    private static Texture2D CreateSource(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(
            graphicsDevice,
            3,
            1,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(
            SourcePixels()
                .Select(color => new HalfVector4(
                    (float)color.Red,
                    (float)color.Green,
                    (float)color.Blue,
                    (float)color.Alpha))
                .ToArray());
        return texture;
    }

    private static Texture2D CreateKernel(GraphicsDevice graphicsDevice)
    {
        HalfVector4[] weights = new HalfVector4[9];
        weights[3] = new HalfVector4(-1, 0, 0, 0);
        weights[4] = new HalfVector4(3, 0, 0, 0);
        Texture2D texture = new(
            graphicsDevice,
            3,
            3,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(weights);
        return texture;
    }

    private static PrismPremultipliedColor[] SourcePixels() =>
    [
        PrismPremultipliedColor.FromStraight(0.05, 0.02, 0.01, 0.5),
        PrismPremultipliedColor.FromStraight(0.1, 0.04, 0.02, 0.5),
        PrismPremultipliedColor.FromStraight(0.15, 0.06, 0.03, 0.5)
    ];

    private static System.Numerics.Vector4 DifferenceKernel(
        System.Numerics.Vector2 uv)
    {
        int x = Math.Clamp((int)(uv.X * 3), 0, 2);
        int y = Math.Clamp((int)(uv.Y * 3), 0, 2);
        float weight = (x, y) switch
        {
            (0, 1) => -1,
            (1, 1) => 3,
            _ => 0
        };
        return new System.Numerics.Vector4(weight, 0, 0, 0);
    }

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
