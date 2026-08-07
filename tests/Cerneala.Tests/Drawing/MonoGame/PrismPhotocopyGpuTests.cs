using System.Collections.Immutable;
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

public sealed class PrismPhotocopyGpuTests
{
    [Theory]
    [InlineData(PrismFilterId.Photocopy)]
    [InlineData(PrismFilterId.Stamp)]
    [InlineData(PrismFilterId.TornEdges)]
    public void RegistryRoutesXDogFiltersToDedicatedShaderTechnique(
        PrismFilterId filter)
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
                filter,
                out PrismKernel kernel));
        Assert.Equal("PhotocopyFilter", kernel.Technique.Name);
    }

    [Theory]
    [InlineData(PrismFilterId.Photocopy)]
    [InlineData(PrismFilterId.Stamp)]
    [InlineData(PrismFilterId.TornEdges)]
    public void XDogPipelineRendersDarkMassesAndPreservesAssociatedAlpha(
        PrismFilterId filter)
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 32;
        const int height = 20;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D original = CreateSubject(graphicsDevice, width, height);
        using RenderTarget2D first = CreateTarget(graphicsDevice, width, height);
        using RenderTarget2D second = CreateTarget(graphicsDevice, width, height);
        PrismCatalogFilterPlan plan = CreatePlan(filter);

        Texture2D current = original;
        RenderTarget2D target = first;
        foreach (PrismCatalogFilterPass pass in plan.Passes)
        {
            PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
                filter,
                pass.Iteration);
            DrawPass(
                graphicsDevice,
                spriteBatch,
                registry,
                kernel,
                current,
                original,
                target,
                plan,
                filter,
                pass);
            current = target;
            target = ReferenceEquals(target, first) ? second : first;
        }

        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] sourcePixels = new HalfVector4[width * height];
        HalfVector4[] resultPixels = new HalfVector4[width * height];
        original.GetData(sourcePixels);
        current.GetData(resultPixels);

        Assert.Equal("PhotocopyFilter", registry.Effect.CurrentTechnique.Name);
        double difference = 0;
        for (int index = 0; index < resultPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index].ToVector4();
            Vector4 result = resultPixels[index].ToVector4();
            difference +=
                Math.Abs(source.X - result.X) +
                Math.Abs(source.Y - result.Y) +
                Math.Abs(source.Z - result.Z);
            Assert.InRange(Math.Abs(source.W - result.W), 0, 0.012f);
            Assert.InRange(result.X, 0, result.W + 0.012f);
            Assert.InRange(result.Y, 0, result.W + 0.012f);
            Assert.InRange(result.Z, 0, result.W + 0.012f);
        }
        Assert.True(difference / resultPixels.Length > 0.02);

        Vector4 dark = resultPixels[(height / 2 * width) + 4].ToVector4();
        Vector4 paper = resultPixels[(height / 2 * width) + width - 5].ToVector4();
        Assert.True(dark.X / dark.W < 0.3f);
        Assert.True(paper.X / paper.W > 0.8f);
    }

    private static void DrawPass(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        PrismKernel kernel,
        Texture2D source,
        Texture2D original,
        RenderTarget2D target,
        PrismCatalogFilterPlan plan,
        PrismFilterId filter,
        PrismCatalogFilterPass pass)
    {
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        float packedPass = (int)pass.Kind + (pass.Iteration * 4);
        PrismKernelParameters parameters = new(
            original,
            1,
            new Vector2(1f / target.Width, 1f / target.Height),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)filter,
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
            FilterTextureSize = new Vector2(original.Width, original.Height),
            FilterAuxiliaryTexture = original
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

    private static PrismCatalogFilterPlan CreatePlan(PrismFilterId filter)
    {
        ImmutableArray<PrismGraphParameter> parameters = filter switch
        {
            PrismFilterId.Photocopy =>
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                Number(1, 8),
                Number(2, 2),
                ColorParameter(3, Cerneala.Drawing.Color.Black)
            ],
            PrismFilterId.Stamp =>
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                ColorParameter(1, Cerneala.Drawing.Color.Black),
                Number(2, 25),
                Number(3, 5)
            ],
            PrismFilterId.TornEdges =>
            [
                ColorParameter(0, Cerneala.Drawing.Color.White),
                Number(1, 17),
                ColorParameter(2, Cerneala.Drawing.Color.Black),
                Number(3, 25),
                Number(4, 4)
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(filter))
        };
        return PrismCatalogFilterPlanner.Create(
            filter,
            parameters,
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, 32, 20));
    }

    private static PrismGraphParameter Number(int slot, float value) =>
        new(slot, PrismGraphParameterValueKind.Number, numberValue: value);

    private static PrismGraphParameter ColorParameter(
        int slot,
        Cerneala.Drawing.Color value) =>
        new(slot, PrismGraphParameterValueKind.Color, colorValue: value);

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
                float value = x < width / 2 ? 0.04f : 0.96f;
                float alpha = 0.42f + (0.5f * x / (width - 1f));
                pixels[(y * width) + x] = new HalfVector4(
                    value * alpha,
                    value * alpha,
                    value * alpha,
                    alpha);
            }
        }
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

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
