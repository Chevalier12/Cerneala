using Microsoft.Win32.SafeHandles;

namespace Cerneala.Scene2D.Packages;

internal sealed class LocalPackageRangeReader : IScene2DPackageRangeReader
{
    private readonly SafeFileHandle catalog;
    private readonly SafeFileHandle payloads;

    private LocalPackageRangeReader(SafeFileHandle catalog, SafeFileHandle payloads)
    {
        this.catalog = catalog;
        this.payloads = payloads;
    }

    internal static LocalPackageRangeReader Open(string root)
    {
        SafeFileHandle catalog = File.OpenHandle(PackageFiles.ExistingFile(root, PackageFiles.CatalogName),
            FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous | FileOptions.RandomAccess);
        try
        {
            SafeFileHandle payloads = File.OpenHandle(PackageFiles.ExistingFile(root, PackageFiles.DataName),
                FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.Asynchronous | FileOptions.RandomAccess);
            return new(catalog, payloads);
        }
        catch
        {
            catalog.Dispose();
            throw;
        }
    }

    public ValueTask<long> GetLengthAsync(Scene2DPackagePart part, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(RandomAccess.GetLength(Handle(part)));
    }

    public ValueTask<int> ReadAsync(Scene2DPackagePart part, long offset, Memory<byte> destination,
        CancellationToken cancellationToken = default) =>
        RandomAccess.ReadAsync(Handle(part), destination, offset, cancellationToken);

    public ValueTask DisposeAsync()
    {
        catalog.Dispose();
        payloads.Dispose();
        return ValueTask.CompletedTask;
    }

    private SafeFileHandle Handle(Scene2DPackagePart part) => part switch
    {
        Scene2DPackagePart.Catalog => catalog,
        Scene2DPackagePart.Payloads => payloads,
        _ => throw new ArgumentOutOfRangeException(nameof(part))
    };
}
