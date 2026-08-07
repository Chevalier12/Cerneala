using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Drawing.Prism.Styles;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismGradientOverlayTextureCache : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly Dictionary<Key, Entry> entries = [];

    public PrismGradientOverlayTextureCache(GraphicsDevice graphicsDevice) =>
        this.graphicsDevice = graphicsDevice;

    public Texture2D GetOrCreate(
        PrismResourceId id,
        PrismGradientMapResource resource,
        long identity,
        long version,
        PrismGradientInterpolation interpolation,
        PrismColorProfile workingProfile)
    {
        Key key = new(id, interpolation, workingProfile);
        if (entries.TryGetValue(key, out Entry current) &&
            ReferenceEquals(current.Resource, resource) &&
            current.Identity == identity &&
            current.Version == version &&
            !current.Texture.IsDisposed)
        {
            return current.Texture;
        }

        PrismCssGradientLut lut = PrismCssGradientLut.Create(
            resource,
            interpolation,
            workingProfile);
        HalfVector4[] pixels = lut.Values.ToArray()
            .Select(value => new HalfVector4(value))
            .ToArray();
        Texture2D texture = new(
            graphicsDevice,
            PrismCssGradientLut.SampleCount,
            1,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(pixels);
        if (entries.Remove(key, out Entry replaced))
        {
            replaced.Texture.Dispose();
        }
        entries[key] = new Entry(resource, identity, version, texture);
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

    private readonly record struct Key(
        PrismResourceId Id,
        PrismGradientInterpolation Interpolation,
        PrismColorProfile WorkingProfile);

    private readonly record struct Entry(
        PrismGradientMapResource Resource,
        long Identity,
        long Version,
        Texture2D Texture);
}
