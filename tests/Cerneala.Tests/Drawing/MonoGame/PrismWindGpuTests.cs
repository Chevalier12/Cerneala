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

public sealed class PrismWindGpuTests
{
    [Fact]
    public void RegistryRoutesWindToDedicatedShaderTechnique()
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
                PrismFilterId.Wind,
                out PrismKernel kernel));
        Assert.Equal("WindFilter", kernel.Technique.Name);
    }

    [Fact]
    public void ShaderPackageContainsWindLicTechnique()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using PrismKernelRegistry registry =
            new(fixture.Session.GraphicsDevice);

        EffectTechnique? technique = registry.Effect.Techniques["WindFilter"];

        Assert.NotNull(technique);
        Assert.Single(technique.Passes);
    }

    [Fact]
    public void LicPipelineChangesImageAndHonorsDirection()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int width = 41;
        const int height = 23;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D original = CreateSubject(graphicsDevice, width, height);
        using RenderTarget2D rightFirst = CreateTarget(graphicsDevice, width, height);
        using RenderTarget2D rightSecond = CreateTarget(graphicsDevice, width, height);
        using RenderTarget2D leftFirst = CreateTarget(graphicsDevice, width, height);
        using RenderTarget2D leftSecond = CreateTarget(graphicsDevice, width, height);
        PrismCatalogFilterPlan rightPlan =
            CreatePlan("FromRight", width, height);
        PrismCatalogFilterPlan leftPlan =
            CreatePlan("FromLeft", width, height);

        Texture2D fromRight = Render(
            graphicsDevice,
            spriteBatch,
            registry,
            original,
            rightFirst,
            rightSecond,
            rightPlan);
        Texture2D fromLeft = Render(
            graphicsDevice,
            spriteBatch,
            registry,
            original,
            leftFirst,
            leftSecond,
            leftPlan);

        graphicsDevice.SetRenderTarget(null);
        HalfVector4[] sourcePixels = new HalfVector4[width * height];
        HalfVector4[] rightPixels = new HalfVector4[width * height];
        HalfVector4[] leftPixels = new HalfVector4[width * height];
        original.GetData(sourcePixels);
        fromRight.GetData(rightPixels);
        fromLeft.GetData(leftPixels);

        double sourceDifference = 0;
        double directionDifference = 0;
        for (int index = 0; index < rightPixels.Length; index++)
        {
            Vector4 source = sourcePixels[index].ToVector4();
            Vector4 right = rightPixels[index].ToVector4();
            Vector4 left = leftPixels[index].ToVector4();
            sourceDifference += ColorDifference(source, right);
            directionDifference += ColorDifference(right, left);
            Assert.InRange(Math.Abs(source.W - right.W), 0, 0.015f);
            AssertAssociated(right);
            AssertAssociated(left);
        }

        double meanSourceDifference = sourceDifference / rightPixels.Length;
        double meanDirectionDifference =
            directionDifference / rightPixels.Length;
        Assert.True(
            meanSourceDifference > 0.015,
            $"Mean source difference was {meanSourceDifference}.");
        Assert.True(
            meanDirectionDifference > 0.0001,
            $"Mean direction difference was {meanDirectionDifference}.");
        Assert.Equal("WindFilter", registry.Effect.CurrentTechnique.Name);
    }

    private static Texture2D Render(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        PrismKernelRegistry registry,
        Texture2D original,
        RenderTarget2D first,
        RenderTarget2D second,
        PrismCatalogFilterPlan plan)
    {
        Texture2D current = original;
        RenderTarget2D target = first;
        foreach (PrismCatalogFilterPass pass in plan.Passes)
        {
            PrismKernel kernel = registry.ResolveCatalogFilterPassKernel(
                PrismFilterId.Wind,
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
                pass);
            current = target;
            target = ReferenceEquals(target, first) ? second : first;
        }
        return current;
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
            SourceTexture = source,
            FilterHeader = new Vector4(
                (int)PrismFilterId.Wind,
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

    private static PrismCatalogFilterPlan CreatePlan(
        string direction,
        int width,
        int height) =>
        PrismCatalogFilterPlanner.Create(
            PrismFilterId.Wind,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "Direction",
                        direction)),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Symbol,
                    integerValue: PrismCatalogRuntime.ResolveSymbol(
                        "Method",
                        "Wind")),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: 17),
                new PrismGraphParameter(
                    3,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 4)
            ],
            PrismBlendMode.Normal,
            1,
            System.Numerics.Matrix3x2.Identity,
            new DrawRect(0, 0, width, height));

    private static Texture2D CreateSubject(
        GraphicsDevice graphicsDevice,
        int width,
        int height)
    {
        HalfVector4[] pixels = new HalfVector4[width * height];
        int center = width / 2;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float alpha = 0.55f + (0.4f * x / (width - 1f));
                float noise = ((x * 7 + y * 13) & 7) / 100f;
                float red = x == center
                    ? 0.92f
                    : x == center + 1
                        ? 0.48f
                        : 0.03f + (0.18f * x / (width - 1f));
                float green = x < center
                    ? 0.08f + noise
                    : 0.3f + noise;
                float blue = x == center ? 0.12f : 0.04f + noise;
                pixels[(y * width) + x] = new HalfVector4(
                    red * alpha,
                    green * alpha,
                    blue * alpha,
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

    private static double ColorDifference(Vector4 left, Vector4 right) =>
        Math.Abs(left.X - right.X) +
        Math.Abs(left.Y - right.Y) +
        Math.Abs(left.Z - right.Z);

    private static void AssertAssociated(Vector4 color)
    {
        Assert.True(float.IsFinite(color.X));
        Assert.True(float.IsFinite(color.Y));
        Assert.True(float.IsFinite(color.Z));
        Assert.True(float.IsFinite(color.W));
        Assert.InRange(color.W, 0, 1.001f);
        Assert.InRange(color.X, 0, color.W + 0.015f);
        Assert.InRange(color.Y, 0, color.W + 0.015f);
        Assert.InRange(color.Z, 0, color.W + 0.015f);
    }

    private static Vector4 ToXna(System.Numerics.Vector4 value) =>
        new(value.X, value.Y, value.Z, value.W);
}
