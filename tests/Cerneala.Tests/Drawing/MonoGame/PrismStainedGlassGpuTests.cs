using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Tests.Drawing.Prism;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismStainedGlassGpuTests
{
    [Fact]
    public void RegistryRoutesStainedGlassToDedicatedShaderTechnique()
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
                PrismFilterId.StainedGlass,
                out PrismKernel kernel));
        Assert.Equal("StainedGlassFilter", kernel.Technique.Name);
    }

    [Fact]
    public void JumpFloodPipelineMatchesCpuFallback()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 32;
        const int height = 24;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        PrismPremultipliedColor[] sourceColors =
            PrismStainedGlassFilterTests.CreateSubject(width, height);
        using Texture2D source = CreateTexture(
            graphicsDevice,
            sourceColors,
            width,
            height);
        using RenderTarget2D first = CreateTarget(
            graphicsDevice,
            width,
            height);
        using RenderTarget2D second = CreateTarget(
            graphicsDevice,
            width,
            height);
        PrismCatalogFilterPlan plan =
            PrismStainedGlassFilterTests.CreatePlan(
                8,
                0,
                0,
                Cerneala.Drawing.Color.Black,
                1937,
                width,
                height);

        Texture2D current = source;
        RenderTarget2D? final = null;
        foreach (PrismCatalogFilterPass pass in plan.Passes)
        {
            RenderTarget2D target = ReferenceEquals(current, first)
                ? second
                : first;
            DrawPass(
                graphicsDevice,
                spriteBatch,
                registry,
                current,
                source,
                target,
                plan,
                pass);
            current = target;
            final = target;
        }
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] gpuPixels = new HalfVector4[width * height];
        final!.GetData(gpuPixels);
        PrismPremultipliedColor[] cpuPixels =
            PrismCatalogFilterMath.Apply(
                plan,
                sourceColors,
                width,
                height,
                PrismColorProfile.LinearSrgb);

        Assert.Equal(
            "StainedGlassFilter",
            registry.Effect.CurrentTechnique.Name);
        for (int index = 0; index < gpuPixels.Length; index++)
        {
            Vector4 gpu = gpuPixels[index].ToVector4();
            PrismPremultipliedColor cpu = cpuPixels[index];
            Assert.InRange(
                Math.Abs(gpu.X - (float)cpu.Red),
                0,
                0.035f);
            Assert.InRange(
                Math.Abs(gpu.Y - (float)cpu.Green),
                0,
                0.035f);
            Assert.InRange(
                Math.Abs(gpu.Z - (float)cpu.Blue),
                0,
                0.035f);
            Assert.InRange(
                Math.Abs(gpu.W - (float)cpu.Alpha),
                0,
                0.012f);
        }
    }

    private static void DrawPass(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        Texture2D input,
        Texture2D original,
        RenderTarget2D target,
        PrismCatalogFilterPlan plan,
        PrismCatalogFilterPass pass)
    {
        Assert.True(
            registry.TryGetFilterKernel(
                PrismFilterId.StainedGlass,
                out PrismKernel kernel));
        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(Microsoft.Xna.Framework.Color.Transparent);
        float packedPass = (int)pass.Kind + (pass.Iteration * 4);
        PrismKernelParameters parameters = new(
            input,
            1,
            new Vector2(1f / target.Width, 1f / target.Height),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.StainedGlass,
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
            FilterTextureSize = new Vector2(input.Width, input.Height),
            FilterAuxiliaryTexture = original
        };
        registry.Bind(kernel, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            input,
            new Rectangle(0, 0, target.Width, target.Height),
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
    }

    private static Texture2D CreateTexture(
        GraphicsDevice graphicsDevice,
        PrismPremultipliedColor[] colors,
        int width,
        int height)
    {
        Texture2D texture = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(colors.Select(color => new HalfVector4(
            (float)color.Red,
            (float)color.Green,
            (float)color.Blue,
            (float)color.Alpha)).ToArray());
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
