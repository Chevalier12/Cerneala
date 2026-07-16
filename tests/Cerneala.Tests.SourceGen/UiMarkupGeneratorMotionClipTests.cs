using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionSetGeneratesTypedImmediateAssignmentsInsideSequence()
    {
        const string markup = """
            <Border Aspect="$Stateful">
              <Border.Resources>
                <MotionClip Name="Replay" TargetType="Border">
                  @sequence
                  {
                    @set { $Status.Text = "ARMING"; Opacity = 0.5; }
                    @animate with Tween(100ms) { @to { Opacity = 1; } }
                  }
                </MotionClip>
                <Aspect Name="Stateful" TargetType="Border">
                  @on Loaded { @run $Replay; }
                </Aspect>
              </Border.Resources>
              <TextBlock Name="Status" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionSet.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("Status.SetValue(global::Cerneala.UI.Controls.TextBlock.TextProperty, \"ARMING\")", generated, StringComparison.Ordinal);
        Assert.Contains("SetValue(global::Cerneala.UI.Elements.UIElement.OpacityProperty, 0.5f)", generated, StringComparison.Ordinal);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("current")]
    [InlineData("1 with Tween(100ms)")]
    public void MotionSetRejectsAnimatedOrImplicitValues(string value)
    {
        string markup = $$"""
            <Border>
              <Border.Resources>
                <MotionClip Name="Invalid" TargetType="Border">
                  @set { Opacity = {{value}}; }
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionSetInvalid.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, "concrete values");
    }

    [Fact]
    public void MotionClipExpandsNestedRecipeAtRunSiteAndResolvesNamedElementsStatically()
    {
        const string markup = """
            <Border Aspect="$Entrance">
              <Border.Resources>
                <MotionClip Name="EntranceClip" TargetType="Cerneala.UI.Controls.Control">
                  @sequence
                  {
                    @animate { @to { Opacity = 1; } }
                    @parallel
                    {
                      @animate { @to { Scale = 1; } }
                      @animate { @to { $Ring.Opacity = 1; } }
                    }
                  }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="Border">
                  @on Loaded { @run $EntranceClip; }
                </Aspect>
              </Border.Resources>
              <Border Name="Ring" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipNested.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("MarkupMotionExecution.Sequence", generated, StringComparison.Ordinal);
        Assert.Contains("MarkupMotionExecution.Parallel", generated, StringComparison.Ordinal);
        Assert.Contains("Ring", generated, StringComparison.Ordinal);
        Assert.Contains("UIElement.OpacityProperty", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("new global::Cerneala.UI.Markup.MotionClip", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetResource", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Dictionary", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void MotionClipCreatesIndependentFactoriesForEveryRunAndAspectInstance()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <MotionClip Name="Pulse" TargetType="Button">
                  @animate { @to { Opacity = 0.5; } }
                </MotionClip>
                <Aspect Name="PulseAspect" TargetType="Button">
                  @on Loaded { @run $Pulse; }
                  @on Click { @run $Pulse; }
                </Aspect>
              </StackPanel.Resources>
              <Button Aspect="$PulseAspect" />
              <Button Aspect="$PulseAspect" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipIndependentRuns.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        string[] factories = Regex.Matches(generated, @"motionExecutionFactory\d+")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(4, factories.Length);
        Assert.Equal(2, generated.Split("AttachMotionSession", StringSplitOptions.None).Length - 1);
        Assert.Equal(4, generated.Split("StartMotionExecution", StringSplitOptions.None).Length - 1);
    }

    [Theory]
    [InlineData("@when IsEnabled { @animate { @to { Opacity = 1; } } }", "activation")]
    [InlineData("@on Loaded { @animate { @to { Opacity = 1; } } }", "@on")]
    [InlineData("@animate { @to { Opacity = 1; } } @animate { @to { Scale = 1; } }", "exactly one")]
    public void MotionClipRejectsActivationAndMultipleTopLevelBodies(string body, string expectedMessage)
    {
        string markup = $$"""
            <Border>
              <Border.Resources>
                <MotionClip Name="Invalid" TargetType="Border">
                  {{body}}
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipInvalidBody.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionClipRejectsRecursiveInvocation()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <MotionClip Name="Recursive" TargetType="Border">
                  @sequence
                  {
                    @animate { @to { Opacity = 1; } }
                    @run $Recursive;
                  }
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipRecursive.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, "recursive");
    }

    [Fact]
    public void MotionClipReportsMissingResource()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionClipMissing.cui.xml",
            MotionAspectMarkup("@on Loaded { @run $Missing; }"),
            out _);

        AssertMotionDiagnostic(result, "$Missing");
    }

    [Fact]
    public void MotionClipReportsWrongTargetType()
    {
        const string markup = """
            <Button Aspect="$RunClip">
              <Button.Resources>
                <MotionClip Name="BorderClip" TargetType="Border">
                  @animate { @to { Opacity = 1; } }
                </MotionClip>
                <Aspect Name="RunClip" TargetType="Button">
                  @on Loaded { @run $BorderClip; }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipWrongTarget.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, "TargetType");
    }

    [Fact]
    public void MotionClipReportsMissingNamedElementAtRunSite()
    {
        const string markup = """
            <Border Aspect="$RunClip">
              <Border.Resources>
                <MotionClip Name="NamedTargetClip" TargetType="Border">
                  @animate { @to { $Missing.Opacity = 1; } }
                </MotionClip>
                <Aspect Name="RunClip" TargetType="Border">
                  @on Loaded { @run $NamedTargetClip; }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipMissingNamedElement.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, "Missing");
    }

    [Fact]
    public void MotionClipCannotBeAssignedDirectlyToAControl()
    {
        const string markup = """
            <Border MotionClip="$Pulse">
              <Border.Resources>
                <MotionClip Name="Pulse" TargetType="Border">
                  @animate { @to { Opacity = 1; } }
                </MotionClip>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipDirectAssignment.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, "cannot be assigned directly");
    }

    [Fact]
    public void MotionRunIsRejectedOutsideAnAspect()
    {
        const string markup = """
            <Border>
              @run $Pulse;
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionRunOutsideAspect.cui.xml", markup, out _);

        AssertMotionDiagnostic(result, "Aspect");
    }
}
