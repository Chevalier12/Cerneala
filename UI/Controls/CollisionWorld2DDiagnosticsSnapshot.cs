using System.Diagnostics;

namespace Cerneala.UI.Controls;

public sealed class CollisionWorld2DDiagnosticsSnapshot
{
    internal CollisionWorld2DDiagnosticsSnapshot(
        int entryCount,
        int cellCount,
        long broadphaseCandidateCount,
        long exactTestCount,
        long rebuildCount,
        long incrementalUpdateCount,
        long updatedEntryCount,
        long queryCount,
        long lastQueryTicks,
        long totalQueryTicks,
        long estimatedRetainedBytes)
    {
        EntryCount = entryCount;
        CellCount = cellCount;
        BroadphaseCandidateCount = broadphaseCandidateCount;
        ExactTestCount = exactTestCount;
        RebuildCount = rebuildCount;
        IncrementalUpdateCount = incrementalUpdateCount;
        UpdatedEntryCount = updatedEntryCount;
        QueryCount = queryCount;
        LastQueryDuration = TimeSpan.FromSeconds((double)lastQueryTicks / Stopwatch.Frequency);
        TotalQueryDuration = TimeSpan.FromSeconds((double)totalQueryTicks / Stopwatch.Frequency);
        EstimatedRetainedBytes = estimatedRetainedBytes;
    }

    public int EntryCount { get; }

    public int CellCount { get; }

    public long BroadphaseCandidateCount { get; }

    public long ExactTestCount { get; }

    public long RebuildCount { get; }

    public long IncrementalUpdateCount { get; }

    public long UpdatedEntryCount { get; }

    public long QueryCount { get; }

    public TimeSpan LastQueryDuration { get; }

    public TimeSpan TotalQueryDuration { get; }

    public long EstimatedRetainedBytes { get; }
}
