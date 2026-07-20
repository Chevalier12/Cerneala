using Cerneala.Drawing.Prism;
using Microsoft.Xna.Framework.Graphics;

namespace Cerneala.Drawing.MonoGame.Prism;

internal static class MonoGameBackdropFrameValidation
{
    public static bool TryValidate(
        Texture2D texture,
        GraphicsDevice graphicsDevice,
        in BackdropFrameMetadata metadata,
        out string diagnostic)
    {
        if (texture.IsDisposed)
        {
            diagnostic = "The backdrop texture is disposed.";
            return false;
        }
        if (!ReferenceEquals(texture.GraphicsDevice, graphicsDevice))
        {
            diagnostic = "The backdrop texture belongs to a different GraphicsDevice.";
            return false;
        }
        if (texture.Width != metadata.PixelWidth ||
            texture.Height != metadata.PixelHeight)
        {
            diagnostic =
                $"Backdrop metadata declares {metadata.PixelWidth}x{metadata.PixelHeight}, " +
                $"but the texture is {texture.Width}x{texture.Height}.";
            return false;
        }
        if (!TryMapPixelFormat(texture.Format, out BackdropPixelFormat pixelFormat))
        {
            diagnostic =
                $"MonoGame surface format '{texture.Format}' is not supported for Prism backdrops.";
            return false;
        }
        if (pixelFormat != metadata.PixelFormat)
        {
            diagnostic =
                $"Backdrop metadata declares pixel format '{metadata.PixelFormat}', " +
                $"but the texture uses '{texture.Format}'.";
            return false;
        }

        diagnostic = string.Empty;
        return true;
    }

    public static bool TryMapPixelFormat(
        SurfaceFormat surfaceFormat,
        out BackdropPixelFormat pixelFormat)
    {
        switch (surfaceFormat)
        {
            case SurfaceFormat.Color:
                pixelFormat = BackdropPixelFormat.Rgba8Unorm;
                return true;
            case SurfaceFormat.Bgra32:
                pixelFormat = BackdropPixelFormat.Bgra8Unorm;
                return true;
            case SurfaceFormat.HalfVector4:
                pixelFormat = BackdropPixelFormat.Rgba16Float;
                return true;
            default:
                pixelFormat = default;
                return false;
        }
    }
}
