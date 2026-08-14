namespace Cerneala.LanguageServer.Workspace;

internal sealed record WorkspaceProjectSummary(
    string ProjectFilePath,
    string? TargetFramework,
    string AssemblyName,
    bool HasCompilationErrors);

internal sealed record VersionedDocumentResult<T>(long Version, T Value);

internal sealed record VersionedWorkspaceResult<T>(long Revision, T Value);
