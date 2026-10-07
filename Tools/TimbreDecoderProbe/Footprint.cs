using Cerneala.Timbre.Decoding;

namespace Cerneala.Timbre.Probe;

// Live heap attributable to one open reader (stream + demuxer + decoder +
// canonical converter), measured as the full-GC heap delta while only the
// converter and one block buffer are held. Short and long fixtures of the
// same codec show whether the live state grows with duration, including
// after seeks and loop rewinds. Managed heap only: native, CLR/OS and
// stack costs are outside this scope.
internal static class Footprint
{
    internal static IEnumerable<object> Measure(string corpus)
    {
        (string File, Func<Stream, string, DecodedSource> Open)[] cases =
        [
            ("mp3-mpeg1-44100-stereo-cbr128.mp3", (s, n) => new Mp3Source(s, n)),
            ("mp3-mpeg2-22050-mono-vbr.mp3", (s, n) => new Mp3Source(s, n)),
            ("mp3-long-22050-mono-cbr64.mp3", (s, n) => new Mp3Source(s, n)),
            ("vorbis-44100-stereo-q4.ogg", (s, n) => new VorbisSource(s, n)),
            ("vorbis-22050-mono-q2.ogg", (s, n) => new VorbisSource(s, n)),
            ("vorbis-192000-stereo-q2.ogg", (s, n) => new VorbisSource(s, n)),
            ("vorbis-long-22050-mono-q2.ogg", (s, n) => new VorbisSource(s, n)),
            ("opus-stereo-48000-96k.opus", (s, n) => new OpusSource(s, n)),
            ("opus-mono-16000-24k.opus", (s, n) => new OpusSource(s, n)),
            ("opus-long-mono-32k.opus", (s, n) => new OpusSource(s, n)),
        ];

        float[] block = new float[2048 * 2];
        foreach ((string file, Func<Stream, string, DecodedSource> open) in cases)
        {
            string path = Path.Combine(corpus, file);
            // The first pass includes one-time static decoder tables; the second
            // is the live state of one more reader.
            yield return new { pass = "cold", result = MeasureOne(path, open, block) };
            yield return new { pass = "warm", result = MeasureOne(path, open, block) };
        }

        string wavDirectory = Path.Combine(Path.GetTempPath(), "timbre-probe-wav");
        foreach (string wav in new[] { "wav-44100-stereo-s16-2s.wav", "wav-22050-mono-s16-2s.wav", "wav-22050-mono-s16-300s.wav" })
        {
            string path = Path.Combine(wavDirectory, wav);
            if (File.Exists(path))
            {
                yield return new { pass = "cold", result = MeasureOne(path, (s, n) => new WavSource(s, n), block) };
                yield return new { pass = "warm", result = MeasureOne(path, (s, n) => new WavSource(s, n), block) };
            }
        }
    }

    private static object MeasureOne(string path, Func<Stream, string, DecodedSource> open, float[] block)
    {
        long baseline = Heap();
        CountingStream stream = new(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096));
        CanonicalConverter converter = new(open(stream, path));
        long afterOpen = Heap() - baseline;
        long frames = 0;
        long bytesAtFirstSecond = 0;
        long afterFirstSecond = 0;
        long peak = afterOpen;
        int read;
        while ((read = converter.Read(block, CancellationToken.None)) > 0)
        {
            frames += read;
            if (afterFirstSecond == 0 && frames >= 48000)
            {
                afterFirstSecond = Heap() - baseline;
                bytesAtFirstSecond = stream.BytesRead;
                peak = Math.Max(peak, afterFirstSecond);
            }

            if (frames % (48000 * 30) < read)
            {
                peak = Math.Max(peak, Heap() - baseline);
            }
        }

        long afterDecode = Heap() - baseline;
        long length = converter.LengthFrames ?? frames;
        foreach (long target in new[] { length / 3, (length * 9) / 10, 0, length / 2, 0 })
        {
            converter.Seek(target, CancellationToken.None);
            for (int index = 0; index < 4 && converter.Read(block, CancellationToken.None) > 0; index++)
            {
            }

            peak = Math.Max(peak, Heap() - baseline);
        }

        // Two full loop passes.
        for (int pass = 0; pass < 2; pass++)
        {
            converter.Seek(0, CancellationToken.None);
            while (converter.Read(block, CancellationToken.None) > 0)
            {
            }

            peak = Math.Max(peak, Heap() - baseline);
        }

        long afterSeeksAndLoops = Heap() - baseline;
        converter.Dispose();
        converter = null!;
        long afterDispose = Heap() - baseline;
        return new
        {
            file = Path.GetFileName(path),
            fileBytes = new FileInfo(path).Length,
            seconds = Math.Round(frames / 48000.0, 1),
            liveBytes = new { afterOpen, afterFirstSecond, afterDecode, afterSeeksAndLoops, peak, afterDispose },
            io = new { bytesAtFirstSecond, totalBytesRead = stream.BytesRead, stream.ReadCalls, stream.SeekCalls, streamDisposed = stream.Disposed },
        };
    }

    private static long Heap()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        return GC.GetTotalMemory(forceFullCollection: true);
    }
}
