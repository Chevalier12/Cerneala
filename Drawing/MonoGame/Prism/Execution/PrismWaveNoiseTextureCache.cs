using Cerneala.Drawing.Prism.Filters;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Cerneala.Drawing.MonoGame.Prism.Execution;

internal sealed class PrismWaveNoiseTextureCache : IDisposable
{
    private const int MaximumEntryCount = 32;

    private readonly GraphicsDevice graphicsDevice;
    private readonly Dictionary<int, Entry> entries = [];
    private bool disposed;

    public PrismWaveNoiseTextureCache(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    public Texture2D GetOrCreate(PrismWaveNoiseTable table)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        int key = ContentHash(table);
        if (entries.TryGetValue(key, out Entry entry) &&
            entry.Table == table &&
            !entry.Texture.IsDisposed)
        {
            return entry.Texture;
        }

        if (entries.Count >= MaximumEntryCount)
        {
            Clear();
        }

        Texture2D texture = CreateTexture(table);
        if (entries.Remove(key, out Entry replaced))
        {
            replaced.Texture.Dispose();
        }
        entries[key] = new Entry(table, texture);
        return texture;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Clear();
        disposed = true;
    }

    private Texture2D CreateTexture(PrismWaveNoiseTable table)
    {
        if (table.PackedSamples.Length !=
            PrismWaveNoise.PackedTableSampleCount)
        {
            throw new InvalidOperationException(
                "Wave Noise texture creation requires a complete table.");
        }

        HalfVector4[] pixels =
            new HalfVector4[table.PackedSamples.Length];
        for (int index = 0; index < pixels.Length; index++)
        {
            pixels[index] = new HalfVector4(
                table.PackedSamples[index]);
        }

        Texture2D texture = new(
            graphicsDevice,
            pixels.Length,
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

    private void Clear()
    {
        foreach (Entry entry in entries.Values)
        {
            entry.Texture.Dispose();
        }
        entries.Clear();
    }

    private static int ContentHash(PrismWaveNoiseTable table)
    {
        HashCode hash = new();
        hash.Add(table.Normalization);
        foreach (System.Numerics.Vector4 sample in table.PackedSamples)
        {
            hash.Add(sample);
        }
        return hash.ToHashCode();
    }

    private readonly record struct Entry(
        PrismWaveNoiseTable Table,
        Texture2D Texture);
}
