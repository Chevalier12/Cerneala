using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;

namespace Cerneala.Tests.SdlGpu;

// The concrete Timbre mixer feeding the SDL output through the fake seam: the
// device side is driven by explicit requests and runtime sync barriers.
public sealed class SdlTimbreOutputRuntimeTests
{
    private const float Step = 1f / 65536f;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
    private static readonly int[] IrregularRequests = [1000, 3840, 8, 2056, 7999, 3840];

    [Fact]
    public async Task MixedFramesReachTheDeviceInOrderAcrossIrregularRequestSizes()
    {
        (FakeSdlAudioApi api, SdlTimbreOutput output, TimbreRuntime runtime) = Create();
        using (runtime)
        {
            TimbrePlayback playback = runtime.CreateScope().Play(Ramp(4800));
            await playback.WhenReady.WaitAsync(Timeout);

            await PullUntilAsync(api, runtime, () => playback.Completion.IsCompleted);

            Assert.Equal(TimbrePlaybackState.Completed, (await playback.Completion).State);
            float[] consumed = api.Consumed;
            Assert.Equal(4800 * 2, consumed.Length);
            for (int frame = 0; frame < 4800; frame++)
            {
                Assert.Equal(frame * Step, consumed[frame * 2]);
                Assert.Equal(-frame * Step, consumed[(frame * 2) + 1]);
            }

            Assert.InRange(api.QueueHighWaterBytes, 1, 1920 * 8);
            Assert.InRange(output.GetDiagnostics().QueueHighWaterFrames, 0, 1920);
            Assert.Equal(["init", "open", "resume"], api.Operations);
        }

        Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
        Assert.Equal(0, api.AudioReferences);
    }

    [Fact]
    public async Task CompletionWaitsForTheDeviceToDrainTheLastSubmittedBlock()
    {
        (FakeSdlAudioApi api, _, TimbreRuntime runtime) = Create();
        using (runtime)
        {
            TimbrePlayback playback = runtime.CreateScope().Play(Ramp(960));
            await playback.WhenReady.WaitAsync(Timeout);
            await PullUntilAsync(api, runtime, () => api.Submitted.Length == 960 * 2, [0]);
            await runtime.SyncAsync().WaitAsync(Timeout);

            // Every block is produced and queued, but nothing has left the queue.
            Assert.Equal(TimbrePlaybackState.Playing, playback.State);
            Assert.Equal(960 * 8, api.QueuedBytes);

            api.Pull((960 * 8) - 8);
            await runtime.SyncAsync().WaitAsync(Timeout);
            Assert.Equal(TimbrePlaybackState.Playing, playback.State);

            api.Pull(8);
            TimbrePlaybackResult result = await playback.Completion.WaitAsync(Timeout);

            Assert.Equal(TimbrePlaybackState.Completed, result.State);
            Assert.Equal(0, api.QueuedBytes);
        }
    }

    [Fact]
    public async Task AResamplingDeviceReceivesTheHeldBackTailAndThePlaybackCompletes()
    {
        (FakeSdlAudioApi api, SdlTimbreOutput output, TimbreRuntime runtime) = Create();
        api.HeldBackBytes = 64 * 8;
        using (runtime)
        {
            TimbrePlayback playback = runtime.CreateScope().Play(Ramp(9600));
            await playback.WhenReady.WaitAsync(Timeout);

            await PullUntilAsync(api, runtime, () => playback.Completion.IsCompleted);

            Assert.Equal(TimbrePlaybackState.Completed, (await playback.Completion).State);
            float[] consumed = api.Consumed;
            Assert.Equal(9600 * 2, consumed.Length);
            for (int frame = 0; frame < 9600; frame++)
            {
                Assert.Equal(frame * Step, consumed[frame * 2]);
            }

            // Only the end of the stream is flushed; continuous playback never is.
            Assert.Equal(1, api.FlushCount);
            Assert.Equal(1, output.GetDiagnostics().Flushes);
        }
    }

    [Fact]
    public async Task CancelingOneOfTwoMixedVoicesKeepsQueuedPcmAndTheOtherVoice()
    {
        (FakeSdlAudioApi api, SdlTimbreOutput output, TimbreRuntime runtime) = Create();
        using (runtime)
        {
            TimbreScope scope = runtime.CreateScope();
            TimbrePlayback canceled = scope.Play(Ramp(48000));
            TimbrePlayback survivor = scope.Play(Constant(9600, 0.25f));
            await Task.WhenAll(canceled.WhenReady, survivor.WhenReady).WaitAsync(Timeout);
            await PullUntilAsync(api, runtime, () => api.Consumed.Length >= 960 * 2);
            await runtime.SyncAsync().WaitAsync(Timeout);
            int consumedBeforeCancel = api.Consumed.Length;
            float[] queuedBeforeCancel = api.QueuedSamples;
            Assert.NotEmpty(queuedBeforeCancel);

            canceled.Cancel();
            await runtime.SyncAsync().WaitAsync(Timeout);

            // PCM already mixed stays queued; nothing clears or closes the output.
            Assert.Equal(queuedBeforeCancel, api.QueuedSamples.Take(queuedBeforeCancel.Length));
            Assert.Equal(TimbrePlaybackState.Canceled, canceled.State);
            Assert.Equal(TimbrePlaybackState.Playing, survivor.State);

            await PullUntilAsync(api, runtime, () => survivor.Completion.IsCompleted);

            Assert.Equal(TimbrePlaybackState.Completed, (await survivor.Completion).State);
            float[] consumed = api.Consumed;
            int queuedEnd = consumedBeforeCancel + queuedBeforeCancel.Length;
            Assert.Equal(queuedBeforeCancel, consumed[consumedBeforeCancel..queuedEnd]);
            Assert.Contains(queuedBeforeCancel, sample => sample > 0.25f);
            float[] afterCancel = consumed[queuedEnd..];
            Assert.NotEmpty(afterCancel);
            Assert.All(afterCancel, sample => Assert.Equal(0.25f, sample));
            Assert.Equal(["init", "open", "resume"], api.Operations);
            Assert.Equal(1, output.GetDiagnostics().OpenCount);
        }
    }

    [Fact]
    public async Task TransportAndParameterChangesNeverClearOrCloseTheSharedOutput()
    {
        (FakeSdlAudioApi api, SdlTimbreOutput output, TimbreRuntime runtime) = Create();
        using (runtime)
        {
            TimbreScope scope = runtime.CreateScope();
            TimbreHandle slot = scope.CreateHandle();
            TimbrePlayback looping = scope.Play(Ramp(2400, scale: Step / 64), start => start.Loop = true, slot);
            TimbrePlayback steady = scope.Play(Constant(48000, 0.25f));
            await Task.WhenAll(looping.WhenReady, steady.WhenReady).WaitAsync(Timeout);
            await PullUntilAsync(api, runtime, () => api.Consumed.Length >= 4800 * 2);

            int queued = api.QueuedBytes;
            looping.Pause();
            await runtime.SyncAsync().WaitAsync(Timeout);
            Assert.True(api.QueuedBytes >= queued);
            await PullUntilAsync(api, runtime, () => api.Consumed.Length >= 9600 * 2);

            looping.Resume();
            looping.Volume = 0.5f;
            await looping.SeekAsync(TimeSpan.FromMilliseconds(10)).WaitAsync(Timeout);
            await PullUntilAsync(api, runtime, () => api.Consumed.Length >= 14400 * 2);

            queued = api.QueuedBytes;
            TimbrePlayback replacement = scope.Play(Constant(4800, 0.125f), handle: slot);
            await runtime.SyncAsync().WaitAsync(Timeout);
            Assert.Equal(TimbrePlaybackState.Canceled, looping.State);
            Assert.True(api.QueuedBytes >= queued);

            await PullUntilAsync(api, runtime, () => steady.Completion.IsCompleted && replacement.Completion.IsCompleted);

            Assert.Equal(TimbrePlaybackState.Completed, (await steady.Completion).State);
            Assert.Equal(TimbrePlaybackState.Completed, (await replacement.Completion).State);
            float[] consumed = api.Consumed;
            int first = 0;
            while (consumed[first * 2] < 0.25f - 1e-6f)
            {
                first++;
            }

            // Both voices were ready before the first request, so at most one
            // full queue budget can precede the steady voice.
            Assert.True(first <= 1920, "The steady voice did not start within the first queue budget.");
            for (int frame = first; frame < first + 48000; frame++)
            {
                // The steady voice is never interrupted by the other voice's transport.
                Assert.True(consumed[frame * 2] >= 0.25f - 1e-6f, $"frame {frame} lost the steady voice");
            }

            Assert.Equal(["init", "open", "resume"], api.Operations);
            Assert.Equal(1, output.GetDiagnostics().OpenCount);
            Assert.InRange(api.QueueHighWaterBytes, 1, 1920 * 8);
        }
    }

    [Fact]
    public async Task OnlyTheLatestSeekPublishesPcmAfterResume()
    {
        (FakeSdlAudioApi api, _, TimbreRuntime runtime) = Create();
        using (runtime)
        {
            TimbrePlayback playback = runtime.CreateScope().Play(Ramp(9600));
            playback.Pause();
            await playback.WhenReady.WaitAsync(Timeout);
            Task superseded;
            Task latest;
            lock (runtime.Sync)
            {
                // Both requests are published before the mixer can apply either.
                superseded = playback.SeekAsync(TimbreTimeFrames(1000));
                latest = playback.SeekAsync(TimbreTimeFrames(2000));
            }

            await latest.WaitAsync(Timeout);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => superseded);
            Assert.Empty(api.Submitted);

            playback.Resume();
            await PullUntilAsync(api, runtime, () => playback.Completion.IsCompleted);

            float[] consumed = api.Consumed;
            const int produced = 9600 - 2000;
            for (int frame = 0; frame < produced; frame++)
            {
                Assert.Equal((2000 + frame) * Step, consumed[frame * 2]);
            }

            // The mixer submits whole blocks; the tail of the last one is silence.
            Assert.Equal(0, consumed.Length / 2 % 480);
            Assert.All(consumed[(produced * 2)..], sample => Assert.Equal(0f, sample));
        }
    }

    [Fact]
    public async Task AFailingPutFailsTheMixedPlaybacksAndClosesTheOutputOnce()
    {
        (FakeSdlAudioApi api, SdlTimbreOutput output, TimbreRuntime runtime) = Create();
        using (runtime)
        {
            TimbrePlayback playback = runtime.CreateScope().Play(Ramp(48000));
            await playback.WhenReady.WaitAsync(Timeout);
            await PullUntilAsync(api, runtime, () => api.Consumed.Length > 0);

            api.PutResult = false;
            await PullUntilAsync(api, runtime, () => playback.Completion.IsCompleted);

            TimbrePlaybackResult result = await playback.Completion;
            Assert.Equal(TimbrePlaybackState.Failed, result.State);
            Assert.Equal(TimbreErrorKind.DeviceUnavailable, result.Error!.Kind);
            Assert.Equal(1, output.GetDiagnostics().PutFailures);
            Assert.Equal(["init", "open", "resume", "destroy", "quit"], api.Operations);
        }
    }

    private static (FakeSdlAudioApi Api, SdlTimbreOutput Output, TimbreRuntime Runtime) Create()
    {
        FakeSdlAudioApi api = new();
        SdlTimbreOutput output = new(api);
        return (api, output, new TimbreRuntime(new TimbreRuntimeOptions { Output = output }));
    }

    private static async Task PullUntilAsync(
        FakeSdlAudioApi api,
        TimbreRuntime runtime,
        Func<bool> done,
        int[]? requests = null)
    {
        requests ??= IrregularRequests;
        for (int iteration = 0; !done(); iteration++)
        {
            Assert.True(iteration < 20000, "The device drive did not converge.");
            await runtime.SyncAsync().WaitAsync(Timeout);
            api.Pull(requests[iteration % requests.Length]);
        }
    }

    private static TimeSpan TimbreTimeFrames(long frames) => TimeSpan.FromTicks(frames * TimeSpan.TicksPerSecond / 48000);

    private static TimbreClip Ramp(int frames, float scale = Step) =>
        new(TimbreSource.FromReader(() => new RampReader(frames, scale), $"ramp-{frames}"));

    private static TimbreClip Constant(int frames, float value) =>
        new(TimbreSource.FromReader(() => new ConstantReader(frames, value), $"constant-{frames}-{value}"));

    private sealed class RampReader(int frames, float scale) : TimbreReader
    {
        private long position;

        public override long? LengthFrames => frames;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int count = (int)Math.Min(destination.Length / 2, frames - position);
            Span<float> span = destination.Span;
            for (int index = 0; index < count; index++)
            {
                float value = (position + index) * scale;
                span[index * 2] = value;
                span[(index * 2) + 1] = -value;
            }

            position += count;
            return ValueTask.FromResult(new TimbreReadResult(count, position == frames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ConstantReader(int frames, float value) : TimbreReader
    {
        private long position;

        public override long? LengthFrames => frames;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int count = (int)Math.Min(destination.Length / 2, frames - position);
            destination.Span[..(count * 2)].Fill(value);
            position += count;
            return ValueTask.FromResult(new TimbreReadResult(count, position == frames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
