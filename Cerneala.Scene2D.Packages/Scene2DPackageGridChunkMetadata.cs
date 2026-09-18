using System.Collections.ObjectModel;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Original authored grid extent, revision and properties, without cell data.</summary>
public sealed class Scene2DPackageGridChunkMetadata
{
    internal Scene2DPackageGridChunkMetadata(TileMapBounds2D cells, long version,
        IReadOnlyDictionary<string, object?> properties)
    {
        if (cells.Width <= 0 || cells.Height <= 0) { throw new ArgumentOutOfRangeException(nameof(cells)); }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        Cells = cells;
        Version = version;
        Properties = new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(properties, StringComparer.Ordinal));
    }

    public TileMapBounds2D Cells { get; }
    public long Version { get; }
    public IReadOnlyDictionary<string, object?> Properties { get; }
}
