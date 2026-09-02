namespace Cerneala.UI.Motion.Core;

public sealed class MotionTimelineRegistry
{
    private readonly Relay.IUiThreadAccess threadAccess;
    private readonly Dictionary<string, MotionTimeline> timelines = new(StringComparer.Ordinal);

    public MotionTimelineRegistry()
        : this(new Relay.CapturedUiThreadAccess())
    {
    }

    internal MotionTimelineRegistry(Relay.IUiThreadAccess threadAccess)
    {
        this.threadAccess = threadAccess ?? throw new ArgumentNullException(nameof(threadAccess));
    }

    public int Count
    {
        get
        {
            threadAccess.VerifyAccess();
            return timelines.Count;
        }
    }

    public IReadOnlyList<string> Names
    {
        get
        {
            threadAccess.VerifyAccess();
            return timelines.Keys.ToArray();
        }
    }

    public void Register(string name, MotionTimeline timeline)
    {
        threadAccess.VerifyAccess();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(timeline);
        if (!timelines.TryAdd(name, timeline))
        {
            throw new InvalidOperationException($"A motion timeline named '{name}' is already registered.");
        }
    }

    public bool TryGet(string name, out MotionTimeline? timeline)
    {
        threadAccess.VerifyAccess();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return timelines.TryGetValue(name, out timeline);
    }

    public MotionTimeline Get(string name)
    {
        return TryGet(name, out MotionTimeline? timeline)
            ? timeline!
            : throw new KeyNotFoundException($"No motion timeline named '{name}' is registered.");
    }

    public bool Remove(string name)
    {
        threadAccess.VerifyAccess();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return timelines.Remove(name);
    }

    public void Clear()
    {
        threadAccess.VerifyAccess();
        timelines.Clear();
    }
}
