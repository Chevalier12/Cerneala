using Cerneala.Timbre.Decoding;

namespace Cerneala.Timbre.Engine;

// Content-detected decoder seam between file/stream sources and the canonical
// SoundReader. The first decoder that recognizes the content owns it: a
// failure is reported, never retried with another decoder.
internal interface ISoundDecoder
{
    int HeaderLength { get; }

    bool CanDecode(ReadOnlySpan<byte> header);

    SoundReader Create(Stream stream, string sourceName, SoundMemoryBudget budget);
}

internal static class SoundDecoders
{
    private static readonly ISoundDecoder[] Registered = SoundFormats.All;

    internal static SoundReader Open(Stream stream, string sourceName, SoundMemoryBudget budget) =>
        Open(stream, sourceName, budget, Registered);

    internal static SoundReader Open(Stream stream, string sourceName, SoundMemoryBudget budget, IReadOnlyList<ISoundDecoder> decoders)
    {
        try
        {
            foreach (ISoundDecoder decoder in decoders)
            {
                byte[] header = new byte[decoder.HeaderLength];
                stream.Position = 0;
                int length = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
                stream.Position = 0;
                if (decoder.CanDecode(header.AsSpan(0, length)))
                {
                    return decoder.Create(stream, sourceName, budget);
                }
            }
        }
        catch (Exception exception) when (IsDecoderUnavailable(exception))
        {
            stream.Dispose();
            throw new SoundException(SoundErrorKind.UnsupportedFormat, $"The decoder for '{sourceName}' is unavailable: {exception.Message}", exception);
        }
        catch (IOException exception)
        {
            stream.Dispose();
            throw new SoundException(SoundErrorKind.SourceUnavailable, $"Sound source '{sourceName}' could not be read.", exception);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        stream.Dispose();
        throw new SoundException(SoundErrorKind.UnsupportedFormat, $"No available decoder recognizes the content of '{sourceName}'.");
    }

    // A decoder assembly that cannot be loaded is a missing codec, not damaged audio.
    private static bool IsDecoderUnavailable(Exception exception) =>
        exception is FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException or MissingMethodException;
}
