using Cerneala.Timbre;
using Cerneala.Timbre.Catalog;

namespace Cerneala.Tests.Timbre.Harness;

// A real TimbreRuntime wired to the deterministic sink. The rig never mixes or
// schedules: it only holds/releases sink capacity and waits on engine signals.
internal sealed class TimbreRig : IDisposable
{
    public const int Block = TimbreCatalog.BlockFrames;
    public const int Budget = TimbreCatalog.OutputQueueBudgetFrames;
    public static readonly int FadeFrames = (int)Math.Round(0.005 * TimbreRuntime.SampleRate);

    public TimbreRig(Action<TimbreRuntimeOptions>? configure = null, bool hold = true)
    {
        if (hold)
        {
            Output.Hold();
        }

        TimbreRuntimeOptions options = new() { Output = Output };
        configure?.Invoke(options);
        Runtime = new TimbreRuntime(options);
        Scope = Runtime.CreateScope();
    }

    public DeterministicTimbreOutput Output { get; } = new();

    public TimbreRuntime Runtime { get; }

    public TimbreScope Scope { get; }

    public static TimbreSound Clip(
        DeterministicTimbreSourceFactory factory,
        TimbreLoading loading = TimbreLoading.Preload,
        bool loop = false,
        float volume = 1f) =>
        new(TimbreSource.FromReader(factory.Open, "deterministic"), volume, loop, loading);

    public static float[] Expected(long frames, Func<long, int, float> signal, float gain = 1f, long sourceStart = 0)
    {
        float[] result = new float[frames * 2];
        for (long frame = 0; frame < frames; frame++)
        {
            result[frame * 2] = signal(sourceStart + frame, 0) * gain;
            result[(frame * 2) + 1] = signal(sourceStart + frame, 1) * gain;
        }

        return result;
    }

    public static float[] Sum(params float[][] parts)
    {
        float[] result = new float[parts[0].Length];
        foreach (float[] part in parts)
        {
            for (int index = 0; index < result.Length; index++)
            {
                result[index] += part[index];
            }
        }

        return result;
    }

    public static float[] ExpectedRamp(long frames, Func<long, int, float> signal, float from, float to, long sourceStart = 0) =>
        Expected(frames, (frame, channel) => signal(sourceStart + frame, channel) *
            (from + (to - from) * Math.Min(1f, (frame + 1f) / FadeFrames)));

    public static float[] Ramp(float[] pcm, float from = 0f, float to = 1f)
    {
        float[] result = (float[])pcm.Clone();
        for (int frame = 0; frame < result.Length / 2; frame++)
        {
            float gain = from + (to - from) * Math.Min(1f, (frame + 1f) / FadeFrames);
            result[frame * 2] *= gain;
            result[frame * 2 + 1] *= gain;
        }
        return result;
    }

    public static void AssertPcm(float[] expected, float[] actual, float tolerance = 1e-6f)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int index = 0; index < expected.Length; index++)
        {
            if (Math.Abs(expected[index] - actual[index]) > tolerance)
            {
                Assert.Fail($"PCM differs at sample {index} (frame {index / 2}, channel {index % 2}): expected {expected[index]}, actual {actual[index]}.");
            }
        }
    }

    public async Task ReadyAsync(params TimbrePlayback[] playbacks)
    {
        foreach (TimbrePlayback playback in playbacks)
        {
            await HarnessWait.WithTimeout(playback.WhenReady, null, "Playback did not become ready.");
        }
    }

    public async Task SettledAsync(params TimbrePlayback[] playbacks)
    {
        foreach (TimbrePlayback playback in playbacks)
        {
            await HarnessWait.WithTimeout(playback.WhenSettledAsync(), null, "Streaming playback did not settle.");
        }
    }

    // Starts mixing: waits for readiness (and settled streaming buffers), then
    // releases the sink and waits for the first full software queue.
    public async Task<float[]> StartAsync(params TimbrePlayback[] playbacks)
    {
        await ReadyAsync(playbacks);
        await SettledAsync(playbacks);
        Output.Release();
        await Output.WaitForSubmittedFramesAsync(Budget);
        return Output.Read(0, Budget);
    }

    // Consumes one block and returns the newly produced block.
    public async Task<float[]> NextBlockAsync()
    {
        long submitted = Output.SubmittedFrames;
        await SyncAsync();
        Output.Consume(Block);
        await Output.WaitForSubmittedFramesAsync(submitted + Block);
        return Output.Read(submitted, Block);
    }

    public Task SyncAsync() => HarnessWait.WithTimeout(Runtime.SyncAsync(), null, "Mixer did not reach an idle point.");

    public static Task<TimbrePlaybackResult> CompletionAsync(TimbrePlayback playback) =>
        HarnessWait.WithTimeout(playback.Completion, null, "Playback did not complete.");

    public static Task ReleasedAsync(TimbrePlayback playback) =>
        HarnessWait.WithTimeout(playback.WhenReleased, null, "Playback resources were not released.");

    public static TimeSpan FramesToTime(long frames) => TimeSpan.FromTicks(frames * TimeSpan.TicksPerSecond / TimbreRuntime.SampleRate);

    public void Dispose() => Runtime.Dispose();
}
