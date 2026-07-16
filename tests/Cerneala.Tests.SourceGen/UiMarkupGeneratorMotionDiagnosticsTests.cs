using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cerneala.SourceGen;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    [Fact]
    public void MachineReadableMotionDirectiveTableIsConsumedByContextDiagnostics()
    {
        Type language = typeof(UiMarkupGenerator).Assembly.GetType("Cerneala.SourceGen.MotionMarkupLanguage")!;
        PropertyInfo namesProperty = language.GetProperty("DirectiveNames", BindingFlags.NonPublic | BindingFlags.Static)!;
        MethodInfo classifier = language.GetMethod("IsDirective", BindingFlags.NonPublic | BindingFlags.Static)!;
        IReadOnlyList<string> directiveNames = (IReadOnlyList<string>)namesProperty.GetValue(null)!;

        Assert.NotEmpty(directiveNames);
        Assert.Equal(directiveNames.Count, directiveNames.Distinct(StringComparer.Ordinal).Count());
        foreach (string directive in directiveNames)
        {
            Assert.True((bool)classifier.Invoke(null, [directive])!);
        }

        Assert.False((bool)classifier.Invoke(null, ["@notMotion"])!);
    }

    [Theory]
    [InlineData("@on Loaded { @animate { @from { Opacity = 0; } } }", "CERNEALAUI020")]
    [InlineData("@on Loaded { @parallel { } }", "CERNEALAUI024")]
    [InlineData("@presence { enter = Tween(100ms); exit = Tween(100ms); } @presence { enter = Tween(100ms); exit = Tween(100ms); }", "CERNEALAUI025")]
    [InlineData("@on Loaded { @animate with Decay(1) { @to { Opacity = 1; } } }", "CERNEALAUI026")]
    public void MotionDiagnosticsUseCategorySpecificIds(string body, string expectedId)
    {
        GeneratorRunResult result = RunGenerator(
            "MotionDiagnosticCategory.cui.xml",
            MotionAspectMarkup(body),
            out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(candidate => candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal(expectedId, diagnostic.Id);
    }

    [Fact]
    public void MotionTargetDiagnosticSelectsTheExactPropertyToken()
    {
        const string token = "DoesNotExist";
        string markup = MotionAspectMarkup("@on Loaded { @animate { @to { " + token + " = 1; } } }");

        GeneratorRunResult result = RunGenerator("MotionTargetSpan.cui.xml", markup, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(candidate => candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI021", diagnostic.Id);
        Assert.Equal(token, markup.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public void MotionSpecDiagnosticSelectsCrossScopeResourceReference()
    {
        const string token = "$MissingOuterSpec";
        const string markup = """
            <StackPanel>
              <Border>
                <Border.Resources>
                  <Aspect Name="Motion" TargetType="Border">
                    @on Loaded { @animate with $MissingOuterSpec { @to { Opacity = 1; } } }
                  </Aspect>
                </Border.Resources>
                <Border Aspect="$Motion" />
              </Border>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("MotionResourceSpan.cui.xml", markup, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(candidate => candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI023", diagnostic.Id);
        Assert.Equal(token, markup.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public void MotionEventDiagnosticSuggestsConcreteTargetTypeAndSelectsEvent()
    {
        const string markup = """
            <Button Aspect="$Motion">
              <Button.Resources>
                <Aspect Name="Motion" TargetType="Cerneala.UI.Elements.UIElement">
                  @on Click { @animate { @to { Opacity = 1; } } }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("MotionEventSuggestion.cui.xml", markup, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics.Where(candidate => candidate.Severity == DiagnosticSeverity.Error));
        Assert.Equal("CERNEALAUI022", diagnostic.Id);
        Assert.Contains("TargetType=\"Cerneala.UI.Controls.Button\"", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Equal("Click", markup.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length));
    }

    [Fact]
    public void MotionGeneratedSourceIsDeterministicAndAvoidsRuntimeDiscoveryOrTickClosures()
    {
        string markup = MotionAspectMarkup(
            "@on Loaded { @sequence { @animate { @to { Opacity = 1; } } @animate { @to { Scale = 1; } } } }");

        GeneratorRunResult first = RunGenerator("MotionGeneratedContract.cui.xml", markup, out Compilation firstCompilation);
        GeneratorRunResult second = RunGenerator("MotionGeneratedContract.cui.xml", markup, out Compilation secondCompilation);

        AssertNoGeneratorOrCompilationErrors(first, firstCompilation);
        AssertNoGeneratorOrCompilationErrors(second, secondCompilation);
        string generated = SingleGeneratedSource(first);
        Assert.Equal(generated, SingleGeneratedSource(second));
        Assert.Contains("motionExecutionFactory", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetEvent(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("FindName(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Tick +=", generated, StringComparison.Ordinal);
    }
}
