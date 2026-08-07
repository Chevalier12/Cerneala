using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismCurveTextureCache : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly Dictionary<PrismResourceId, Entry> entries = [];
    private bool disposed;

    public PrismCurveTextureCache(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    public Texture2D GetOrCreate(
        PrismResourceId id,
        PrismCurvesResource resource,
        long identity,
        long version)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (entries.TryGetValue(id, out Entry entry) &&
            ReferenceEquals(entry.Resource, resource) &&
            entry.Identity == identity &&
            entry.Version == version &&
            !entry.Texture.IsDisposed)
        {
            return entry.Texture;
        }

        Texture2D texture = CreateTexture(resource);
        if (entries.Remove(id, out Entry replaced))
        {
            replaced.Texture.Dispose();
        }
        entries[id] = new Entry(
            resource,
            identity,
            version,
            texture);
        return texture;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (Entry entry in entries.Values)
        {
            entry.Texture.Dispose();
        }
        entries.Clear();
        disposed = true;
    }

    private Texture2D CreateTexture(
        PrismCurvesResource resource)
    {
        PrismCurveLut lut = PrismCurveLut.Create(resource);
        ReadOnlySpan<System.Numerics.Vector4> values = lut.Values;
        HalfVector4[] pixels = new HalfVector4[values.Length];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new HalfVector4(values[index]);
        }

        Texture2D texture = new(
            graphicsDevice,
            PrismCurveLut.SampleCount,
            1,
            false,
            SurfaceFormat.HalfVector4);
        try
        {
            texture.SetData(pixels);
            return texture;
        }
        catch
        {
            texture.Dispose();
            throw;
        }
    }

    private readonly record struct Entry(
        PrismCurvesResource Resource,
        long Identity,
        long Version,
        Texture2D Texture);
}
