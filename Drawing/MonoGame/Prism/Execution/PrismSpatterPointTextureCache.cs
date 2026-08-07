using Cerneala.Drawing.Prism.Filters;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismSpatterPointTextureCache : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private Texture2D? texture;
    private bool disposed;

    public PrismSpatterPointTextureCache(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    public Texture2D GetOrCreate()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (texture is not null && !texture.IsDisposed)
        {
            return texture;
        }

        PrismSpatterPointField field =
            PrismRecursiveWangBlueNoise.PointField;
        HalfVector4[] pixels = new HalfVector4[
            field.PackedPoints.Length];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new HalfVector4(
                field.PackedPoints[index]);
        }

        Texture2D created = new(
            graphicsDevice,
            field.TextureWidth,
            field.GridSize,
            false,
            SurfaceFormat.HalfVector4);
        try
        {
            created.SetData(pixels);
            texture = created;
            return created;
        }
        catch
        {
            created.Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        texture?.Dispose();
        texture = null;
        disposed = true;
    }
}
