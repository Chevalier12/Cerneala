using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

// docs/plans/2026-10-08-timbre-prism-in-aspect.md: @timbre and @prism belong to
// the Aspect behavior; commands address (element, sound).
public sealed partial class UiMarkupGeneratorTests
{
    private const string TimbreAspectContract = """
        <StackPanel>
          <StackPanel.Resources>
            <TimbreClip Name="UiSounds">
              @parameter Brightness: float = 1200;
              @sound Click { Source = "audio/click.wav"; }
              @sound Hover
              {
                Source = "audio/hover.wav";
                Volume = 0.3;
                @modifier LowPass { Cutoff = Brightness; }
              }
            </TimbreClip>
            <PrismClip Name="CardFx">
              @parameter GlowRadius: float = 18;
              @layer Foreground { @filter Blur { Radius = GlowRadius; } }
            </PrismClip>
            <Aspect Name="MenuButton" TargetType="Button">
              @timbre $UiSounds(Brightness = 800);
              @prism $CardFx(GlowRadius = 24);
              @on Click { @play $self.timbre.Click; }
              @on MouseEnter
              {
                @play $self.timbre.Hover;
                @animate with Tween(300ms, EaseOut) { @to { $self.timbre.Hover.Volume = 0.8; } }
              }
              @on MouseEnter { @animate with Tween(300ms, EaseOut) { @to { $self.prism.Foreground.Opacity = 0.5; } } }
              @on MouseLeave { @stop $self.timbre.Hover; }
            </Aspect>
          </StackPanel.Resources>
          <Border Name="Speaker">
            <Border.Aspect>
              @timbre { @sound Music { Source = "audio/music.ogg"; Loop = true; Volume = 0.4; AutoPlay = true; } }
            </Border.Aspect>
          </Border>
          <Button Content="Start" Aspect="$MenuButton" />
          <Button Content="Music">
            <Button.Aspect>@on Click { @pause $Speaker.timbre.Music; }</Button.Aspect>
          </Button>
        </StackPanel>
        """;

    [Fact]
    public void TimbreAndPrismInAnAspectAttachFromTheAspectBehavior()
    {
        GeneratorRunResult result = RunGenerator("TimbreAspectContract.crn", TimbreAspectContract, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string source = SingleGeneratedSource(result);
        Assert.Contains("global::Cerneala.UI.Markup.GeneratedMarkup.AttachTimbre(target, new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.Timbre.TimbreClipDefinition>(\"UiSounds\")", source);
        Assert.Contains("global::Cerneala.UI.Markup.GeneratedMarkup.AttachPrism(target", source);
        Assert.Contains("global::Cerneala.UI.Markup.GeneratedMarkup.PlayTimbre(target, \"Click\")", source);
        Assert.Contains("global::Cerneala.UI.Markup.GeneratedMarkup.StopTimbre(target, \"Hover\")", source);
        Assert.DoesNotContain("AttachTimbreSession(element", source);
    }

    [Fact]
    public void InlineTimbreLowersToATimbreClipDefinitionWithNamedSounds()
    {
        GeneratorRunResult result = RunGenerator("TimbreAspectContract.crn", TimbreAspectContract, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string source = SingleGeneratedSource(result);
        Assert.Contains("new global::Cerneala.Timbre.TimbreClipDefinition(", source);
        Assert.Contains("new global::Cerneala.Timbre.TimbreClipSound(\"Music\"", source);
        Assert.Contains("autoPlay: true", source);
    }
}
