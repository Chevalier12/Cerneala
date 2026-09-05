namespace Cerneala.UI.Detective;

/// <summary>Copies the latest tilemap recording counters without initiating render work.</summary>
public readonly record struct TileMapDiagnosticsSnapshot(
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
    int TileInvalidations,
    int PromotedInstancesVisible,
    int PromotedInstancesCulled,
    int Promotions,
    int Demotions,
    int BatchSplits);
