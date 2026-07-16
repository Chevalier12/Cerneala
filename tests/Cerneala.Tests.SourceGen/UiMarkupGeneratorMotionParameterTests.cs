using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MotionClipParametersAreSpecializedStaticallyAtEachRunSite()
    {
        const string markup = """
            <Border Aspect="$Entrance">
              <Border.Resources>
                <Tween Name="QuickOut" Duration="160ms" Easing="EaseOut" />
                <MotionClip Name="SlideIn" TargetType="Border">
                  @parameter Distance: float = 24;
                  @parameter EntranceSpec: MotionSpec[float] = $QuickOut;
                  @parameter Hold: bool = true;
                  @parameter Label: string = "default";
                  @animate with EntranceSpec
                  {
                    holdOnComplete = Hold;
                    debugName = Label;
                    @from { TranslateY = Distance; }
                    @to { TranslateY = 0; }
                  }
                </MotionClip>
                <Aspect Name="Entrance" TargetType="Border">
                  @on Loaded { @run $SlideIn(Distance = 40, Label = "loaded"); }
                  @on Unloaded { @run $SlideIn(EntranceSpec = Tween(90ms, Linear), Hold = false); }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipParameters.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.Contains("40f", generated, StringComparison.Ordinal);
        Assert.Contains("24f", generated, StringComparison.Ordinal);
        Assert.Contains("FromMilliseconds(90", generated, StringComparison.Ordinal);
        Assert.Contains("HoldOnComplete = false", generated, StringComparison.Ordinal);
        Assert.Contains("DebugName = \"loaded\"", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Dictionary<string, object", generated, StringComparison.Ordinal);
        Assert.DoesNotContain(" dynamic ", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ParameterizedNestedClipEmitsANewFactoryTreeForEveryRun()
    {
        const string markup = """
            <Border Aspect="$NestedMotion">
              <Border.Resources>
                <MotionClip Name="Nested" TargetType="Border">
                  @parameter Distance: float;
                  @sequence
                  {
                    @animate { @to { Opacity = 1; } }
                    @parallel
                    {
                      @animate { @to { TranslateY = Distance; } }
                      @animate { @to { Scale = 1; } }
                    }
                  }
                </MotionClip>
                <Aspect Name="NestedMotion" TargetType="Border">
                  @on Loaded { @run $Nested(Distance = 12); }
                  @on Unloaded { @run $Nested(Distance = 20); }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("MotionClipNestedParameters.cui.xml", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        string[] factories = Regex.Matches(generated, @"motionExecutionFactory\d+")
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(10, factories.Length);
        Assert.Contains("12f", generated, StringComparison.Ordinal);
        Assert.Contains("20f", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetResource", generated, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        "@parameter Distance: float; @parameter Distance: float; @animate { @to { TranslateY = Distance; } }",
        "Duplicate")]
    [InlineData(
        "@parameter Payload: object; @animate { @to { Opacity = 1; } }",
        "unsupported type")]
    [InlineData(
        "@parameter Distance: float = nope; @animate { @to { TranslateY = Distance; } }",
        "not compatible")]
    [InlineData(
        "@parameter Distance: float; @animate { @to { TranslateY = Distance; } }",
        "requires argument")]
    [InlineData(
        "@parameter Count: int = 2; @animate { @to { TranslateY = Count; } }",
        "not compatible with property type")]
    [InlineData(
        "@animate { @to { Opacity = 1; } } @parameter Distance: float = 2;",
        "before")]
    public void MotionClipRejectsInvalidParameterDeclarationsAndUses(string body, string expectedMessage)
    {
        string markup = MotionClipParameterMarkup(body, "@run $Parameterized;");

        GeneratorRunResult result = RunGenerator("MotionClipInvalidParameter.cui.xml", markup, out _);

        AssertContainsMotionDiagnostic(result, expectedMessage);
    }

    [Theory]
    [InlineData("@run $Parameterized(Unknown = 2);", "Unknown parameter")]
    [InlineData("@run $Parameterized(Distance = 2, Distance = 3);", "Duplicate")]
    [InlineData("@run $Parameterized(Distance = false);", "not compatible")]
    public void MotionRunRejectsInvalidNamedArguments(string run, string expectedMessage)
    {
        string markup = MotionClipParameterMarkup(
            "@parameter Distance: float; @animate { @to { TranslateY = Distance; } }",
            run);

        GeneratorRunResult result = RunGenerator("MotionClipInvalidArgument.cui.xml", markup, out _);

        AssertContainsMotionDiagnostic(result, expectedMessage);
    }

    [Fact]
    public void MotionClipRejectsSpecParameterWithWrongValueType()
    {
        string markup = MotionClipParameterMarkup(
            "@parameter Spec: MotionSpec[double] = Tween(100ms); @animate with Spec { @to { TranslateY = 0; } }",
            "@run $Parameterized;");

        GeneratorRunResult result = RunGenerator("MotionClipWrongSpecType.cui.xml", markup, out _);

        AssertContainsMotionDiagnostic(result, "not compatible with property type");
    }

    [Fact]
    public void MotionClipRejectsCSharpGenericParameterSyntax()
    {
        string markup = MotionClipParameterMarkup(
            "@parameter Spec: MotionSpec&lt;float&gt; = Tween(100ms); @animate with Spec { @to { Opacity = 1; } }",
            "@run $Parameterized;");

        GeneratorRunResult result = RunGenerator("MotionClipCSharpGenericSyntax.cui.xml", markup, out _);

        AssertContainsMotionDiagnostic(result, "MotionSpec[float]");
    }

    [Fact]
    public void MotionParameterIsRejectedOutsideMotionClip()
    {
        GeneratorRunResult result = RunGenerator(
            "MotionParameterOutsideClip.cui.xml",
            MotionAspectMarkup("@parameter Distance: float = 2; @on Loaded { @animate { @to { Opacity = 1; } } }"),
            out _);

        AssertContainsMotionDiagnostic(result, "MotionClip");
    }

    private static string MotionClipParameterMarkup(string clipBody, string run)
    {
        return $$"""
            <Border Aspect="$ParameterAspect">
              <Border.Resources>
                <MotionClip Name="Parameterized" TargetType="Border">
                  {{clipBody}}
                </MotionClip>
                <Aspect Name="ParameterAspect" TargetType="Border">
                  @on Loaded { {{run}} }
                </Aspect>
              </Border.Resources>
            </Border>
            """;
    }

    private static void AssertContainsMotionDiagnostic(GeneratorRunResult result, string expectedMessage)
    {
        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.GetMessage().Contains(expectedMessage, StringComparison.OrdinalIgnoreCase));
    }
}
