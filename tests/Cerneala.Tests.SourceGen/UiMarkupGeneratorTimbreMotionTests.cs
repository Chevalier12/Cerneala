using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

// `$X.timbre.Sound.Property` Motion targets: a typed audio binding on the
// running playback of a sound, never an element property.
public sealed partial class UiMarkupGeneratorTests
{
    private const string TimbreMotionApprovedExample = """
        <Button Content="Confirmă">
          <Button.Resources>
            <TimbreClip Name="ConfirmTimbre">
              @parameter ToneCutoff: float = 1200;
              @parameter EchoMix: float = 0.15;
              @sound Confirm
              {
                Source = "audio/confirm.wav";
                Volume = 0.2;
                @modifier LowPass { Cutoff = ToneCutoff; }
                @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
              }
            </TimbreClip>
          </Button.Resources>
          <Button.Aspect>
            @timbre $ConfirmTimbre(ToneCutoff = 800);

            @on Click
            {
                @play $self.timbre.Confirm;
                @animate with Tween(300ms, EaseOut)
                {
                    @to
                    {
                        $self.timbre.Confirm.Volume = 0.8;
                        $self.timbre.Confirm.ToneCutoff = 6000;
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
    // target element, the sound and the parameter; no element property is
    // involved.
    [Fact]
    public void TimbreMotionLowersToTheTimbreSessionThroughTypedGeneratedMarkupHelpers()
    {
        GeneratorRunResult result = RunGenerator("TimbreMotionLowering.crn", TimbreMotionApprovedExample, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.DoesNotContain("CanStartMotionExecution", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("StartMotionProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        AssertTimbreMotionCalls(compilation, ("Confirm", "Volume"), ("Confirm", "ToneCutoff"));
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
        AssertTimbreMotionCalls(compilation, ("Confirm", "Volume"), ("Confirm", "ToneCutoff"));
    }

    // `$Speaker.timbre.Music.Volume` animates the sound of another element: the
    // leaf receives that element, not the Aspect's own target.
    [Fact]
    public void TimbreMotionOnANamedElementPassesThatElement()
    {
        const string markup = """
            <StackPanel>
              <Border Name="Speaker">
                <Border.Aspect>@timbre { @sound Music { Source = "audio/music.ogg"; Loop = true; AutoPlay = true; } }</Border.Aspect>
              </Border>
              <Button Content="Quiet">
                <Button.Aspect>@on Click { @animate with Tween(500ms, Linear) { @to { $Speaker.timbre.Music.Volume = 0.1; } } }</Button.Aspect>
              </Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("TimbreMotionNamed.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreMotionCalls(compilation, ("Music", "Volume"));
        Assert.DoesNotContain("StartTimbreMotionProperty(target,", SingleGeneratedSource(result), StringComparison.Ordinal);
    }

    private static void AssertTimbreMotionCalls(Compilation compilation, params (string Sound, string Parameter)[] expected)
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
                    Assert.Equal("Cerneala.UI.Elements.UIElement", method.Parameters[0].Type.ToDisplayString());
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

    // A `.timbre.` path names a sound property; an element property with the
    // same last segment must never be animated in its place.
    [Fact]
    public void TimbreMotionTargetNeverFallsBackToAnElementProperty()
    {
        string markup = TimbreMotionApprovedExample
            .Replace("$self.timbre.Confirm.ToneCutoff = 6000;", string.Empty, StringComparison.Ordinal)
            .Replace("$self.timbre.Confirm.Volume = 0.8;", "$self.timbre.Confirm.Opacity = 0.5;", StringComparison.Ordinal);

        GeneratorRunResult result = RunGenerator("TimbreMotionElementFallback.crn", markup, out _);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.GetMessage().Contains("Opacity", StringComparison.Ordinal) &&
            diagnostic.GetMessage().Contains("Confirm", StringComparison.Ordinal));
    }
}
