using System;
using Microsoft.CodeAnalysis;
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

        GeneratorRunResult result = RunGenerator("MotionHandles.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("StartMotionExecution(", generated, StringComparison.Ordinal);
        Assert.Contains("\"Active\", motionExecutionFactory", generated, StringComparison.Ordinal);
        Assert.Contains("CancelMotionExecution(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("@handle", generated, StringComparison.Ordinal);
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

        GeneratorRunResult result = RunGenerator("MotionHandleInvalid.cui.xml", markup, out _);

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

        GeneratorRunResult result = RunGenerator("MotionClipCancel.cui.xml", markup, out _);

        AssertContainsHandleDiagnostic(result, "cannot contain @cancel");
    }

    [Theory]
    [InlineData("@handle Active;", "Aspect")]
    [InlineData("@cancel Active;", "Aspect")]
    [InlineData("@complete Active;", "Unsupported")]
    public void MotionHandleCommandsAreRejectedOutsideAspect(string command, string expectedMessage)
    {
        string markup = $"<Border>{command}</Border>";

        GeneratorRunResult result = RunGenerator("MotionHandleOutsideAspect.cui.xml", markup, out _);

        AssertContainsHandleDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void HandledRunCanParticipateInComposition()
    {
        string markup = MotionHandleMarkup(
            "@handle Active; @on Loaded { @sequence { @run $Pulse as Active; } }");

        GeneratorRunResult result = RunGenerator(
            "MotionHandleComposition.cui.xml",
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
