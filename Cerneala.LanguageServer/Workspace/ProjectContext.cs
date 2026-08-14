using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using LanguageSourceText = Cerneala.Language.Text.SourceText;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cerneala.LanguageServer.Workspace;

internal sealed class ProjectContext : IDisposable
{
    private readonly Dictionary<string, CernealaDocument> documents;

    private ProjectContext(
        string projectFilePath,
        string? targetFramework,
        Compilation roslynCompilation,
        Dictionary<string, CernealaDocument> documents,
        CernealaCompilation languageCompilation)
    {
        ProjectFilePath = projectFilePath;
        TargetFramework = targetFramework;
        RoslynCompilation = roslynCompilation;
        this.documents = documents;
        LanguageCompilation = languageCompilation;
        Summary = new WorkspaceProjectSummary(
            projectFilePath,
            targetFramework,
            roslynCompilation.AssemblyName ?? Path.GetFileNameWithoutExtension(projectFilePath),
            roslynCompilation.GetDiagnostics().Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
    }

    public string ProjectFilePath { get; }

    public string? TargetFramework { get; }

    public Compilation RoslynCompilation { get; }

    public CernealaCompilation LanguageCompilation { get; }

    public WorkspaceProjectSummary Summary { get; }

    public IReadOnlyCollection<string> DocumentPaths => documents.Keys;

    public static async Task<ProjectContext?> CreateAsync(Project project, long revision, CancellationToken cancellationToken)
    {
        if (project.FilePath is null)
        {
            return null;
        }

        Compilation? compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        if (compilation is null)
        {
            return null;
        }

        Dictionary<string, CernealaDocument> documents = new(PathComparer.Instance);
        foreach (TextDocument additionalDocument in project.AdditionalDocuments)
        {
            if (additionalDocument.FilePath is null ||
                !additionalDocument.FilePath.EndsWith(".cui.xml", StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(additionalDocument.FilePath))
            {
                continue;
            }

            Microsoft.CodeAnalysis.Text.SourceText? text =
                await additionalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (text is null)
            {
                continue;
            }

            string path = PathComparer.Normalize(additionalDocument.FilePath);
            documents[path] = new CernealaDocument(path, LanguageSourceText.From(text.ToString(), revision));
        }

        RoslynCompilationSymbols symbols = new(compilation, revision);
        CernealaCompilation languageCompilation = new(symbols, documents.Values);
        return new ProjectContext(
            PathComparer.Normalize(project.FilePath),
            GetTargetFramework(project),
            compilation,
            documents,
            languageCompilation);
    }

    public bool TryGetDocument(string path, out CernealaDocument? document) =>
        documents.TryGetValue(path, out document);

    public CernealaCompilation CreateOverlayCompilation(CernealaDocument document) =>
        LanguageCompilation.WithDocument(document);

    public void Dispose() => LanguageCompilation.Dispose();

    private static string? GetTargetFramework(Project project)
    {
        AnalyzerConfigOptions options = project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions;
        return options.TryGetValue("build_property.TargetFramework", out string? targetFramework) &&
            !string.IsNullOrWhiteSpace(targetFramework)
            ? targetFramework
            : null;
    }
}
