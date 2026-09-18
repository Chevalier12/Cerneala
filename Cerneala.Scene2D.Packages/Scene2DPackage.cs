using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Packages;

/// <summary>Owns a metadata-only package index and independent asynchronous payload reads.</summary>
public sealed class Scene2DPackage : IDisposable
{
    private readonly object gate = new();
    private readonly string directory;
    private readonly SafeFileHandle data;
    private readonly PackageBlock metadata;
    private readonly int maximumPayloadBytes;
    private readonly HashSet<string> files;
    private bool disposed;
    private int readers;

    private Scene2DPackage(string directory, SafeFileHandle data, PackageIndex index, int maximumPayloadBytes)
    {
        this.directory = directory;
        this.data = data;
        this.maximumPayloadBytes = maximumPayloadBytes;
        metadata = index.Metadata;
        Assets = Array.AsReadOnly(index.Assets);
        ReferencedFiles = Array.AsReadOnly(index.Files);
        files = new(index.Files, StringComparer.Ordinal);
        Levels = Array.AsReadOnly(index.Levels.Select(level => new Scene2DPackageLevel(this, level)).ToArray());
    }

    public IReadOnlyList<Scene2DAsset> Assets { get; }
    public IReadOnlyList<string> ReferencedFiles { get; }
    public IReadOnlyList<Scene2DPackageLevel> Levels { get; }

    public static async Task<Scene2DPackage> OpenAsync(string directory, Scene2DPackageReadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options ??= new();
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxCatalogBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.MaxPayloadBytes);
        string root = PackageFiles.Root(directory);
        string catalogPath = PackageFiles.ExistingFile(root, PackageFiles.CatalogName);
        PackageIndex index;
        await using (FileStream catalog = new(catalogPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536, useAsync: true))
        {
            if (catalog.Length <= 32 || catalog.Length - 32 > options.MaxCatalogBytes) { throw new InvalidDataException("The package catalog exceeds its read limit or is truncated."); }
            byte[] encoded = new byte[(int)(catalog.Length - 32)];
            byte[] hash = new byte[32];
            await catalog.ReadExactlyAsync(encoded, cancellationToken).ConfigureAwait(false);
            await catalog.ReadExactlyAsync(hash, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(encoded), hash)) { throw new InvalidDataException("Package catalog checksum mismatch."); }
            index = PackageValueCodec.Decode(encoded) as PackageIndex ?? throw new InvalidDataException("Expected a scene package catalog.");
        }
        ValidateIndex(index);
        SafeFileHandle handle = File.OpenHandle(PackageFiles.ExistingFile(root, PackageFiles.DataName),
            FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous | FileOptions.RandomAccess);
        try
        {
            if (RandomAccess.GetLength(handle) != index.DataLength) { throw new InvalidDataException("Package data length differs from its catalog."); }
            cancellationToken.ThrowIfCancellationRequested();
            return new(root, handle, index, options.MaxPayloadBytes);
        }
        catch { handle.Dispose(); throw; }
    }

    public ValueTask<SceneSpatialLease2D<Scene2DPackageMetadata>> LoadMetadataAsync(CancellationToken cancellationToken = default) =>
        LoadAsync<Scene2DPackageMetadata>(metadata, cancellationToken);

    public string GetFilePath(string relativePath)
    {
        ThrowIfDisposed();
        string normalized = PackageFiles.Normalize(relativePath);
        if (!files.Contains(normalized)) { throw new ArgumentException("The file is not declared by this package.", nameof(relativePath)); }
        return PackageFiles.ExistingFile(directory, PackageFiles.AssetsName + "/" + normalized);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) { return; }
            disposed = true;
            if (readers == 0) { data.Dispose(); }
        }
    }

    internal async ValueTask<SceneSpatialLease2D<T>> LoadAsync<T>(PackageBlock block, CancellationToken cancellationToken) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (block.Length > maximumPayloadBytes) { throw new InvalidDataException("The payload exceeds the configured read limit."); }
            readers++;
        }
        try
        {
            byte[] encoded = new byte[block.Length];
            int read = 0;
            while (read < encoded.Length)
            {
                int count = await RandomAccess.ReadAsync(data, encoded.AsMemory(read), block.Offset + read, cancellationToken).ConfigureAwait(false);
                if (count == 0) { throw new InvalidDataException("Truncated scene payload."); }
                read += count;
            }
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(encoded), block.Hash)) { throw new InvalidDataException("Scene payload checksum mismatch."); }
            T value = PackageValueCodec.Decode(encoded) as T ?? throw new InvalidDataException("Unexpected scene payload type.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(value);
        }
        finally
        {
            lock (gate)
            {
                readers--;
                if (disposed && readers == 0) { data.Dispose(); }
            }
        }
    }

    private void ThrowIfDisposed() { lock (gate) { ObjectDisposedException.ThrowIf(disposed, this); } }

    private static void ValidateIndex(PackageIndex index)
    {
        try
        {
            Dictionary<string, Scene2DAsset> assets = index.Assets.ToDictionary(static item => item.ResourceId.Key, StringComparer.Ordinal);
            HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);
            foreach (string file in index.Files)
            {
                if (PackageFiles.Normalize(file) != file || !files.Add(file)) { throw new InvalidDataException("Invalid or duplicated package file path."); }
            }
            foreach (Scene2DAsset asset in index.Assets)
            {
                if (!index.Files.Contains(asset.Path, StringComparer.Ordinal) || !float.IsFinite(asset.Size.Width) || !float.IsFinite(asset.Size.Height))
                { throw new InvalidDataException("Package atlas is not declared or has invalid dimensions."); }
            }
            List<PackageBlock> blocks = [index.Metadata];
            HashSet<string> levelIds = new(StringComparer.Ordinal);
            long mapCount = 0, entityCount = 0, cells = 0, chunks = 0;
            foreach (PackageLevel level in index.Levels)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(level.Id);
                if (!levelIds.Add(level.Id)) { throw new InvalidDataException("Duplicated level identity."); }
                _ = new SceneSpatialEntry2D(level.Id, new(level.WorldOffset.X, level.WorldOffset.Y, 0, 0), collisionBounds: null);
                if (level.TileSize is { } size && (!float.IsFinite(size.Width) || !float.IsFinite(size.Height) || size.Width <= 0 || size.Height <= 0))
                { throw new InvalidDataException("Invalid level grid."); }
                if (level.Bounds is { } bounds && (level.TileSize is null || bounds.Width <= 0 || bounds.Height <= 0))
                { throw new InvalidDataException("Invalid level bounds."); }
                blocks.Add(level.Metadata);
                HashSet<string> mapIds = new(StringComparer.Ordinal);
                foreach (PackageMap map in level.Maps)
                {
                    mapCount++;
                    if (!mapIds.Add(map.Catalog.Id) || map.Chunks.Length != map.Catalog.Chunks.Count) { throw new InvalidDataException("Invalid package map index."); }
                    blocks.Add(map.Metadata);
                    blocks.AddRange(map.Chunks);
                    foreach (TileMapChunkInfo2D chunk in map.Catalog.Chunks)
                    {
                        chunks++; cells += chunk.TileCount;
                        foreach (var image in chunk.Images)
                        {
                            if (image.ResourceId is not { } id || !assets.TryGetValue(id.Key, out Scene2DAsset? asset) ||
                                !map.Catalog.TryGetImageSize(image, out var declared) || declared != asset.Size)
                            { throw new InvalidDataException("Unknown or inconsistent package image dependency."); }
                        }
                    }
                }
                HashSet<string> entityIds = new(StringComparer.Ordinal);
                foreach (PackageEntity entity in level.Entities)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(entity.Id);
                    if (!entityIds.Add(entity.Id) || !mapIds.Contains(entity.Info.MapId))
                    { throw new InvalidDataException("Duplicated entity identity or unknown owning map."); }
                    blocks.Add(entity.Data); entityCount++;
                }
                HashSet<TileCellKey2D> promotionCells = [];
                foreach (PackagePromotion promotion in level.Promotions)
                {
                    if (!mapIds.Contains(promotion.Cell.MapId) || !promotionCells.Add(promotion.Cell)) { throw new InvalidDataException("Invalid promotion identity."); }
                    blocks.Add(promotion.Data); entityCount++;
                }
            }
            if (index.Assets.Length > 4096 || index.Levels.Length > 4096 || mapCount > 4096 || entityCount > 65_536 || cells > 1_048_576 || chunks > 65_536)
            { throw new InvalidDataException("The package exceeds the core scene model budgets."); }
            long position = 0;
            foreach (PackageBlock block in blocks.OrderBy(static block => block.Offset))
            {
                if (block.Offset != position || block.Length < 5 || block.Hash.Length != 32) { throw new InvalidDataException("Invalid, overlapping or incomplete package byte ranges."); }
                position = checked(position + block.Length);
            }
            if (position != index.DataLength) { throw new InvalidDataException("Package byte ranges do not cover the declared data."); }
        }
        catch (Exception error) when (error is ArgumentException or OverflowException)
        { throw new InvalidDataException("Invalid scene package catalog.", error); }
    }
}
