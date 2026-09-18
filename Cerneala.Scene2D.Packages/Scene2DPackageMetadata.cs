using System.Collections.ObjectModel;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Explicitly acquired nonspatial metadata; never part of a package catalog.</summary>
public sealed class Scene2DPackageMetadata
{
    internal Scene2DPackageMetadata(IReadOnlyDictionary<string, object?> properties, IEnumerable<TileSet2D> tileSets,
        IEnumerable<Scene2DPackageGridChunkMetadata>? gridChunks = null)
    {
        Properties = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(properties, StringComparer.Ordinal));
        TileSets = Array.AsReadOnly(tileSets.ToArray());
        GridChunks = Array.AsReadOnly((gridChunks ?? []).ToArray());
    }

    public IReadOnlyDictionary<string, object?> Properties { get; }
    public IReadOnlyList<TileSet2D> TileSets { get; }
    public IReadOnlyList<Scene2DPackageGridChunkMetadata> GridChunks { get; }
}

/// <summary>Allocation admission limits checked before reading an encoded block.</summary>
public sealed class Scene2DPackageReadOptions
{
    public int MaxCatalogBytes { get; init; } = 16 * 1024 * 1024;
    public int MaxPayloadBytes { get; init; } = 64 * 1024 * 1024;
}
