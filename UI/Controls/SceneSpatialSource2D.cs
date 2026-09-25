using Cerneala.Drawing;

namespace Cerneala.UI.Controls;

/// <summary>Describes a spatial payload without materializing that payload.</summary>
internal sealed class SceneSpatialEntry2D
{
    public SceneSpatialEntry2D(
        string id,
        DrawRect bounds,
        bool isSimulated = false,
        long version = 1)
        : this(id, bounds, bounds, isSimulated, version)
    {
    }

    public SceneSpatialEntry2D(
        string id,
        DrawRect bounds,
        DrawRect? collisionBounds,
        bool isSimulated = false,
        long version = 1)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A spatial entry requires a stable identity.", nameof(id));
        }
        ValidateBounds(bounds, nameof(bounds));
        if (collisionBounds is DrawRect collision) { ValidateBounds(collision, nameof(collisionBounds)); }
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);
        Id = id;
        Bounds = bounds;
        CollisionBounds = collisionBounds;
        IsSimulated = isSimulated;
        Version = version;
    }

    internal static void ValidateBounds(DrawRect bounds, string parameterName)
    {
        if (!float.IsFinite(bounds.X) || !float.IsFinite(bounds.Y) ||
            !float.IsFinite(bounds.Width) || !float.IsFinite(bounds.Height) ||
            bounds.Width < 0 || bounds.Height < 0 ||
            !float.IsFinite(bounds.Right) || !float.IsFinite(bounds.Bottom))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Spatial bounds must have finite endpoints and nonnegative dimensions.");
        }
    }

    public string Id { get; }

    public DrawRect Bounds { get; }

    public DrawRect? CollisionBounds { get; }

    public bool IsSimulated { get; }

    public long Version { get; }
}

/// <summary>Owns one acquisition of a payload, not every use of that payload.</summary>
internal sealed class SceneSpatialLease2D<T> : IDisposable where T : class
{
    private Acquisition? acquisition;

    public SceneSpatialLease2D(T value, Action<T>? release = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        acquisition = new(value, release);
    }

    public T Value => Volatile.Read(ref acquisition)?.Value ??
        throw new ObjectDisposedException(nameof(SceneSpatialLease2D<T>));

    public void Dispose()
    {
        Acquisition? released = Interlocked.Exchange(ref acquisition, null);
        released?.Release?.Invoke(released.Value);
    }

    private sealed record Acquisition(T Value, Action<T>? Release);
}

/// <summary>Separates an immutable spatial catalog from asynchronously acquired payloads.</summary>
internal interface ISceneSpatialSource2D<T> where T : class
{
    IReadOnlyList<SceneSpatialEntry2D> Entries { get; }

    event EventHandler? Changed;

    ValueTask<SceneSpatialLease2D<T>> LoadAsync(
        SceneSpatialEntry2D entry,
        CancellationToken cancellationToken = default);
}

/// <summary>Adapts application-owned data loading to the spatial-source contract.</summary>
internal sealed class SceneSpatialSource2D<T> : ISceneSpatialSource2D<T> where T : class
{
    private readonly Func<SceneSpatialEntry2D, CancellationToken, ValueTask<SceneSpatialLease2D<T>>> load;
    private Catalog catalog;

    public SceneSpatialSource2D(
        IEnumerable<SceneSpatialEntry2D> entries,
        Func<SceneSpatialEntry2D, CancellationToken, ValueTask<SceneSpatialLease2D<T>>> load)
    {
        ArgumentNullException.ThrowIfNull(load);
        catalog = CreateCatalog(entries);
        this.load = load;
    }

    public IReadOnlyList<SceneSpatialEntry2D> Entries => Volatile.Read(ref catalog).Entries;

    public event EventHandler? Changed;

    public void SetEntries(IEnumerable<SceneSpatialEntry2D> entries)
    {
        Catalog replacement = CreateCatalog(entries);
        Interlocked.Exchange(ref catalog, replacement);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public ValueTask<SceneSpatialLease2D<T>> LoadAsync(
        SceneSpatialEntry2D entry,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        cancellationToken.ThrowIfCancellationRequested();
        Catalog snapshot = Volatile.Read(ref catalog);
        if (!snapshot.ById.TryGetValue(entry.Id, out SceneSpatialEntry2D? current) ||
            current.Version != entry.Version)
        {
            throw new InvalidOperationException("The requested spatial entry is not part of the current source revision.");
        }
        return load(entry, cancellationToken);
    }

    private static Catalog CreateCatalog(IEnumerable<SceneSpatialEntry2D> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        List<SceneSpatialEntry2D> copied = [];
        Dictionary<string, SceneSpatialEntry2D> byId = new(StringComparer.Ordinal);
        foreach (SceneSpatialEntry2D entry in entries)
        {
            ArgumentNullException.ThrowIfNull(entry);
            if (!byId.TryAdd(entry.Id, entry))
            {
                throw new ArgumentException($"Spatial identity '{entry.Id}' occurs more than once.", nameof(entries));
            }
            copied.Add(entry);
        }
        return new(Array.AsReadOnly(copied.ToArray()), byId);
    }

    private sealed record Catalog(
        IReadOnlyList<SceneSpatialEntry2D> Entries,
        Dictionary<string, SceneSpatialEntry2D> ById);
}
