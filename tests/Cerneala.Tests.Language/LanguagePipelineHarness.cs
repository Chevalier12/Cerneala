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

    // The companion C# source is given to both the semantic model (editor) and the source generator (build).
    public static LanguagePipelineResult Analyze(string path, string text, string sourcePath, string source)
    {
        LanguageParseResult syntax = ParseWithLanguageParser(path, text);
        IReadOnlyList<HarnessDiagnostic> sourceGenerator = AnalyzePairedSourceGenerator(path, text, sourcePath, source);
        IReadOnlyList<HarnessDiagnostic> semantic = syntax.Diagnostics.Count == 0
            ? RunSemanticModel(path, text, CreateCompilation(sourcePath, source))
            : [];
        return new LanguagePipelineResult(syntax, semantic, sourceGenerator);
    }

    // Same as above, with extra referenced assemblies (for example a library compiled in memory); the editor and the
    // generator get the identical compilation.
    public static LanguagePipelineResult Analyze(
        string path,
        string text,
        string sourcePath,
        string source,
        IReadOnlyList<MetadataReference> extraReferences,
        MetadataImportOptions importOptions = MetadataImportOptions.All)
    {
        LanguageParseResult syntax = ParseWithLanguageParser(path, text);
        CSharpCompilation compilation = CreateCompilation(sourcePath, source, extraReferences, importOptions);
        IReadOnlyList<HarnessDiagnostic> sourceGenerator = RunSourceGenerator(compilation, path, text);
        IReadOnlyList<HarnessDiagnostic> semantic = syntax.Diagnostics.Count == 0
            ? RunSemanticModel(path, text, compilation)
            : [];
        return new LanguagePipelineResult(syntax, semantic, sourceGenerator);
    }

    // Emits a C# library that references the same assemblies as the harness compilation.
    public static MetadataReference CreateLibraryReference(string assemblyName, string source)
    {
        CSharpCompilation library = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source)],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emitted = library.Emit(stream);
        if (!emitted.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emitted.Diagnostics));
        }

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    public static IReadOnlyList<HarnessDiagnostic> AnalyzePairedSourceGenerator(
        string path,
        string text,
        string sourcePath,
        string source)
    {
        CSharpCompilation compilation = CreateCompilation(sourcePath, source);
        return RunSourceGenerator(compilation, path, text);
    }

    // Errors of the consumer compilation after the generator added its output.
    public static IReadOnlyList<string> GeneratedCompilationErrors(
        string path,
        string text,
        string? companionPath = null,
        string? companion = null)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UiMarkupGenerator().AsSourceGenerator()],
            [new InMemoryAdditionalText(path, text)],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
        CSharpCompilation input = companion is null ? CreateCompilation() : CreateCompilation(companionPath!, companion);
        driver.RunGeneratorsAndUpdateCompilation(input, out Compilation output, out _);
        return output.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(diagnostic => diagnostic.ToString())
            .ToArray();
    }

    public static CernealaSemanticModel BindSemanticModel(string path, string text, out IDisposable lifetime)
    {
        CernealaDocument document = new(path, LanguageSourceText.From(text));
        CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(CreateCompilation()),
            [document],
            AnalysisMode.Build);
        lifetime = workspace;
        return workspace.GetSemanticModel(path);
    }

    private static IReadOnlyList<HarnessDiagnostic> RunSemanticModel(
        string path,
        string text,
        CSharpCompilation? companionCompilation = null)
    {
        CSharpCompilation compilation = companionCompilation ?? CreateCompilation();
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
        return RunSourceGenerator(compilation, path, text);
    }

    private static IReadOnlyList<HarnessDiagnostic> RunSourceGenerator(
        CSharpCompilation compilation,
        string path,
        string text)
    {
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

    private static CSharpCompilation CreateCompilation(string sourcePath, string source) => CSharpCompilation.Create(
        "LanguageCorpus",
        [CSharpSyntaxTree.ParseText(source, path: sourcePath)],
        References(),
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static CSharpCompilation CreateCompilation(
        string sourcePath,
        string source,
        IReadOnlyList<MetadataReference> extraReferences,
        MetadataImportOptions importOptions) => CSharpCompilation.Create(
        "LanguageCorpus",
        [CSharpSyntaxTree.ParseText(source, path: sourcePath)],
        References().Concat(extraReferences),
        // The IDE workspace imports non-public members of referenced assemblies; the default (Public) hides them.
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithMetadataImportOptions(importOptions));

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
