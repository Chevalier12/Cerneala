using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public abstract class SoundSource
{
    // FileStream read buffer of a file source, reserved before the file opens.
    internal const int FileBufferBytes = 64 * 1024;

    private protected SoundSource(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public static SoundSource FromFile(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A sound file path cannot be empty.", nameof(path));
        }

        if (path.Contains("://", StringComparison.Ordinal))
        {
            throw new ArgumentException("Sound sources must be local files or seekable stream factories; URIs are not supported.", nameof(path));
        }

        return new FileSource(path);
    }

    public static SoundSource FromStream(Func<Stream> openStream, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openStream);
        return new StreamSource(_ => openStream(), openStream, name ?? "stream");
    }

    public static SoundSource FromStream(Func<SoundMemoryBudget, Stream> openStream, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openStream);
        return new StreamSource(openStream, openStream, name ?? "stream");
    }

    public static SoundSource FromReader(Func<SoundReader> openReader, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openReader);
        return new ReaderSource(_ => openReader(), openReader, name ?? "reader");
    }

    public static SoundSource FromReader(Func<SoundMemoryBudget, SoundReader> openReader, string? name = null)
    {
        ArgumentNullException.ThrowIfNull(openReader);
        return new ReaderSource(openReader, openReader, name ?? "reader");
    }

    public static implicit operator SoundSource(string path) => FromFile(path);

    public override string ToString() => Name;

    // Identity of the decoded payload in the runtime preload cache.
    internal abstract object GetCacheKey(string baseDirectory);

    // Runs on a background worker; never on the UI thread or an audio callback.
    // Live memory is reserved through `budget` before it is allocated.
    internal abstract SoundReader Open(string baseDirectory, SoundMemoryBudget budget);

    private static SoundException Unavailable(string message, Exception? inner = null) =>
        new(SoundErrorKind.SourceUnavailable, message, inner);

    private sealed class FileSource(string path) : SoundSource(path)
    {
        internal override object GetCacheKey(string baseDirectory) => new FileKey(Resolve(baseDirectory));

        internal override SoundReader Open(string baseDirectory, SoundMemoryBudget budget)
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
                throw Unavailable($"Sound file '{fullPath}' could not be opened.", exception);
            }

            return SoundDecoders.Open(stream, fullPath, budget);
        }

        private string Resolve(string baseDirectory) => Path.GetFullPath(Path.Combine(baseDirectory, Name));
    }

    private sealed class StreamSource(Func<SoundMemoryBudget, Stream> openStream, object cacheKey, string name) : SoundSource(name)
    {
        internal override object GetCacheKey(string baseDirectory) => cacheKey;

        internal override SoundReader Open(string baseDirectory, SoundMemoryBudget budget)
        {
            Stream stream;
            try
            {
                stream = openStream(budget) ?? throw Unavailable($"Stream factory for '{Name}' returned null.");
            }
            catch (SoundException)
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
                throw Unavailable($"Sound stream '{Name}' must be readable and seekable.");
            }

            return SoundDecoders.Open(stream, Name, budget);
        }
    }

    private sealed class ReaderSource(Func<SoundMemoryBudget, SoundReader> openReader, object cacheKey, string name) : SoundSource(name)
    {
        internal override object GetCacheKey(string baseDirectory) => cacheKey;

        internal override SoundReader Open(string baseDirectory, SoundMemoryBudget budget)
        {
            try
            {
                return openReader(budget) ?? throw Unavailable($"Reader factory for '{Name}' returned null.");
            }
            catch (SoundException)
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
