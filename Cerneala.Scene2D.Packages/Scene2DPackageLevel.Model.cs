using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

public sealed partial class Scene2DPackageLevel
{
    /// <summary>Loads and reconstructs one complete authored map for application-owned editing.</summary>
    public async ValueTask<TileMap2DModel> LoadMapModelAsync(string mapId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PackageMap map = maps[mapId];
        Scene2DPackageMetadata metadata = await package.LoadAsync<Scene2DPackageMetadata>(map.Metadata,
            cancellationToken).ConfigureAwait(false);
        try
        {
            return map.Catalog.IsFreePlacement
                ? await LoadFreePlacementModelAsync(map, metadata, cancellationToken).ConfigureAwait(false)
                : await LoadGridModelAsync(map, metadata, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception error) when (error is ArgumentException or OverflowException)
        {
            throw new InvalidDataException("The package map cannot be reconstructed as an authored model.", error);
        }
    }

    private async ValueTask<TileMap2DModel> LoadFreePlacementModelAsync(PackageMap map,
        Scene2DPackageMetadata metadata, CancellationToken cancellationToken)
    {
        TileMapCatalog2D catalog = map.Catalog;
        if (metadata.GridChunks.Count != 0 || metadata.TileSets.Count != 0 || metadata.Properties.Count != 0 ||
            catalog.Bounds is not null || catalog.Order != 0 || !catalog.IsVisible || catalog.Offset != default ||
            catalog.Opacity != 1 || catalog.Tint != Color.White)
        {
            throw new InvalidDataException("Free-placement metadata cannot be represented by the authored map model.");
        }
        List<Tile> placements = [];
        for (int index = 0; index < map.Chunks.Length; index++)
        {
            TileMapChunkInfo2D info = catalog.Chunks[index];
            string expectedId = FormattableString.Invariant($"placements:{placements.Count}");
            if (info.Cells is not null || info.Spatial.Id != expectedId)
            { throw new InvalidDataException("Free-placement package groups are missing or out of order."); }
            TileMapChunkData2D data = await package.LoadAsync<TileMapChunkData2D>(map.Chunks[index],
                cancellationToken).ConfigureAwait(false);
            catalog.ValidatePayload(info, data);
            if ((long)placements.Count + data.Placements.Count > Scene2DModelValidator.MaximumCells)
            { throw new InvalidDataException("The package map exceeds the authored model cell budget."); }
            placements.AddRange(data.Placements);
        }
        return new TileMap2DModel(placements, catalog.Version, catalog.Id);
    }

    private async ValueTask<TileMap2DModel> LoadGridModelAsync(PackageMap map,
        Scene2DPackageMetadata metadata, CancellationToken cancellationToken)
    {
        TileMapCatalog2D catalog = map.Catalog;
        TileMapBounds2D[] authoredBounds = metadata.GridChunks.Select(static chunk => chunk.Cells).ToArray();
        TileMap2DModel.ValidateNoOverlaps(authoredBounds);
        long totalCells = 0;
        TileCell2D[][] cells = new TileCell2D[authoredBounds.Length][];
        bool[][] filled = new bool[authoredBounds.Length][];
        for (int index = 0; index < authoredBounds.Length; index++)
        {
            TileMapBounds2D bounds = authoredBounds[index];
            totalCells = checked(totalCells + (long)bounds.Width * bounds.Height);
            if (totalCells > Scene2DModelValidator.MaximumCells)
            { throw new InvalidDataException("The package map exceeds the authored model cell budget."); }
            cells[index] = new TileCell2D[checked(bounds.Width * bounds.Height)];
            filled[index] = new bool[cells[index].Length];
        }

        Dictionary<string, TileSet2D> completeSets = metadata.TileSets.ToDictionary(static set => set.Id,
            StringComparer.Ordinal);
        Dictionary<string, Dictionary<int, TileDefinition2D>> completeDefinitions = completeSets.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Tiles.ToDictionary(static tile => tile.Id), StringComparer.Ordinal);
        for (int index = 0; index < map.Chunks.Length; index++)
        {
            TileMapChunkInfo2D info = catalog.Chunks[index];
            if (info.Cells is not TileMapBounds2D preparedBounds)
            { throw new InvalidDataException("A grid map contains a free-placement package group."); }
            TileMapChunkData2D data = await package.LoadAsync<TileMapChunkData2D>(map.Chunks[index],
                cancellationToken).ConfigureAwait(false);
            catalog.ValidatePayload(info, data);
            TileChunk2D piece = data.Grid!;
            int owner = FindAuthoredChunk(authoredBounds, preparedBounds);
            Scene2DPackageGridChunkMetadata authored = metadata.GridChunks[owner];
            if (piece.Version != authored.Version || !SameValue(piece.Properties, authored.Properties))
            { throw new InvalidDataException("Prepared chunk revision or properties differ from authored metadata."); }
            ValidatePalette(data.TileSets, completeSets, completeDefinitions);
            TileMapBounds2D target = authoredBounds[owner];
            for (int y = 0; y < piece.Height; y++)
            for (int x = 0; x < piece.Width; x++)
            {
                int targetIndex = checked((piece.Origin.Y + y - target.Y) * target.Width +
                    piece.Origin.X + x - target.X);
                if (filled[owner][targetIndex])
                { throw new InvalidDataException("Prepared package chunks overlap an authored cell."); }
                filled[owner][targetIndex] = true;
                cells[owner][targetIndex] = piece.Tiles[y * piece.Width + x];
            }
        }

        List<TileChunk2D> chunks = new(authoredBounds.Length);
        for (int index = 0; index < authoredBounds.Length; index++)
        {
            if (filled[index].Any(static present => !present))
            { throw new InvalidDataException("Prepared package chunks leave a hole in an authored extent."); }
            TileMapBounds2D bounds = authoredBounds[index];
            Scene2DPackageGridChunkMetadata authored = metadata.GridChunks[index];
            chunks.Add(new TileChunk2D(new(bounds.X, bounds.Y), bounds.Width, bounds.Height,
                cells[index], authored.Version, authored.Properties));
        }
        return new TileMap2DModel(catalog.Id, catalog.TileSize, metadata.TileSets, chunks,
            catalog.Bounds, catalog.Order, catalog.IsVisible, catalog.Offset, catalog.Opacity, catalog.Tint,
            catalog.Version, metadata.Properties);
    }

    private static int FindAuthoredChunk(IReadOnlyList<TileMapBounds2D> authored,
        TileMapBounds2D piece)
    {
        int owner = -1;
        for (int index = 0; index < authored.Count; index++)
        {
            TileMapBounds2D bounds = authored[index];
            if (piece.X < bounds.X || piece.Y < bounds.Y ||
                (long)piece.X + piece.Width > (long)bounds.X + bounds.Width ||
                (long)piece.Y + piece.Height > (long)bounds.Y + bounds.Height)
            { continue; }
            if (owner >= 0) { throw new InvalidDataException("A prepared piece belongs to multiple authored chunks."); }
            owner = index;
        }
        if (owner < 0) { throw new InvalidDataException("A prepared piece lies outside authored map extents."); }
        return owner;
    }

    private static void ValidatePalette(IReadOnlyList<TileSet2D> loaded,
        IReadOnlyDictionary<string, TileSet2D> complete,
        IReadOnlyDictionary<string, Dictionary<int, TileDefinition2D>> definitionsBySet)
    {
        foreach (TileSet2D partial in loaded)
        {
            if (!complete.TryGetValue(partial.Id, out TileSet2D? full) ||
                partial.AtlasResourceId != full.AtlasResourceId || partial.Version != full.Version ||
                !SameValue(partial.Properties, full.Properties))
            { throw new InvalidDataException("A prepared palette differs from authored tile-set metadata."); }
            Dictionary<int, TileDefinition2D> definitions = definitionsBySet[partial.Id];
            foreach (TileDefinition2D tile in partial.Tiles)
            {
                if (!definitions.TryGetValue(tile.Id, out TileDefinition2D? original) ||
                    !SameValue(tile, original))
                { throw new InvalidDataException("A prepared tile definition differs from authored metadata."); }
            }
        }
    }

    private static bool SameValue(object left, object right) =>
        PackageValueCodec.Encode(left).AsSpan().SequenceEqual(PackageValueCodec.Encode(right));
}
