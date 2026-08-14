namespace Cerneala.LanguageServer.Workspace;

internal sealed record WorkspaceInfoDiagnostic(string Id, string Message)
{
    public static readonly WorkspaceInfoDiagnostic StandaloneDocument = new(
        "CERNEALAWORKSPACE001",
        "The document is not included as an AdditionalFile in a loaded project; semantic features are unavailable.");
}
