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
        DeterministicSoundOutput output = new() { AutoConsume = true, RecordSamples = false };
        SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output, MaxVoices = 24 });
        SoundParameter<float> cutoff = new("Cutoff", 2000f);
        List<DeterministicSoundSourceFactory> factories = [];
        SoundClip[] clips = CreateClips(cutoff, factories);
        List<SoundPlayback> all = [];
        List<Task> seeks = [];
        List<string> unexpected = [];
        object gate = new();

        // Dedicated threads: spinning workers must not starve the thread pool that
        // runs loaders and pumps.
        Thread[] workers = Enumerable.Range(0, Threads).Select(worker => new Thread(() =>
        {
            Random random = new(seed + worker);
            SoundScope scope = runtime.CreateScope();
            SoundHandle slot = scope.CreateHandle();
            List<SoundPlayback> mine = [];
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

        using (SoundScope failureScope = runtime.CreateScope())
        {
            SoundPlayback failing = failureScope.Play(clips[4]);
            Track(failing, [], all, gate);
            Assert.Equal(SoundPlaybackState.Failed, (await TimbreRig.CompletionAsync(failing)).State);
        }

        runtime.Dispose();

        SoundPlayback[] playbacks;
        Task[] seekTasks;
        lock (gate)
        {
            playbacks = all.ToArray();
            seekTasks = seeks.ToArray();
        }

        Assert.Empty(unexpected);
        Assert.NotEmpty(playbacks);
        foreach (SoundPlayback playback in playbacks)
        {
            SoundPlaybackResult result = await TimbreRig.CompletionAsync(playback);
            Assert.Equal(result.State, playback.State);
            Assert.True(result.State is SoundPlaybackState.Completed or SoundPlaybackState.Canceled or SoundPlaybackState.Failed);
            Assert.Equal(result.State == SoundPlaybackState.Failed, result.Error is not null);
            await TimbreRig.ReleasedAsync(playback);
        }

        await HarnessWait.WithTimeout(
            Task.WhenAll(seekTasks.Select(task => task.ContinueWith(_ => { }, TaskScheduler.Default))),
            null,
            "A seek request was left pending after shutdown.");

        SoundRuntimeDiagnostics diagnostics = runtime.GetDiagnostics();
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
        ref SoundScope scope,
        ref SoundHandle slot,
        List<SoundPlayback> mine,
        SoundClip[] clips,
        SoundParameter<float> cutoff,
        SoundRuntime runtime,
        List<SoundPlayback> all,
        List<Task> seeks,
        object gate)
    {
        SoundPlayback? target = mine.Count == 0 ? null : mine[random.Next(mine.Count)];
        switch (random.Next(11))
        {
            case 0:
            case 1:
            {
                SoundClip clip = clips[random.Next(clips.Length)];
                bool loop = random.Next(2) == 0;
                float volume = random.NextSingle();
                SoundPlayback playback = scope.Play(clip, start =>
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
                if (target is not null && target.Clip.Parameters.Count > 0)
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

    private static void Track(SoundPlayback playback, List<SoundPlayback> mine, List<SoundPlayback> all, object gate)
    {
        mine.Add(playback);
        lock (gate)
        {
            all.Add(playback);
        }
    }

    private static SoundClip[] CreateClips(SoundParameter<float> cutoff, List<DeterministicSoundSourceFactory> factories)
    {
        DeterministicSoundSourceFactory Factory(long frames, int maxRead = int.MaxValue, Action<DeterministicSoundReader>? configure = null)
        {
            DeterministicSoundSourceFactory factory = new(frames, maxFramesPerRead: maxRead) { Configure = configure };
            factories.Add(factory);
            return factory;
        }

        return
        [
            new SoundClip(SoundSource.FromReader(Factory(48000).Open), loading: SoundLoading.Preload),
            new SoundClip(SoundSource.FromReader(Factory(48000, 333).Open), loading: SoundLoading.Streaming),
            new SoundClip(
                SoundSource.FromReader(Factory(24000).Open),
                loading: SoundLoading.Preload,
                parameters: [cutoff],
                modifiers: [new LowPass(cutoff: cutoff), new Delay(time: 0.05f, feedback: 0.5f, mix: 0.3f)]),
            new SoundClip(
                SoundSource.FromReader(Factory(96000, 700).Open),
                loading: SoundLoading.Streaming,
                parameters: [cutoff],
                modifiers: [new Delay(time: 0.02f, feedback: 0.7f, mix: 0.5f), new LowPass(cutoff: cutoff)]),
            new SoundClip(
                SoundSource.FromReader(Factory(48000, 256, reader => reader.FailAtFrame = 1).Open),
                loading: SoundLoading.Streaming),
            new SoundClip(SoundSource.FromReader(Factory(0).Open), loop: true, loading: SoundLoading.Preload)
        ];
    }

    private static bool IsContractRejection(Exception exception) => exception switch
    {
        InvalidOperationException => true, // terminal playback or disposed scope/runtime
        ArgumentOutOfRangeException => true, // seek beyond a known duration
        SoundException { Kind: SoundErrorKind.VoiceLimitExceeded } => true,
        _ => false
    };
}
