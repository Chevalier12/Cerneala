using Cerneala.UI.Controls;

namespace Cerneala.Scene2D.Importers;

public sealed class Scene2DImportResult
{
    internal Scene2DImportResult(Scene2DDocument? document, Scene2DValidationResult validation,
        string assetRootDirectory, IEnumerable<string> referencedFiles)
    {
        Success = document is not null && validation.Success;
        Document = Success ? document : null;
        AssetRootDirectory = Success ? assetRootDirectory : null;
        ReferencedFiles = Success
            ? Array.AsReadOnly(referencedFiles.Order(StringComparer.Ordinal).ToArray())
            : Array.Empty<string>();
        Diagnostics = validation.Diagnostics;
        DiagnosticsTruncated = validation.DiagnosticsTruncated;
    }

    public bool Success { get; }
    public Scene2DDocument? Document { get; }
    public string? AssetRootDirectory { get; }
    public IReadOnlyList<string> ReferencedFiles { get; }
    public IReadOnlyList<Scene2DDiagnostic> Diagnostics { get; }
    public bool DiagnosticsTruncated { get; }
}
