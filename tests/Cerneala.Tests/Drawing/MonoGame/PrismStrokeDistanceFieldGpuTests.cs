using Cerneala.Drawing.MonoGame.Prism.Kernels;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace Cerneala.Tests.Drawing.MonoGame;

public sealed class PrismStrokeDistanceFieldGpuTests
{
    [Fact]
    public void SeedPassUsesDirectionalCoverageForDiagonalEdge()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        const int size = 5;
        const float strokeShaderKind = 9f;
        const int center = size / 2;
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
        using RenderTarget2D target = new(
            graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.Vector4,
            DepthFormat.None,
            preferredMultiSampleCount: 0,
            RenderTargetUsage.PreserveContents);

        HalfVector4[] sourcePixels = new HalfVector4[size * size];
        sourcePixels[(center * size) + center] = Alpha(0.25f);
        sourcePixels[(center * size) + center + 1] = Alpha(1f);
        sourcePixels[((center + 1) * size) + center] = Alpha(1f);
        source.SetData(sourcePixels);

        graphicsDevice.SetRenderTarget(target);
        graphicsDevice.Clear(XnaColor.Transparent);
        PrismKernelParameters parameters = new(
            source,
            1,
            new Vector2(1f / size, 1f / size),
            Vector2.One,
            Vector2.Zero)
        {
            StyleModes0 = new Vector4(
                strokeShaderKind,
                0,
                0,
                0)
        };
        registry.Bind(registry.StrokeDistanceSeed, in parameters);
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
            XnaColor.White);
        spriteBatch.End();
        graphicsDevice.SetRenderTarget(null);

        Vector4[] seeds = new Vector4[size * size];
        target.GetData(seeds);
        Vector4 seed = seeds[(center * size) + center];
        float expectedCoordinate =
            ((center + 0.5f) + 0.1464466f) / size;

        Assert.InRange(seed.X, expectedCoordinate - 0.001f, expectedCoordinate + 0.001f);
        Assert.InRange(seed.Y, expectedCoordinate - 0.001f, expectedCoordinate + 0.001f);
        Assert.Equal(1f, seed.W);
    }

    private static HalfVector4 Alpha(float alpha) =>
        new(new Vector4(0, 0, 0, alpha));
}
