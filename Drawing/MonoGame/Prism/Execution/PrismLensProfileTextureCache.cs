using System.Numerics;
using Cerneala.Drawing.Prism.Filters;
using Cerneala.UI.Prism.Definitions;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismLensProfileTextureCache : IDisposable
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly Dictionary<PrismResourceId, Entry> entries = [];

    public PrismLensProfileTextureCache(GraphicsDevice graphicsDevice) =>
        this.graphicsDevice = graphicsDevice;

    public Texture2D GetOrCreate(
        PrismResourceId id,
        PrismLensProfileResource resource,
        long identity,
        long version,
        int width,
        int height,
        Vector2 lightPosition,
        float brightness)
    {
        if (entries.TryGetValue(id, out Entry current) &&
            ReferenceEquals(current.Resource, resource) &&
            current.Identity == identity &&
            current.Version == version &&
            current.Width == width &&
            current.Height == height &&
            current.LightPosition == lightPosition &&
            current.Brightness == brightness &&
            !current.Texture.IsDisposed)
        {
            return current.Texture;
        }

        HalfVector4[] pixels = PrismLensFlareRenderer.Render(
                resource,
                width,
                height,
                lightPosition,
                brightness)
            .Select(value => new HalfVector4(value))
            .ToArray();
        Texture2D texture = new(
            graphicsDevice,
            width,
            height,
            false,
            SurfaceFormat.HalfVector4);
        texture.SetData(pixels);
        if (entries.Remove(id, out Entry replaced))
        {
            replaced.Texture.Dispose();
        }
        entries[id] = new Entry(
            resource,
            identity,
            version,
            width,
            height,
            lightPosition,
            brightness,
            texture);
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
        PrismLensProfileResource Resource,
        long Identity,
        long Version,
        int Width,
        int Height,
        Vector2 LightPosition,
        float Brightness,
        Texture2D Texture);
}
