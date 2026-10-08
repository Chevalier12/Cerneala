using Cerneala.Tests.Timbre.Markup;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Timbre;
using static Cerneala.Tests.Timbre.Motion.TimbreMotionTestKit;

namespace Cerneala.Tests.Timbre.Motion;

// Executed generated consumers: `.timbre.` animations follow the same samples
// and lifecycle as the C# facade on the identity captured by @timbre.
public sealed class TimbreMotionMarkupTests
{
    private const string Animated =
        "@handle Playback; " +
        "@on Click { @timbre $Tone(Volume = 0.2, Cut = 800) as Playback; " +
        "@animate with Tween(300ms, Linear) { @to { $self.timbre.Playback.Volume = 0.8; $self.timbre.Playback.Cut = 6000; } } } " +
        "@when $self.Opacity { @if value < 0.5 { @cancel Playback; } }";

    [Fact]
    public void TimbreMotionGeneratedConsumerMatchesTheCSharpFacadeThroughTransport()
    {
        using MarkupTimbreFixture fixture = new(Panel(Animated), "TimbreMotionParity.crn");
        Click(fixture);
        TimbrePlayback markup = Assert.Single(fixture.Started);
        TimbreParameter<float> cut = CutOf(markup.Clip);
        TimbrePlayback csharp = PlayButton(fixture).Timbre.Play(markup.Clip, start =>
        {
            start.Volume = 0.2f;
            start.Set(cut, 800f);
        });
        csharp.Motion().Animate(TimbrePlayback.VolumeParameter).To(0.8f).With(Linear(300));
        csharp.Motion().Animate(cut).To(6000f).With(Linear(300));

        // Both are pending: identity and values are captured, time has not started.
        fixture.Pump();
        Advance(fixture, 1);
        AssertSame(markup, csharp, 0.2f, 800f);

        Wait(() => fixture.Rig.StartAsync(markup, csharp));
        Advance(fixture, 1);
        AssertSame(markup, csharp, 0.35f, 2100f);

        markup.Pause();
        csharp.Pause();
        Advance(fixture, 2);
        AssertSame(markup, csharp, 0.35f, 2100f);

        markup.Resume();
        csharp.Resume();
        Advance(fixture, 1);
        AssertSame(markup, csharp, 0.5f, 3400f);

        // Mid-animation replacement: only the old identity is canceled; the
        // new occupant animates from its own start values.
        Click(fixture);
        TimbrePlayback replacement = fixture.Started[^1];
        Advance(fixture, 1);
        Assert.Equal(TimbrePlaybackState.Canceled, markup.State);
        AssertNear(0.5f, markup.Volume);
        Assert.Equal(0.2f, replacement.Volume);
        AssertNear(0.65f, csharp.Volume);
    }

    [Fact]
    public void TimbreMotionOnAnEmptySlotIsANoOpWithoutAutoplay()
    {
        using MarkupTimbreFixture fixture = new(Panel(
            "@handle Playback; @on Click { @animate { @to { $self.timbre.Playback.Volume = 0.5; } } } @when $self.Opacity { @if value < 0.1 { @timbre $Tone as Playback; } }"),
            "TimbreMotionEmpty.crn");

        Click(fixture);
        fixture.Pump(TimeSpan.FromMilliseconds(75));

        Assert.Empty(fixture.Started);
        Assert.Equal(0, TimbrePlaybackMotion.ActiveTargets(fixture.Root));
    }

    [Fact]
    public void TimbreMotionInvalidSpringSampleMatchesTheCSharpFacade()
    {
        using MarkupTimbreFixture fixture = new(Panel(
            "@handle Playback; @on Click { @timbre $Tone(Volume = 0.5) as Playback; " +
            "@animate with Spring(400, 4, 1) { @to { $self.timbre.Playback.Volume = 1; } } }"),
            "TimbreMotionSpring.crn");
        Click(fixture);
        TimbrePlayback markup = Assert.Single(fixture.Started);
        TimbrePlayback csharp = PlayButton(fixture).Timbre.Play(markup.Clip, start => start.Volume = 0.5f);
        MotionHandle handle = csharp.Motion().Animate(TimbrePlayback.VolumeParameter).To(1f).With(new SpringSpec<float>(400f, 4f, 1f));
        Wait(() => fixture.Rig.StartAsync(markup, csharp));
        fixture.Pump();
        for (int frame = 0; frame < 40 && handle.IsActive; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(16));
        }

        Assert.True(handle.IsCanceled);
        Assert.Equal(csharp.Volume, markup.Volume);
        Assert.Equal(2, fixture.Rig.Runtime.GetDiagnostics().MotionSamplesRejected);
        Assert.Equal(TimbrePlaybackState.Playing, markup.State);
    }

    [Fact]
    public void TimbreMotionOfAHiddenOwnerContinuesAndHideShowRestartsNothing()
    {
        using MarkupTimbreFixture fixture = new(Panel(Animated), "TimbreMotionHidden.crn");
        Click(fixture);
        TimbrePlayback playback = Assert.Single(fixture.Started);
        Wait(() => fixture.Rig.StartAsync(playback));
        fixture.Pump();
        Advance(fixture, 1);

        PlayButton(fixture).Visibility = Visibility.Collapsed;
        Advance(fixture, 1);
        AssertNear(0.5f, playback.Volume);

        PlayButton(fixture).Visibility = Visibility.Visible;
        Advance(fixture, 3);

        AssertNear(0.8f, playback.Volume);
        AssertNear(6000f, Cut(playback), 0.5f);
        Assert.Single(fixture.Started);
    }

    [Fact]
    public void TimbreMotionDetachStopsPublishingToTheRetiredIdentityAndReattachRevivesNothing()
    {
        using MarkupTimbreFixture fixture = new(Panel(Animated), "TimbreMotionDetach.crn");
        Click(fixture);
        TimbrePlayback playback = Assert.Single(fixture.Started);
        Wait(() => fixture.Rig.StartAsync(playback));
        fixture.Pump();
        Advance(fixture, 1);
        Assert.Equal(1, TimbrePlaybackMotion.ActiveTargets(fixture.Root));

        fixture.Detach();
        long published = playback.AnimatedPublications;
        Advance(fixture, 3);

        Assert.Equal(TimbrePlaybackState.Canceled, playback.State);
        Assert.Equal(published, playback.AnimatedPublications);
        Assert.Equal(0, TimbrePlaybackMotion.ActiveTargets(fixture.Root));

        fixture.Attach();
        Advance(fixture, 1);
        Assert.Single(fixture.Started);
        Assert.Equal(published, playback.AnimatedPublications);
    }

    [Fact]
    public void TimbreMotionAspectReplacementCancelsItsExecutionsSynchronously()
    {
        using MarkupTimbreFixture fixture = new(Panel(Animated), "TimbreMotionAspect.crn");
        Click(fixture);
        TimbrePlayback playback = Assert.Single(fixture.Started);
        Wait(() => fixture.Rig.StartAsync(playback));
        fixture.Pump();
        Advance(fixture, 1);

        PlayButton(fixture).Aspect = null;
        long published = playback.AnimatedPublications;
        Advance(fixture, 2);

        Assert.Equal(TimbrePlaybackState.Canceled, playback.State);
        Assert.Equal(published, playback.AnimatedPublications);
        Assert.Equal(0, TimbrePlaybackMotion.ActiveTargets(fixture.Root));
    }

    [Fact]
    public void TimbreMotionTwoScopesAnimateIndependently()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" + Clips +
            "<Aspect Name=\"Animated\" TargetType=\"Button\">" + Animated + "</Aspect></StackPanel.Resources>" +
            "<Button Content=\"A\" Aspect=\"$Animated\" /><Button Content=\"B\" Aspect=\"$Animated\" /></StackPanel>",
            "TimbreMotionTwoScopes.crn");
        fixture.ClickAsync("A").GetAwaiter().GetResult();
        fixture.ClickAsync("B").GetAwaiter().GetResult();
        TimbrePlayback a = fixture.Started[0];
        TimbrePlayback b = fixture.Started[1];
        Wait(() => fixture.Rig.StartAsync(a, b));
        fixture.Pump();
        Advance(fixture, 1);

        fixture.All<Button>().First().Opacity = 0.2f;
        fixture.Pump();
        Advance(fixture, 1);

        Assert.Equal(TimbrePlaybackState.Canceled, a.State);
        AssertNear(0.35f, a.Volume);
        AssertNear(0.5f, b.Volume);
        Assert.Equal(1, TimbrePlaybackMotion.ActiveTargets(fixture.Root));
    }

    [Fact]
    public void TimbreMotionHundredReplacementCyclesLeaveNoBindingOrVoiceBehind()
    {
        using MarkupTimbreFixture fixture = new(Panel(Animated), "TimbreMotionChurn.crn", hold: false);
        Dictionary<TimbrePlayback, long> retired = [];
        for (int cycle = 0; cycle < 100; cycle++)
        {
            Click(fixture);
            if (fixture.Started.Count > 1)
            {
                TimbrePlayback previous = fixture.Started[^2];
                retired[previous] = previous.AnimatedPublications;
            }

            fixture.Rig.Output.ConsumeAll();
            Wait(() => fixture.Rig.SyncAsync());
            fixture.Pump(TimeSpan.FromMilliseconds(10));
        }

        Assert.Equal(100, fixture.Started.Count);
        Assert.All(retired, entry =>
        {
            Assert.Equal(TimbrePlaybackState.Canceled, entry.Key.State);
            Assert.Equal(entry.Value, entry.Key.AnimatedPublications);
        });
        Assert.Equal(1, TimbrePlaybackMotion.ActiveTargets(fixture.Root));

        PlayButton(fixture).Opacity = 0.2f;
        fixture.Pump(TimeSpan.FromMilliseconds(10));
        fixture.Pump(TimeSpan.FromMilliseconds(10));
        fixture.Rig.Output.ConsumeAll();
        Wait(() => fixture.Rig.SyncAsync());

        Assert.Equal(0, TimbrePlaybackMotion.ActiveTargets(fixture.Root));
        Assert.Equal(0, fixture.Rig.Runtime.GetDiagnostics().ActiveVoices);
    }

    [Fact]
    public void TimbreMotionOfARetiredTemplateOccurrenceIsCanceled()
    {
        using MarkupTimbreFixture fixture = new(
            "<ItemsControl>@templates { <ContentTemplate DataType=\"System.String\"><Border Width=\"20\" Height=\"20\"><Border.Resources>" + Clips +
            "</Border.Resources><Border.Aspect>@handle Playback; @when $self.Opacity { @if value > 0.5 { @timbre $Tone(Volume = 0.2) as Playback; " +
            "@animate with Tween(300ms, Linear) { @to { $self.timbre.Playback.Volume = 0.8; } } } }</Border.Aspect></Border></ContentTemplate> }</ItemsControl>",
            "TimbreMotionTemplate.crn");
        ItemsControl items = fixture.All<ItemsControl>().Single();
        items.SetItems(new[] { "a" });
        fixture.Pump();
        fixture.Pump();
        TimbrePlayback playback = Assert.Single(fixture.Started);
        Wait(() => fixture.Rig.StartAsync(playback));
        fixture.Pump();
        Advance(fixture, 1);
        AssertNear(0.35f, playback.Volume);

        items.SetItems(null);
        fixture.Pump();
        long published = playback.AnimatedPublications;
        Advance(fixture, 2);

        Assert.Equal(TimbrePlaybackState.Canceled, playback.State);
        Assert.Equal(published, playback.AnimatedPublications);
        Assert.Equal(0, TimbrePlaybackMotion.ActiveTargets(fixture.Root));
    }

    // Window.Hide stops pumping the root: no samples are taken while the
    // transport keeps producing PCM, and the first frame after showing the
    // window again uses the existing Motion delta policy (clamped to 100 ms).
    [Fact]
    public void TimbreMotionUnpumpedRootHoldsSamplesWhileTransportContinues()
    {
        using MarkupTimbreFixture fixture = new(Panel(Animated), "TimbreMotionUnpumped.crn");
        Click(fixture);
        TimbrePlayback playback = Assert.Single(fixture.Started);
        Wait(() => fixture.Rig.StartAsync(playback));
        fixture.Pump();
        Advance(fixture, 1);
        AssertNear(0.35f, playback.Volume);
        TimeSpan position = playback.Position;

        for (int block = 0; block < 10; block++)
        {
            Wait(() => fixture.Rig.NextBlockAsync());
        }

        fixture.Clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(playback.Position > position);
        AssertNear(0.35f, playback.Volume);

        fixture.Pump();
        AssertNear(0.55f, playback.Volume);
        Assert.Equal(TimbrePlaybackState.Playing, playback.State);
    }

    private static void AssertSame(TimbrePlayback markup, TimbrePlayback csharp, float volume, float cut)
    {
        Assert.Equal(csharp.Volume, markup.Volume);
        Assert.Equal(Cut(csharp), Cut(markup));
        AssertNear(volume, markup.Volume);
        AssertNear(cut, Cut(markup), 0.5f);
    }
}
