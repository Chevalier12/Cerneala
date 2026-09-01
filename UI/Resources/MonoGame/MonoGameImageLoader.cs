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

        using FileStream stream = File.OpenRead(ResolvePath(path));
        return Load(stream);
    }

    public IDrawImage Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanRead)
        {
            throw new ArgumentException("Image stream must be readable.", nameof(stream));
        }

        return new MonoGameImage(Texture2D.FromStream(
            graphicsDevice,
            stream,
            PremultiplyRgba));
    }

    private static void PremultiplyRgba(byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if ((pixels.Length & 3) != 0)
        {
            throw new InvalidDataException("Decoded RGBA pixels were not four-byte aligned.");
        }

        for (int offset = 0; offset < pixels.Length; offset += 4)
        {
            byte alpha = pixels[offset + 3];
            pixels[offset] = Premultiply(pixels[offset], alpha);
            pixels[offset + 1] = Premultiply(pixels[offset + 1], alpha);
            pixels[offset + 2] = Premultiply(pixels[offset + 2], alpha);
        }
    }

    private static byte Premultiply(byte channel, byte alpha) =>
        checked((byte)(((channel * alpha) + 127) / 255));

    internal static string ResolvePath(string path)
    {
        string workingDirectoryPath = Path.GetFullPath(path);
        return Path.IsPathFullyQualified(path) || File.Exists(workingDirectoryPath)
            ? workingDirectoryPath
            : Path.GetFullPath(path, AppContext.BaseDirectory);
    }
}
