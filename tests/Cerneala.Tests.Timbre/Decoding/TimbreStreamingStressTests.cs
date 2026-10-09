using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Decoding;

// 100 seeded cycles of start/pause/resume/seek/loop/cancel/replace over real
// decoded streams, every step synchronized on engine signals (no sleeps),
// then shutdown: readers, pumps, reservations, buffers and file streams all
// return to baseline.
public sealed class TimbreStreamingStressTests
{
    private const int Cycles = 100;

    [Theory]
    [InlineData(20261007)]
    public async Task TransportCyclesOverDecodedStreamsReturnEveryResourceToBaseline(int seed)
    {
        // The sink only consumes when a step drives it, so every step ends at a
        // mixer wait point (a free-running sink would never let SyncAsync settle).
        DeterministicTimbreOutput output = new() { RecordSamples = false };
        TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output });
        TimbreScope scope = runtime.CreateScope();
        TimbreHandle slot = scope.CreateHandle();
        ObservedSource[] sources =
        [
            new(DecodingCorpus.PathOf("mp3-mpeg1-44100-stereo-cbr128.mp3"), loop: true),
            new(DecodingCorpus.PathOf("vorbis-44100-stereo-q4.ogg")),
            new(DecodingCorpus.PathOf("opus-stereo-48000-96k.opus"), loop: true),
            new(WavFixture.Write("stress.wav", 22050, 1, 16, seconds: 1)),
        ];
        Random random = new(seed);
        List<TimbrePlayback> all = [];

        for (int cycle = 0; cycle < Cycles; cycle++)
        {
            ObservedSource source = sources[random.Next(sources.Length)];
            TimbrePlayback playback = random.Next(3) == 0 ? scope.Play(source.Clip) : scope.Play(source.Clip, handle: slot);
            all.Add(playback);
            await HarnessWait.WithTimeout(playback.WhenReady, null, "Playback did not become ready.");
            for (int step = random.Next(1, 6); step > 0 && !playback.Completion.IsCompleted; step--)
            {
                try
                {
                    switch (random.Next(5))
                    {
                        case 0:
                            playback.Pause();
                            break;
                        case 1:
                            playback.Resume();
                            break;
                        case 2:
                            long length = (long)Math.Round(playback.Duration!.Value.TotalSeconds * TimbreRuntime.SampleRate);
                            Task seek = playback.SeekAsync(TimeSpan.FromSeconds(random.Next((int)length) / (double)TimbreRuntime.SampleRate));
                            output.ConsumeAll(); // dispatch follows the old-position release fade
                            if (random.Next(2) == 0)
                            {
                                await HarnessWait.WithTimeout(seek.ContinueWith(_ => { }, TaskScheduler.Default), null, "Seek did not finish.");
                            }

                            break;
                        case 3:
                            output.ConsumeAll();
                            await HarnessWait.WithTimeout(runtime.SyncAsync(), null, "Mixer did not reach a wait point.");
                            break;
                        default:
                            playback.Cancel();
                            break;
                    }
                }
                catch (InvalidOperationException) when (playback.Completion.IsCompleted)
                {
                    // Transport on a playback that ended meanwhile is rejected.
                }
            }

            output.ConsumeAll();
            await HarnessWait.WithTimeout(runtime.SyncAsync(), null, "Mixer did not reach a wait point.");
            if (random.Next(4) == 0)
            {
                playback.Cancel();
                output.ConsumeAll();
                await TimbreRig.ReleasedAsync(playback);
            }
        }

        foreach (TimbrePlayback playback in all)
        {
            playback.Cancel();
        }

        output.ConsumeAll(); // all remaining release fades can share one output block

        foreach (TimbrePlayback playback in all)
        {
            await TimbreRig.ReleasedAsync(playback);
            Assert.True(playback.Completion.IsCompleted);
        }

        TimbreRuntimeDiagnostics drained = runtime.GetDiagnostics();
        Assert.Equal(0, drained.LiveReaders);
        Assert.Equal(0, drained.LiveSourcePumps);
        Assert.Equal(0, drained.ReaderMemoryBytes);
        Assert.Equal(0, drained.StreamingBufferBytes);
        Assert.Equal(0, drained.ActiveVoices);
        runtime.Dispose();

        ObservedFileStream[] streams = sources.SelectMany(source => source.Streams).ToArray();
        Assert.True(streams.Length >= Cycles);
        Assert.All(streams, stream => Assert.True(stream.IsDisposed));
        TimbreRuntimeDiagnostics final = runtime.GetDiagnostics();
        Assert.Equal(0, final.PendingLoads);
        Assert.Equal(0, final.LiveScopes);
        Assert.False(final.OutputOpen);
    }
}
