using Cerneala.Drawing.Prism.Filters;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGradientDitherTexture : IDisposable
{
    private const int Size = 16;

    public PrismGradientDitherTexture(GraphicsDevice graphicsDevice)
    {
        Texture = new Texture2D(
            graphicsDevice,
            Size,
            Size,
            false,
            SurfaceFormat.Color);
        Microsoft.Xna.Framework.Color[] pixels =
            new Microsoft.Xna.Framework.Color[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                byte rank = (byte)PrismIncrementalVoronoiSet.Rank(
                    x,
                    y,
                    0);
                pixels[(y * Size) + x] = new Microsoft.Xna.Framework.Color(
                    rank,
                    rank,
                    rank,
                    byte.MaxValue);
            }
        }
        Texture.SetData(pixels);
    }

    public Texture2D Texture { get; }

    public void Dispose() => Texture.Dispose();
}
