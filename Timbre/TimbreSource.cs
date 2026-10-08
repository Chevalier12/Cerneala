using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public abstract class TimbreSource
{
    // FileStream read buffer of a file source, reserved before the file opens.
    internal const int FileBufferBytes = 64 * 1024;

    private protected TimbreSource(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static TimbreSource FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A sound file path cannot be empty.", nameof(path));
        }

        if (path.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException("Timbre sources must be local files or seekable stream factories; URIs are not supported.", nameof(path));
        }

        return new FileSource(path);
    }

    public static TimbreSource FromStream(Func<Stream> openStream, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openStream);
        return new StreamSource(_ => openStream(), openStream, name ?? "stream");
    }

    public static TimbreSource FromStream(Func<TimbreMemoryBudget, Stream> openStream, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openStream);
        return new StreamSource(openStream, openStream, name ?? "stream");
    }

    public static TimbreSource FromReader(Func<TimbreReader> openReader, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openReader);
        return new ReaderSource(_ => openReader(), openReader, name ?? "reader");
    }

    public static TimbreSource FromReader(Func<TimbreMemoryBudget, TimbreReader> openReader, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openReader);
        return new ReaderSource(openReader, openReader, name ?? "reader");
    }

    public static implicit operator TimbreSource(string path) => FromFile(path);

    public override string ToString() => Name;

    // Identity of the decoded payload in the runtime preload cache.
    internal abstract object GetCacheKey(string baseDirectory);

    // Runs on a background worker; never on the UI thread or an audio callback.
    // Live memory is reserved through `budget` before it is allocated.
    internal abstract TimbreReader Open(string baseDirectory, TimbreMemoryBudget budget);

    private static TimbreException Unavailable(string message, Exception? inner = null) =>
        new(TimbreErrorKind.SourceUnavailable, message, inner);

    private sealed class FileSource(string path) : TimbreSource(path)
    {
        internal override object GetCacheKey(string baseDirectory) => new FileKey(Resolve(baseDirectory));

        internal override TimbreReader Open(string baseDirectory, TimbreMemoryBudget budget)
        {
            string fullPath = Resolve(baseDirectory);
            budget.Reserve(FileBufferBytes);
            FileStream stream;
            try
            {
                stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, FileBufferBytes);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                throw Unavailable($"Timbre file '{fullPath}' could not be opened.", exception);
            }

            return TimbreDecoders.Open(stream, fullPath, budget);
        }

        private string Resolve(string baseDirectory) => Path.GetFullPath(Path.Combine(baseDirectory, Name));
    }

    private sealed class StreamSource(Func<TimbreMemoryBudget, Stream> openStream, object cacheKey, string name) : TimbreSource(name)
    {
        internal override object GetCacheKey(string baseDirectory) => cacheKey;

        internal override TimbreReader Open(string baseDirectory, TimbreMemoryBudget budget)
        {
            Stream stream;
            try
            {
                stream = openStream(budget) ?? throw Unavailable($"Stream factory for '{Name}' returned null.");
            }
            catch (TimbreException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Unavailable($"Stream factory for '{Name}' failed.", exception);
            }

            if (!stream.CanRead || !stream.CanSeek)
            {
                stream.Dispose();
                throw Unavailable($"Timbre stream '{Name}' must be readable and seekable.");
            }

            return TimbreDecoders.Open(stream, Name, budget);
        }
    }

    private sealed class ReaderSource(Func<TimbreMemoryBudget, TimbreReader> openReader, object cacheKey, string name) : TimbreSource(name)
    {
        internal override object GetCacheKey(string baseDirectory) => cacheKey;

        internal override TimbreReader Open(string baseDirectory, TimbreMemoryBudget budget)
        {
            try
            {
                return openReader(budget) ?? throw Unavailable($"Reader factory for '{Name}' returned null.");
            }
            catch (TimbreException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw Unavailable($"Reader factory for '{Name}' failed.", exception);
            }
        }
    }

    private sealed record FileKey(string FullPath)
    {
        public bool Equals(FileKey? other) =>
            other is not null && string.Equals(FullPath, other.FullPath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        public override int GetHashCode() =>
            OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase.GetHashCode(FullPath) : StringComparer.Ordinal.GetHashCode(FullPath);
    }
}
