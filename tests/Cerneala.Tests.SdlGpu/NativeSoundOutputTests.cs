using Cerneala.Platforms.Sdl3;
using Cerneala.Timbre;
using Cerneala.UI;
using Cerneala.UI.Controls;
using Cerneala.UI.Hosting.Windowing;
using SDL3;

namespace Cerneala.Tests.SdlGpu;

// Real SDL3 audio on the opt-in native runner. The default driver must have a
// playback device; the dummy driver checks interop/lifecycle only.
[Collection(SdlNativeTestCollection.Name)]
public sealed class NativeSoundOutputTests : IDisposable
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public NativeSoundOutputTests()
    {
        Application.ResetForTesting();
        WindowApplicationRuntime.ResetForTesting();
    }

    public void Dispose()
    {
        WindowApplicationRuntime.ResetForTesting();
        Application.ResetForTesting();
        SDL.ResetHint(SDL.Hints.AudioDriver);
    }

    [TimbreAudioDeviceFact]
    [Trait("Category", "Native")]
    public void ARealDevicePlaysToCompletionAndReleasesEverythingBeforeSdlQuits()
    {
        PlayToCompletionAndReleaseBeforeSdlQuits(driver: null);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void TheDummyDriverPlaysToCompletionAndReleasesEverythingBeforeSdlQuits()
    {
        PlayToCompletionAndReleaseBeforeSdlQuits(driver: "dummy");
    }

    private static void PlayToCompletionAndReleaseBeforeSdlQuits(string? driver)
    {
        if (driver is not null)
        {
            Assert.True(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, driver, SDL.HintPriority.Override));
        }

        long lateBefore = NativeSdlAudioApi.LateCallbacks;
        int handlersBefore = NativeSdlAudioApi.RegisteredHandlers;
        NativeSdlApi api = new();
        SdlPlatformLifetime lifetime = new(api);
        SdlSoundOutput output = new(new NativeSdlAudioApi());
        SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });
        try
        {
            SoundPlayback playback = runtime.CreateScope().Play(Tone(14400));
            SoundPlaybackResult result = Wait(playback.Completion);

            Assert.Equal(SoundPlaybackState.Completed, result.State);
            Assert.Equal((uint)SDL.InitFlags.Audio, (uint)SDL.WasInit(SDL.InitFlags.Audio));
            if (driver is not null)
            {
                Assert.Equal(driver, SDL.GetCurrentAudioDriver());
            }
            else
            {
                // Physical delivery must not be satisfied by the interop driver.
                Assert.NotEqual("dummy", SDL.GetCurrentAudioDriver());
            }

            SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
            Assert.Equal(1, diagnostics.OpenCount);
            Assert.True(diagnostics.IsOpen);
            Assert.NotEqual(0u, diagnostics.Device);
            Assert.True(diagnostics.DeviceFormat.Frequency > 0);
            Assert.True(diagnostics.Requests > 0);
            Assert.True(diagnostics.FramesSubmitted >= 14400);
            Assert.InRange(diagnostics.QueueHighWaterFrames, 0, 1920);
        }
        finally
        {
            runtime.Dispose();
        }

        Assert.False(output.GetDiagnostics().IsOpen);
        Assert.Equal(1, output.GetDiagnostics().CloseCount);
        Assert.Equal(0u, (uint)SDL.WasInit(SDL.InitFlags.Audio));
        Assert.NotEqual(0u, (uint)SDL.WasInit(SDL.InitFlags.Video));
        Assert.Equal(handlersBefore, NativeSdlAudioApi.RegisteredHandlers);
        lifetime.Dispose();
        Assert.Equal(lateBefore, NativeSdlAudioApi.LateCallbacks);
        Assert.Equal(0, NativeSdlAudioApi.HandlerFailures);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void SdlConvertsTheMixFormatForADeviceWithAnotherRateAndSampleFormat()
    {
        Assert.True(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, "dummy", SDL.HintPriority.Override));
        using SdlPlatformLifetime lifetime = new(new NativeSdlApi());
        // Another logical device opened first fixes the shared physical format,
        // so the Timbre stream has to be converted by SDL.
        Assert.True(SDL.InitSubSystem(SDL.InitFlags.Audio));
        SDL.AudioSpec deviceSpec = new() { Format = SDL.AudioFormat.AudioS16LE, Channels = 2, Freq = 44100 };
        uint other = SDL.OpenAudioDevice(SDL.AudioDeviceDefaultPlayback, in deviceSpec);
        Assert.NotEqual(0u, other);
        try
        {
            SdlSoundOutput output = new(new NativeSdlAudioApi());
            using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });

            SoundPlaybackResult result = Wait(runtime.CreateScope().Play(Tone(9600)).Completion);

            Assert.Equal(SoundPlaybackState.Completed, result.State);
            SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
            Assert.Equal(new SdlAudioFormat(0x8010, 2, 44100), diagnostics.DeviceFormat);
            Assert.True(diagnostics.FramesSubmitted >= 9600);
            Assert.InRange(diagnostics.QueueHighWaterFrames, 0, 1920);
        }
        finally
        {
            SDL.CloseAudioDevice(other);
            SDL.QuitSubSystem(SDL.InitFlags.Audio);
        }

        Assert.Equal(0u, (uint)SDL.WasInit(SDL.InitFlags.Audio));
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void AnAbsentAudioDriverFailsThePlaybackExplicitlyWithoutLeakingTheSubsystem()
    {
        Assert.True(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, "cerneala-absent-driver", SDL.HintPriority.Override));
        using SdlPlatformLifetime lifetime = new(new NativeSdlApi());
        SdlSoundOutput output = new(new NativeSdlAudioApi());
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });

        SoundPlaybackResult result = Wait(runtime.CreateScope().Play(Tone(4800)).Completion);

        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.DeviceUnavailable, result.Error!.Kind);
        Assert.Contains("cerneala-absent-driver", result.Error.InnerException!.Message, StringComparison.Ordinal);
        Assert.Equal(0u, (uint)SDL.WasInit(SDL.InitFlags.Audio));
        Assert.Equal(0, output.GetDiagnostics().OpenCount);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void ARemovedDeviceEventFailsThePlaybackWithoutReconnecting()
    {
        Assert.True(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, "dummy", SDL.HintPriority.Override));
        using SdlWindowPlatform platform = new(new NativeSdlApi(), new RecordingGraphicsFactory(), audioApi: new NativeSdlAudioApi());
        SdlSoundOutput output = Assert.IsType<SdlSoundOutput>(platform.SoundOutput);
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });
        SoundPlayback playback = runtime.CreateScope().Play(Tone(480000));
        Assert.True(SpinWait.SpinUntil(() => output.GetDiagnostics().Requests > 4, Timeout));
        uint device = output.GetDiagnostics().Device;

        SDL.Event removed = default;
        removed.ADevice.Type = SDL.EventType.AudioDeviceRemoved;
        removed.ADevice.Which = device;
        Assert.True(SDL.PushEvent(ref removed));

        SoundPlaybackResult result = Wait(playback.Completion);
        Assert.Equal(SoundPlaybackState.Failed, result.State);
        Assert.Equal(SoundErrorKind.DeviceUnavailable, result.Error!.Kind);
        Assert.True(SpinWait.SpinUntil(() => !output.GetDiagnostics().IsOpen, Timeout));
        SdlSoundOutputDiagnostics diagnostics = output.GetDiagnostics();
        Assert.Equal(1, diagnostics.DevicesLost);
        Assert.Equal(1, diagnostics.OpenCount);
        Assert.Equal(1, diagnostics.CloseCount);
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void AudioKeepsFlowingWhileOneWindowIsHiddenAnotherIsClosedAndNothingRedraws()
    {
        // Real windows; the dummy driver's device thread consumes on its own clock.
        Assert.True(SDL.SetHintWithPriority(SDL.Hints.AudioDriver, "dummy", SDL.HintPriority.Override));
        NativeSdlApi api = new();
        SdlWindowPlatform platform = new(api, new RecordingGraphicsFactory(), audioApi: new NativeSdlAudioApi());
        WindowApplicationRuntime runtime = new(platform);
        WindowApplicationRuntime.Install(runtime);
        Application app = new() { ShutdownMode = ApplicationShutdownMode.OnExplicitShutdown };
        app.Install(runtime);
        Window hidden = new() { Title = "Timbre hidden", Width = 160, Height = 120 };
        Window closed = new() { Title = "Timbre closed", Width = 160, Height = 120 };
        hidden.Show();
        closed.Show();
        SdlSoundOutput output = Assert.IsType<SdlSoundOutput>(platform.SoundOutput);

        SoundPlayback music = app.Sounds.Play(Tone(96000));
        SoundPlayback windowed = closed.Sounds.Play(Tone(96000));
        Assert.True(SpinWait.SpinUntil(() => output.GetDiagnostics().Requests > 4, Timeout));

        hidden.Hide();
        closed.Close();
        Assert.Equal(SoundPlaybackState.Canceled, windowed.State);
        long requests = output.GetDiagnostics().Requests;
        long submitted = output.GetDiagnostics().FramesSubmitted;

        // No PumpOnce: the UI neither redraws nor processes events here.
        Assert.True(SpinWait.SpinUntil(
            () => output.GetDiagnostics().Requests >= requests + 20 &&
                output.GetDiagnostics().FramesSubmitted >= submitted + 9600,
            Timeout));
        Assert.Equal(SoundPlaybackState.Playing, music.State);
        Assert.Equal(1, output.GetDiagnostics().OpenCount);
        Assert.Equal(0, output.GetDiagnostics().CloseCount);

        runtime.Dispose();

        Assert.True(music.Completion.IsCompleted);
        Assert.False(output.GetDiagnostics().IsOpen);
        Assert.Equal(0u, (uint)SDL.WasInit(0));
    }

    private static T Wait<T>(Task<T> task)
    {
        Assert.True(task.Wait(Timeout), "The playback did not finish in time.");
        return task.Result;
    }

    private static SoundClip Tone(int frames) =>
        new(SoundSource.FromReader(() => new ToneReader(frames), $"tone-{frames}"));

    private sealed class ToneReader(int frames) : SoundReader
    {
        private long position;

        public override long? LengthFrames => frames;

        public override ValueTask<SoundReadResult> ReadAsync(Memory<float> destination, CancellationToken cancellationToken)
        {
            int count = (int)Math.Min(destination.Length / 2, frames - position);
            Span<float> span = destination.Span;
            for (int index = 0; index < count; index++)
            {
                float value = 0.05f * MathF.Sin(2 * MathF.PI * 440 * (position + index) / 48000f);
                span[index * 2] = value;
                span[(index * 2) + 1] = value;
            }

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
