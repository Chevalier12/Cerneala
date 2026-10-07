using System.Diagnostics;
using Cerneala.Timbre.Catalog;
using Cerneala.Timbre.Dsp;
using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public sealed partial class SoundRuntime
{
    private const int Block = TimbreCatalog.BlockFrames;
    private const int QueueBudget = TimbreCatalog.OutputQueueBudgetFrames;

    private int deviceLost;
    private Exception? deviceLostError;

    private void ReportDeviceLost(Exception? error)
    {
        Volatile.Write(ref deviceLostError, error);
        Interlocked.Exchange(ref deviceLost, 1);
        wake.Set();
    }

    // The single producer of the final mix. All decisions about voices are
    // taken under Sync at the start of an iteration; reading, DSP, and mixing
    // run outside it on buffers owned by this thread.
    private void MixerLoop()
    {
        float[] mix = new float[Block * TimbreCatalog.ChannelCount];
        float[] voiceBuffer = new float[Block * TimbreCatalog.ChannelCount];
        List<SoundPlayback> voices = new(maxVoices);
        List<SoundPlayback> blockVoices = new(maxVoices);
        List<(SoundPlayback Playback, SoundFeed? Feed)> releases = new(maxVoices);
        List<TaskCompletionSource> syncs = [];
        long consumed = 0;
        try
        {
            while (true)
            {
                bool exiting;
                bool lostDevice;
                lock (Sync)
                {
                    exiting = disposed;
                    voices.AddRange(adopted);
                    adopted.Clear();
                    lostDevice = Interlocked.Exchange(ref deviceLost, 0) != 0 && outputOpen;
                    if (lostDevice)
                    {
                        FailLiveLocked(new SoundException(SoundErrorKind.DeviceUnavailable, "The audio device was lost.", Volatile.Read(ref deviceLostError)));
                    }

                    ApplyControlsLocked(voices, releases);
                    syncs.AddRange(syncRequests);
                    syncRequests.Clear();
                }

                ReleaseVoices(releases);
                if (exiting)
                {
                    break;
                }

                if (lostDevice)
                {
                    CloseOutput();
                }

                if (voices.Count > 0 && !outputOpen && !TryOpenOutput())
                {
                    continue;
                }

                int queued = 0;
                if (outputOpen)
                {
                    try
                    {
                        queued = output!.QueuedFrames;
                    }
                    catch (Exception exception)
                    {
                        ReportDeviceLost(exception);
                        continue;
                    }

                    consumed = Math.Max(consumed, Interlocked.Read(ref framesSubmitted) - queued);
                    Interlocked.Exchange(ref framesConsumed, consumed);
                    if (CompleteDrained(voices, consumed))
                    {
                        continue; // release the completed voices before waiting
                    }
                }

                // Compared without addition: an output may report any queue depth.
                if (outputOpen && queued <= QueueBudget - Block)
                {
                    ISoundBlockObserver? observer = BlockObserver;
                    long started = observer is null ? 0 : Stopwatch.GetTimestamp();
                    long allocatedBefore = observer is null ? 0 : GC.GetAllocatedBytesForCurrentThread();
                    if (SelectBlockVoices(voices, blockVoices) && !DeferForSource(blockVoices, queued))
                    {
                        bool submit = MixBlock(blockVoices, mix, voiceBuffer);
                        if (submit)
                        {
                            try
                            {
                                output!.Submit(mix);
                            }
                            catch (Exception exception)
                            {
                                blockVoices.Clear();
                                ReportDeviceLost(exception);
                                continue;
                            }

                            Interlocked.Add(ref framesSubmitted, Block);
                            Interlocked.Increment(ref blocksMixed);
                            StampFirstQueued(blockVoices);
                            observer?.OnBlockSubmitted(
                                Stopwatch.GetTimestamp() - started,
                                GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);
                        }

                        blockVoices.Clear();
                        continue;
                    }
                }

                foreach (TaskCompletionSource sync in syncs)
                {
                    sync.TrySetResult();
                }

                syncs.Clear();
                wake.WaitOne();
            }
        }
        finally
        {
            lock (Sync)
            {
                voices.AddRange(adopted);
                adopted.Clear();
                foreach (SoundPlayback playback in voices)
                {
                    releases.Add((playback, playback.RequestReleaseLocked()));
                }

                syncs.AddRange(syncRequests);
                syncRequests.Clear();
            }

            ReleaseVoices(releases);
            CloseOutput();
            foreach (TaskCompletionSource sync in syncs)
            {
                sync.TrySetResult();
            }
        }
    }

    // Caller holds Sync.
    private void ApplyControlsLocked(List<SoundPlayback> voices, List<(SoundPlayback, SoundFeed?)> releases)
    {
        for (int index = voices.Count - 1; index >= 0; index--)
        {
            SoundPlayback playback = voices[index];
            if (playback.IsTerminal)
            {
                voices.RemoveAt(index);
                releases.Add((playback, playback.RequestReleaseLocked()));
                continue;
            }

            playback.ApplyControlsLocked(out bool seekRequested, out long target, out int generation);
            SoundFeed? feed = playback.FeedLocked;
            if (seekRequested)
            {
                feed!.RequestSeek(target, generation);
            }

            SoundVoice voice = playback.Render;
            if (voice.SeekPending && feed!.TryTakeSeekOutcome(voice.AppliedSeekGeneration, out Exception? error))
            {
                voice.SeekPending = false;
                if (error is null)
                {
                    // Only a successful manual seek resets the chain.
                    voice.ResetDsp();
                    voice.ProductionEnded = false;
                    playback.PublishPosition(feed.Position);
                }

                playback.CompleteSeekLocked(voice.AppliedSeekGeneration, error);
            }
        }
    }

    private bool CompleteDrained(List<SoundPlayback> voices, long consumed)
    {
        bool completed = false;
        lock (Sync)
        {
            foreach (SoundPlayback playback in voices)
            {
                SoundVoice voice = playback.Render;
                if (voice.ProductionEnded &&
                    playback.StateLocked == SoundPlaybackState.Playing &&
                    voice.EndFrame <= consumed)
                {
                    playback.CompleteLocked(SoundPlaybackState.Completed, null, voice.TailTruncated);
                    completed = true;
                }
            }
        }

        return completed;
    }

    private bool SelectBlockVoices(List<SoundPlayback> voices, List<SoundPlayback> blockVoices)
    {
        lock (Sync)
        {
            foreach (SoundPlayback playback in voices)
            {
                SoundVoice voice = playback.Render;
                if (playback.IsTerminal ||
                    playback.IsPausedLocked ||
                    voice.SeekPending ||
                    voice.ProductionEnded ||
                    playback.FeedLocked is not { HasData: true })
                {
                    continue;
                }

                playback.MarkStartedLocked();
                blockVoices.Add(playback);
            }
        }

        return blockVoices.Count > 0;
    }

    // A voice whose source has not yet delivered a whole block defers the block
    // while the output still holds at least one: padding is produced only once
    // the device would otherwise run dry, so a decoder that is merely late is
    // not turned into an underrun.
    private static bool DeferForSource(List<SoundPlayback> blockVoices, int queued)
    {
        if (queued < Block)
        {
            return false;
        }

        foreach (SoundPlayback playback in blockVoices)
        {
            if (!playback.Render.InTail && !playback.FeedLocked!.HasBlock(Block))
            {
                blockVoices.Clear();
                return true;
            }
        }

        return false;
    }

    private static void StampFirstQueued(List<SoundPlayback> blockVoices)
    {
        long now = Stopwatch.GetTimestamp();
        foreach (SoundPlayback playback in blockVoices)
        {
            playback.StampFirstQueued(now);
        }
    }

    // Produces one block of pre-volume voice PCM and returns the frames to mix.
    // Source PCM runs through the chain; after a non-looping end of source the
    // chain keeps processing silence until the tail criterion or cap. An
    // underrun pads silence without advancing the source or the DSP state.
    private int RenderVoice(SoundFeed feed, SoundVoice voice, float[] buffer, out bool underrun)
    {
        underrun = false;
        SoundDspChain? chain = voice.Chain;
        int sourceFrames = 0;
        if (!voice.InTail)
        {
            long wrapsBefore = feed.LoopWraps;
            sourceFrames = feed.Read(buffer, Block);
            if (feed.LoopWraps != wrapsBefore)
            {
                Interlocked.Add(ref loopWraps, feed.LoopWraps - wrapsBefore);
            }
        }

        if (!feed.Ended && !voice.InTail)
        {
            if (sourceFrames < Block)
            {
                underrun = true;
                Interlocked.Add(ref underrunFrames, Block - sourceFrames);
            }

            chain?.Process(buffer, sourceFrames, voice.Values);
            return sourceFrames;
        }

        if (chain is null)
        {
            voice.ProductionEnded = true;
            return sourceFrames;
        }

        buffer.AsSpan(sourceFrames * TimbreCatalog.ChannelCount).Clear();
        voice.InTail = true;
        chain.Process(buffer, Block, voice.Values);
        int window = chain.TailWindowFrames;
        for (int frame = sourceFrames; frame < Block; frame++)
        {
            float peak = Math.Max(Math.Abs(buffer[frame * 2]), Math.Abs(buffer[(frame * 2) + 1]));
            voice.SilentRun = peak < TimbreCatalog.TailSilenceThreshold ? voice.SilentRun + 1 : 0;
            voice.TailFrames++;
            if (voice.SilentRun >= window)
            {
                voice.ProductionEnded = true;
                break;
            }

            if (voice.TailFrames >= tailCapFrames)
            {
                // The cap cuts the echo: nothing past it is produced.
                buffer.AsSpan((frame + 1) * TimbreCatalog.ChannelCount).Clear();
                voice.ProductionEnded = true;
                voice.TailTruncated = true;
                break;
            }
        }

        return Block;
    }

    // Returns whether the block carries PCM or counted underrun padding.
    private bool MixBlock(List<SoundPlayback> blockVoices, float[] mix, float[] voiceBuffer)
    {
        Array.Clear(mix);
        bool submit = false;
        long blockEnd = Interlocked.Read(ref framesSubmitted) + Block;
        foreach (SoundPlayback playback in blockVoices)
        {
            SoundFeed feed = playback.FeedLocked!;
            SoundVoice voice = playback.Render;
            int frames = RenderVoice(feed, voice, voiceBuffer, out bool underrun);
            submit |= underrun;
            float gain = voice.Volume;
            int samples = frames * TimbreCatalog.ChannelCount;
            for (int index = 0; index < samples; index++)
            {
                mix[index] += voiceBuffer[index] * gain;
            }

            if (frames > 0)
            {
                submit = true;
                voice.LastContributedEnd = blockEnd;
            }

            playback.PublishPosition(feed.Position);
            playback.PublishLength(feed.LengthFrames);
            if (voice.ProductionEnded)
            {
                voice.EndFrame = voice.LastContributedEnd;
            }
        }

        long clipped = 0;
        for (int index = 0; index < mix.Length; index++)
        {
            float sample = mix[index];
            if (sample > 1f)
            {
                mix[index] = 1f;
                clipped++;
            }
            else if (sample < -1f)
            {
                mix[index] = -1f;
                clipped++;
            }
        }

        if (clipped > 0)
        {
            Interlocked.Add(ref clippedSamples, clipped);
        }

        return submit;
    }

    private bool TryOpenOutput()
    {
        if (output is null)
        {
            lock (Sync)
            {
                FailLiveLocked(new SoundException(SoundErrorKind.DeviceUnavailable, "No audio output is configured for this sound runtime."));
            }

            return false;
        }

        try
        {
            output.Open(client);
        }
        catch (Exception exception)
        {
            lock (Sync)
            {
                FailLiveLocked(new SoundException(SoundErrorKind.DeviceUnavailable, "The audio output could not be opened.", exception));
            }

            return false;
        }

        Interlocked.Exchange(ref deviceLost, 0);
        outputOpen = true;
        Interlocked.Increment(ref outputOpenCount);
        return true;
    }

    private void CloseOutput()
    {
        if (!outputOpen)
        {
            return;
        }

        outputOpen = false;
        try
        {
            output!.Close();
        }
        catch (Exception)
        {
            // A failing close cannot affect playbacks that already ended.
        }
    }

    private void ReleaseVoices(List<(SoundPlayback Playback, SoundFeed? Feed)> releases)
    {
        foreach ((SoundPlayback playback, SoundFeed? feed) in releases)
        {
            if (playback.Render.Chain is { } chain)
            {
                Interlocked.Add(ref dspStateBytes, -chain.StateBytes);
            }

            if (feed is not null)
            {
                StopFeed(playback, feed);
            }
        }

        releases.Clear();
    }
}
