using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace TimbreConsumer;

// Executable scenarios for an application assembly that sees only the public
// Timbre and UI surface. The sink below is this consumer's own ISoundOutput.
public static class ConsumerScenarios
{
    public static async Task<IReadOnlyList<string>> RunStandaloneAsync()
    {
        List<string> log = [];
        ManualOutput output = new();
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });
        using SoundScope sounds = runtime.CreateScope();

        var plainSound = new SoundClip(SoundSource.FromReader(() => new RampReader(48000), "ramp"), loading: SoundLoading.Preload);
        await runtime.PrepareAsync(plainSound);
        var first = sounds.Play(plainSound, start => start.Loop = true); // looping: only Cancel ends it
        var second = sounds.Play(plainSound, start => start.Loop = true); // looping: only Cancel ends it
        first.Cancel();
        log.Add($"overlap firstCanceled={first.State == SoundPlaybackState.Canceled} secondActive={IsActive(second)}");

        var toneCutoff = new SoundParameter<float>(name: "ToneCutoff", defaultValue: 1200f);
        var filteredSound = new SoundClip(
            source: SoundSource.FromReader(() => new RampReader(96000), "filtered"),
            parameters: [toneCutoff],
            modifiers: [new LowPass(cutoff: toneCutoff), new Delay(time: 0.05f, feedback: 0.3f, mix: 0.2f)]);

        var slot = sounds.CreateHandle();
        var playback = sounds.Play(filteredSound, start =>
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

        var replacement = sounds.Play(plainSound, start => start.Loop = true, handle: slot); // looping: a free-running output cannot finish it before the checks
        log.Add($"slot old={playback.State} current={(ReferenceEquals(slot.Current, replacement) ? "replacement" : "other")}");
        playback.Cancel(); // already replaced: does not touch the new occupant
        log.Add($"stale cancel replacementActive={IsActive(replacement)}");
        slot.Cancel();
        log.Add($"slot cancel current={replacement.State} empty={slot.Current is null}");

        second.Cancel();
        SoundPlaybackResult result = await second.Completion;
        log.Add($"result={result.State} error={result.Error is null}");
        return log;
    }

    // Owner access must run on the thread that created the root.
    public static IReadOnlyList<string> RunElementAndSceneOwners()
    {
        List<string> log = [];
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = new ManualOutput() });
        UIRoot root = new();
        root.SetSoundRuntime(runtime);
        Border button = new();
        RenderSurface2D surface = new();
        Scene2D scene = new();
        surface.Scene = scene;
        root.VisualChildren.Add(button);
        root.VisualChildren.Add(surface);
        SoundClip clip = new(SoundSource.FromReader(() => new RampReader(48000)));

        SoundPlayback click = button.Sounds.Play(clip, start => start.Loop = true); // only detach ends it
        SoundPlayback ambience = scene.Sounds.Play(clip, start => start.Loop = true);
        root.VisualChildren.Remove(button);
        log.Add($"detached button={click.State} sceneActive={IsActive(ambience)}");

        root.VisualChildren.Add(button);
        log.Add($"reattached scope fresh={!button.Sounds.IsDisposed}");
        runtime.Dispose();
        log.Add($"runtime disposed scene={ambience.State}");
        return log;
    }

    private static bool IsActive(SoundPlayback playback) =>
        playback.State is SoundPlaybackState.Pending or SoundPlaybackState.Playing;

    public sealed class ManualOutput : ISoundOutput
    {
        private readonly object gate = new();
        private readonly List<(long Frames, TaskCompletionSource Signal)> waiters = [];
        private ISoundOutputClient? client;
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

        public void Open(ISoundOutputClient client)
        {
            lock (gate)
            {
                this.client = client;
            }
        }

        public void Submit(ReadOnlySpan<float> samples)
        {
            List<TaskCompletionSource> ready = [];
            ISoundOutputClient? notify;
            lock (gate)
            {
                // Behave like a device that plays each block immediately.
                submitted += samples.Length / SoundRuntime.ChannelCount;
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

    private sealed class RampReader(long lengthFrames) : SoundReader
    {
        private long position;

        public override long? LengthFrames => lengthFrames;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int frames = (int)Math.Min(destination.Length / SoundRuntime.ChannelCount, lengthFrames - position);
            Span<float> span = destination.Span;
            for (int frame = 0; frame < frames; frame++)
            {
                float value = ((position + frame) % 480) / 960f;
                span[frame * 2] = value;
                span[(frame * 2) + 1] = -value;
            }

            position += frames;
            return ValueTask.FromResult(new SoundReadResult(frames, position == lengthFrames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(frame, lengthFrames);
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
