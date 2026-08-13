using System.Text;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Syntax;
using Cerneala.Language.Text;
using Cerneala.SourceGen;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using LanguageLinePosition = Cerneala.Language.Text.LinePosition;
using LanguageSourceText = Cerneala.Language.Text.SourceText;
using RoslynSourceText = Microsoft.CodeAnalysis.Text.SourceText;

namespace Cerneala.Tests.Language;

internal sealed record HarnessDiagnostic(
    string Id,
    string Severity,
    string Message,
    string Path,
    int StartLine,
    int StartCharacter,
    int EndLine,
    int EndCharacter);

internal sealed record LanguageParseResult(
    bool Succeeded,
    IReadOnlyList<string> ElementNames,
    IReadOnlyList<HarnessDiagnostic> Diagnostics);

internal sealed record LanguagePipelineResult(
    LanguageParseResult Syntax,
    IReadOnlyList<HarnessDiagnostic> SemanticDiagnostics,
    IReadOnlyList<HarnessDiagnostic> SourceGeneratorDiagnostics);

internal static class LanguagePipelineHarness
{
    public static LanguagePipelineResult Analyze(string path, string text)
    {
        LanguageParseResult syntax = ParseWithLanguageParser(path, text);
        IReadOnlyList<HarnessDiagnostic> sourceGenerator = RunSourceGenerator(path, text);
        IReadOnlyList<HarnessDiagnostic> semantic = syntax.Diagnostics.Count == 0
            ? RunSemanticModel(path, text)
            : [];
        return new LanguagePipelineResult(syntax, semantic, sourceGenerator);
    }

    private static IReadOnlyList<HarnessDiagnostic> RunSemanticModel(string path, string text)
    {
        CSharpCompilation compilation = CreateCompilation();
        CernealaDocument document = new(path, LanguageSourceText.From(text));
        using CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(compilation),
            [document],
            AnalysisMode.Build);
        CernealaSemanticModel model = workspace.GetSemanticModel(path);
        return model.Diagnostics
            .Where(diagnostic => diagnostic.Id != "CERNEALAUI001")
            .Select(diagnostic => ToHarnessDiagnostic(path, document.Text, diagnostic))
            .ToArray();
    }

    private static LanguageParseResult ParseWithLanguageParser(string path, string text)
    {
        LanguageSourceText source = LanguageSourceText.From(text);
        DocumentSyntax document = MarkupParser.Parse(source);
        return new LanguageParseResult(
            true,
            document.DescendantElements().Select(element => element.Name).ToArray(),
            document.Diagnostics.Select(diagnostic =>
            {
                LanguageLinePosition start = source.GetLinePosition(diagnostic.Span.Start);
                LanguageLinePosition end = source.GetLinePosition(diagnostic.Span.End);
                return new HarnessDiagnostic(
                    diagnostic.Id,
                    nameof(DiagnosticSeverity.Error),
                    diagnostic.Message,
                    path,
                    start.Line,
                    start.Character,
                    end.Line,
                    end.Character);
            }).ToArray());
    }

    private static IReadOnlyList<HarnessDiagnostic> RunSourceGenerator(string path, string text)
    {
        CSharpCompilation compilation = CreateCompilation();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UiMarkupGenerator().AsSourceGenerator()],
            [new InMemoryAdditionalText(path, text)],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return driver.GetRunResult().Results.Single().Diagnostics
            .OrderBy(diagnostic => diagnostic.Location.SourceSpan.Start)
            .ThenBy(diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .Select(ToHarnessDiagnostic)
            .ToArray();
    }

    private static CSharpCompilation CreateCompilation() => CSharpCompilation.Create(
            "LanguageCorpus",
            [CSharpSyntaxTree.ParseText("namespace CorpusInput { public static class Anchor { } }")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static MetadataReference[] References()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .ToArray();
    }

    private static HarnessDiagnostic ToHarnessDiagnostic(Diagnostic diagnostic)
    {
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        return new HarnessDiagnostic(
            diagnostic.Id,
            diagnostic.Severity.ToString(),
            diagnostic.GetMessage(),
            span.Path,
            span.StartLinePosition.Line,
            span.StartLinePosition.Character,
            span.EndLinePosition.Line,
            span.EndLinePosition.Character);
    }

    private static HarnessDiagnostic ToHarnessDiagnostic(
        string path,
        LanguageSourceText source,
        LanguageDiagnostic diagnostic)
    {
        LanguageLinePosition start = source.GetLinePosition(diagnostic.Span.Start);
        LanguageLinePosition end = source.GetLinePosition(diagnostic.Span.End);
        return new HarnessDiagnostic(
            diagnostic.Id,
            diagnostic.Severity.ToString(),
            diagnostic.Message,
            path,
            start.Line,
            start.Character,
            end.Line,
            end.Character);
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly RoslynSourceText text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            this.text = RoslynSourceText.From(text, Encoding.UTF8);
        }

        public override string Path { get; }

        public override RoslynSourceText GetText(CancellationToken cancellationToken = default) => text;
    }
}
