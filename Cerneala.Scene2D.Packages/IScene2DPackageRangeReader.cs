namespace Cerneala.Scene2D.Packages;

/// <summary>The two immutable byte streams of a prepared CPV2 scene package.</summary>
public enum Scene2DPackagePart
{
    Catalog,
    Payloads
}

/// <summary>Supplies independent byte ranges from one immutable CPV2 package revision.</summary>
public interface IScene2DPackageRangeReader : IAsyncDisposable
{
    ValueTask<long> GetLengthAsync(Scene2DPackagePart part, CancellationToken cancellationToken = default);

    ValueTask<int> ReadAsync(Scene2DPackagePart part, long offset, Memory<byte> destination,
        CancellationToken cancellationToken = default);
}
