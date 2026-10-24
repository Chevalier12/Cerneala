namespace Cerneala.UI.Controls;

// Cumulative per-instance counts of the work a scene container does while recording a frame. They are never reset
// by the control; callers take deltas between frames.
internal sealed class SceneRecordCounters2D
{
    // Child Record(...) calls made.
    internal long RecordedChildren { get; private set; }

    // Order entries built for sorting or ordering.
    internal long OrderedChildren { get; private set; }

    internal void CountRecorded() => RecordedChildren++;

    internal void CountOrdered() => OrderedChildren++;
}
