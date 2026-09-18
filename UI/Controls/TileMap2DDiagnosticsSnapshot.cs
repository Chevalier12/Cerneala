namespace Cerneala.UI.Controls;

internal readonly record struct TileMap2DDiagnosticsSnapshot(
    int TotalChunks,
    int CandidateChunks,
    int VisibleChunks,
    int CandidateTiles,
    int DrawnTiles,
    int BatchesBuilt,
    int BatchesRebuilt,
    int BatchesReused,
    int DrawCommands,
    long RetainedBytes,
    int RetainedObjects,
    int TileInvalidations)
{
    internal int WarmChunks { get; init; }
    internal int WarmBatchesPrepared { get; init; }
    internal int WarmTilesPrepared { get; init; }
    internal long WarmRetainedBytes { get; init; }
    internal long WarmChargedBytes { get; init; }
    internal long WarmImageBytes { get; init; }
    internal long WarmDataBytes { get; init; }
    internal int WarmPendingChunks { get; init; }
    internal int ResidentDataChunks { get; init; }
    internal int PendingDataChunks { get; init; }
}
