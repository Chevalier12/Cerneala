using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame.Prism.Execution;
using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.ColorManagement;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.Drawing.Prism.Graph;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismSpatterPointTextureCacheTests
{
    [Fact]
    public void CacheUploadsTheRecursiveWangPointFieldOnce()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        using PrismSpatterPointTextureCache cache =
            new(fixture.Session.GraphicsDevice);

        Texture2D first = cache.GetOrCreate();
        Texture2D second = cache.GetOrCreate();

        Assert.Same(first, second);
        Assert.Equal(
            PrismRecursiveWangBlueNoise.GridSize *
                PrismRecursiveWangBlueNoise.LayerCount,
            first.Width);
        Assert.Equal(PrismRecursiveWangBlueNoise.GridSize, first.Height);
        Assert.Equal(SurfaceFormat.HalfVector4, first.Format);
    }

    [Fact]
    public void SpatterGpuTracksTheCpuRecursiveWangEvaluation()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 32;
        const int seed = 1_234_567_890;
        const float tone = 0.2f;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using PrismSpatterPointTextureCache cache =
            new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        source.SetData(
            Enumerable.Repeat(
                    new HalfVector4(new Vector4(tone, tone, tone, 1)),
                    size * size)
                .ToArray());
        using RenderTarget2D target = new(
            graphicsDevice,
            size,
            size,
            mipMap: false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);

        PrismCatalogFilterPlan plan = PrismCatalogFilterPlanner.Create(
            PrismFilterId.Spatter,
            [
                new PrismGraphParameter(
                    0,
                    PrismGraphParameterValueKind.Integer,
                    integerValue: seed),
                new PrismGraphParameter(
                    1,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 5),
                new PrismGraphParameter(
                    2,
                    PrismGraphParameterValueKind.Number,
                    numberValue: 8)
            ],
            PrismBlendMode.Normal,
            1,
            NumericsMatrix3x2.Identity,
            new DrawRect(0, 0, size, size));
        Texture2D pointTexture = cache.GetOrCreate();
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / size, 1f / size),
            Vector2.One,
            Vector2.Zero)
        {
            FilterHeader = new Vector4(
                (int)PrismFilterId.Spatter,
                (int)PrismColorProfile.LinearSrgb,
                (int)PrismCatalogFilterPrimitive.Artistic,
                0),
            FilterOptions0 = new Vector4(seed, 0, 0, 0),
            FilterOptions1 = new Vector4(5, 0, 0, 0),
            FilterOptions2 = new Vector4(8, 0, 0, 0),
            FilterOptions3 = new Vector4(
                unchecked((uint)seed) & 0xffffu,
                unchecked((uint)seed) >> 16,
                0,
                0),
            FilterOptions9 = new Vector4(
                0,
                0,
                0,
                (int)PrismBlendMode.Normal),
            FilterAuxiliaryTexture = pointTexture
        };

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(XnaColor.Transparent);
        registry.Bind(registry.SpatterFilter, in parameters);
        spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.Opaque,
            SamplerState.LinearClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            registry.Effect);
        spriteBatch.Draw(
            source,
            new Rectangle(0, 0, size, size),
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        PrismPremultipliedColor[] cpuSource = Enumerable
            .Repeat(
                PrismPremultipliedColor.FromStraight(
                    tone,
                    tone,
                    tone,
                    1),
                size * size)
            .ToArray();
        PrismPremultipliedColor[] expected =
            PrismCatalogFilterMath.Apply(
                plan,
                cpuSource,
                size,
                size,
                PrismColorProfile.LinearSrgb);
        HalfVector4[] actual = new HalfVector4[size * size];
        target.GetData(actual);
        double totalDifference = 0;
        double maximumDifference = 0;
        for (int index = 0; index < actual.Length; index++)
        {
            Vector4 pixel = actual[index].ToVector4();
            double difference = Math.Abs(pixel.X - expected[index].Red);
            totalDifference += difference;
            maximumDifference = Math.Max(maximumDifference, difference);
            Assert.InRange(pixel.W, 0.999f, 1);
        }

        double meanDifference = totalDifference / actual.Length;
        Assert.True(
            meanDifference < 0.06,
            $"Mean GPU/CPU difference was {meanDifference}; " +
            $"maximum was {maximumDifference}.");
    }
}
