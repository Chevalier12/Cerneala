using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Timbre.Markup;

// Executed generated consumers on a real tree and runtime. Events come from
// Servo user-like input; direct property writes only drive condition sources.
public sealed class TimbreAspectIntegrationTests
{
    private const string Clips = """
        <TimbreClip Name="Tone">Source = "audio/tone.wav";</TimbreClip>
        <TimbreClip Name="Other">Source = "audio/other.wav";</TimbreClip>
        """;

    [Fact]
    public async Task UnhandledEventTimbreStartOnEachUserClickAndOverlap()
    {
        using MarkupTimbreFixture fixture = new(Button("@on Click { @timbre $Tone; }"));

        await fixture.ClickAsync("Play");
        await fixture.ClickAsync("Play");

        Assert.Equal(2, fixture.Started.Count);
        Assert.NotSame(fixture.Started[0], fixture.Started[1]);
        Assert.All(fixture.Started, playback => Assert.False(IsTerminal(playback)));
        Assert.All(fixture.Started, playback => Assert.Equal(MarkupTimbreFixture.ToneSource, playback.Clip.Source.Name));
    }

    [Fact]
    public async Task HandledTimbreReplacesItsOccupantAndCancelLeavesOtherPlaybacks()
    {
        using MarkupTimbreFixture fixture = new(Button(
            "@handle Playback; " +
            "@on Click { @timbre $Other; @timbre $Tone as Playback; } " +
            "@when $self.IsEnabled { @if value == false { @cancel Playback; } }"));
        Button button = fixture.All<Button>().Single();

        await fixture.ClickAsync("Play");
        TimbrePlayback firstOther = fixture.Started[0];
        TimbrePlayback first = fixture.Started[1];
        await fixture.ClickAsync("Play");
        TimbrePlayback secondOther = fixture.Started[2];
        TimbrePlayback second = fixture.Started[3];

        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
        Assert.False(IsTerminal(second));
        Assert.False(IsTerminal(firstOther));
        Assert.False(IsTerminal(secondOther));

        button.IsEnabled = false;
        fixture.Pump();

        Assert.Equal(TimbrePlaybackState.Canceled, second.State);
        Assert.False(IsTerminal(firstOther));
        Assert.False(IsTerminal(secondOther));
        Assert.Equal(4, fixture.Started.Count);
    }

    [Fact]
    public void InitialTrueStartsOnceAndStableReevaluationDoesNotRepeat()
    {
        using MarkupTimbreFixture fixture = new(Border("@when $self.Opacity { @if value > 0.5 { @timbre $Tone; } }"));
        Border border = fixture.All<Border>().Single();

        Assert.Single(fixture.Started);
        border.Opacity = 0.9f;
        fixture.Pump();
        Assert.Single(fixture.Started);

        border.Opacity = 0.2f;
        fixture.Pump();
        Assert.Single(fixture.Started);
        Assert.False(IsTerminal(fixture.Started[0]));

        border.Opacity = 0.8f;
        fixture.Pump();
        Assert.Equal(2, fixture.Started.Count);
        Assert.False(IsTerminal(fixture.Started[0]));
    }

    [Theory]
    [InlineData("Collapsed")]
    [InlineData("Hidden")]
    public void HiddenControlStartsInitialTrueAndFalseToTrueWithoutReplayOnShow(string visibility)
    {
        using MarkupTimbreFixture fixture = new(Border(
            "@when $self.Opacity { @if value > 0.5 { @timbre $Tone; } }",
            "Visibility=\"" + visibility + "\""));
        Border border = fixture.All<Border>().Single();

        Assert.Single(fixture.Started);
        border.Opacity = 0.2f;
        fixture.Pump();
        border.Opacity = 0.7f;
        fixture.Pump();
        Assert.Equal(2, fixture.Started.Count);

        border.Visibility = Visibility.Visible;
        fixture.Pump();
        border.Visibility = Enum.Parse<Visibility>(visibility);
        fixture.Pump();
        border.Visibility = Visibility.Visible;
        fixture.Pump();

        Assert.Equal(2, fixture.Started.Count);
        Assert.All(fixture.Started, playback => Assert.False(IsTerminal(playback)));
    }

    [Fact]
    public void HiddenAncestorDoesNotCancelOrReplayAudio()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" + Clips + "</StackPanel.Resources>" +
            "<Border><Border.Aspect>@when $self.Opacity { @if value > 0.5 { @timbre $Tone; } }</Border.Aspect></Border>" +
            "</StackPanel>");
        UIElement panel = fixture.Element;

        Assert.Single(fixture.Started);
        panel.Visibility = Visibility.Collapsed;
        fixture.Pump();
        panel.Visibility = Visibility.Visible;
        fixture.Pump();

        Assert.Single(fixture.Started);
        Assert.False(IsTerminal(fixture.Started[0]));
    }

    [Fact]
    public async Task DetachCancelsOwnedPlaybacksAndReattachActivatesAgain()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" + Clips + "</StackPanel.Resources>" +
            "<Button Content=\"Play\"><Button.Aspect>@handle Playback; @on Click { @timbre $Tone as Playback; @timbre $Other; }</Button.Aspect></Button>" +
            "<Border><Border.Aspect>@when $self.Opacity { @if value > 0.5 { @timbre $Tone; } }</Border.Aspect></Border>" +
            "</StackPanel>");
        await fixture.ClickAsync("Play");
        Assert.Equal(3, fixture.Started.Count);

        fixture.Detach();

        Assert.All(fixture.Started, playback => Assert.Equal(TimbrePlaybackState.Canceled, playback.State));

        fixture.Attach();
        Assert.Equal(4, fixture.Started.Count);
        Assert.False(IsTerminal(fixture.Started[3]));
        Assert.Equal(TimbrePlaybackState.Canceled, fixture.Started[0].State);
    }

    internal static string Button(string aspect, string attributes = "") =>
        "<Button Content=\"Play\" " + attributes + "><Button.Resources>" + Clips + "</Button.Resources>" +
        "<Button.Aspect>" + aspect + "</Button.Aspect></Button>";

    internal static string Border(string aspect, string attributes = "") =>
        "<Border Width=\"40\" Height=\"40\" " + attributes + "><Border.Resources>" + Clips + "</Border.Resources>" +
        "<Border.Aspect>" + aspect + "</Border.Aspect></Border>";

    internal static bool IsTerminal(TimbrePlayback playback) =>
        playback.State is TimbrePlaybackState.Completed or TimbrePlaybackState.Canceled or TimbrePlaybackState.Failed;
}
