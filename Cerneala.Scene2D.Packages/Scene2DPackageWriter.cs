using System.Security.Cryptography;
using Cerneala.Drawing;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Prepares a self-contained directory from a validated in-memory scene at build/import time.</summary>
public static class Scene2DPackageWriter
{
    public static async Task WriteAsync(string directory, Scene2DDocument document, string assetRootDirectory,
        IEnumerable<string>? referencedFiles = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();
        string destination = PackageFiles.Root(directory);
        string assetRoot = PackageFiles.Root(assetRootDirectory);
        string parent = Path.GetDirectoryName(destination) ?? throw new ArgumentException("A package needs a parent directory.", nameof(directory));
        if (!Directory.Exists(parent)) { throw new DirectoryNotFoundException("The package parent directory must already exist."); }
        if (Directory.Exists(destination) || File.Exists(destination)) { throw new IOException("A package writer never overwrites an existing destination."); }
        Scene2DValidationResult validation = Scene2DModelValidator.Validate(document);
        if (!validation.Success) { throw new ArgumentException("The scene document failed package validation.", nameof(document)); }

        HashSet<string> files = new(StringComparer.Ordinal);
        Dictionary<string, string> portablePaths = new(StringComparer.OrdinalIgnoreCase);
        foreach (string file in document.Assets.Select(static asset => asset.Path).Concat(referencedFiles ?? []))
        {
            string normalized = PackageFiles.Normalize(file);
            if (portablePaths.TryGetValue(normalized, out string? previous) && previous != normalized)
            {
                throw new ArgumentException("Package file paths cannot differ only by case.", nameof(referencedFiles));
            }
            portablePaths[normalized] = normalized;
            files.Add(normalized);
            PackageFiles.ExistingFile(assetRoot, normalized);
        }
        string staging = Path.Combine(parent, ".cerneala-package-" + Guid.NewGuid().ToString("N"));
        List<string> createdFiles = [];
        List<string> createdDirectories = [staging];
        Directory.CreateDirectory(staging);
        try
        {
            string dataPath = Path.Combine(staging, PackageFiles.DataName);
            PackageIndex index;
            await using (FileStream data = new(dataPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536, useAsync: true))
            {
                createdFiles.Add(dataPath);
                PackageBlock documentMetadata = (await StoreAsync(new Scene2DPackageMetadata(document.Properties, []))).Block;
                Dictionary<string, DrawSize> sizes = document.Assets.ToDictionary(static asset => asset.ResourceId.Key, static asset => asset.Size, StringComparer.Ordinal);
                List<PackageLevel> levels = [];
                foreach (Scene2DLevel level in document.Levels)
                {
                    PackageBlock levelMetadata = (await StoreAsync(new Scene2DPackageMetadata(level.Properties, level.TileSets))).Block;
                    List<PackageMap> maps = [];
                    foreach (TileMap2DModel model in level.TileMaps)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        TileMapSource2D backing = TileMapSource2D.FromModel(PrepareGrid(model, cancellationToken), sizes);
                        TileMapCatalog2D original = backing.Catalog;
                        PackageBlock metadata = (await StoreAsync(new Scene2DPackageMetadata(model.Properties, model.TileSets,
                            model.Chunks.Select(static chunk => new Scene2DPackageGridChunkMetadata(
                                new(chunk.Origin.X, chunk.Origin.Y, chunk.Width, chunk.Height), chunk.Version, chunk.Properties))))).Block;
                        List<PackageBlock> chunks = [];
                        List<TileMapChunkInfo2D> headers = [];
                        foreach (TileMapChunkInfo2D info in original.Chunks)
                        {
                            using SceneSpatialLease2D<TileMapChunkData2D> lease = await backing.LoadAsync(info.Spatial, cancellationToken).ConfigureAwait(false);
                            var stored = await StoreAsync(lease.Value);
                            chunks.Add(stored.Block);
                            // The typed encoder accounts for the decoder's owned data,
                            // independently of encoded block length and image residency.
                            headers.Add(info.Cells is TileMapBounds2D cells
                                ? new(info.Spatial, cells, info.TileIds, info.Images, info.ExpandedColliderCount, stored.DataCharge)
                                : new(info.Spatial, info.TileCount, info.Images, info.ExpandedColliderCount, stored.DataCharge));
                        }
                        TileMapCatalog2D catalog = new(original.Id, headers, original.IsFreePlacement ? null : original.TileSize,
                            original.Bounds, original.Order, original.IsVisible, original.Offset, original.Opacity, original.Tint, original.Version, original.ImageSizes);
                        maps.Add(new(catalog, metadata, chunks.ToArray()));
                    }
                    List<PackageEntity> entities = [];
                    foreach (Scene2DEntity entity in level.Entities) { entities.Add(new(Scene2DPackageEntityInfo.FromEntity(entity), (await StoreAsync(entity)).Block)); }
                    List<PackagePromotion> promotions = [];
                    foreach (TilePromotion2D promotion in level.Promotions) { promotions.Add(new(promotion.Cell, (await StoreAsync(promotion)).Block)); }
                    levels.Add(new(level.Id, level.WorldOffset, level.TileSize, level.Bounds, levelMetadata, maps.ToArray(), entities.ToArray(), promotions.ToArray()));
                }
                index = new(document.Assets.ToArray(), files.Order(StringComparer.Ordinal).ToArray(), documentMetadata, levels.ToArray(), data.Position);
                await data.FlushAsync(cancellationToken).ConfigureAwait(false);

                async ValueTask<(PackageBlock Block, long DataCharge)> StoreAsync(object value)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    byte[] encoded = PackageValueCodec.Encode(value, out long dataCharge);
                    PackageBlock block = new(data.Position, encoded.Length, SHA256.HashData(encoded));
                    await data.WriteAsync(encoded, cancellationToken).ConfigureAwait(false);
                    return (block, dataCharge);
                }
            }

            string assetsDirectory = Path.Combine(staging, PackageFiles.AssetsName);
            CreateOwnedDirectory(assetsDirectory);
            foreach (string file in index.Files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string sourcePath = PackageFiles.ExistingFile(assetRoot, file);
                string outputPath = Path.Combine(assetsDirectory, file.Replace('/', Path.DirectorySeparatorChar));
                CreateOwnedDirectory(Path.GetDirectoryName(outputPath)!);
                await using FileStream source = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, useAsync: true);
                await using FileStream output = new(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536, useAsync: true);
                createdFiles.Add(outputPath);
                await source.CopyToAsync(output, cancellationToken).ConfigureAwait(false);
            }
            string catalogPath = Path.Combine(staging, PackageFiles.CatalogName);
            byte[] catalogBytes = PackageValueCodec.Encode(index);
            await using (FileStream catalog = new(catalogPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536, useAsync: true))
            {
                createdFiles.Add(catalogPath);
                await catalog.WriteAsync(catalogBytes, cancellationToken).ConfigureAwait(false);
                await catalog.WriteAsync(SHA256.HashData(catalogBytes), cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(staging, destination);
        }
        catch (Exception failure)
        {
            // Remove only files/directories created by this invocation. Never
            // recursively delete the destination, asset root or unexpected files.
            try
            {
                foreach (string file in createdFiles) { File.Delete(file); }
                foreach (string folder in createdDirectories.OrderByDescending(static path => path.Length)) { Directory.Delete(folder, recursive: false); }
            }
            catch (Exception cleanupFailure) { throw new AggregateException(failure, cleanupFailure); }
            throw;
        }

        void CreateOwnedDirectory(string path)
        {
            if (Directory.Exists(path)) { return; }
            CreateOwnedDirectory(Path.GetDirectoryName(path)!);
            Directory.CreateDirectory(path);
            createdDirectories.Add(path);
        }
    }

    private static TileMap2DModel PrepareGrid(TileMap2DModel model, CancellationToken cancellationToken)
    {
        const int edge = 16;
        // This is a build/import transformation, not a change to FromModel or
        // the authored document. Reuse the core's palette/header derivation.
        if (model.Chunks.All(static chunk => chunk.Width <= edge && chunk.Height <= edge)) { return model; }
        return new(model.Id, model.TileSize, model.TileSets, Pieces(), model.Bounds, model.Order,
            model.IsVisible, model.Offset, model.Opacity, model.Tint, model.Version, model.Properties);

        IEnumerable<TileChunk2D> Pieces()
        {
            foreach (TileChunk2D chunk in model.Chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (chunk.Width <= edge && chunk.Height <= edge) { yield return chunk; continue; }
                for (int y = 0; y < chunk.Height; y += edge)
                for (int x = 0; x < chunk.Width; x += edge)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int width = Math.Min(edge, chunk.Width - x), height = Math.Min(edge, chunk.Height - y);
                    TileCell2D[] cells = new TileCell2D[width * height];
                    for (int row = 0; row < height; row++)
                    for (int column = 0; column < width; column++)
                    {
                        cells[row * width + column] = chunk.Tiles[(y + row) * chunk.Width + x + column];
                    }
                    // Empty pieces still own authored extent. Do not omit them.
                    yield return new(new(chunk.Origin.X + x, chunk.Origin.Y + y), width, height,
                        cells, chunk.Version, chunk.Properties);
                }
            }
        }
    }
}
