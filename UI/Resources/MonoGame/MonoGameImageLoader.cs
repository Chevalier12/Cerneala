using Cerneala.Drawing;
using Cerneala.Drawing.MonoGame;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.UI.Resources.MonoGame;

public sealed class MonoGameImageLoader : IImageLoader
{
    private readonly GraphicsDevice graphicsDevice;

    public MonoGameImageLoader(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
    }

    public IDrawImage Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Image path cannot be empty.", nameof(path));
        }

        using FileStream stream = File.OpenRead(path);
        return new MonoGameImage(Texture2D.FromStream(graphicsDevice, stream));
    }
}
