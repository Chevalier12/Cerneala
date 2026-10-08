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
        <TimbreClip Name="Tone">Source = "audio/tone.wav";</TimbreClip>
        <TimbreClip Name="Ramp">Source = "audio/ramp.wav";</TimbreClip>
        <TimbreClip Name="Short">Source = "audio/short.wav";</TimbreClip>
        <TimbreClip Name="Looped">Source = "audio/tone.wav"; Loop = true;</TimbreClip>
        <TimbreClip Name="Filtered">Source = "audio/ramp.wav"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</TimbreClip>
        """;

    // Opacity bands drive one transport statement each; 1.0 starts the occupant.
    private const string Bands = """
        @handle Music;
        @when $self.Opacity
        {
            @if value > 0.95 { @timbre $Ramp as Music; }
            @if value > 0.85 and value < 0.95 { @seek Music to 50ms; }
            @if value > 0.75 and value < 0.85 { @seek Music to 100ms; }
            @if value > 0.65 and value < 0.75 { @timbre $Tone as Music; }
            @if value > 0.45 and value < 0.55 { @resume Music; }
            @if value > 0.25 and value < 0.35 { @cancel Music; }
            @if value < 0.15 { @pause Music; }
        }
        """;

    [Fact]
    public void EmptySlotTransportIsANoOpWithoutAutoplay()
    {
        using MarkupTimbreFixture fixture = new(Button("@handle Music; @on Click { @pause Music; @resume Music; @seek Music to 1s; @cancel Music; }"));

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
            MarkupTimbreFixture.Expected(MarkupTimbreFixture.Ramp, TimbreRig.Budget),
            fixture.Rig.Output.Read(resumedAt, TimbreRig.Budget));
    }

    [Fact]
    public void LatestPendingSeekWinsAndFirstPcmStartsAtItsTarget()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands), attach: false);
        GatedSource gated = GatedSource.Replace(fixture, "Ramp");
        fixture.Attach();
        Border border = fixture.All<Border>().Single();
        TimbrePlayback music = Assert.Single(fixture.Started);

        SetOpacity(fixture, border, 0.9f);
        SetOpacity(fixture, border, 0.8f);
        TimbreRuntimeDiagnostics pending = fixture.Rig.Runtime.GetDiagnostics();
        Assert.Equal((2L, 1L, 0L), (pending.SeeksRequested, pending.SeeksSuperseded, pending.SeeksCompleted));

        gated.Open();
        float[] first = fixture.Rig.StartAsync(music).GetAwaiter().GetResult();

        TimbreRig.AssertPcm(MarkupTimbreFixture.Expected(MarkupTimbreFixture.Ramp, TimbreRig.Budget, sourceStart: 4800), first);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().SeeksCompleted);
    }

    [Fact]
    public void CancelAlsoCancelsAPendingSeek()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands), attach: false);
        GatedSource gated = GatedSource.Replace(fixture, "Ramp");
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
    public void ReplacementCancelsOldOccupantAndItsPendingSeekDoesNotMoveTheNewOne()
    {
        using MarkupTimbreFixture fixture = new(Border(Bands), attach: false);
        GatedSource gated = GatedSource.Replace(fixture, "Ramp");
        fixture.Attach();
        Border border = fixture.All<Border>().Single();
        TimbrePlayback ramp = Assert.Single(fixture.Started);

        SetOpacity(fixture, border, 0.8f);
        SetOpacity(fixture, border, 0.7f);
        TimbrePlayback tone = fixture.Started[1];
        Assert.Equal(TimbrePlaybackState.Canceled, ramp.State);
        gated.Open();

        float[] first = fixture.Rig.StartAsync(tone).GetAwaiter().GetResult();
        TimbreRig.AssertPcm(MarkupTimbreFixture.Expected(MarkupTimbreFixture.Tone, TimbreRig.Budget), first);
        Assert.Equal(0, fixture.Rig.Runtime.GetDiagnostics().SeeksCompleted);

        SetOpacity(fixture, border, 0.3f);
        Assert.Equal(TimbrePlaybackState.Canceled, tone.State);
    }

    [Fact]
    public void TransportOnATerminalOccupantIsAnEmptySlot()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@handle Music; @on Loaded { @timbre $Short as Music; } @on Click { @pause Music; @resume Music; @seek Music to 1ms; }"));
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
    public void LoopDefaultsAndStartOverridesDoNotMutateTheClip()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@on Click { @timbre $Looped; @timbre $Looped(Loop = false); @timbre $Tone(Loop = true); @timbre $Tone; }"));

        fixture.ClickAsync("Play").GetAwaiter().GetResult();

        Assert.Equal([true, false, true, false], fixture.Started.Select(playback => playback.Loop));
        Button button = fixture.All<Button>().Single();
        Assert.True(button.FindResource(new ResourceId<TimbreClip>("Looped")).Loop);
        Assert.False(button.FindResource(new ResourceId<TimbreClip>("Tone")).Loop);
        Assert.Same(fixture.Started[0].Clip, fixture.Started[1].Clip);
    }

    [Fact]
    public void OneHandleAcceptsClipsWithDifferentParameterSchemas()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@handle Music; @on Click { @timbre $Filtered(Cut = 400) as Music; @timbre $Tone(Volume = 0.3) as Music; @timbre $Filtered as Music; }"));

        fixture.ClickAsync("Play").GetAwaiter().GetResult();

        Assert.Equal(3, fixture.Started.Count);
        Assert.Equal(
            [TimbrePlaybackState.Canceled, TimbrePlaybackState.Canceled, TimbrePlaybackState.Pending],
            fixture.Started.Select(playback => playback.State));
        Assert.Equal(0.3f, fixture.Started[1].Volume);
        Assert.Equal(["audio/ramp.wav", "audio/tone.wav", "audio/ramp.wav"], fixture.Started.Select(playback => playback.Clip.Source.Name));
    }

    [Fact]
    public void PausedPlaybacksKeepTheirVoiceAndASynchronousErrorStopsTheBody()
    {
        using MarkupTimbreFixture fixture = new(
            Button(
                "@handle First; @handle Second; " +
                "@on Loaded { @timbre $Tone as First; @timbre $Tone as Second; } " +
                "@on MouseEnter { @pause First; @pause Second; } " +
                "@on Click { @timbre $Ramp; @timbre $Short; }"),
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

    // Replaces a markup clip resource with the same signal behind a reader
    // whose first read waits for Open, so its seeks stay genuinely pending.
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
            TimbreClip clip = TimbreRig.Clip(factory);
            fixture.Element.Resources.SetResource(new ResourceId<TimbreClip>(name), clip);
            return source;
        }

        public void Open() => gate.TrySetResult();
    }
}
