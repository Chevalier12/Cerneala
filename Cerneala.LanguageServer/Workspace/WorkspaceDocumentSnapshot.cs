using Cerneala.Language.Semantics;
using Cerneala.Language.Syntax;
using Cerneala.LanguageServer.Logging;

namespace Cerneala.LanguageServer.Workspace;

internal sealed class WorkspaceDocumentSnapshot : IDisposable
{
    private readonly WorkspaceState state;
    private readonly CernealaCompilation[] semanticCompilations;
    private readonly ServerTelemetry telemetry;

    public WorkspaceDocumentSnapshot(
        WorkspaceState state,
        CernealaDocument document,
        ProjectContext[] owners,
        ServerTelemetry telemetry)
    {
        this.state = state;
        this.telemetry = telemetry;
        Document = document;
        ProjectSummaries = owners.Select(owner => owner.Summary).ToArray();
        semanticCompilations = owners
            .Select(owner => owner.CreateOverlayCompilation(document))
            .ToArray();
        InformationDiagnostics = owners.Length == 0
            ? [WorkspaceInfoDiagnostic.StandaloneDocument]
            : [];
    }

    public CernealaDocument Document { get; }

    public long Version => Document.Version;

    public DocumentSyntax Syntax => Document.Syntax;

    public bool IsStandalone => semanticCompilations.Length == 0;

    public IReadOnlyList<WorkspaceProjectSummary> ProjectSummaries { get; }

    public IReadOnlyList<WorkspaceInfoDiagnostic> InformationDiagnostics { get; }

    public IReadOnlyList<CernealaSemanticModel> GetSemanticModels(CancellationToken cancellationToken) =>
        telemetry.Measure("bind", () => semanticCompilations
            .Select(compilation => compilation.GetSemanticModel(Document.Path, cancellationToken))
            .ToArray());

    public IReadOnlyList<CernealaSemanticModel> GetWorkspaceSemanticModels(CancellationToken cancellationToken) =>
        telemetry.Measure("bind", () => semanticCompilations
            .SelectMany(compilation => compilation.Documents.Select(document =>
                compilation.GetSemanticModel(document.Path, cancellationToken)))
            .ToArray());

    public IReadOnlyList<string> ResolveTypeAssemblies(string metadataName) =>
        semanticCompilations
            .Select(compilation => compilation.Symbols.FindType(metadataName)?.AssemblyName)
            .Where(assemblyName => assemblyName is not null)
            .Cast<string>()
            .Distinct(StringComparer.Ordinal)
            .OrderBy(assemblyName => assemblyName, StringComparer.Ordinal)
            .ToArray();

    public void Dispose()
    {
        foreach (CernealaCompilation compilation in semanticCompilations)
        {
            compilation.Dispose();
        }

        state.Release();
    }
}
