using Cerneala.Timbre;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Timbre.Markup;

// docs/plans/2026-10-08-timbre-prism-in-aspect.md: the sounds and the Prism
// effect an Aspect brings live exactly as long as that application.
public sealed class TimbreAttachmentMarkupTests
{
    private const string Resources = """
        <TimbreClip Name="Sounds">
          @parameter Cutoff: float = 1200;
          @sound Click { Source = "audio/tone.wav"; }
          @sound Hum { Source = "audio/other.wav"; Loop = true; AutoPlay = true; @modifier LowPass { Cutoff = Cutoff; } }
        </TimbreClip>
        <TimbreClip Name="Quiet">
          @parameter Cutoff: float = 1200;
          @sound Click { Source = "audio/short.wav"; }
        </TimbreClip>
        <PrismClip Name="Glow">
          @layer Foreground { @filter Blur { Radius = 4; } }
        </PrismClip>
        <Aspect Name="A" TargetType="Button">
          @timbre $Sounds(Cutoff = 800);
          @prism $Glow;
          @on Click { @play $self.timbre.Click; }
        </Aspect>
        <Aspect Name="B" TargetType="Button">
          @timbre $Quiet;
          @on Click { @play $self.timbre.Click; }
        </Aspect>
        """;

    [Fact]
    public void AutoPlaySoundStartsWhenTheAspectIsApplied()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));

        Assert.Equal(["audio/other.wav"], Sources(fixture));
        Assert.Contains(fixture.Started.Single().State, new[] { TimbrePlaybackState.Pending, TimbrePlaybackState.Playing });
    }

    [Fact]
    public void PlayRestartsTheSameSound()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));

        Click(fixture, "Play");
        TimbrePlayback first = fixture.Started.Last();
        Click(fixture, "Play");

        Assert.Equal(["audio/other.wav", "audio/tone.wav", "audio/tone.wav"], Sources(fixture));
        Assert.Equal(TimbrePlaybackState.Canceled, first.State);
    }

    [Fact]
    public void ReplacingTheAspectRemovesItsSoundsAndPrismAndAppliesTheNewOnes()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));
        Button button = fixture.All<Button>().Single();
        TimbrePlayback hum = fixture.Started.Single();
        Assert.True(GeneratedMarkup.TryGetPrismInstance(button, out PrismInstance? glow));
        Assert.Equal("Glow", glow!.Definition.Name);

        button.Aspect = Aspect(button, "B");
        fixture.Pump();
        Click(fixture, "Play");

        Assert.Equal(TimbrePlaybackState.Canceled, hum.State);
        Assert.False(GeneratedMarkup.TryGetPrismInstance(button, out _));
        Assert.Equal(["audio/other.wav", "audio/short.wav"], Sources(fixture));

        button.Aspect = Aspect(button, "A");
        fixture.Pump();

        Assert.Equal(["audio/other.wav", "audio/short.wav", "audio/other.wav"], Sources(fixture));
        Assert.True(GeneratedMarkup.TryGetPrismInstance(button, out _));
    }

    [Fact]
    public void ACommandToADetachedElementThrows()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel>" +
            "<Border Name=\"Speaker\" Width=\"20\"><Border.Aspect>@timbre { @sound Music { Source = \"audio/tone.wav\"; Loop = true; AutoPlay = true; } }</Border.Aspect></Border>" +
            "<Button Content=\"Pause\"><Button.Aspect>@on Click { @pause $Speaker.timbre.Music; }</Button.Aspect></Button>" +
            "</StackPanel>");
        StackPanel panel = (StackPanel)fixture.Element;
        Border speaker = fixture.All<Border>().Single(border => border.Width == 20);

        panel.LogicalChildren.Remove(speaker);
        panel.VisualChildren.Remove(speaker);
        fixture.Pump();
        Assert.False(speaker.IsAttached);

        Assert.Throws<InvalidOperationException>(() => Click(fixture, "Pause"));
    }

    // `$Speaker.timbre.Music` follows the Aspect Speaker has now: a new Aspect
    // that also brings Music keeps the command working, one without it throws.
    [Fact]
    public void ACommandFollowsTheTargetsCurrentAspectAndThrowsWhenItHasNoSuchSound()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" +
            "<Aspect Name=\"WithMusic\" TargetType=\"Border\">@timbre { @sound Music { Source = \"audio/tone.wav\"; Loop = true; AutoPlay = true; } }</Aspect>" +
            "<Aspect Name=\"OtherMusic\" TargetType=\"Border\">@timbre { @sound Music { Source = \"audio/other.wav\"; Loop = true; AutoPlay = true; } }</Aspect>" +
            "<Aspect Name=\"Silent\" TargetType=\"Border\">@timbre { @sound Hum { Source = \"audio/short.wav\"; } }</Aspect>" +
            "</StackPanel.Resources>" +
            "<Border Name=\"Speaker\" Width=\"20\" Aspect=\"$WithMusic\" />" +
            "<Button Content=\"Pause\"><Button.Aspect>@on Click { @pause $Speaker.timbre.Music; }</Button.Aspect></Button>" +
            "</StackPanel>");
        Border speaker = fixture.All<Border>().Single(border => border.Width == 20);

        Click(fixture, "Pause");
        Assert.Equal(TimbrePlaybackState.Paused, fixture.Started[0].State);

        speaker.Aspect = Aspect(speaker, "OtherMusic");
        fixture.Pump();
        Assert.Equal("audio/other.wav", Sources(fixture).Last());
        Click(fixture, "Pause");
        Assert.Equal(TimbrePlaybackState.Paused, fixture.Started[1].State);

        speaker.Aspect = Aspect(speaker, "Silent");
        fixture.Pump();
        InvalidOperationException error = Assert.Throws<InvalidOperationException>(() => Click(fixture, "Pause"));
        Assert.Contains("Music", error.Message, StringComparison.Ordinal);
    }

    // The TimbreClip resource is resolved when the Aspect is applied: replacing
    // it keeps the running application and reaches only the next application.
    [Fact]
    public void ReplacingTheTimbreClipResourceReachesOnlyTheNextApplication()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));
        StackPanel panel = (StackPanel)fixture.Element;

        panel.Resources["Sounds"] = panel.Resources["Quiet"];
        Click(fixture, "Play");
        Assert.Equal("audio/tone.wav", Sources(fixture).Last());

        fixture.Detach();
        fixture.Attach();
        Click(fixture, "Play");

        Assert.Equal(["audio/other.wav", "audio/tone.wav", "audio/short.wav"], Sources(fixture));
    }

    private static string Panel(string children) =>
        "<StackPanel><StackPanel.Resources>" + Resources + "</StackPanel.Resources>" + children + "</StackPanel>";

    private static ElementAspect Aspect(UIElement element, string name) =>
        element.TryFindResource(new ResourceId<ElementAspect>(name), out ElementAspect aspect)
            ? aspect
            : throw new InvalidOperationException("Aspect resource '" + name + "' was not found.");

    private static string[] Sources(MarkupTimbreFixture fixture) =>
        fixture.Started.Select(playback => playback.Sound.Source.ToString()).ToArray();

    private static void Click(MarkupTimbreFixture fixture, string name) => fixture.ClickAsync(name).GetAwaiter().GetResult();
}
