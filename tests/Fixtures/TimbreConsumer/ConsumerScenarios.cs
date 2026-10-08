using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace TimbreConsumer;

// Executable scenarios for an application assembly that sees only the public
// Timbre and UI surface. The sink below is this consumer's own ITimbreOutput.
public static class ConsumerScenarios
{
    public static async Task<IReadOnlyList<string>> RunStandaloneAsync()
    {
        List<string> log = [];
        ManualOutput output = new();
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output });
        using TimbreScope sounds = runtime.CreateScope();

        var plainTimbre = new TimbreClip(TimbreSource.FromReader(() => new RampReader(48000), "ramp"), loading: TimbreLoading.Preload);
        await runtime.PrepareAsync(plainTimbre);
        var first = sounds.Play(plainTimbre, start => start.Loop = true); // looping: only Cancel ends it
        var second = sounds.Play(plainTimbre, start => start.Loop = true); // looping: only Cancel ends it
        first.Cancel();
        log.Add($"overlap firstCanceled={first.State == TimbrePlaybackState.Canceled} secondActive={IsActive(second)}");

        var toneCutoff = new TimbreParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
        var filteredTimbre = new TimbreClip(
            source: TimbreSource.FromReader(() => new RampReader(96000), "filtered"),
            parameters: [toneCutoff],
            modifiers: [new LowPass(cutoff: toneCutoff), new Delay(time: 0.05f, feedback: 0.3f, mix: 0.2f)]);

        var slot = sounds.CreateHandle();
        var playback = sounds.Play(filteredTimbre, start =>
        {
            start.Volume = 0.2f;
            start.Set(toneCutoff, 800f);
            start.Loop = true;
        }, handle: slot);

        playback.Volume = 0.8f;
        playback.Set(toneCutoff, 6000f);
        playback.Pause();
        log.Add($"paused={playback.State}");
        await playback.SeekAsync(TimeSpan.FromSeconds(1)); // paused: the position stays at the target
        log.Add($"seek position={playback.Position.TotalSeconds:0.000} duration={playback.Duration?.TotalSeconds:0.000} loop={playback.Loop} state={playback.State}");
        playback.Resume();
        await output.WaitForFramesAsync(1);

        var replacement = sounds.Play(plainTimbre, start => start.Loop = true, handle: slot); // looping: a free-running output cannot finish it before the checks
        log.Add($"slot old={playback.State} current={(ReferenceEquals(slot.Current, replacement) ? "replacement" : "other")}");
        playback.Cancel(); // already replaced: does not touch the new occupant
        log.Add($"stale cancel replacementActive={IsActive(replacement)}");
        slot.Cancel();
        log.Add($"slot cancel current={replacement.State} empty={slot.Current is null}");

        second.Cancel();
        TimbrePlaybackResult result = await second.Completion;
        log.Add($"result={result.State} error={result.Error is null}");
        return log;
    }

    // Owner access must run on the thread that created the root.
    public static IReadOnlyList<string> RunElementAndSceneOwners()
    {
        List<string> log = [];
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = new ManualOutput() });
        UIRoot root = new();
        root.SetTimbreRuntime(runtime);
        Border button = new();
        RenderSurface2D surface = new();
        Scene2D scene = new();
        surface.Scene = scene;
        root.VisualChildren.Add(button);
        root.VisualChildren.Add(surface);
        TimbreClip clip = new(TimbreSource.FromReader(() => new RampReader(48000)));

        TimbrePlayback click = button.Timbre.Play(clip, start => start.Loop = true); // only detach ends it
        TimbrePlayback ambience = scene.Timbre.Play(clip, start => start.Loop = true);
        root.VisualChildren.Remove(button);
        log.Add($"detached button={click.State} sceneActive={IsActive(ambience)}");

        root.VisualChildren.Add(button);
        log.Add($"reattached scope fresh={!button.Timbre.IsDisposed}");
        runtime.Dispose();
        log.Add($"runtime disposed scene={ambience.State}");
        return log;
    }

    private static bool IsActive(TimbrePlayback playback) =>
        playback.State is TimbrePlaybackState.Pending or TimbrePlaybackState.Playing;

    public sealed class ManualOutput : ITimbreOutput
    {
        private readonly object gate = new();
        private readonly List<(long Frames, TaskCompletionSource Signal)> waiters = [];
        private ITimbreOutputClient? client;
        private long submitted;
        private long consumed;

        public int QueuedFrames
        {
            get
            {
                lock (gate)
                {
                    return (int)(submitted - consumed);
                }
            }
        }

        public void Open(ITimbreOutputClient client)
        {
            lock (gate)
            {
                this.client = client;
            }
        }

        public void Submit(ReadOnlySpan<float> samples)
        {
            List<TaskCompletionSource> ready = [];
            ITimbreOutputClient? notify;
            lock (gate)
            {
                // Behave like a device that plays each block immediately.
                submitted += samples.Length / TimbreRuntime.ChannelCount;
                consumed = submitted;
                notify = client;
                waiters.RemoveAll(waiter =>
                {
                    if (waiter.Frames > submitted)
                    {
                        return false;
                    }

                    ready.Add(waiter.Signal);
                    return true;
                });
            }

            ready.ForEach(signal => signal.TrySetResult());
            notify?.NotifyCapacityAvailable();
        }

        public void Close()
        {
            lock (gate)
            {
                client = null;
            }
        }

        public Task WaitForFramesAsync(long frames)
        {
            TaskCompletionSource signal = new(TaskCreationOptions.RunContinuationsAsynchronously);
            lock (gate)
            {
                if (submitted >= frames)
                {
                    return Task.CompletedTask;
                }

                waiters.Add((frames, signal));
            }

            return signal.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    private sealed class RampReader(long lengthFrames) : TimbreReader
    {
        private long position;

        public override long? LengthFrames => lengthFrames;

        public override ValueTask<TimbreReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / TimbreRuntime.ChannelCount, lengthFrames - position);
            Span<float> span = destination.Span;
            for (int frame = 0; frame < frames; frame++)
            {
                float value = ((position + frame) % 480) / 960f;
                span[frame * 2] = value;
                span[(frame * 2) + 1] = -value;
            }

            position += frames;
            return ValueTask.FromResult(new TimbreReadResult(frames, position == lengthFrames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, lengthFrames);
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
