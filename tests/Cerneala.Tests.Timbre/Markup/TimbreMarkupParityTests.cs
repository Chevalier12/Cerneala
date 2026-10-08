using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.Timbre.Markup;

// The same sources, schema, block partitions and operations driven once by a
// manual C# consumer and once by executed generated markup (factory and
// paired partial). Both must produce the same PCM, operation trace and states
// on the real engine with the deterministic sink.
public sealed class TimbreMarkupParityTests
{
    private const string Clips = """
        <TimbreClip Name="Sounds">
          @parameter Cut: float = 1200;
          @parameter Echo: float = 0.15;
          @sound Confirm
          {
            Source = "audio/ramp.wav";
            Volume = 0.5;
            @modifier LowPass { Cutoff = Cut; }
            @modifier Delay { Time = 20ms; Feedback = 0.4; Mix = Echo; }
          }
          @sound Blip { Source = "audio/short.wav"; Loop = true; }
          @sound Unused { Source = "audio/tone.wav"; }
        </TimbreClip>
        """;

    private const string Aspect = """
        @timbre $Sounds(Cut = 800);
        @when $self.Opacity
        {
            @if value > 0.95 { @play $self.timbre.Confirm; @play $self.timbre.Blip; }
            @if value > 0.85 and value < 0.95 { @play $self.timbre.Confirm; }
            @if value > 0.75 and value < 0.85 { @pause $self.timbre.Confirm; }
            @if value > 0.65 and value < 0.75 { @resume $self.timbre.Confirm; }
            @if value > 0.55 and value < 0.65 { @seek $self.timbre.Confirm to 100ms; }
            @if value > 0.45 and value < 0.55 { @stop $self.timbre.Confirm; }
        }
        """;

    private static readonly float[] Bands = [0.9f, 0.8f, 0.7f, 0.6f, 0.5f];

    [Fact]
    public void FactoryMarkupMatchesManualCSharp() =>
        AssertParity(new MarkupTimbreFixture(
            "<Border Width=\"40\" Height=\"40\"><Border.Resources>" + Clips + "</Border.Resources><Border.Aspect>" + Aspect + "</Border.Aspect></Border>",
            "ParityFactory.crn"));

    [Fact]
    public void PairedPartialMarkupMatchesManualCSharp() =>
        AssertParity(new MarkupTimbreFixture(
            "<UserControl><Border Width=\"40\" Height=\"40\"><Border.Resources>" + Clips + "</Border.Resources><Border.Aspect>" + Aspect + "</Border.Aspect></Border></UserControl>",
            "ParityView.crn",
            create: consumer => (UIElement)consumer.CreateInstance("TimbreMarkupConsumer.ParityView"),
            companionSource: "namespace TimbreMarkupConsumer { public partial class ParityView : Cerneala.UI.Controls.UserControl { } }"));

    private static void AssertParity(MarkupTimbreFixture markupFixture)
    {
        using MarkupTimbreFixture fixture = markupFixture;
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
        List<TimbrePlayback> manualStarted = [];
        manualRig.Runtime.PlaybackAccepted = playback => manualStarted.Add(playback);
        TimbreParameter<float> cut = new("Cut", 1200f);
        TimbreParameter<float> echo = new("Echo", 0.15f);
        TimbreClipDefinition sounds = new(
            "Sounds",
            [
                new TimbreClipSound("Confirm", new TimbreSound(
                    "audio/ramp.wav",
                    volume: 0.5f,
                    parameters: [cut, echo],
                    modifiers: [new LowPass(cutoff: cut), new Delay(time: 0.02f, feedback: 0.4f, mix: echo)])),
                new TimbreClipSound("Blip", new TimbreSound("audio/short.wav", loop: true)),
                new TimbreClipSound("Unused", new TimbreSound("audio/tone.wav"))
            ],
            [cut, echo]);
        TimbreSound confirm = sounds.Sounds["Confirm"].Sound;
        TimbreScope scope = manualRig.Runtime.CreateScope();
        TimbreHandle playback = scope.CreateHandle();
        scope.Play(confirm, start => start.Set(cut, 800f), playback);
        scope.Play(sounds.Sounds["Blip"].Sound, configure: null, scope.CreateHandle());
        Run manual = Drive(
            manualRig,
            manualStarted,
            band =>
            {
                switch (band)
                {
                    case 0.9f:
                        scope.Play(confirm, start => start.Set(cut, 800f), playback);
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
        Assert.DoesNotContain(markup.Started, started => started.Sound.Source.Name == "audio/tone.wav");
    }

    // Initial starts, then one operation every two blocks, then a tail.
    private static Run Drive(TimbreRig rig, List<TimbrePlayback> started, Action<float> operation)
    {
        Assert.Equal(2, started.Count);
        rig.StartAsync([.. started]).GetAwaiter().GetResult();
        foreach (float band in Bands)
        {
            NextBlocks(rig, 2);
            operation(band);
            rig.ReadyAsync([.. started.Where(playback => !TimbreAspectIntegrationTests.IsTerminal(playback))]).GetAwaiter().GetResult();
            rig.SettledAsync([.. started.Where(playback => !TimbreAspectIntegrationTests.IsTerminal(playback))]).GetAwaiter().GetResult();
        }

        NextBlocks(rig, 4);
        rig.SyncAsync().GetAwaiter().GetResult();
        TimbreRuntimeDiagnostics diagnostics = rig.Runtime.GetDiagnostics();
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
        IReadOnlyList<TimbrePlayback> Started,
        TimbrePlaybackState[] States,
        TimeSpan[] Positions,
        (long Started, long Canceled, long Paused, long Resumed, long Seeks, long SeeksDone, long LoopWraps, long Blocks) Trace,
        float[] Pcm);
}
