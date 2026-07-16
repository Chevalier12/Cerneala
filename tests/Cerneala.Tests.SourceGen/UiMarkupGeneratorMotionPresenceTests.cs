using System;
using System.IO;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;
using Cerneala.UI.Motion.Presence;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionPresenceGeneratesTypedOptionsBeforeAttachment()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionPresence.cui.xml",
            PresenceMarkup(excludeInputWhileExiting: false),
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MotionSpec<float>", generated);
        Assert.Contains("PresenceOptions.FadeAndScale", generated);
        Assert.Contains(", false);", generated);
        Assert.Contains("@presence must be applied before the element is attached", generated);
        Assert.DoesNotContain("SetPresenceVisual", generated);
        Assert.DoesNotContain("SetPresenceExiting", generated);
    }

    [Fact]
    public void GeneratedPresenceUsesRuntimeStateMachineForEnterExitInputAndSingleRemoval()
    {
        Border border = CreateGeneratedPresenceBorder(PresenceMarkup(excludeInputWhileExiting: false));
        ManualClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);

        Assert.False(border.IsAttached);
        Assert.NotNull(border.Presence);
        root.VisualChildren.Add(border);
        Assert.Equal(PresenceState.Present, root.Motion.Presence.GetState(border));
        Assert.Equal(0, border.PresenceOpacity);

        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();
        Assert.Equal(1, border.PresenceOpacity);

        Assert.True(root.VisualChildren.Remove(border));
        root.ProcessFrame();
        Assert.False(border.Presence!.ExcludeInputWhileExiting);
        Assert.Equal(PresenceState.Exiting, root.Motion.Presence.GetState(border));

        clock.Advance(TimeSpan.FromMilliseconds(120));
        root.ProcessFrame();
        root.ProcessFrame();
        Assert.False(border.IsAttached);
        Assert.Equal(0, root.Motion.Presence.ActiveExitCount);
        Assert.Equal(PresenceState.Detached, root.Motion.Presence.GetState(border));
    }

    [Fact]
    public void GeneratedPresenceCompletesEnterAndExitUnderReducedMotion()
    {
        Border border = CreateGeneratedPresenceBorder(PresenceMarkup(excludeInputWhileExiting: true));
        UIRoot root = new(
            100,
            100,
            reducedMotion: new ReducedMotionPolicy(ReducedMotionMode.Reduce));

        root.VisualChildren.Add(border);
        Assert.Equal(1, border.PresenceOpacity);
        Assert.Equal(1, border.PresenceScale);
        Assert.False(root.Motion.HasActiveMotion);

        Assert.True(root.VisualChildren.Remove(border));
        Assert.False(border.IsAttached);
        Assert.Equal(0, root.Motion.Presence.ActiveExitCount);
    }

    [Theory]
    [InlineData("@presence { exit = Tween(100ms); }", "both enter and exit")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); initial = skip; }", "initial mode")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); enterOpacity = 0; }", "custom endpoints")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); @enter { } }", "custom @enter")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); } @presence { enter = Tween(100ms); exit = Tween(100ms); }", "only one")]
    [InlineData("@when IsEnabled { @presence { enter = Tween(100ms); exit = Tween(100ms); } }", "directly inside")]
    public void MotionPresenceRejectsUnsupportedOrRetroactiveShapes(string body, string expectedMessage)
    {
        GeneratorRunResult result = RunGenerator(
            "InvalidMotionPresence.cui.xml",
            MotionAspectMarkup(body),
            out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    private static Border CreateGeneratedPresenceBorder(string markup)
    {
        GeneratorRunResult result = RunGenerator("RuntimeMotionPresence.cui.xml", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.RuntimeMotionPresenceFactory"));
    }

    private static string PresenceMarkup(bool excludeInputWhileExiting)
    {
        return $$"""
            <Border Background="#FFFFFFFF">
              <Border.Resources>
                <Tween Name="QuickOut" Duration="100ms" Easing="EaseOut" />
                <Aspect Target="Border">
                  @presence
                  {
                    enter = $QuickOut;
                    exit = Tween(100ms, EaseIn);
                    excludeInputWhileExiting = {{excludeInputWhileExiting.ToString().ToLowerInvariant()}};
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;
    }

    private sealed class ManualClock : IMotionClock
    {
        public TimeSpan Now { get; private set; }

        public void Advance(TimeSpan delta)
        {
            Now += delta;
        }
    }
}
