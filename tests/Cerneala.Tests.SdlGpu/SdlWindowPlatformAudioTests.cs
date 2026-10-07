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

        Assert.Null(platform.SoundOutput);
        Assert.Equal(1, api.InitializeCount);
    }

    [Fact]
    public void CreatingThePlatformAndItsOutputTouchesNoAudioState()
    {
        FakeSdlApi api = new();
        FakeSdlAudioApi audio = new();
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: audio);

        Assert.IsType<SdlSoundOutput>(platform.SoundOutput);
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
        SdlSoundOutputTests.RecordingClient client = new();
        platform.SoundOutput!.Open(client);

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
        SdlSoundOutputTests.RecordingClient client = new();
        platform.SoundOutput!.Open(client);

        Thread sdlAudioThread = new(() =>
        {
            api.RaiseWatchedEvent(new SdlEvent(SdlEventKind.AudioDeviceRemoved, Data1: 22));
            api.RaiseWatchedEvent(new SdlEvent(SdlEventKind.AudioDeviceRemoved, Data1: 23));
        });
        sdlAudioThread.Start();
        Assert.True(sdlAudioThread.Join(Timeout));

        SoundException lost = Assert.IsType<SoundException>(Assert.Single(client.Lost));
        Assert.Equal(SoundErrorKind.DeviceUnavailable, lost.Kind);
        platform.SoundOutput.Close();
    }

    [Fact]
    public void ApplicationSoundsUseThePlatformOutputSharedByTwoWindows()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1, WindowDisplayScale = 1 };
        FakeSdlAudioApi audio = new();
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: audio);
        WindowApplicationRuntime runtime = new(platform);
        WindowApplicationRuntime.Install(runtime);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown };
        // The application runtime may be created before the window runtime exists.
        SoundRuntime soundRuntime = app.SoundRuntime;
        app.Install(runtime);
        Window first = new() { Width = 200, Height = 120 };
        Window second = new() { Width = 200, Height = 120 };
        first.Show();
        second.Show();

        SoundPlayback fromFirst = first.Sounds.Play(Constant(96000, 0.25f));
        SoundPlayback fromSecond = second.Sounds.Play(Constant(96000, 0.125f));
        Assert.True(Task.WhenAll(fromFirst.WhenReady, fromSecond.WhenReady).Wait(Timeout));
        Drive(audio, soundRuntime, () => audio.Consumed.Length >= 4800 * 2);

        Assert.Same(soundRuntime, first.Root!.SoundRuntime);
        Assert.Equal(["init", "open", "resume"], audio.Operations);
        Assert.Contains(0.375f, audio.Consumed);

        first.Close();
        Sync(soundRuntime);
        Assert.Equal(SoundPlaybackState.Canceled, fromFirst.State);
        Assert.Equal(SoundPlaybackState.Playing, fromSecond.State);
        int consumed = audio.Consumed.Length;
        Drive(audio, soundRuntime, () => audio.Consumed.Length >= consumed + (4800 * 2));
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
        Assert.NotEqual(SoundPlaybackState.Playing, fromSecond.State);
        Assert.Equal(["init", "open", "resume", "destroy", "quit"], audio.Operations);
        Assert.Equal(0, sdlQuitsWhenAudioQuit);
        Assert.Equal(1, api.QuitCount);
        Assert.True(soundRuntime.IsDisposed);
    }

    [Fact]
    public void PlayingAndUpdatingSoundsRequestsNoFrames()
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
        SoundPlayback playback = window.Sounds.Play(Constant(96000, 0.25f));
        Assert.True(playback.WhenReady.Wait(Timeout));
        bool rendered = false;
        for (int frame = 0; frame < 20; frame++)
        {
            playback.Volume = frame % 2 == 0 ? 0.5f : 1f;
            Drive(audio, app.SoundRuntime, () => true);
            audio.Pull(3840);
            Sync(app.SoundRuntime);
            rendered |= runtime.PumpOnce(TimeSpan.FromMilliseconds(16));
        }

        Assert.False(rendered);
        Assert.Equal(presents, session.PresentCount);
        Assert.NotEmpty(audio.Consumed);
        Assert.Equal(SoundPlaybackState.Playing, playback.State);
    }

    // Blocking waits keep every SDL platform call on the test's UI thread.
    private static void Sync(SoundRuntime runtime) => Assert.True(runtime.SyncAsync().Wait(Timeout));

    private static void Drive(FakeSdlAudioApi audio, SoundRuntime runtime, Func<bool> done)
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

    private static SoundClip Constant(int frames, float value) =>
        new(SoundSource.FromReader(() => new ConstantReader(frames, value), $"constant-{frames}-{value}"));

    private sealed class ConstantReader(int frames, float value) : SoundReader
    {
        private long position;

        public override long? LengthFrames => frames;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int count = (int)Math.Min(destination.Length / 2, frames - position);
            destination.Span[..(count * 2)].Fill(value);
            position += count;
            return ValueTask.FromResult(new SoundReadResult(count, position == frames));
        }

        public override ValueTask SeekAsync(long frame, CancellationToken cancellationToken)
        {
            position = frame;
            return ValueTask.CompletedTask;
        }
    }
}
