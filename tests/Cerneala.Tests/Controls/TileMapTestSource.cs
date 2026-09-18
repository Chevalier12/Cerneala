using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

namespace Cerneala.Tests.Controls;

// Test-owned immutable model publication, not a production Model compatibility path.
// Every acquisition goes through Source and the real simulation/presentation owner.
internal static class TileMapTestSource
{
    private static readonly ConditionalWeakTable<TileMapSource2D, Publication> publications = new();

    internal static TileMapSource2D Create(TileMap2DModel model,
        IReadOnlyDictionary<string, DrawSize>? imageSizes = null)
    {
        Publication publication = new(model, imageSizes);
        publications.Add(publication.Source, publication);
        return publication.Source;
    }

    internal static void PublishModel(this TileMap2D map, TileMap2DModel model)
    {
        if (map.Source is { } source && publications.TryGetValue(source, out Publication? publication))
            publication.Publish(model);
        else
            map.Source = Create(model);
    }

    internal static TileMap2DModel GetSourceModel(this TileMap2D map) =>
        publications.GetValue(map.Source!, _ => throw new InvalidOperationException("Not a test-owned model source.")).Model;

    internal static void PrepareFrame(RenderSurface2D surface, DrawRect bounds)
    {
        UIRoot root = surface.Root ?? throw new InvalidOperationException("Attach the test surface before preparing a frame.");
        root.Width = surface.Width = bounds.Width;
        root.Height = surface.Height = bounds.Height;
        root.ProcessFrame();
        ((ITimeSensitiveRenderElement)surface).UpdateRenderTime(TimeSpan.Zero);
    }

    private sealed class Publication
    {
        private TileMapSource2D backing;
        private readonly IReadOnlyDictionary<string, DrawSize>? imageSizes;
        internal TileMapSource2D Source { get; }
        internal TileMap2DModel Model { get; private set; }

        internal Publication(TileMap2DModel model, IReadOnlyDictionary<string, DrawSize>? imageSizes)
        {
            Model = model;
            this.imageSizes = imageSizes;
            backing = TileMapSource2D.FromModel(model, imageSizes);
            Source = new(backing.Catalog, (_, chunk, token) =>
                Volatile.Read(ref backing).LoadAsync(chunk.Spatial, token));
        }

        internal void Publish(TileMap2DModel model)
        {
            TileMapSource2D replacement = TileMapSource2D.FromModel(model, imageSizes);
            Volatile.Write(ref backing, replacement);
            Model = model;
            Source.SetCatalog(replacement.Catalog);
        }
    }
}

internal sealed class TileMapTestSurface : IDisposable
{
    private readonly UIRoot root;
    private readonly RenderSurface2D surface;

    internal TileMapTestSurface(RenderSurface2D surface, DrawRect bounds, UIRoot? root = null)
    {
        this.surface = surface;
        this.root = root ?? new UIRoot();
        this.root.VisualChildren.Add(surface);
        TileMapTestSource.PrepareFrame(surface, bounds);
    }

    public void Dispose() => root.VisualChildren.Remove(surface);
}
