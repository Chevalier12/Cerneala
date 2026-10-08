using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Timbre.Markup;

// Ownership of markup audio per concrete Aspect application: occurrences,
// templates, Aspect replacement, hiding, detach churn, failures, reentrancy,
// routed input and idle frames. Synchronous: the UI root is thread-affine.
public sealed class TimbreAspectLifecycleTests
{
    private const string Clips = """
        <TimbreClip Name="Tone">Source = "audio/tone.wav";</TimbreClip>
        <TimbreClip Name="Other">Source = "audio/other.wav";</TimbreClip>
        <TimbreClip Name="Short">Source = "audio/short.wav";</TimbreClip>
        <TimbreClip Name="Ramp">Source = "audio/ramp.wav";</TimbreClip>
        """;

    [Fact]
    public void TwoOccurrencesOfANamedAspectOwnSeparateHandles()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" + Clips +
            "<Aspect Name=\"Clicky\" TargetType=\"Button\">@handle Playback; @on Click { @timbre $Tone as Playback; } @on MouseEnter { @cancel Playback; }</Aspect>" +
            "</StackPanel.Resources><Button Content=\"A\" Aspect=\"$Clicky\" /><Button Content=\"B\" Aspect=\"$Clicky\" /></StackPanel>");

        Click(fixture, "A");
        Click(fixture, "B");
        Click(fixture, "A");
        TimbrePlayback a1 = fixture.Started[0];
        TimbrePlayback b1 = fixture.Started[1];
        TimbrePlayback a2 = fixture.Started[2];
        Assert.Equal(TimbrePlaybackState.Canceled, a1.State);
        Assert.False(IsTerminal(b1));
        Assert.False(IsTerminal(a2));

        fixture.HoverAsync("B").GetAwaiter().GetResult();

        Assert.Equal(TimbrePlaybackState.Canceled, b1.State);
        Assert.False(IsTerminal(a2));
    }

    [Fact]
    public void TemplateOccurrencesOwnTheirAudioAndRetiringThemCancelsIt()
    {
        using MarkupTimbreFixture fixture = new(
            "<ItemsControl>@templates { <ContentTemplate DataType=\"System.String\"><Border Width=\"20\" Height=\"20\"><Border.Resources>" + Clips +
            "</Border.Resources><Border.Aspect>@handle Playback; @when $self.Opacity { @if value > 0.5 { @timbre $Tone as Playback; @timbre $Other; } @if value < 0.5 { @cancel Playback; } }</Border.Aspect></Border></ContentTemplate> }</ItemsControl>");
        ItemsControl items = fixture.All<ItemsControl>().Single();

        items.SetItems(new[] { "a", "b" });
        fixture.Pump();
        fixture.Pump();
        Assert.Equal(4, fixture.Started.Count);
        Border[] borders = fixture.All<Border>().ToArray();
        Assert.Equal(2, borders.Length);

        borders[0].Opacity = 0.2f;
        fixture.Pump();
        Assert.Equal(TimbrePlaybackState.Canceled, fixture.Started[0].State);
        Assert.All(fixture.Started.Skip(1), playback => Assert.False(IsTerminal(playback)));

        // Replacing the items retires the realized occurrences; only the
        // occurrence realized for "b" now owns live audio.
        items.SetItems(new[] { "b" });
        fixture.Pump();
        fixture.Pump();
        Border current = Assert.Single(fixture.All<Border>());
        TimbrePlayback[] live = fixture.Started.Where(playback => !IsTerminal(playback)).ToArray();
        Assert.Equal(2, live.Length);
        Assert.All(fixture.Started.Take(4), playback => Assert.Equal(TimbrePlaybackState.Canceled, playback.State));
        Assert.Equal(current.Opacity > 0.5f ? 6 : 4, fixture.Started.Count);

        items.SetItems(null);
        fixture.Pump();
        Assert.All(fixture.Started, playback => Assert.Equal(TimbrePlaybackState.Canceled, playback.State));
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().LiveScopes);
    }

    [Fact]
    public void ComponentTemplateAudioRetiresWithTheReplacedTemplate()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@template { <Border Width=\"30\" Height=\"30\"><Border.Aspect>@handle Playback; @on Loaded { @timbre $Tone as Playback; @timbre $Other; }</Border.Aspect></Border> }"));
        Button button = fixture.All<Button>().Single();
        fixture.Pump();
        Assert.Equal(2, fixture.Started.Count);

        button.Aspect = null;
        fixture.Pump();

        Assert.All(fixture.Started, playback => Assert.Equal(TimbrePlaybackState.Canceled, playback.State));
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().LiveScopes);
    }

    [Fact]
    public void AspectReplacementRetiresItsAudioAndTriggers()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@handle Playback; @on Loaded { @timbre $Tone as Playback; @timbre $Other; } @on Click { @timbre $Ramp; }"));
        Button button = fixture.All<Button>().Single();
        Assert.Equal(2, fixture.Started.Count);

        button.Aspect = null;
        fixture.Pump();
        Click(fixture, "Play");

        Assert.All(fixture.Started, playback => Assert.Equal(TimbrePlaybackState.Canceled, playback.State));
        Assert.Equal(2, fixture.Started.Count);
    }

    [Fact]
    public void HiddenControlKeepsAudioTransportWhileVisualMotionKeepsItsLifecycle()
    {
        using MarkupTimbreFixture fixture = new(Border(
            "@handle Playback; @when $self.Opacity { " +
            "@if value > 0.5 { @timbre $Tone as Playback; @animate with Tween(100ms, Linear) { @to { Width = 80; } } } " +
            "@if value < 0.3 { @pause Playback; } }",
            "Visibility=\"Collapsed\""));
        Border border = fixture.All<Border>().Single();
        TimbrePlayback playback = Assert.Single(fixture.Started);
        Advance(fixture, 4);
        Assert.Equal(40f, border.Width);

        border.Opacity = 0.2f;
        fixture.Pump();
        Assert.Equal(TimbrePlaybackState.Paused, playback.State);
        border.Opacity = 0.9f;
        fixture.Pump();
        Assert.Equal(2, fixture.Started.Count);

        border.Visibility = Visibility.Visible;
        Advance(fixture, 4);

        Assert.Equal(80f, border.Width);
        Assert.Equal(2, fixture.Started.Count);
        Assert.False(IsTerminal(fixture.Started[1]));
    }

    [Fact]
    public void HundredAttachDetachCyclesStartOncePerAttachAndReleaseEveryScope()
    {
        using MarkupTimbreFixture fixture = new(Border("@when $self.Opacity { @if value > 0.5 { @timbre $Tone; } }"));

        for (int cycle = 0; cycle < 100; cycle++)
        {
            fixture.Detach();
            fixture.Attach();
        }

        Assert.Equal(101, fixture.Started.Count);
        Assert.All(fixture.Started.Take(100), playback => Assert.Equal(TimbrePlaybackState.Canceled, playback.State));
        Assert.False(IsTerminal(fixture.Started[100]));
        fixture.Detach();
        Assert.Equal(TimbrePlaybackState.Canceled, fixture.Started[100].State);
        Assert.Equal(1, fixture.Rig.Runtime.GetDiagnostics().LiveScopes);
        fixture.Rig.SyncAsync().GetAwaiter().GetResult();
        Assert.Equal(0, fixture.Rig.Runtime.GetDiagnostics().ActiveVoices);
    }

    [Fact]
    public void DetachCancelsAPendingPlaybackAndReleasesItsReader()
    {
        using MarkupTimbreFixture fixture = new(Border("@on Loaded { @timbre $Ramp; }"), attach: false);
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        DeterministicTimbreSourceFactory factory = new(480)
        {
            Configure = reader => reader.ReadGate = token => gate.Task.WaitAsync(token)
        };
        fixture.Element.Resources.SetResource(new ResourceId<TimbreClip>("Ramp"), TimbreRig.Clip(factory));
        fixture.Attach();
        TimbrePlayback pending = Assert.Single(fixture.Started);

        fixture.Detach();
        gate.TrySetResult();

        Assert.Equal(TimbrePlaybackState.Canceled, TimbreRig.CompletionAsync(pending).GetAwaiter().GetResult().State);
        TimbreRig.ReleasedAsync(pending).GetAwaiter().GetResult();
        Assert.Equal(0, factory.LiveReaders);
    }

    [Fact]
    public void DeviceOpenFailureFailsAcceptedPlaybacksWithoutStoppingTheBody()
    {
        using MarkupTimbreFixture fixture = new(Button("@on Click { @timbre $Tone; @timbre $Other; }"));
        fixture.Rig.Output.OpenFailure = new InvalidOperationException("No audio device in this probe.");

        Click(fixture, "Play");

        Assert.Equal(2, fixture.Started.Count);
        Assert.All(fixture.Started, playback =>
            Assert.Equal(TimbrePlaybackState.Failed, TimbreRig.CompletionAsync(playback).GetAwaiter().GetResult().State));

        fixture.Rig.Output.OpenFailure = null;
        Click(fixture, "Play");
        Assert.Equal(4, fixture.Started.Count);
        Assert.All(fixture.Started.Skip(2), playback => Assert.False(IsTerminal(playback)));
    }

    [Fact]
    public void ReentrantActivationDoesNotRepeatTheTimbre()
    {
        using MarkupTimbreFixture fixture = new(Border(
            "@when $self.Opacity { @if value > 0.5 { @timbre $Tone; @set { Opacity = 0.7; } } }"));
        Border border = fixture.All<Border>().Single();

        Assert.Single(fixture.Started);
        Assert.Equal(0.7f, border.Opacity);
        border.Opacity = 0.9f;
        fixture.Pump();
        Assert.Single(fixture.Started);
    }

    [Fact]
    public void TransportRacingNaturalCompletionNeverThrowsOnTheUiThread()
    {
        using MarkupTimbreFixture fixture = new(Border(
            "@handle Playback; @when $self.Opacity { @if value > 0.95 { @timbre $Short as Playback; } " +
            "@if value > 0.2 and value < 0.4 { @pause Playback; } @if value > 0.4 and value < 0.6 { @resume Playback; } " +
            "@if value > 0.6 and value < 0.8 { @seek Playback to 1ms; } }"));
        Border border = fixture.All<Border>().Single();
        fixture.Rig.Output.Release();
        using CancellationTokenSource stop = new();
        using Barrier barrier = new(2);
        Task consumer = Task.Run(() =>
        {
            barrier.SignalAndWait();
            while (!stop.IsCancellationRequested)
            {
                fixture.Rig.Output.ConsumeAll();
                Thread.Yield();
            }
        });

        barrier.SignalAndWait();
        for (int cycle = 0; cycle < 200; cycle++)
        {
            border.Opacity = (cycle % 4) switch { 0 => 1f, 1 => 0.3f, 2 => 0.5f, _ => 0.7f };
            fixture.Pump();
        }

        stop.Cancel();
        consumer.GetAwaiter().GetResult();
        Assert.Equal(50, fixture.Started.Count);
        Assert.All(fixture.Started.Take(49), playback => Assert.True(IsTerminal(playback)));
    }

    [Fact]
    public void RoutedEventsBubbleFromUserInputToTheOwningAspect()
    {
        using MarkupTimbreFixture fixture = new(
            "<Border Width=\"120\" Height=\"60\" Background=\"Black\"><Border.Resources>" + Clips +
            "</Border.Resources><Border.Aspect>@on MouseLeftButtonDown { @timbre $Tone; } @on MouseEnter { @timbre $Other; }</Border.Aspect>" +
            "<Button Content=\"Inner\" /></Border>");

        fixture.HoverAsync("Inner").GetAwaiter().GetResult();
        Click(fixture, "Inner");

        Assert.Equal([MarkupTimbreFixture.OtherSource, MarkupTimbreFixture.ToneSource], fixture.Started.Select(playback => playback.Clip.Source.Name));
    }

    [Fact]
    public void IdleFramesStartNothingReevaluateNothingAndInvalidateNothing()
    {
        InvalidationTrace trace = new(capacity: 1 << 16);
        using MarkupTimbreFixture fixture = new(
            Border("@handle Playback; @when $self.Opacity { @if value > 0.5 { @timbre $Tone as Playback; } } @on MouseEnter { @pause Playback; }"),
            trace: trace);
        Border border = fixture.All<Border>().Single();
        Advance(fixture, 3);
        MarkupConditionController[] controllers = border.LifecycleBehaviors.OfType<MarkupConditionController>().ToArray();
        Assert.NotEmpty(controllers);
        int evaluations = controllers.Sum(controller => controller.EvaluationCount);
        int invalidations = Invalidations(trace);
        int started = fixture.Started.Count;

        Advance(fixture, 30);

        Assert.Equal(started, fixture.Started.Count);
        Assert.Equal(evaluations, controllers.Sum(controller => controller.EvaluationCount));
        Assert.Equal(invalidations, Invalidations(trace));
    }

    private static int Invalidations(InvalidationTrace trace) => trace.Entries.Count(entry =>
        entry.Kind is InvalidationTraceEventKind.Request or InvalidationTraceEventKind.Queue or InvalidationTraceEventKind.Phase);

    private static void Advance(MarkupTimbreFixture fixture, int frames)
    {
        for (int frame = 0; frame < frames; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(50));
        }
    }

    private static void Click(MarkupTimbreFixture fixture, string name) => fixture.ClickAsync(name).GetAwaiter().GetResult();

    private static string Button(string aspect) =>
        "<Button Content=\"Play\"><Button.Resources>" + Clips + "</Button.Resources><Button.Aspect>" + aspect + "</Button.Aspect></Button>";

    private static string Border(string aspect, string attributes = "") =>
        "<Border Width=\"40\" Height=\"40\" " + attributes + "><Border.Resources>" + Clips + "</Border.Resources><Border.Aspect>" + aspect + "</Border.Aspect></Border>";

    private static bool IsTerminal(TimbrePlayback playback) => TimbreAspectIntegrationTests.IsTerminal(playback);
}
