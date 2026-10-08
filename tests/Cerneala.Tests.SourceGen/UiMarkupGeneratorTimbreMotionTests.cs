using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

// `$self.timbre.Handle.Parameter` Motion targets: a typed audio binding on the
// playback captured from a Timbre handle, never an element property.
public sealed partial class UiMarkupGeneratorTests
{
    private const string TimbreMotionApprovedExample = """
        <Button Content="Confirmă">
          <Button.Resources>
            <TimbreClip Name="ConfirmTimbre">
              Source = "audio/confirm.wav";
              Volume = 0.8;
              @parameter ToneCutoff: float = 1200;
              @parameter EchoMix: float = 0.15;
              @modifier LowPass { Cutoff = ToneCutoff; }
              @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
            </TimbreClip>
          </Button.Resources>
          <Button.Aspect>
            @handle Playback;

            @on Click
            {
                @timbre $ConfirmTimbre(Volume = 0.2, ToneCutoff = 800) as Playback;
                @animate with Tween(300ms, EaseOut)
                {
                    @to
                    {
                        $self.timbre.Playback.Volume = 0.8;
                        $self.timbre.Playback.ToneCutoff = 6000;
                    }
                }
            }
          </Button.Aspect>
        </Button>
        """;

    [Fact]
    public void TimbreMotionApprovedExampleGeneratesWithoutDiagnostics()
    {
        GeneratorRunResult result = RunGenerator("TimbreMotionApproved.crn", TimbreMotionApprovedExample, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    // The audio execution is owned by the Timbre session (no renderability
    // guard) and each leaf binds the shared GeneratedMarkup helper with the
    // handle and parameter names; no element property is involved.
    [Fact]
    public void TimbreMotionLowersToTheTimbreSessionThroughTypedGeneratedMarkupHelpers()
    {
        GeneratorRunResult result = RunGenerator("TimbreMotionLowering.crn", TimbreMotionApprovedExample, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.DoesNotContain("CanStartMotionExecution", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("StartMotionProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        AssertTimbreMotionCalls(compilation, ("Playback", "Volume"), ("Playback", "ToneCutoff"));
    }

    [Fact]
    public void TimbreMotionInPairedPartialAndReactiveBodiesBindsTheSameHelpers()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class TimbreMotionPanel : UserControl { }
            """;
        string markup = "<UserControl>" + TimbreMotionApprovedExample
            .Replace("@on Click", "@when $self.Opacity { @if value > 0.5 ", StringComparison.Ordinal)
            .Replace("</Button.Aspect>", "} </Button.Aspect>", StringComparison.Ordinal) + "</UserControl>";

        GeneratorRunResult result = RunPairedGenerator("Views/TimbreMotionPanel.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreMotionCalls(compilation, ("Playback", "Volume"), ("Playback", "ToneCutoff"));
    }

    private static void AssertTimbreMotionCalls(Compilation compilation, params (string Handle, string Parameter)[] expected)
    {
        List<(string, string)> leaves = [];
        int executions = 0;
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax invocation in tree.GetRoot()
                .DescendantNodes()
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is not IMethodSymbol method ||
                    method.ContainingType.ToDisplayString() != "Cerneala.UI.Markup.GeneratedMarkup")
                {
                    continue;
                }

                if (method.Name == "StartTimbreMotion")
                {
                    executions++;
                }
                else if (method.Name == "StartTimbreMotionProperty")
                {
                    Assert.Equal("float", method.Parameters[4].Type.ToDisplayString());
                    leaves.Add((
                        (string)model.GetConstantValue(invocation.ArgumentList.Arguments[1].Expression).Value!,
                        (string)model.GetConstantValue(invocation.ArgumentList.Arguments[2].Expression).Value!));
                }
            }
        }

        Assert.True(executions > 0, "The audio execution must start through GeneratedMarkup.StartTimbreMotion.");
        Assert.Equal(expected.OrderBy(item => item.Parameter), leaves.OrderBy(item => item.Item2));
    }

    // A `.timbre.` path names a playback parameter; an element property with
    // the same last segment must never be animated in its place.
    [Fact]
    public void TimbreMotionTargetNeverFallsBackToAnElementProperty()
    {
        string markup = TimbreMotionApprovedExample
            .Replace("$self.timbre.Playback.ToneCutoff = 6000;", string.Empty, StringComparison.Ordinal)
            .Replace("$self.timbre.Playback.Volume = 0.8;", "$self.timbre.Playback.Opacity = 0.5;", StringComparison.Ordinal);

        GeneratorRunResult result = RunGenerator("TimbreMotionElementFallback.crn", markup, out _);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.GetMessage().Contains("Opacity", StringComparison.Ordinal) &&
            diagnostic.GetMessage().Contains("Playback", StringComparison.Ordinal));
    }
}
