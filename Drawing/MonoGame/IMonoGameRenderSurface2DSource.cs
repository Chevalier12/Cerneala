using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

internal interface IMonoGameRenderSurface2DSource : IRenderSurface2DSource
{
    Texture2D? ResolveSurface(GraphicsDevice graphicsDevice, int pixelWidth, int pixelHeight);
}
