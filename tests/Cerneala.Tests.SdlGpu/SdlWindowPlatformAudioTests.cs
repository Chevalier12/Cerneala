using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;

namespace Cerneala.Tests.SdlGpu;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class SdlApplicationAudioCollection
{
    public const string Name = "SDL application audio";
}

[Collection(SdlApplicationAudioCollection.Name)]
public sealed class SdlWindowPlatformAudioTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public SdlWindowPlatformAudioTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
    }

    [Fact]
    public void APlatformWithoutAnAudioSeamHasNoOutput()
    {
        FakeSdlApi api = new();
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory());

        Assert.Null(platform.TimbreOutput);
        Assert.Equal(1, api.InitializeCount);
    }

    [Fact]
    public void CreatingThePlatformAndItsOutputTouchesNoAudioState()
    {
        FakeSdlApi api = new();
        FakeSdlAudioApi audio = new();
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: audio);

        Assert.IsType<SdlTimbreOutput>(platform.TimbreOutput);
        platform.Dispose();

        Assert.Empty(audio.Operations);
        Assert.Equal(1, api.QuitCount);
    }

    [Fact]
    public void DisposingThePlatformReleasesAudioBeforeSdlQuits()
    {
        FakeSdlApi api = new();
        FakeSdlAudioApi audio = new();
        int sdlQuitsWhenAudioQuit = -1;
        audio.OperationRecorded = name =>
        {
            if (name == "quit")
            {
                sdlQuitsWhenAudioQuit = api.QuitCount;
            }
        };
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: audio);
        SdlTimbreOutputTests.RecordingClient client = new();
        platform.TimbreOutput!.Open(client);

        platform.Dispose();

        Assert.Equal(["init", "open", "resume", "destroy", "quit"], audio.Operations);
        Assert.Equal(0, sdlQuitsWhenAudioQuit);
        Assert.Equal(1, api.QuitCount);
        Assert.Equal(0, audio.AudioReferences);
        Assert.Single(client.Lost);
    }

    [Fact]
    public void AudioDeviceRemovalWatchedOnAnotherThreadReachesTheOutput()
    {
        FakeSdlApi api = new();
        FakeSdlAudioApi audio = new() { Device = 23 };
        using SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: audio);
        SdlTimbreOutputTests.RecordingClient client = new();
        platform.TimbreOutput!.Open(client);

        Thread sdlAudioThread = new(() =>
        {
            api.RaiseWatchedEvent(new SdlEvent(SdlEventKind.AudioDeviceRemoved, Data1: 22));
            api.RaiseWatchedEvent(new SdlEvent(SdlEventKind.AudioDeviceRemoved, Data1: 23));
        });
        sdlAudioThread.Start();
        Assert.True(sdlAudioThread.Join(Timeout));

        TimbreException lost = Assert.IsType<TimbreException>(Assert.Single(client.Lost));
        Assert.Equal(TimbreErrorKind.DeviceUnavailable, lost.Kind);
        platform.TimbreOutput.Close();
    }

    [Fact]
    public void ApplicationTimbreUseThePlatformOutputSharedByTwoWindows()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        FakeSdlAudioApi audio = new();
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: audio);
        WindowApplicationRuntime runtime = new(platform);
        WindowApplicationRuntime.Install(runtime);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown };
        // The application runtime may be created before the window runtime exists.
        TimbreRuntime timbreRuntime = app.TimbreRuntime;
        app.Install(runtime);
        Window first = new() { Width = 200, Height = 120 };
        Window second = new() { Width = 200, Height = 120 };
        first.Show();
        second.Show();

        TimbrePlayback fromFirst = first.Timbre.Play(Constant(96000, 0.25f));
        TimbrePlayback fromSecond = second.Timbre.Play(Constant(96000, 0.125f));
        Assert.True(Task.WhenAll(fromFirst.WhenReady, fromSecond.WhenReady).Wait(Timeout));
        Drive(audio, timbreRuntime, () => audio.Consumed.Length >= 4800 * 2);

        Assert.Same(timbreRuntime, first.Root!.TimbreRuntime);
        Assert.Equal(["init", "open", "resume"], audio.Operations);
        Assert.Contains(0.375f, audio.Consumed);

        first.Close();
        Sync(timbreRuntime);
        Assert.Equal(TimbrePlaybackState.Canceled, fromFirst.State);
        Assert.Equal(TimbrePlaybackState.Playing, fromSecond.State);
        int consumed = audio.Consumed.Length;
        Drive(audio, timbreRuntime, () => audio.Consumed.Length >= consumed + (4800 * 2));
        Assert.Equal(["init", "open", "resume"], audio.Operations);
        Assert.Equal(0.125f, audio.Consumed[^1]);

        int sdlQuitsWhenAudioQuit = -1;
        audio.OperationRecorded = name =>
        {
            if (name == "quit")
            {
                sdlQuitsWhenAudioQuit = api.QuitCount;
            }
        };
        runtime.Dispose();

        Assert.True(fromSecond.Completion.IsCompleted);
        Assert.NotEqual(TimbrePlaybackState.Playing, fromSecond.State);
        Assert.Equal(["init", "open", "resume", "destroy", "quit"], audio.Operations);
        Assert.Equal(0, sdlQuitsWhenAudioQuit);
        Assert.Equal(1, api.QuitCount);
        Assert.True(timbreRuntime.IsDisposed);
    }

    [Fact]
    public void PlayingAndUpdatingTimbreRequestsNoFrames()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        FakeSdlAudioApi audio = new();
        RecordingGraphicsFactory graphics = new();
        using WindowApplicationRuntime runtime = new(new SdlWindowPlatform(api, graphics, audioApi: audio));
        WindowApplicationRuntime.Install(runtime);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown };
        app.Install(runtime);
        Window window = new() { Width = 200, Height = 120 };
        window.Show();
        while (runtime.PumpOnce(TimeSpan.FromMilliseconds(16)))
        {
        }

        RecordingGraphicsSession session = Assert.Single(graphics.Sessions);
        int presents = session.PresentCount;
        TimbrePlayback playback = window.Timbre.Play(Constant(96000, 0.25f));
        Assert.True(playback.WhenReady.Wait(Timeout));
        bool rendered = false;
        for (int frame = 0; frame < 20; frame++)
        {
            playback.Volume = frame % 2 == 0 ? 0.5f : 1f;
            Drive(audio, app.TimbreRuntime, () => true);
            audio.Pull(3840);
            Sync(app.TimbreRuntime);
            rendered |= runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.False(rendered);
        Assert.Equal(presents, session.PresentCount);
        Assert.NotEmpty(audio.Consumed);
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
    }

    // Blocking waits keep every SDL platform call on the test's UI thread.
    private static void Sync(TimbreRuntime runtime) => Assert.True(runtime.SyncAsync().Wait(Timeout));

    private static void Drive(FakeSdlAudioApi audio, TimbreRuntime runtime, Func<bool> done)
    {
        for (int iteration = 0; ; iteration++)
        {
            Assert.True(iteration < 20000, "The device drive did not converge.");
            Sync(runtime);
            if (done())
            {
                return;
            }

            audio.Pull(3840);
        }
    }

    private static TimbreSound Constant(int frames, float value) =>
        new(TimbreSource.FromReader(() => new ConstantReader(frames, value), $"constant-{frames}-{value}"));

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
