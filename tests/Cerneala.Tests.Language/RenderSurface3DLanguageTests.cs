using Cerneala.Language.Diagnostics;
using Cerneala.Language.Features;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.Tests.Language;

public sealed class RenderSurface3DLanguageTests
{
    [Fact]
    public void ExistingSchemaCompletesSurfaceContentAndDrawEvent()
    {
        AssertCompletion("<Window><RenderSur| /></Window>", "RenderSurface3D");
        AssertCompletion("<RenderSurface3D | />", "Draw");
        AssertCompletion("<RenderSurface3D | />", "RedrawMode");
        AssertCompletion("<RenderSurface3D><RenderSurface3D.| /></RenderSurface3D>", "RenderSurface3D.Content");
    }

    [Fact]
    public void MarkupDiagnosticsAcceptOverlayButRejectUnknownRigProperty()
    {
        const string valid = "<RenderSurface3D RedrawMode=\"OnDemand\"><Button Content=\"Overlay\" /></RenderSurface3D>";
        LanguagePipelineResult good = LanguagePipelineHarness.Analyze("Surface.crn", valid);
        Assert.DoesNotContain(good.Syntax.Diagnostics.Concat(good.SemanticDiagnostics).Concat(good.SourceGeneratorDiagnostics),
            d => d.Severity == "Error");
        LanguagePipelineResult invalid = LanguagePipelineHarness.Analyze("Surface.crn", "<RenderSurface3D BoneCount=\"20\" />");
        Assert.Contains(invalid.SemanticDiagnostics.Concat(invalid.SourceGeneratorDiagnostics),
            d => d.Severity == "Error" && d.Message.Contains("BoneCount", StringComparison.Ordinal));
    }

    private static void AssertCompletion(string marked, string label)
    {
        int offset = marked.IndexOf('|');
        string text = marked.Remove(offset, 1);
        CernealaDocument document = new("Surface.crn", LanguageSourceText.From(text));
        using CernealaCompilation workspace = new(new RoslynCompilationSymbols(CreateProject()), [document]);
        Assert.Contains(new CernealaCompletionService().GetCompletions(document,
            workspace.GetSemanticModel(document.Path), offset), item => item.Label == label);
    }

    private static CSharpCompilation CreateProject()
    {
        string[] paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        MetadataReference[] references = paths.Append(typeof(UIElement).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase).Select(path => MetadataReference.CreateFromFile(path)).ToArray();
        return CSharpCompilation.Create("RenderSurface3DLanguageCorpus",
            [CSharpSyntaxTree.ParseText("namespace Corpus { public static class Anchor { } }")],
            references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
