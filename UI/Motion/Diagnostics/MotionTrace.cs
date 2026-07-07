namespace Cerneala.UI.Motion.Diagnostics;

public sealed class MotionTrace
{
    private readonly List<MotionTraceEvent> events = [];

    public IReadOnlyList<MotionTraceEvent> Events => events;

    internal void Record(MotionTraceEvent traceEvent)
    {
        events.Add(traceEvent);
    }

    public void Clear()
    {
        events.Clear();
    }
}
