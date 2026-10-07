using System;
using System.IO;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionHandlesEmitSessionScopedReplacementAndCancellation()
    {
        const string markup = """
            <Border Aspect="$HandledMotion">
              <Border.Resources>
                <MotionClip Name="Pulse" TargetType="Border">
                  @parameter Destination: float = 0.5;
                  @animate { @to { Opacity = Destination; } }
                </MotionClip>
                <Aspect Name="HandledMotion" TargetType="Border">
                  @handle Active;
                  @on Loaded { @run $Pulse(Destination = 0.75) as Active; }
                  @on Unloaded { @cancel Active; }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionHandles.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("StartMotionExecution(", generated, StringComparison.Ordinal);
        Assert.Contains("\"Active\", motionExecutionFactory", generated, StringComparison.Ordinal);
        Assert.Contains("CancelMotionExecution(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("@handle", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ConditionalCancelStopsHandledExecutionWhenConditionBecomesTrue()
    {
        const string markup = """
            <Border Aspect="$HandledMotion" Opacity="1">
              <Border.Resources>
                <MotionClip Name="Pulse" TargetType="Border">
                  @animate with Tween(1000ms) { @from { Opacity = 1; } @to { Opacity = 0; } }
                </MotionClip>
                <Aspect Name="HandledMotion" TargetType="Border">
                  @handle Active;
                  @on Loaded { @run $Pulse as Active; }
                  @when $self.IsEnabled { @if value == false { @cancel Active; } }
                </Aspect>
              </Border.Resources>
            </Border>
            """;
        GeneratorRunResult result = RunGenerator("ConditionalMotionCancel.crn", markup, out Compilation compilation);
        AssertNoGeneratorOrCompilationErrors(result, compilation);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.ConditionalMotionCancelFactory"));
        ManualClock clock = new();
        UIRoot root = new(100, 100, motionClock: clock);
        root.VisualChildren.Add(border);
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(250));
        root.ProcessFrame();
        Assert.True(root.Motion.HasActiveMotion);
        float runningOpacity = border.Opacity;
        Assert.InRange(runningOpacity, 0.01f, 0.99f);

        border.IsEnabled = false;
        root.ProcessFrame();
        float canceledOpacity = border.Opacity;
        clock.Advance(TimeSpan.FromMilliseconds(500));
        root.ProcessFrame();

        Assert.False(root.Motion.HasActiveMotion);
        Assert.Equal(canceledOpacity, border.Opacity);
    }

    [Theory]
    [InlineData(
        "@on Loaded { @run $Pulse as Missing; } @handle Missing;",
        "used before")]
    [InlineData(
        "@handle Active; @handle Active; @on Loaded { @run $Pulse as Active; }",
        "Duplicate")]
    [InlineData(
        "@on Loaded { @run $Pulse as Missing; }",
        "undeclared")]
    [InlineData(
        "@on Loaded { @cancel Missing; }",
        "undeclared")]
    public void MotionHandlesRejectInvalidDeclarationAndUseOrder(string aspectBody, string expectedMessage)
    {
        string markup = MotionHandleMarkup(aspectBody);

        GeneratorRunResult result = RunGenerator("MotionHandleInvalid.crn", markup, out _);

        AssertContainsHandleDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionClipRejectsCancelCommands()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <MotionClip Name="Invalid" TargetType="Border">
                  @cancel Active;
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipCancel.crn", markup, out _);

        AssertContainsHandleDiagnostic(result, "cannot contain @cancel");
    }

    [Theory]
    [InlineData("@handle Active;", "Aspect")]
    [InlineData("@cancel Active;", "Aspect")]
    [InlineData("@complete Active;", "Unsupported")]
    public void MotionHandleCommandsAreRejectedOutsideAspect(string command, string expectedMessage)
    {
        string markup = $"<Border>{command}</Border>";

        GeneratorRunResult result = RunGenerator("MotionHandleOutsideAspect.crn", markup, out _);

        AssertContainsHandleDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void HandledRunCanParticipateInComposition()
    {
        string markup = MotionHandleMarkup(
            "@handle Active; @on Loaded { @sequence { @run $Pulse as Active; } }");

        GeneratorRunResult result = RunGenerator(
            "MotionHandleComposition.crn",
            markup,
            out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
        Assert.Contains("\"Active\", motionExecutionFactory", generated, StringComparison.Ordinal);
    }

    private static string MotionHandleMarkup(string aspectBody)
    {
        return $$"""
            <Border Aspect="$HandledMotion">
              <Border.Resources>
                <MotionClip Name="Pulse" TargetType="Border">
                  @animate { @to { Opacity = 0.5; } }
                </MotionClip>
                <Aspect Name="HandledMotion" TargetType="Border">
                  {{aspectBody}}
                </Aspect>
              </Border.Resources>
            </Border>
            """;
    }

    private static void AssertContainsHandleDiagnostic(GeneratorRunResult result, string expectedMessage)
    {
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));
    }
}
