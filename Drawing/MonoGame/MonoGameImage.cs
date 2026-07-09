using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame;

public sealed class MonoGameImage : IDrawImage, IDisposable
{
    public MonoGameImage(Texture2D texture)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
    }

    public Texture2D Texture { get; }

    public int Width => Texture.Width;

    public int Height => Texture.Height;

    public void Dispose()
    {
        Texture.Dispose();
    }
}
