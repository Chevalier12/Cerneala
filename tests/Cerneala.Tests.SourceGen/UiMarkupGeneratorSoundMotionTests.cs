using Microsoft.CodeAnalysis;
using Xunit;

namespace Cerneala.Tests.SourceGen;

// `$self.sound.Handle.Parameter` Motion targets: a typed audio binding on the
// playback captured from a Sound handle, never an element property.
public sealed partial class UiMarkupGeneratorTests
{
    private const string SoundMotionApprovedExample = """
        <Button Content="Confirmă">
          <Button.Resources>
            <SoundClip Name="ConfirmSound">
              Source = "audio/confirm.wav";
              Volume = 0.8;
              @parameter ToneCutoff: float = 1200;
              @parameter EchoMix: float = 0.15;
              @modifier LowPass { Cutoff = ToneCutoff; }
              @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
            </SoundClip>
          </Button.Resources>
          <Button.Aspect>
            @handle Playback;

            @on Click
            {
                @sound $ConfirmSound(Volume = 0.2, ToneCutoff = 800) as Playback;
                @animate with Tween(300ms, EaseOut)
                {
                    @to
                    {
                        $self.sound.Playback.Volume = 0.8;
                        $self.sound.Playback.ToneCutoff = 6000;
                    }
                }
            }
          </Button.Aspect>
        </Button>
        """;

    [Fact]
    public void SoundMotionApprovedExampleGeneratesWithoutDiagnostics()
    {
        GeneratorRunResult result = RunGenerator("SoundMotionApproved.crn", SoundMotionApprovedExample, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    // The audio execution is owned by the Sound session (no renderability
    // guard) and each leaf binds the shared GeneratedMarkup helper with the
    // handle and parameter names; no element property is involved.
    [Fact]
    public void SoundMotionLowersToTheSoundSessionThroughTypedGeneratedMarkupHelpers()
    {
        GeneratorRunResult result = RunGenerator("SoundMotionLowering.crn", SoundMotionApprovedExample, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = SingleGeneratedSource(result);
        Assert.DoesNotContain("CanStartMotionExecution", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("StartMotionProperty(", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProperty(", generated, StringComparison.Ordinal);
        AssertSoundMotionCalls(compilation, ("Playback", "Volume"), ("Playback", "ToneCutoff"));
    }

    [Fact]
    public void SoundMotionInPairedPartialAndReactiveBodiesBindsTheSameHelpers()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class SoundMotionPanel : UserControl { }
            """;
        string markup = "<UserControl>" + SoundMotionApprovedExample
            .Replace("@on Click", "@when $self.Opacity { @if value > 0.5 ", StringComparison.Ordinal)
            .Replace("</Button.Aspect>", "} </Button.Aspect>", StringComparison.Ordinal) + "</UserControl>";

        GeneratorRunResult result = RunPairedGenerator("Views/SoundMotionPanel.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertSoundMotionCalls(compilation, ("Playback", "Volume"), ("Playback", "ToneCutoff"));
    }

    private static void AssertSoundMotionCalls(Compilation compilation, params (string Handle, string Parameter)[] expected)
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

                if (method.Name == "StartSoundMotion")
                {
                    executions++;
                }
                else if (method.Name == "StartSoundMotionProperty")
                {
                    Assert.Equal("float", method.Parameters[4].Type.ToDisplayString());
                    leaves.Add((
                        (string)model.GetConstantValue(invocation.ArgumentList.Arguments[1].Expression).Value!,
                        (string)model.GetConstantValue(invocation.ArgumentList.Arguments[2].Expression).Value!));
                }
            }
        }

        Assert.True(executions > 0, "The audio execution must start through GeneratedMarkup.StartSoundMotion.");
        Assert.Equal(expected.OrderBy(item => item.Parameter), leaves.OrderBy(item => item.Item2));
    }

    // A `.sound.` path names a playback parameter; an element property with
    // the same last segment must never be animated in its place.
    [Fact]
    public void SoundMotionTargetNeverFallsBackToAnElementProperty()
    {
        string markup = SoundMotionApprovedExample
            .Replace("$self.sound.Playback.ToneCutoff = 6000;", string.Empty, StringComparison.Ordinal)
            .Replace("$self.sound.Playback.Volume = 0.8;", "$self.sound.Playback.Opacity = 0.5;", StringComparison.Ordinal);

        GeneratorRunResult result = RunGenerator("SoundMotionElementFallback.crn", markup, out _);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.GetMessage().Contains("Opacity", StringComparison.Ordinal) &&
            diagnostic.GetMessage().Contains("Playback", StringComparison.Ordinal));
    }
}
