using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Timbre.Markup;

// The same sources, schema, block partitions and operations driven once by a
// manual C# consumer and once by executed generated markup (factory and
// paired partial). Both must produce the same PCM, operation trace and states
// on the real engine with the deterministic sink.
public sealed class SoundMarkupParityTests
{
    private const string Clips = """
        <SoundClip Name="Confirm">
          Source = "audio/ramp.wav";
          Volume = 0.8;
          @parameter Cut: float = 1200;
          @parameter Echo: float = 0.15;
          @modifier LowPass { Cutoff = Cut; }
          @modifier Delay { Time = 20ms; Feedback = 0.4; Mix = Echo; }
        </SoundClip>
        <SoundClip Name="Blip">Source = "audio/short.wav";</SoundClip>
        <SoundClip Name="Unused">Source = "audio/tone.wav";</SoundClip>
        """;

    private const string Aspect = """
        @handle Playback;
        @when $self.Opacity
        {
            @if value > 0.95 { @sound $Confirm(Volume = 0.5, Cut = 800) as Playback; @sound $Blip(Loop = true); }
            @if value > 0.85 and value < 0.95 { @sound $Confirm(Cut = 2000, Echo = 0.6) as Playback; }
            @if value > 0.75 and value < 0.85 { @pause Playback; }
            @if value > 0.65 and value < 0.75 { @resume Playback; }
            @if value > 0.55 and value < 0.65 { @seek Playback to 100ms; }
            @if value > 0.45 and value < 0.55 { @cancel Playback; }
        }
        """;

    private static readonly float[] Bands = [0.9f, 0.8f, 0.7f, 0.6f, 0.5f];

    [Fact]
    public void FactoryMarkupMatchesManualCSharp() =>
        AssertParity(new MarkupSoundFixture(
            "<Border Width=\"40\" Height=\"40\"><Border.Resources>" + Clips + "</Border.Resources><Border.Aspect>" + Aspect + "</Border.Aspect></Border>",
            "ParityFactory.crn"));

    [Fact]
    public void PairedPartialMarkupMatchesManualCSharp() =>
        AssertParity(new MarkupSoundFixture(
            "<UserControl><Border Width=\"40\" Height=\"40\"><Border.Resources>" + Clips + "</Border.Resources><Border.Aspect>" + Aspect + "</Border.Aspect></Border></UserControl>",
            "ParityView.crn",
            create: consumer => (UIElement)consumer.CreateInstance("SoundConsumer.ParityView"),
            companionSource: "namespace SoundConsumer { public partial class ParityView : Cerneala.UI.Controls.UserControl { } }"));

    private static void AssertParity(MarkupSoundFixture markupFixture)
    {
        using MarkupSoundFixture fixture = markupFixture;
        Border border = fixture.All<Border>().Single();
        Run markup = Drive(
            fixture.Rig,
            fixture.Started,
            band =>
            {
                border.Opacity = band;
                fixture.Pump();
            });

        using TimbreRig manualRig = new(options => options.BaseDirectory = fixture.AssetsDirectory);
        List<SoundPlayback> manualStarted = [];
        manualRig.Runtime.PlaybackAccepted = playback => manualStarted.Add(playback);
        SoundParameter<float> cut = new("Cut", 1200f);
        SoundParameter<float> echo = new("Echo", 0.15f);
        SoundClip confirm = new(
            "audio/ramp.wav",
            volume: 0.8f,
            parameters: [cut, echo],
            modifiers: [new LowPass(cutoff: cut), new Delay(time: 0.02f, feedback: 0.4f, mix: echo)]);
        SoundClip blip = new("audio/short.wav");
        _ = new SoundClip("audio/tone.wav");
        SoundScope scope = manualRig.Runtime.CreateScope();
        SoundHandle playback = scope.CreateHandle();
        scope.Play(confirm, start => { start.Volume = 0.5f; start.Set(cut, 800f); }, playback);
        scope.Play(blip, start => start.Loop = true);
        Run manual = Drive(
            manualRig,
            manualStarted,
            band =>
            {
                switch (band)
                {
                    case 0.9f:
                        scope.Play(confirm, start => { start.Set(cut, 2000f); start.Set(echo, 0.6f); }, playback);
                        break;
                    case 0.8f:
                        playback.Current?.Pause();
                        break;
                    case 0.7f:
                        playback.Current?.Resume();
                        break;
                    case 0.6f:
                        _ = playback.Current?.SeekAsync(TimeSpan.FromMilliseconds(100));
                        break;
                    default:
                        playback.Cancel();
                        break;
                }
            });

        Assert.Equal(manual.States, markup.States);
        Assert.Equal(manual.Positions, markup.Positions);
        Assert.Equal(manual.Trace, markup.Trace);
        Assert.Contains(markup.Pcm, sample => sample != 0f);
        TimbreRig.AssertPcm(manual.Pcm, markup.Pcm);
        Assert.DoesNotContain(markup.Started, started => started.Clip.Source.Name == "audio/tone.wav");
    }

    // Initial starts, then one operation every two blocks, then a tail.
    private static Run Drive(TimbreRig rig, List<SoundPlayback> started, Action<float> operation)
    {
        Assert.Equal(2, started.Count);
        rig.StartAsync([.. started]).GetAwaiter().GetResult();
        foreach (float band in Bands)
        {
            NextBlocks(rig, 2);
            operation(band);
            rig.ReadyAsync([.. started.Where(playback => !SoundAspectIntegrationTests.IsTerminal(playback))]).GetAwaiter().GetResult();
            rig.SettledAsync([.. started.Where(playback => !SoundAspectIntegrationTests.IsTerminal(playback))]).GetAwaiter().GetResult();
        }

        NextBlocks(rig, 4);
        rig.SyncAsync().GetAwaiter().GetResult();
        SoundRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
        return new Run(
            [.. started],
            started.Select(playback => playback.State).ToArray(),
            started.Select(playback => playback.Position).ToArray(),
            (diagnostics.PlaybacksStarted, diagnostics.PlaybacksCanceled, diagnostics.PausesApplied, diagnostics.ResumesApplied,
                diagnostics.SeeksRequested, diagnostics.SeeksCompleted, diagnostics.LoopWraps, diagnostics.BlocksMixed),
            rig.Output.ReadAll());
    }

    private static void NextBlocks(TimbreRig rig, int count)
    {
        for (int block = 0; block < count; block++)
        {
            rig.NextBlockAsync().GetAwaiter().GetResult();
        }
    }

    private sealed record Run(
        IReadOnlyList<SoundPlayback> Started,
        SoundPlaybackState[] States,
        TimeSpan[] Positions,
        (long Started, long Canceled, long Paused, long Resumed, long Seeks, long SeeksDone, long LoopWraps, long Blocks) Trace,
        float[] Pcm);
}
