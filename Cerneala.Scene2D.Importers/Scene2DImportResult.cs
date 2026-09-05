using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Importers;

public sealed class Scene2DImportResult
{
    internal Scene2DImportResult(Scene2DDocument? document, Scene2DValidationResult validation)
    {
        Success = document is not null && validation.Success;
        Document = Success ? document : null;
        Diagnostics = validation.Diagnostics;
        DiagnosticsTruncated = validation.DiagnosticsTruncated;
    }

    public bool Success { get; }
    public Scene2DDocument? Document { get; }
    public IReadOnlyList<Scene2DDiagnostic> Diagnostics { get; }
    public bool DiagnosticsTruncated { get; }
}
