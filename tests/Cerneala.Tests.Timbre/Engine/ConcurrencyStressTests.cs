using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Engine;

// Bounded, seeded concurrency: several threads drive start/pause/resume/seek/
// loop/cancel/replace/parameter/failure operations against one runtime whose
// sink consumes freely. The run is frozen by seed and iteration count; the
// checks are state legality and return of every resource to baseline.
public sealed class ConcurrencyStressTests
{
    private const int Threads = 4;
    private const int IterationsPerThread = 2000;

    [Theory]
    [InlineData(20261007)]
    [InlineData(17)]
    public async Task ConcurrentTransportAndLifecycleReturnEveryResourceToBaseline(int seed)
    {
        DeterministicTimbreOutput output = new() { AutoConsume = true, RecordSamples = false };
        TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output, MaxVoices = 24 });
        TimbreParameter<float> cutoff = new("Cutoff", 2000f);
        List<DeterministicTimbreSourceFactory> factories = [];
        TimbreSound[] clips = CreateClips(cutoff, factories);
        List<TimbrePlayback> all = [];
        List<Task> seeks = [];
        List<string> unexpected = [];
        object gate = new();

        // Dedicated threads: spinning workers must not starve the thread pool that
        // runs loaders and pumps.
        Thread[] workers = Enumerable.Range(0, Threads).Select(worker => new Thread(() =>
        {
            Random random = new(seed + worker);
            TimbreScope scope = runtime.CreateScope();
            TimbreHandle slot = scope.CreateHandle();
            List<TimbrePlayback> mine = [];
            for (int iteration = 0; iteration < IterationsPerThread; iteration++)
            {
                try
                {
                    Step(random, ref scope, ref slot, mine, clips, cutoff, runtime, all, seeks, gate);
                }
                catch (Exception exception) when (IsContractRejection(exception))
                {
                    // Terminal-state, disposed-scope, and voice-limit rejections are
                    // part of the contract under concurrent use.
                }
                catch (Exception exception)
                {
                    lock (gate)
                    {
                        unexpected.Add(exception.ToString());
                    }
                }
            }
        }) { IsBackground = true, Name = $"Timbre stress {worker}" }).ToArray();

        foreach (Thread thread in workers)
        {
            thread.Start();
        }

        foreach (Thread thread in workers)
        {
            Assert.True(thread.Join(TimeSpan.FromSeconds(60)), "Stress worker did not finish.");
        }

        // Deterministic coverage of the failure path in the same runtime, after the
        // workers' leftover playbacks release their voices.
        lock (gate)
        {
            all.ForEach(playback => playback.Cancel());
        }

        using (TimbreScope failureScope = runtime.CreateScope())
        {
            TimbrePlayback failing = failureScope.Play(clips[4]);
            Track(failing, [], all, gate);
            Assert.Equal(TimbrePlaybackState.Failed, (await TimbreRig.CompletionAsync(failing)).State);
        }

        runtime.Dispose();

        TimbrePlayback[] playbacks;
        Task[] seekTasks;
        lock (gate)
        {
            playbacks = all.ToArray();
            seekTasks = seeks.ToArray();
        }

        Assert.Empty(unexpected);
        Assert.NotEmpty(playbacks);
        foreach (TimbrePlayback playback in playbacks)
        {
            TimbrePlaybackResult result = await TimbreRig.CompletionAsync(playback);
            Assert.Equal(result.State, playback.State);
            Assert.True(result.State is TimbrePlaybackState.Completed or TimbrePlaybackState.Canceled or TimbrePlaybackState.Failed);
            Assert.Equal(result.State == TimbrePlaybackState.Failed, result.Error is not null);
            await TimbreRig.ReleasedAsync(playback);
        }

        await HarnessWait.WithTimeout(
            Task.WhenAll(seekTasks.Select(task => task.ContinueWith(_ => { }, TaskScheduler.Default))),
            null,
            "A seek request was left pending after shutdown.");

        TimbreRuntimeDiagnostics diagnostics = runtime.GetDiagnostics();
        Assert.Equal(0, diagnostics.ActiveVoices);
        Assert.Equal(0, diagnostics.LiveReaders);
        Assert.Equal(0, diagnostics.LiveSourcePumps);
        Assert.Equal(0, diagnostics.PendingLoads);
        Assert.Equal(0, diagnostics.StreamingBufferBytes);
        Assert.Equal(0, diagnostics.DspStateBytes);
        Assert.Equal(0, diagnostics.LiveScopes);
        Assert.Equal(0, diagnostics.CacheBytes);
        Assert.False(diagnostics.OutputOpen);
        Assert.Equal(diagnostics.PlaybacksStarted, diagnostics.PlaybacksCompleted + diagnostics.PlaybacksCanceled + diagnostics.PlaybacksFailed);
        Assert.Equal(playbacks.Length, diagnostics.PlaybacksStarted);
        Assert.All(factories, factory => Assert.Equal(0, factory.LiveReaders));
        Assert.True(output.MaxQueuedFrames <= TimbreRig.Budget);
        Assert.True(diagnostics.BlocksMixed > 0);
        Assert.True(diagnostics.PlaybacksFailed > 0);
        Assert.True(diagnostics.SeeksRequested > 0);
        Assert.True(diagnostics.PausesApplied > 0 && diagnostics.ResumesApplied > 0);
        Assert.True(diagnostics.PlaybacksCanceled > 0);
        Assert.Equal(output.OpenCount, output.CloseCount);
    }

    private static void Step(
        Random random,
        ref TimbreScope scope,
        ref TimbreHandle slot,
        List<TimbrePlayback> mine,
        TimbreSound[] clips,
        TimbreParameter<float> cutoff,
        TimbreRuntime runtime,
        List<TimbrePlayback> all,
        List<Task> seeks,
        object gate)
    {
        TimbrePlayback? target = mine.Count == 0 ? null : mine[random.Next(mine.Count)];
        switch (random.Next(11))
        {
            case 0:
            case 1:
            {
                TimbreSound clip = clips[random.Next(clips.Length)];
                bool loop = random.Next(2) == 0;
                float volume = random.NextSingle();
                TimbrePlayback playback = scope.Play(clip, start =>
                {
                    start.Loop = loop;
                    start.Volume = volume;
                }, random.Next(3) == 0 ? slot : null);
                Track(playback, mine, all, gate);
                break;
            }
            case 2:
                target?.Pause();
                break;
            case 3:
                target?.Resume();
                break;
            case 4:
                if (target is not null)
                {
                    Task seek = target.SeekAsync(TimeSpan.FromMilliseconds(random.Next(0, 900)));
                    lock (gate)
                    {
                        seeks.Add(seek);
                    }
                }

                break;
            case 5:
                target?.Cancel();
                break;
            case 6:
                if (target is not null)
                {
                    target.Volume = random.NextSingle();
                }

                break;
            case 7:
                if (target is not null && target.Sound.Parameters.Count > 0)
                {
                    target.Set(cutoff, 20f + (random.NextSingle() * 19980f));
                }

                break;
            case 8:
                slot.Cancel();
                break;
            case 9:
                if (random.Next(8) == 0)
                {
                    scope.Dispose();
                    scope = runtime.CreateScope();
                    slot = scope.CreateHandle();
                    mine.Clear();
                }

                break;
            default:
                Track(scope.Play(clips[0], handle: slot), mine, all, gate);
                break;
        }
    }

    private static void Track(TimbrePlayback playback, List<TimbrePlayback> mine, List<TimbrePlayback> all, object gate)
    {
        mine.Add(playback);
        lock (gate)
        {
            all.Add(playback);
        }
    }

    private static TimbreSound[] CreateClips(TimbreParameter<float> cutoff, List<DeterministicTimbreSourceFactory> factories)
    {
        DeterministicTimbreSourceFactory Factory(long frames, int maxRead = int.MaxValue, Action<DeterministicTimbreReader>? configure = null)
        {
            DeterministicTimbreSourceFactory factory = new(frames, maxFramesPerRead: maxRead) { Configure = configure };
            factories.Add(factory);
            return factory;
        }

        return
        [
            new TimbreSound(TimbreSource.FromReader(Factory(48000).Open), loading: TimbreLoading.Preload),
            new TimbreSound(TimbreSource.FromReader(Factory(48000, 333).Open), loading: TimbreLoading.Streaming),
            new TimbreSound(
                TimbreSource.FromReader(Factory(24000).Open),
                loading: TimbreLoading.Preload,
                parameters: [cutoff],
                modifiers: [new LowPass(cutoff: cutoff), new Delay(time: 0.05f, feedback: 0.5f, mix: 0.3f)]),
            new TimbreSound(
                TimbreSource.FromReader(Factory(96000, 700).Open),
                loading: TimbreLoading.Streaming,
                parameters: [cutoff],
                modifiers: [new Delay(time: 0.02f, feedback: 0.7f, mix: 0.5f), new LowPass(cutoff: cutoff)]),
            new TimbreSound(
                TimbreSource.FromReader(Factory(48000, 256, reader => reader.FailAtFrame = 1).Open),
                loading: TimbreLoading.Streaming),
            new TimbreSound(TimbreSource.FromReader(Factory(0).Open), loop: true, loading: TimbreLoading.Preload)
        ];
    }

    private static bool IsContractRejection(Exception exception) => exception switch
    {
        InvalidOperationException => true, // terminal playback or disposed scope/runtime
        ArgumentOutOfRangeException => true, // seek beyond a known duration
        TimbreException { Kind: TimbreErrorKind.VoiceLimitExceeded } => true,
        _ => false
    };
}
