namespace Cerneala.Timbre.Engine;

// Content-detected decoder seam between file/stream sources and the canonical
// SoundReader. The core registers no decoders; the decoding plan adds them.
internal interface ISoundDecoder
{
    int HeaderLength { get; }

    bool CanDecode(ReadOnlySpan<byte> header);

    SoundReader Create(Stream stream, string sourceName);
}

internal static class SoundDecoders
{
    private static readonly ISoundDecoder[] Registered = [];

    internal static SoundReader Open(Stream stream, string sourceName)
    {
        try
        {
            foreach (ISoundDecoder decoder in Registered)
            {
                byte[] header = new byte[decoder.HeaderLength];
                stream.Position = 0;
                int length = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
                stream.Position = 0;
                if (decoder.CanDecode(header.AsSpan(0, length)))
                {
                    return decoder.Create(stream, sourceName);
                }
            }
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
}
