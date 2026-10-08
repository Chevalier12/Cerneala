using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cerneala.Tests.Timbre.Motion;

// The approved C# ergonomics compile in an ordinary consumer and bind the
// audio overload, never the generic object facade (ObjectMotionRuntime).
public sealed class TimbreMotionApiBindingTests
{
    private const string Consumer = """
        using Cerneala.Timbre;
        using Cerneala.UI.Elements;
        using Cerneala.UI.Motion;
        using Cerneala.UI.Motion.Core;
        using Cerneala.UI.Motion.Specs;

        public static class AudioMotionConsumer
        {
            public static void AnimatePlayback(TimbrePlayback playback, TimbreParameter<float> toneCutoff, MotionSpec<float> transition)
            {
                playback.Motion()
                    .Animate(TimbrePlayback.VolumeParameter)
                    .To(0.8f)
                    .With(transition);

                playback.Motion()
                    .Animate(toneCutoff)
                    .To(6000f)
                    .With(transition);
            }

            public static MotionHandle AnimateFromRoot(TimbrePlayback playback, UIRoot root, MotionSpec<float> transition) =>
                playback.Motion(root).Animate(TimbrePlayback.VolumeParameter).From(0f).To(1f).With(transition);

            public static object Generic(object target) => target.Motion();
        }
        """;

    [Fact]
    public void TimbreMotionPlaybackReceiverBindsTheAudioOverloadInAnOrdinaryConsumer()
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            "AudioMotionConsumer",
            [CSharpSyntaxTree.ParseText(Consumer, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest))],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        SyntaxTree tree = Assert.Single(compilation.SyntaxTrees);
        SemanticModel model = compilation.GetSemanticModel(tree);
        IMethodSymbol[] motion = tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression is MemberAccessExpressionSyntax { Name.Identifier.Text: "Motion" })
            .Select(invocation => (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!)
            .ToArray();

        Assert.Equal(4, motion.Length);
        foreach (IMethodSymbol method in motion.Take(3))
        {
            IMethodSymbol definition = method.ReducedFrom ?? method;
            Assert.False(definition.IsGenericMethod);
            Assert.Equal("Cerneala.Timbre.TimbrePlayback", definition.Parameters[0].Type.ToDisplayString());
            Assert.Equal("Cerneala.UI.Timbre.TimbreMotionFacade", method.ReturnType.ToDisplayString());
        }

        Assert.Equal("Cerneala.UI.Motion.ObjectMotionFacade", motion[3].ReturnType.ToDisplayString());
    }

    private static MetadataReference[] References()
    {
        string trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trusted
            .Split(Path.PathSeparator)
            .Where(path => !Path.GetFileName(path).StartsWith("Cerneala.Tests", StringComparison.Ordinal))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }
}
