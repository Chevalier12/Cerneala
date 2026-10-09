using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.Timbre.Engine;

namespace Cerneala.Tests.Timbre.Engine;

// The mixer thread's steady-state path (selection, render, DSP, mix, Submit)
// must not allocate after warmup. Measured with the same observer hook as the
// cost gate, per loading mode so an allocation is attributed to its owner.
public sealed class RealtimeAllocationTests
{
    private const int Warmup = 200;
    private const int Measured = 2000;

    [Theory]
    [InlineData(TimbreLoading.Preload)]
    [InlineData(TimbreLoading.Streaming)]
    public async Task SteadyStateMixerBlocksDoNotAllocate(TimbreLoading loading)
    {
        DeterministicTimbreOutput output = new() { AutoConsume = true, RecordSamples = false };
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output });
        AllocationRecorder recorder = new(Warmup, Measured);
        runtime.BlockObserver = recorder;
        using TimbreScope scope = runtime.CreateScope();
        TimbreSound clip = new(
            TimbreSource.FromReader(new DeterministicTimbreSourceFactory(10 * 48000, maxFramesPerRead: 777).Open),
            volume: 0.1f,
            loop: true,
            loading: loading,
            modifiers: [new LowPass(cutoff: 2000f), new Delay(time: 0.05f, feedback: 0.4f, mix: 0.3f)]);

        for (int voice = 0; voice < 4; voice++)
        {
            scope.Play(clip);
        }

        await HarnessWait.WithTimeout(recorder.Completed, TimeSpan.FromSeconds(30), "Mixer did not produce the measured blocks.");

        Assert.True(
            recorder.AllocatedBytes == 0,
            $"{recorder.AllocatingBlocks} of {Measured} measured blocks allocated {recorder.AllocatedBytes} bytes on the mixer thread.");
    }

    [Fact]
    public async Task VolumeAndPauseResumeRampsDoNotAllocateAfterWarmup()
    {
        const int warmup = 32;
        const int measured = 128;
        DeterministicTimbreOutput output = new() { RecordSamples = false };
        output.Hold();
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output });
        AllocationRecorder recorder = new(warmup, measured);
        runtime.BlockObserver = recorder;
        using TimbreScope scope = runtime.CreateScope();
        TimbreSound clip = new(
            TimbreSource.FromReader(new DeterministicTimbreSourceFactory(48000).Open),
            loop: true,
            loading: TimbreLoading.Preload,
            modifiers: [new LowPass(), new Delay()]);
        TimbrePlayback voice = scope.Play(clip);
        TimbrePlayback clock = scope.Play(clip, start => start.Volume = 0f);
        await voice.WhenReady;
        await clock.WhenReady;
        output.Release();
        await output.WaitForSubmittedFramesAsync(TimbreRig.Budget);
        for (int block = 0; block < warmup + measured; block++)
        {
            switch (block % 4)
            {
                case 0: voice.Volume = 0.2f; break;
                case 1: voice.Volume = 0.8f; break;
                case 2: voice.Pause(); break;
                case 3: voice.Resume(); break;
            }
            await HarnessWait.WithTimeout(runtime.SyncAsync(), null, "Mixer did not apply the ramp target.");
            long submitted = output.SubmittedFrames;
            output.Consume(TimbreRig.Block);
            // A sink submission waiter allocates its ready-list inside Submit.
            // Sync waits outside the measured path and the preloaded clock
            // guarantees exactly one block fills this released capacity.
            await HarnessWait.WithTimeout(runtime.SyncAsync(), null, "Mixer did not submit the ramp block.");
            Assert.Equal(submitted + TimbreRig.Block, output.SubmittedFrames);
        }
        await HarnessWait.WithTimeout(recorder.Completed, null, "Ramp allocation measurement did not finish.");
        Assert.Equal(0, recorder.AllocatedBytes);
        Assert.Equal(0, recorder.AllocatingBlocks);
    }

    [Fact]
    public async Task FirstReplacementOverlapDoesNotAllocateInTheMixingPath()
    {
        DeterministicTimbreOutput output = new() { RecordSamples = false };
        output.Hold();
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output, MaxVoices = 2 });
        using TimbreScope scope = runtime.CreateScope();
        TimbreHandle handle = scope.CreateHandle();
        TimbreSound clip = TimbreRig.Clip(new DeterministicTimbreSourceFactory(48000), loop: true, volume: 0.1f);
        TimbrePlayback old = scope.Play(clip, handle: handle);
        TimbrePlayback clock = scope.Play(clip, start => start.Volume = 0f);
        await old.WhenReady;
        await clock.WhenReady;
        output.Release();
        for (int block = 0; block < 32; block++)
        {
            await runtime.SyncAsync();
            output.Consume(TimbreRig.Block);
        }
        await runtime.SyncAsync();
        AllocationRecorder recorder = new(0, 1);
        runtime.BlockObserver = recorder;
        TimbrePlayback replacement = scope.Play(clip, handle: handle);
        await replacement.WhenReady;
        output.Consume(TimbreRig.Block);
        await HarnessWait.WithTimeout(recorder.Completed, null, "Replacement overlap did not render.");
        Assert.Equal(0, recorder.AllocatedBytes);
    }

    private sealed class AllocationRecorder(int warmup, int measured) : ITimbreBlockObserver
    {
        private readonly TaskCompletionSource completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int seen;

        public Task Completed => completed.Task;

        public long AllocatedBytes { get; private set; }

        public int AllocatingBlocks { get; private set; }

        public void OnBlockSubmitted(long elapsedTicks, long allocatedBytes)
        {
            int index = seen++ - warmup;
            if (index < 0 || index >= measured)
            {
                return;
            }

            AllocatedBytes += allocatedBytes;
            if (allocatedBytes != 0)
            {
                AllocatingBlocks++;
            }

            if (index == measured - 1)
            {
                completed.TrySetResult();
            }
        }
    }
}
