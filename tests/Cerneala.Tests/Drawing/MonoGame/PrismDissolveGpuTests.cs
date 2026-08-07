using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Cerneala.Drawing.Prism.Blending;
using Cerneala.Drawing.Prism.Catalog;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismDissolveGpuTests
{
    [Fact]
    public void GpuUsesTheSameRankMapAndSeedOffsetAsCpu()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        int size = PrismDissolveBlend.ThresholdSize;
        const int normalizedSeed = 0x1234;
        using PrismGraphExecutorTests.WindowsDxFixture fixture = new();
        GraphicsDevice graphicsDevice = fixture.Session.GraphicsDevice;
        using PrismKernelRegistry registry = new(graphicsDevice);
        using SpriteBatch spriteBatch = new(graphicsDevice);
        using Texture2D source = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        using Texture2D backdrop = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4);
        using RenderTarget2D target = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.HalfVector4,
            DepthFormat.None);

        HalfVector4[] sourcePixels = Enumerable.Repeat(
            new HalfVector4(0.375f, 0.25f, 0.125f, 0.5f),
            size * size).ToArray();
        source.SetData(sourcePixels);
        backdrop.SetData(new HalfVector4[size * size]);
        Assert.True(registry.TryGetBlendKernel(
            PrismBlendMode.Dissolve,
            out PrismKernel kernel));
        PrismKernelParameters parameters = new(
            backdrop,
            1,
            new Vector2(1f / size, 1f / size),
            Vector2.One,
            Vector2.Zero)
        {
            SourceTexture = source,
            DissolveSeed = normalizedSeed,
            BackgroundAvailable = 0
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
            new Rectangle(0, 0, size, size),
            Microsoft.Xna.Framework.Color.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        HalfVector4[] gpuPixels = new HalfVector4[size * size];
        target.GetData(gpuPixels);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float expected = PrismDissolveBlend.IsSelected(
                    x,
                    y,
                    normalizedSeed,
                    0.5)
                    ? 1
                    : 0;
                Assert.Equal(
                    expected,
                    gpuPixels[(y * size) + x].ToVector4().W);
            }
        }
    }
}
