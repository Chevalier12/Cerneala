using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGradientMapTextureCache : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly Dictionary<PrismResourceId, Entry> entries = [];

    public PrismGradientMapTextureCache(GraphicsDevice graphicsDevice) =>
        this.graphicsDevice = graphicsDevice;

    public Texture2D GetOrCreate(
        PrismResourceId id,
        PrismGradientMapResource resource,
        long identity,
        long version)
    {
        if (entries.TryGetValue(id, out Entry current) &&
            ReferenceEquals(current.Resource, resource) &&
            current.Identity == identity && current.Version == version &&
            !current.Texture.IsDisposed)
        {
            return current.Texture;
        }
        PrismGradientMapLut lut = PrismGradientMapLut.Create(resource);
        HalfVector4[] pixels = lut.Values.ToArray()
            .Select(value => new HalfVector4(
                new System.Numerics.Vector4(value, 1)))
            .ToArray();
        Texture2D texture = new(
            graphicsDevice,
            PrismGradientMapLut.SampleCount,
            1,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(pixels);
        if (entries.Remove(id, out Entry replaced))
        {
            replaced.Texture.Dispose();
        }
        entries[id] = new Entry(resource, identity, version, texture);
        return texture;
    }

    public void Dispose()
    {
        foreach (Entry entry in entries.Values)
        {
            entry.Texture.Dispose();
        }
        entries.Clear();
    }

    private readonly record struct Entry(
        PrismGradientMapResource Resource,
        long Identity,
        long Version,
        Texture2D Texture);
}
