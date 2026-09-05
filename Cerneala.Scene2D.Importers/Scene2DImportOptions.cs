namespace Cerneala.Scene2D.Importers;

public sealed class Scene2DImportOptions
{
    public string? AssetRootDirectory { get; init; }
    public long MaxFileBytes { get; init; } = 16 * 1024 * 1024;
    public long MaxTotalBytes { get; init; } = 64 * 1024 * 1024;
    public int MaxFiles { get; init; } = 1024;
    public int MaxJsonDepth { get; init; } = 64;
    public int MaxCells { get; init; } = 1_048_576;
    public int MaxChunks { get; init; } = 65_536;
    public int MaxLayers { get; init; } = 4096;
    public int MaxEntities { get; init; } = 65_536;
    public int MaxPoints { get; init; } = 4096;
    public int MaxDiagnostics { get; init; } = 128;
}
