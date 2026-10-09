using System.Diagnostics;
using Cerneala.Timbre.Catalog;
using Cerneala.Timbre.Dsp;
using Cerneala.Timbre.Engine;

namespace Cerneala.Timbre;

public sealed partial class TimbreRuntime
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
        // Each admitted voice can overlap one previously rendering release.
        // Reserve that space up front, not on the first replacement block.
        int renderCapacity = checked(maxVoices * 2);
        List<TimbrePlayback> voices = new(renderCapacity);
        List<TimbrePlayback> blockVoices = new(renderCapacity);
        List<(TimbrePlayback Playback, TimbreFeed? Feed)> releases = new(renderCapacity);
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
                        FailLiveLocked(new TimbreException(TimbreErrorKind.DeviceUnavailable, "The audio device was lost.", Volatile.Read(ref deviceLostError)));
                    }

                    ApplyControlsLocked(voices, releases, exiting || lostDevice);
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
                    ITimbreBlockObserver? observer = BlockObserver;
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
                foreach (TimbrePlayback playback in voices)
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
    private void ApplyControlsLocked(List<TimbrePlayback> voices, List<(TimbrePlayback, TimbreFeed?)> releases, bool abort)
    {
        for (int index = voices.Count - 1; index >= 0; index--)
        {
            TimbrePlayback playback = voices[index];
            TimbreVoice voice = playback.Render;
            TimbreFeed? feed = playback.FeedLocked;
            // Failure/disposal cannot depend on device capacity or source readiness.
            // Cancellation stays publicly terminal while its render-side release fades.
            if (playback.IsTerminal && (abort || playback.StateLocked != TimbrePlaybackState.Canceled ||
                !voice.HasRendered || voice.Silent || voice.SeekPending || voice.ProductionEnded || feed is null ||
                (feed is StreamingFeed && feed.Stopped.IsCompleted)))
            {
                voices.RemoveAt(index);
                releases.Add((playback, playback.RequestReleaseLocked()));
                continue;
            }

            SnapshotVoiceControlsLocked(playback);
            if (!playback.IsTerminal && voice.SeekRequested && (voice.Silent || voice.ProductionEnded || voice.SeekPending))
            {
                if (voice.ProductionEnded)
                {
                    // No old PCM remains to fade, but the new position must
                    // still start from silence rather than a stale gain.
                    voice.Silence();
                }
                voice.AppliedSeekGeneration = voice.RequestedSeekGeneration;
                voice.SeekRequested = false;
                voice.SeekPending = true;
                feed!.RequestSeek(voice.SeekTarget, voice.AppliedSeekGeneration);
            }

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
                voice.SuspendAfterFade = playback.IsPausedLocked;
                voice.SetGainTarget(voice.SuspendAfterFade ? 0f : voice.Volume);
            }
        }
    }

    // Caller holds the existing Sync boundary. Intent can arrive during the
    // output's queue query, between the initial control pass and selection.
    private static void SnapshotVoiceControlsLocked(TimbrePlayback playback)
    {
        TimbreVoice voice = playback.Render;
        if (!playback.IsTerminal)
        {
            playback.ApplyControlsLocked(out _, out _, out _);
        }
        voice.SuspendAfterFade = playback.IsTerminal || playback.IsPausedLocked || voice.SeekRequested || voice.SeekPending;
        if (!voice.HasRendered && voice.SuspendAfterFade)
        {
            voice.Silence();
        }
        voice.SetGainTarget(voice.SuspendAfterFade ? 0f : voice.Volume);
    }

    private bool CompleteDrained(List<TimbrePlayback> voices, long consumed)
    {
        bool completed = false;
        lock (Sync)
        {
            foreach (TimbrePlayback playback in voices)
            {
                SnapshotVoiceControlsLocked(playback);
                TimbreVoice voice = playback.Render;
                if (voice.ProductionEnded &&
                    !voice.SeekRequested && !voice.SeekPending &&
                    playback.StateLocked == TimbrePlaybackState.Playing &&
                    voice.EndFrame <= consumed)
                {
                    playback.CompleteLocked(TimbrePlaybackState.Completed, null, voice.TailTruncated);
                    completed = true;
                }
            }
        }

        return completed;
    }

    private bool SelectBlockVoices(List<TimbrePlayback> voices, List<TimbrePlayback> blockVoices)
    {
        lock (Sync)
        {
            foreach (TimbrePlayback playback in voices)
            {
                if (playback.IsTerminal && playback.StateLocked != TimbrePlaybackState.Canceled)
                {
                    continue;
                }
                SnapshotVoiceControlsLocked(playback);
                TimbreVoice voice = playback.Render;
                if (voice.SeekPending ||
                    (voice.SuspendAfterFade && voice.Silent) ||
                    voice.ProductionEnded ||
                    playback.FeedLocked is not { } feed ||
                    (!feed.HasData && !voice.SuspendAfterFade))
                {
                    continue;
                }

                if (!playback.IsTerminal)
                {
                    playback.MarkStartedLocked();
                }
                blockVoices.Add(playback);
            }
        }

        return blockVoices.Count > 0;
    }

    // A voice whose source has not yet delivered a whole block defers the block
    // while the output still holds at least one: padding is produced only once
    // the device would otherwise run dry, so a decoder that is merely late is
    // not turned into an underrun.
    private static bool DeferForSource(List<TimbrePlayback> blockVoices, int queued)
    {
        if (queued < Block)
        {
            return false;
        }

        foreach (TimbrePlayback playback in blockVoices)
        {
            if (!playback.Render.InTail && !playback.Render.SuspendAfterFade && !playback.FeedLocked!.HasBlock(Block))
            {
                blockVoices.Clear();
                return true;
            }
        }

        return false;
    }

    private static void StampFirstQueued(List<TimbrePlayback> blockVoices)
    {
        long now = Stopwatch.GetTimestamp();
        foreach (TimbrePlayback playback in blockVoices)
        {
            playback.StampFirstQueued(now);
        }
    }

    // Produces one block of pre-volume voice PCM and returns the frames to mix.
    // Source PCM runs through the chain; after a non-looping end of source the
    // chain keeps processing silence until the tail criterion or cap. An
    // underrun pads silence without advancing the source or the DSP state.
    private int RenderVoice(TimbreFeed feed, TimbreVoice voice, float[] buffer, int requestedFrames, out bool underrun)
    {
        underrun = false;
        TimbreDspChain? chain = voice.Chain;
        int sourceFrames = 0;
        if (!voice.InTail)
        {
            long wrapsBefore = feed.LoopWraps;
            sourceFrames = feed.Read(buffer, requestedFrames);
            if (feed.LoopWraps != wrapsBefore)
            {
                Interlocked.Add(ref loopWraps, feed.LoopWraps - wrapsBefore);
            }
        }

        if (!feed.Ended && !voice.InTail)
        {
            if (sourceFrames < requestedFrames)
            {
                underrun = true;
                Interlocked.Add(ref underrunFrames, requestedFrames - sourceFrames);
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
        chain.Process(buffer, requestedFrames, voice.Values);
        int window = chain.TailWindowFrames;
        for (int frame = sourceFrames; frame < requestedFrames; frame++)
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

        return requestedFrames;
    }

    // Returns whether the block carries PCM or counted underrun padding.
    private bool MixBlock(List<TimbrePlayback> blockVoices, float[] mix, float[] voiceBuffer)
    {
        Array.Clear(mix);
        bool submit = false;
        long blockEnd = Interlocked.Read(ref framesSubmitted) + Block;
        foreach (TimbrePlayback playback in blockVoices)
        {
            TimbreFeed feed = playback.FeedLocked!;
            TimbreVoice voice = playback.Render;
            int requestedFrames = voice.SuspendAfterFade ? Math.Min(Block, voice.GainFramesRemaining) : Block;
            int frames = RenderVoice(feed, voice, voiceBuffer, requestedFrames, out bool underrun);
            voice.HasRendered |= frames > 0;
            submit |= underrun;
            for (int frame = 0; frame < requestedFrames; frame++)
            {
                float gain = voice.NextGain();
                if (frame < frames)
                {
                    mix[frame * 2] += voiceBuffer[frame * 2] * gain;
                    mix[frame * 2 + 1] += voiceBuffer[frame * 2 + 1] * gain;
                }
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
                FailLiveLocked(new TimbreException(TimbreErrorKind.DeviceUnavailable, "No audio output is configured for this sound runtime."));
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
                FailLiveLocked(new TimbreException(TimbreErrorKind.DeviceUnavailable, "The audio output could not be opened.", exception));
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

    private void ReleaseVoices(List<(TimbrePlayback Playback, TimbreFeed? Feed)> releases)
    {
        foreach ((TimbrePlayback playback, TimbreFeed? feed) in releases)
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
