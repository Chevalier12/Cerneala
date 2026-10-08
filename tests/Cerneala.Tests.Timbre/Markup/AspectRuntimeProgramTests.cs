using Cerneala.Timbre;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Timbre.Markup;

// docs/plans/2026-10-08-aspect-runtime-program.md: an Aspect brings its whole
// program (@on, Motion, Timbre) with every application, including a
// replacement at runtime, and takes all of it away when it leaves.
public sealed class AspectRuntimeProgramTests
{
    private const string Resources = """
        <TimbreClip Name="Tone">@sound Tone { Source = "audio/tone.wav"; }</TimbreClip>
        <TimbreClip Name="Other">@sound Other { Source = "audio/other.wav"; }</TimbreClip>
        <Aspect Name="A" TargetType="Button">@timbre $Tone; @on Click { @play $self.timbre.Tone; }</Aspect>
        <Aspect Name="B" TargetType="Button">@timbre $Other; @on Click { @play $self.timbre.Other; @animate with Tween(10ms, Linear) { @to { Opacity = 0.25; } } }</Aspect>
        """;

    // Characterization: a statically applied Aspect runs its program.
    [Fact]
    public void StaticAspectRunsItsProgram()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$B\" />"));
        Button button = fixture.All<Button>().Single();

        Click(fixture, "Play");
        Settle(fixture);

        Assert.Equal(["audio/other.wav"], Sources(fixture));
        Assert.Equal(0.25f, button.Opacity);
    }

    [Fact]
    public void ReplacementAtRuntimeAppliesTheNewAspectProgram()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));
        Button button = fixture.All<Button>().Single();

        button.Aspect = Aspect(button, "B");
        fixture.Pump();
        Click(fixture, "Play");
        Settle(fixture);

        Assert.Equal(["audio/other.wav"], Sources(fixture));
        Assert.Equal(0.25f, button.Opacity);
    }

    [Fact]
    public void ReplacingBackAndForthKeepsExactlyOneProgram()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));
        Button button = fixture.All<Button>().Single();
        ElementAspect a = Aspect(button, "A");
        ElementAspect b = Aspect(button, "B");

        button.Aspect = b;
        fixture.Pump();
        button.Aspect = a;
        fixture.Pump();
        button.Aspect = b;
        fixture.Pump();
        Click(fixture, "Play");

        Assert.Equal(["audio/other.wav"], Sources(fixture));
    }

    [Fact]
    public void OneResourceAppliedAtRuntimeGivesEachElementItsOwnProgram()
    {
        using MarkupTimbreFixture fixture = new(Panel(
            "<Button Content=\"First\" Aspect=\"$A\" /><Button Content=\"Second\" Aspect=\"$A\" />"));
        Button[] buttons = fixture.All<Button>().ToArray();
        ElementAspect b = Aspect(buttons[0], "B");

        buttons[0].Aspect = b;
        buttons[1].Aspect = b;
        fixture.Pump();
        Click(fixture, "First");
        Click(fixture, "Second");

        Assert.Equal(["audio/other.wav", "audio/other.wav"], Sources(fixture));
    }

    // Characterization: detach and reattach with an unchanged Aspect keep one program.
    [Fact]
    public void DetachAndReattachKeepExactlyOneProgram()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));

        fixture.Detach();
        fixture.Attach();
        Click(fixture, "Play");

        Assert.Equal(["audio/tone.wav"], Sources(fixture));
    }

    [Fact]
    public void DefaultAspectBringsItsProgramToAnElementCreatedAtRuntime()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" +
            "<TimbreClip Name=\"Tone\">@sound Tone { Source = \"audio/tone.wav\"; }</TimbreClip>" +
            "<Aspect TargetType=\"Button\">@timbre $Tone; @on Click { @play $self.timbre.Tone; }</Aspect>" +
            "</StackPanel.Resources><Button Content=\"Static\" /></StackPanel>");
        StackPanel panel = fixture.All<StackPanel>().First();

        panel.VisualChildren.Add(new Button { Content = "Late" });
        fixture.Pump();
        Click(fixture, "Late");

        Assert.Equal(["audio/tone.wav"], Sources(fixture));
    }

    // A default Aspect attaches its program with the element, before Loaded:
    // the Aspect pass of the next frame would be too late for @on Loaded.
    [Fact]
    public void DefaultAspectOnLoadedRunsWhenTheElementAttaches()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" +
            "<TimbreClip Name=\"Tone\">@sound Tone { Source = \"audio/tone.wav\"; }</TimbreClip>" +
            "<Aspect TargetType=\"Button\">@timbre $Tone; @on Loaded { @play $self.timbre.Tone; }</Aspect>" +
            "</StackPanel.Resources><Button Content=\"Static\" /></StackPanel>");
        StackPanel panel = fixture.All<StackPanel>().First();
        Assert.Equal(["audio/tone.wav"], Sources(fixture));

        panel.VisualChildren.Add(new Button { Content = "Late" });
        fixture.Pump();

        Assert.Equal(["audio/tone.wav", "audio/tone.wav"], Sources(fixture));
    }

    [Fact]
    public void InlineAspectAssignedToAnotherElementKeepsItsNamedReferences()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><Border Name=\"Speaker\" Width=\"20\" Height=\"21\" />" +
            "<Button Content=\"Pause\"><Button.Aspect>" +
            "@on Click { @animate with Tween(10ms, Linear) { @to { $Speaker.Opacity = 0.25; } } }" +
            "</Button.Aspect></Button></StackPanel>");
        StackPanel panel = fixture.All<StackPanel>().First();
        Border speaker = fixture.All<Border>().Single(border => border.Width == 20);
        Button pause = fixture.All<Button>().Single();
        Button extra = new() { Content = "Extra" };

        extra.Aspect = pause.Aspect;
        panel.VisualChildren.Add(extra);
        fixture.Pump();
        Click(fixture, "Extra");
        Settle(fixture);

        Assert.Equal(0.25f, speaker.Opacity);
    }

    // Etapa 3: @presence and @layout are values an Aspect brings, so they come
    // with a runtime Aspect (also on an attached element) and leave with it.
    [Fact]
    public void PresenceAndLayoutComeAndGoWithARuntimeAspect()
    {
        using MarkupTimbreFixture fixture = new(
            "<StackPanel><StackPanel.Resources>" +
            "<Aspect Name=\"Plain\" TargetType=\"TextBlock\">@default { Opacity = 1; }</Aspect>" +
            "<Aspect Name=\"Fancy\" TargetType=\"TextBlock\">" +
            "@presence { enter = Tween(100ms, EaseOut); exit = Tween(100ms, EaseIn); } " +
            "@layout id $self.Text with Tween(100ms, EaseOut);</Aspect>" +
            "</StackPanel.Resources><TextBlock Text=\"card\" Aspect=\"$Plain\" /></StackPanel>");
        TextBlock text = fixture.All<TextBlock>().Single();
        Assert.Null(text.Presence);
        Assert.Null(text.LayoutMotion);

        text.Aspect = Aspect(text, "Fancy");
        fixture.Pump();
        Assert.NotNull(text.Presence);
        Assert.NotNull(text.LayoutMotion);
        Assert.NotNull(text.LayoutMotionId);

        text.Aspect = Aspect(text, "Plain");
        fixture.Pump();
        Assert.Null(text.Presence);
        Assert.Null(text.LayoutMotion);
        Assert.Null(text.LayoutMotionId);
    }

    [Fact]
    public void ReplacingTheAspectStopsItsRunningAnimationAndSoundAtOnce()
    {
        using MarkupTimbreFixture fixture = new(Panel(
            "<Button Content=\"Play\" Aspect=\"$Slow\" />",
            "<Aspect Name=\"Slow\" TargetType=\"Button\">@timbre $Tone; @on Click { @play $self.timbre.Tone; " +
            "@animate with Tween(1000ms, Linear) { @to { Opacity = 0.2; } } }</Aspect>"));
        Button button = fixture.All<Button>().Single();
        Click(fixture, "Play");
        fixture.Pump(TimeSpan.FromMilliseconds(100));
        fixture.Pump(TimeSpan.FromMilliseconds(100));
        TimbrePlayback playback = Assert.Single(fixture.Started);
        float midway = button.Opacity;
        Assert.InRange(midway, 0.21f, 0.99f);

        button.Aspect = Aspect(button, "A");

        // What the old Aspect brought leaves with it: its sound is canceled at
        // once and the value its animation was writing returns to the base.
        Assert.Equal(TimbrePlaybackState.Canceled, playback.State);
        fixture.Pump(TimeSpan.FromMilliseconds(100));
        Assert.Equal(1f, button.Opacity);
        fixture.Pump(TimeSpan.FromMilliseconds(500));
        Assert.Equal(1f, button.Opacity);
    }

    [Fact]
    public void AHandlerCanReplaceTheAspectWhileItsClickIsDispatched()
    {
        using MarkupTimbreFixture fixture = new(Panel("<Button Content=\"Play\" Aspect=\"$A\" />"));
        Button button = fixture.All<Button>().Single();
        ElementAspect b = Aspect(button, "B");
        button.Click += (_, _) => button.Aspect = b;

        Click(fixture, "Play");
        fixture.Pump();
        int afterFirst = fixture.Started.Count;
        Click(fixture, "Play");
        fixture.Pump();

        Assert.InRange(afterFirst, 0, 1);
        Assert.Equal("audio/other.wav", Sources(fixture).Last());
        Assert.Equal(afterFirst + 1, fixture.Started.Count);
    }

    [Fact]
    public void IdleFramesAfterAReplacementDoNoWork()
    {
        InvalidationTrace trace = new(capacity: 1 << 16);
        using MarkupTimbreFixture fixture = new(
            Panel(
                "<Border Width=\"40\" Height=\"40\" Opacity=\"0.9\" Aspect=\"$WhenA\" />",
                "<Aspect Name=\"WhenA\" TargetType=\"Border\">@timbre $Tone; @when $self.Opacity { @if value > 0.5 { @play $self.timbre.Tone; } }</Aspect>" +
                "<Aspect Name=\"WhenB\" TargetType=\"Border\">@timbre $Other; @when $self.Opacity { @if value > 0.5 { @play $self.timbre.Other; } }</Aspect>"),
            trace: trace);
        Border border = fixture.All<Border>().Single();
        border.Aspect = Aspect(border, "WhenB");
        for (int frame = 0; frame < 3; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(50));
        }

        MarkupConditionController controller = Assert.Single(border.LifecycleBehaviors.OfType<MarkupConditionController>());
        int evaluations = controller.EvaluationCount;
        int invalidations = Invalidations(trace);
        int started = fixture.Started.Count;

        for (int frame = 0; frame < 30; frame++)
        {
            fixture.Pump(TimeSpan.FromMilliseconds(50));
        }

        Assert.Equal(["audio/tone.wav", "audio/other.wav"], Sources(fixture));
        Assert.Equal(started, fixture.Started.Count);
        Assert.Equal(evaluations, controller.EvaluationCount);
        Assert.Equal(invalidations, Invalidations(trace));
    }

    private static int Invalidations(InvalidationTrace trace) => trace.Entries.Count(entry =>
        entry.Kind is InvalidationTraceEventKind.Request or InvalidationTraceEventKind.Queue or InvalidationTraceEventKind.Phase);

    private static string Panel(string children, string extraResources) =>
        "<StackPanel><StackPanel.Resources>" + Resources + extraResources + "</StackPanel.Resources>" + children + "</StackPanel>";

    private static string Panel(string children) =>
        "<StackPanel><StackPanel.Resources>" + Resources + "</StackPanel.Resources>" + children + "</StackPanel>";

    private static ElementAspect Aspect(UIElement element, string name) =>
        element.TryFindResource(new ResourceId<ElementAspect>(name), out ElementAspect aspect)
            ? aspect
            : throw new InvalidOperationException("Aspect resource '" + name + "' was not found.");

    private static string[] Sources(MarkupTimbreFixture fixture) =>
        fixture.Started.Select(playback => playback.Sound.Source.ToString()).ToArray();

    private static void Click(MarkupTimbreFixture fixture, string name) => fixture.ClickAsync(name).GetAwaiter().GetResult();

    private static void Settle(MarkupTimbreFixture fixture)
    {
        fixture.Pump();
        fixture.Pump(TimeSpan.FromMilliseconds(50));
        fixture.Pump(TimeSpan.FromMilliseconds(50));
    }
}
