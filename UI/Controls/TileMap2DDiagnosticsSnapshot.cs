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
    int TileInvalidations,
    int PromotedInstancesVisible,
    int PromotedInstancesCulled,
    int Promotions,
    int Demotions,
    int BatchSplits);
