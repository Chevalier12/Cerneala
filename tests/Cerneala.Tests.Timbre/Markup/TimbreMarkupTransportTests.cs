using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Timbre.Markup;

// Pause/Resume/Seek/Loop lowered from markup and driven through user input or
// reactive condition sources, observed on the real engine and its PCM. The
// UI root is thread-affine, so tests stay on the test thread and block only on
// engine tasks.
public sealed class TimbreMarkupTransportTests
{
    private const string Clips = """
        <TimbreClip Name="Sounds">
          @parameter Cut: float = 900;
          @sound Music { Source = "audio/ramp.wav"; }
          @sound Tone { Source = "audio/tone.wav"; }
          @sound Short { Source = "audio/short.wav"; }
          @sound Looped { Source = "audio/tone.wav"; Loop = true; }
          @sound Filtered { Source = "audio/ramp.wav"; @modifier LowPass { Cutoff = Cut; } }
        </TimbreClip>
        """;

    // Opacity bands drive one command each on the Music sound; 1.0 starts it
    // and 0.7 restarts it.
    private const string Bands = """
        @timbre $Sounds;
        @when $self.Opacity
        {
            @if value > 0.95 { @play $self.timbre.Music; }
            @if value > 0.85 and value < 0.95 { @seek $self.timbre.Music to 50ms; }
            @if value > 0.75 and value < 0.85 { @seek $self.timbre.Music to 100ms; }
            @if value > 0.65 and value < 0.75 { @play $self.timbre.Music; }
            @if value > 0.45 and value < 0.55 { @resume $self.timbre.Music; }
            @if value > 0.25 and value < 0.35 { @stop $self.timbre.Music; }
            @if value < 0.15 { @pause $self.timbre.Music; }
        }
        """;

    [Fact]
    public void TransportOnASoundThatIsNotPlayingIsANoOp()
    {
        using MarkupTimbreFixture fixture = new(Button("@timbre $Sounds; @on Click { @pause $self.timbre.Tone; @resume $self.timbre.Tone; @seek $self.timbre.Tone to 1s; @stop $self.timbre.Tone; }"));

        fixture.ClickAsync("Play").GetAwaiter().GetResult();

        Assert.Empty(fixture.Started);
        TimbreRuntimeDiagnostics diagnostics = fixture.Rig.Runtime.GetDiagnostics();
        Assert.Equal((0L, 0L, 0L), (diagnostics.PausesApplied, diagnostics.ResumesApplied, diagnostics.SeeksRequested));
    }

    [Fact]
    public void PausePendingPreventsFirstPcmUntilResume()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands));
        Border border = fixture.All<Border>().Single();
        TimbrePlayback music = Assert.Single(fixture.Started);

        SetOpacity(fixture, border, 0.1f);
        Assert.Equal(TimbrePlaybackState.Paused, music.State);
        fixture.Rig.ReadyAsync(music).GetAwaiter().GetResult();
        fixture.Rig.Output.Release();
        fixture.Rig.SyncAsync().GetAwaiter().GetResult();
        Assert.All(fixture.Rig.Output.ReadAll(), sample => Assert.Equal(0f, sample));

        long resumedAt = fixture.Rig.Output.SubmittedFrames;
        SetOpacity(fixture, border, 0.5f);
        fixture.Rig.Output.ConsumeAll();
        fixture.Rig.Output.WaitForSubmittedFramesAsync(resumedAt + TimbreRig.Budget).GetAwaiter().GetResult();

        Assert.Equal(TimbrePlaybackState.Playing, music.State);
        TimbreRig.AssertPcm(
            TimbreRig.Ramp(MarkupTimbreFixture.Expected(MarkupTimbreFixture.Ramp, TimbreRig.Budget)),
            fixture.Rig.Output.Read(resumedAt, TimbreRig.Budget));
    }

    [Fact]
    public void LatestPendingSeekWinsAndFirstPcmStartsAtItsTarget()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands), attach: false);
        GatedSource gated = GatedSource.Replace(fixture, "Music");
        fixture.Attach();
        Border border = fixture.All<Border>().Single();
        TimbrePlayback music = Assert.Single(fixture.Started);

        SetOpacity(fixture, border, 0.9f);
        SetOpacity(fixture, border, 0.8f);
        TimbreRuntimeDiagnostics pending = fixture.Rig.Runtime.GetDiagnostics();
        Assert.Equal((2L, 1L, 0L), (pending.SeeksRequested, pending.SeeksSuperseded, pending.SeeksCompleted));

        gated.Open();
        float[] first = fixture.Rig.StartAsync(music).GetAwaiter().GetResult();

        TimbreRig.AssertPcm(TimbreRig.Ramp(MarkupTimbreFixture.Expected(MarkupTimbreFixture.Ramp, TimbreRig.Budget, sourceStart: 4800)), first);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().SeeksCompleted);
    }

    [Fact]
    public void CancelAlsoCancelsAPendingSeek()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands), attach: false);
        GatedSource gated = GatedSource.Replace(fixture, "Music");
        fixture.Attach();
        Border border = fixture.All<Border>().Single();
        TimbrePlayback music = Assert.Single(fixture.Started);

        SetOpacity(fixture, border, 0.8f);
        SetOpacity(fixture, border, 0.3f);
        gated.Open();

        Assert.Equal(TimbrePlaybackState.Canceled, TimbreRig.CompletionAsync(music).GetAwaiter().GetResult().State);
        TimbreRuntimeDiagnostics diagnostics = fixture.Rig.Runtime.GetDiagnostics();
        Assert.Equal((1L, 0L), (diagnostics.SeeksRequested, diagnostics.SeeksCompleted));
    }

    [Fact]
    public void RestartCancelsTheRunningPlaybackAndItsPendingSeekDoesNotMoveTheNewOne()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands), attach: false);
        GatedSource gated = GatedSource.Replace(fixture, "Music");
        fixture.Attach();
        Border border = fixture.All<Border>().Single();
        TimbrePlayback first = Assert.Single(fixture.Started);

        SetOpacity(fixture, border, 0.8f);
        SetOpacity(fixture, border, 0.7f);
        TimbrePlayback restarted = fixture.Started[1];
        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
        gated.Open();

        float[] pcm = fixture.Rig.StartAsync(restarted).GetAwaiter().GetResult();
        TimbreRig.AssertPcm(TimbreRig.Ramp(MarkupTimbreFixture.Expected(MarkupTimbreFixture.Ramp, TimbreRig.Budget)), pcm);
        Assert.Equal(0, fixture.Rig.Runtime.GetDiagnostics().SeeksCompleted);

        SetOpacity(fixture, border, 0.3f);
        Assert.Equal(TimbrePlaybackState.Canceled, restarted.State);
    }

    [Fact]
    public void TransportOnACompletedSoundIsANoOp()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@timbre $Sounds; @on Loaded { @play $self.timbre.Short; } @on Click { @pause $self.timbre.Short; @resume $self.timbre.Short; @seek $self.timbre.Short to 1ms; }"));
        TimbrePlayback music = Assert.Single(fixture.Started);
        fixture.Rig.ReadyAsync(music).GetAwaiter().GetResult();
        fixture.Rig.Output.Release();
        for (int attempt = 0; attempt < 50 && !music.Completion.IsCompleted; attempt++)
        {
            fixture.Rig.SyncAsync().GetAwaiter().GetResult();
            fixture.Rig.Output.ConsumeAll();
        }

        Assert.Equal(TimbrePlaybackState.Completed, TimbreRig.CompletionAsync(music).GetAwaiter().GetResult().State);

        fixture.ClickAsync("Play").GetAwaiter().GetResult();

        TimbreRuntimeDiagnostics diagnostics = fixture.Rig.Runtime.GetDiagnostics();
        Assert.Equal((0L, 0L, 0L), (diagnostics.PausesApplied, diagnostics.ResumesApplied, diagnostics.SeeksRequested));
        Assert.Single(fixture.Started);
    }

    [Fact]
    public void LoopComesFromTheSound()
    {
        using MarkupTimbreFixture fixture = new(Button("@timbre $Sounds; @on Click { @play $self.timbre.Looped; @play $self.timbre.Tone; }"));

        fixture.ClickAsync("Play").GetAwaiter().GetResult();

        Assert.Equal([true, false], fixture.Started.Select(playback => playback.Loop));
        TimbreClipDefinition clip = fixture.All<Button>().Single().FindResource(new ResourceId<TimbreClipDefinition>("Sounds"));
        Assert.True(clip.Sounds["Looped"].Sound.Loop);
        Assert.False(clip.Sounds["Tone"].Sound.Loop);
    }

    // "@timbre $Sounds(Cut = 400);" sets the parameter on every playback of
    // a sound that uses it; other sounds and the clip are unchanged.
    [Fact]
    public void AttachmentArgumentsReachOnlyTheSoundsThatUseTheParameter()
    {
        using MarkupTimbreFixture fixture = new(Button("@timbre $Sounds(Cut = 400); @on Click { @play $self.timbre.Filtered; @play $self.timbre.Tone; }"));

        fixture.ClickAsync("Play").GetAwaiter().GetResult();

        Assert.Equal(["audio/ramp.wav", "audio/tone.wav"], fixture.Started.Select(playback => playback.Sound.Source.Name));
        Assert.Equal(400f, fixture.Started[0].GetMotionSlotValue(1));
        Assert.Empty(fixture.Started[1].Sound.Parameters);
        TimbreClipDefinition clip = fixture.All<Button>().Single().FindResource(new ResourceId<TimbreClipDefinition>("Sounds"));
        Assert.Equal(900f, ((TimbreParameter<float>)clip.Parameters.Single()).DefaultValue);
    }

    [Fact]
    public void PausedPlaybacksKeepTheirVoiceAndASynchronousErrorStopsTheBody()
    {
        using MarkupTimbreFixture fixture = new(
            Button(
                "@timbre $Sounds; " +
                "@on Loaded { @play $self.timbre.Tone; @play $self.timbre.Looped; } " +
                "@on MouseEnter { @pause $self.timbre.Tone; @pause $self.timbre.Looped; } " +
                "@on Click { @play $self.timbre.Music; @play $self.timbre.Short; }"),
            configure: options => options.MaxVoices = 2);
        fixture.HoverAsync("Play").GetAwaiter().GetResult();
        Assert.All(fixture.Started, playback => Assert.Equal(TimbrePlaybackState.Paused, playback.State));

        TimbreException error = Assert.Throws<TimbreException>(() => fixture.ClickAsync("Play").GetAwaiter().GetResult());

        Assert.Equal(TimbreErrorKind.VoiceLimitExceeded, error.Kind);
        Assert.Equal(2, fixture.Started.Count);
    }

    private static void SetOpacity(MarkupTimbreFixture fixture, UIElement element, float opacity)
    {
        element.Opacity = opacity;
        fixture.Pump();
    }

    private static string Button(string aspect) =>
        "<Button Content=\"Play\"><Button.Resources>" + Clips + "</Button.Resources><Button.Aspect>" + aspect + "</Button.Aspect></Button>";

    private static string Border(string aspect) =>
        "<Border Width=\"40\" Height=\"40\"><Border.Resources>" + Clips + "</Border.Resources><Border.Aspect>" + aspect + "</Border.Aspect></Border>";

    // Replaces one sound of the markup clip (before the Aspect is applied)
    // with the same ramp signal behind a reader whose first read waits for
    // Open, so its seeks stay genuinely pending.
    private sealed class GatedSource
    {
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public static GatedSource Replace(MarkupTimbreFixture fixture, string name)
        {
            GatedSource source = new();
            DeterministicTimbreSourceFactory factory = new(MarkupTimbreFixture.Ramp.Frames, MarkupTimbreFixture.Ramp.Signal)
            {
                Configure = reader => reader.ReadGate = token => source.gate.Task.WaitAsync(token)
            };
            ResourceId<TimbreClipDefinition> id = new("Sounds");
            TimbreClipDefinition markup = fixture.Element.FindResource(id);
            fixture.Element.Resources.SetResource(id, new TimbreClipDefinition(
                markup.Name,
                markup.Sounds.Values.Select(sound => sound.Name == name ? new TimbreClipSound(name, TimbreRig.Clip(factory), sound.AutoPlay) : sound),
                markup.Parameters));
            return source;
        }

        public void Open() => gate.TrySetResult();
    }
}
