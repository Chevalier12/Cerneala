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
    [InlineData(SoundLoading.Preload)]
    [InlineData(SoundLoading.Streaming)]
    public async Task SteadyStateMixerBlocksDoNotAllocate(SoundLoading loading)
    {
        DeterministicSoundOutput output = new() { AutoConsume = true, RecordSamples = false };
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });
        AllocationRecorder recorder = new(Warmup, Measured);
        runtime.BlockObserver = recorder;
        using SoundScope scope = runtime.CreateScope();
        SoundClip clip = new(
            SoundSource.FromReader(new DeterministicSoundSourceFactory(10 * 48000, maxFramesPerRead: 777).Open),
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

    private sealed class AllocationRecorder(int warmup, int measured) : ISoundBlockObserver
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
