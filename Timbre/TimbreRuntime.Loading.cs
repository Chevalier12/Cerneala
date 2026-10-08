using Cerneala.Timbre.Catalog;
using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public sealed partial class TimbreRuntime
{
    private const int UnknownLengthStepFrames = 64 * 1024;

    private async Task LoadAsync(TimbrePlayback playback, object? key)
    {
        TimbreSound sound = playback.Sound;
        try
        {
            TimbreFeed feed;
            if (sound.Loading != TimbreLoading.Streaming && key is not null && cache.TryAcquire(key, out TimbrePayloadCache.Entry? cached))
            {
                feed = new PreloadedFeed(cache, cached, playback.Loop);
            }
            else
            {
                (TimbreReader reader, TimbreMemoryBudget budget) = OpenReader(sound.Source);
                bool readerTransferred = false;
                try
                {
                    if (ShouldPreload(sound.Loading, reader.LengthFrames))
                    {
                        TimbrePayloadCache.Entry entry = await LoadPayloadAsync(reader, key, sound.Source.Name, pin: true, CancellationToken.None).ConfigureAwait(false);
                        feed = new PreloadedFeed(cache, entry, playback.Loop);
                    }
                    else
                    {
                        feed = CreateStreamingFeed(playback, reader, budget);
                        readerTransferred = true;
                    }
                }
                finally
                {
                    if (!readerTransferred)
                    {
                        ReleaseReader(reader, budget);
                    }
                }
            }

            bool attached;
            lock (Sync)
            {
                attached = playback.TryAttachFeedLocked(feed, fromLoader: true);
            }

            if (!attached)
            {
                StopFeed(playback, feed);
            }

            SignalMixer();
        }
        catch (Exception exception)
        {
            TimbreException error = Classify(exception, sound.Source.Name);
            lock (Sync)
            {
                playback.FailLocked(error);
                playback.LoaderFailedLocked();
            }

            SignalMixer();
        }
        finally
        {
            Interlocked.Decrement(ref pendingLoads);
        }
    }

    private async Task PrepareCoreAsync(TimbreSound sound, CancellationToken cancellationToken)
    {
        try
        {
            object? key = TryGetCacheKey(sound.Source);
            if (sound.Loading != TimbreLoading.Streaming && key is not null && cache.Contains(key))
            {
                return;
            }

            (TimbreReader reader, TimbreMemoryBudget budget) = OpenReader(sound.Source);
            try
            {
                if (ShouldPreload(sound.Loading, reader.LengthFrames))
                {
                    await LoadPayloadAsync(reader, key, sound.Source.Name, pin: false, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ReleaseReader(reader, budget);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw Classify(exception, sound.Source.Name);
        }
    }

    // The budget lives as long as the reader: it is closed, releasing every
    // reservation still held, only after the reader has been disposed.
    private (TimbreReader Reader, TimbreMemoryBudget Budget) OpenReader(TimbreSource source)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        TimbreMemoryBudget budget = new(memory, source.Name);
        TimbreReader reader;
        try
        {
            reader = source.Open(baseDirectory, budget);
        }
        catch
        {
            budget.Close();
            throw;
        }

        Interlocked.Increment(ref liveReaders);
        return (reader, budget);
    }

    private void ReleaseReader(TimbreReader reader, TimbreMemoryBudget budget)
    {
        try
        {
            reader.Dispose();
        }
        finally
        {
            budget.Close();
            Interlocked.Decrement(ref liveReaders);
        }
    }

    private bool ShouldPreload(TimbreLoading loading, long? lengthFrames) => loading switch
    {
        TimbreLoading.Preload => true,
        TimbreLoading.Streaming => false,
        _ => lengthFrames is long length && length * TimbreCatalog.BytesPerFrame <= autoPreloadMaxBytes
    };

    private StreamingFeed CreateStreamingFeed(TimbrePlayback playback, TimbreReader reader, TimbreMemoryBudget budget)
    {
        if (!memory.TryReserve(StreamingFeed.BufferBytes))
        {
            throw new TimbreException(
                TimbreErrorKind.ResourceLimitExceeded,
                $"Streaming '{playback.Sound.Source.Name}' needs {StreamingFeed.BufferBytes} bytes of buffers; {memory.Reserved} of the {memory.Limit}-byte streaming memory limit are reserved.");
        }

        Interlocked.Add(ref streamingBytes, StreamingFeed.BufferBytes);

        Interlocked.Increment(ref liveSourcePumps);
        StreamingFeed feed = new(
            reader,
            playback.Sound.Source.Name,
            playback.Loop,
            error => FailFromWorker(playback, error),
            SignalMixer,
            () =>
            {
                budget.Close();
                Interlocked.Decrement(ref liveReaders);
                Interlocked.Decrement(ref liveSourcePumps);
                Interlocked.Add(ref streamingBytes, -StreamingFeed.BufferBytes);
                memory.Release(StreamingFeed.BufferBytes);
            },
            pumpDispatcher);
        try
        {
            feed.Start();
        }
        catch
        {
            // The runtime was disposed meanwhile: the caller still owns the reader.
            Interlocked.Decrement(ref liveSourcePumps);
            Interlocked.Add(ref streamingBytes, -StreamingFeed.BufferBytes);
            memory.Release(StreamingFeed.BufferBytes);
            throw;
        }

        return feed;
    }

    // Decodes a whole source into the cache, reserving the bytes before the
    // allocation so an oversized source is refused without allocating it.
    private async Task<TimbrePayloadCache.Entry> LoadPayloadAsync(
        TimbreReader reader,
        object? key,
        string sourceName,
        bool pin,
        CancellationToken cancellationToken)
    {
        object cacheKey = key ?? new object();
        if (reader.LengthFrames is long known)
        {
            long bytes = checked(known * TimbreCatalog.BytesPerFrame);
            Reserve(bytes, sourceName);
            try
            {
                float[] samples = new float[known * TimbreCatalog.ChannelCount];
                long read = await ReadIntoAsync(reader, samples, 0, known, sourceName, cancellationToken).ConfigureAwait(false);
                if (read != known)
                {
                    throw new TimbreException(TimbreErrorKind.InvalidData, $"Timbre source '{sourceName}' ended after {read} of its {known} declared frames.");
                }

                return cache.Add(cacheKey, samples, known, bytes, pin);
            }
            catch
            {
                cache.Unreserve(bytes);
                throw;
            }
        }

        List<float[]> parts = [];
        long total = 0;
        long reserved = 0;
        try
        {
            while (true)
            {
                long allowance = maxPreloadBytes - reserved;
                long stepFrames = Math.Min(UnknownLengthStepFrames, allowance / TimbreCatalog.BytesPerFrame);
                if (stepFrames <= 0)
                {
                    throw PreloadLimit(sourceName, reserved + TimbreCatalog.BytesPerFrame);
                }

                Reserve(stepFrames * TimbreCatalog.BytesPerFrame, sourceName);
                reserved += stepFrames * TimbreCatalog.BytesPerFrame;
                float[] part = new float[stepFrames * TimbreCatalog.ChannelCount];
                long frames = await ReadIntoAsync(reader, part, 0, stepFrames, sourceName, cancellationToken).ConfigureAwait(false);
                parts.Add(part);
                total += frames;
                if (frames < stepFrames)
                {
                    break;
                }
            }

            float[] samples = new float[total * TimbreCatalog.ChannelCount];
            long offset = 0;
            foreach (float[] part in parts)
            {
                int count = (int)Math.Min(part.Length, samples.Length - offset);
                Array.Copy(part, 0, samples, offset, count);
                offset += count;
            }

            long bytes = total * TimbreCatalog.BytesPerFrame;
            cache.Unreserve(reserved - bytes);
            reserved = bytes;
            return cache.Add(cacheKey, samples, total, bytes, pin);
        }
        catch
        {
            cache.Unreserve(reserved);
            throw;
        }
    }

    private void Reserve(long bytes, string sourceName)
    {
        if (bytes > maxPreloadBytes)
        {
            throw PreloadLimit(sourceName, bytes);
        }

        if (!cache.TryReserve(bytes))
        {
            throw new TimbreException(
                TimbreErrorKind.ResourceLimitExceeded,
                $"Preloading '{sourceName}' needs {bytes} bytes; the sound cache cannot admit it without evicting payloads in use.");
        }
    }

    private TimbreException PreloadLimit(string sourceName, long bytes) => new(
        TimbreErrorKind.ResourceLimitExceeded,
        $"Preloading '{sourceName}' needs at least {bytes} bytes of decoded PCM; the preload limit is {maxPreloadBytes} bytes.");

    // Reads until `frames` frames or the end of the source; returns frames read.
    private static async Task<long> ReadIntoAsync(
        TimbreReader reader,
        float[] samples,
        long startFrame,
        long frames,
        string sourceName,
        CancellationToken cancellationToken)
    {
        long read = 0;
        bool previousWasEmpty = false;
        while (read < frames)
        {
            int request = (int)Math.Min(frames - read, int.MaxValue / TimbreCatalog.ChannelCount);
            Memory<float> destination = samples.AsMemory((int)((startFrame + read) * TimbreCatalog.ChannelCount), request * TimbreCatalog.ChannelCount);
            ValueTask<TimbreReadResult> pending = reader.ReadAsync(destination, cancellationToken);
            bool completedSynchronously = pending.IsCompleted;
            TimbreReadResult result = await pending.ConfigureAwait(false);
            if (result.Frames > request)
            {
                throw new TimbreException(TimbreErrorKind.InvalidData, $"Reader for '{sourceName}' returned more frames than requested.");
            }

            EnsureFinite(samples.AsSpan((int)((startFrame + read) * TimbreCatalog.ChannelCount), result.Frames * TimbreCatalog.ChannelCount), sourceName);
            read += result.Frames;
            if (result.EndOfSource)
            {
                break;
            }

            if (result.Frames == 0)
            {
                if (previousWasEmpty && completedSynchronously)
                {
                    throw new TimbreException(TimbreErrorKind.InvalidData, $"Reader for '{sourceName}' returned no data again synchronously instead of waiting.");
                }

                previousWasEmpty = true;
            }
            else
            {
                previousWasEmpty = false;
            }
        }

        return read;
    }

    private static void EnsureFinite(ReadOnlySpan<float> samples, string sourceName)
    {
        foreach (float sample in samples)
        {
            if (!float.IsFinite(sample))
            {
                throw new TimbreException(TimbreErrorKind.InvalidData, $"Reader for '{sourceName}' produced a non-finite sample.");
            }
        }
    }
}
