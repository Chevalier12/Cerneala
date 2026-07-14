namespace Cerneala.UI.Relay;

internal readonly record struct UiRelayDrainResult(
    int SnapshotCount,
    int Dequeued,
    int Executed,
    int Canceled,
    int Faulted,
    int Deferred,
    int Backlog);
