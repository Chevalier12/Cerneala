using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Styling;

namespace Cerneala.UI.Motion.Core;

public sealed class MotionSystem
{
    private readonly IMotionClock clock;
    private TimeSpan? previousTimestamp;
    private int frameIndex;
    private bool wasActiveLastTick;

    /// <summary>
    /// Creates the root-owned motion system and captures thread affinity immediately.
    /// Construct the owning root on the UI thread; marshal cross-thread motion requests
    /// through the platform UI dispatcher before calling motion APIs.
    /// </summary>
    public MotionSystem(UIRoot root, IMotionClock clock, ReducedMotionPolicy reducedMotion)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        ReducedMotion = reducedMotion ?? throw new ArgumentNullException(nameof(reducedMotion));
        ThreadGuard = new MotionThreadGuard(Environment.CurrentManagedThreadId);
        Graph = new MotionGraph(ThreadGuard);
        Timelines = new MotionTimelineRegistry();
        Diagnostics = new MotionDiagnostics();
        Tokens = new MotionTokens();
        Frames = new MotionFrameCoordinator(root, this);
    }

    public UIRoot Root { get; }

    public MotionThreadGuard ThreadGuard { get; }

    public ReducedMotionPolicy ReducedMotion { get; }

    public MotionGraph Graph { get; }

    public MotionTimelineRegistry Timelines { get; }

    public MotionDiagnostics Diagnostics { get; }

    public MotionFrameCoordinator Frames { get; }

    public MotionTokens Tokens { get; }

    public TimeSpan MaxDelta { get; set; } = TimeSpan.FromMilliseconds(100);

    public bool HasActiveMotion => Graph.HasActiveMotion;

    public MotionFrameResult Tick(
        MotionFrameReason reason = MotionFrameReason.Scheduled,
        MotionFramePhase phase = MotionFramePhase.BeforeRender)
    {
        ThreadGuard.VerifyAccess();
        TimeSpan now = clock.Now;
        MotionFrame idleFrame = new(now, TimeSpan.Zero, frameIndex, reason, phase);
        if (!HasActiveMotion)
        {
            previousTimestamp = null;
            wasActiveLastTick = false;
            return MotionFrameResult.Empty(idleFrame);
        }

        TimeSpan delta = !wasActiveLastTick || previousTimestamp is null ? TimeSpan.Zero : now - previousTimestamp.Value;
        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }

        if (delta > MaxDelta)
        {
            delta = MaxDelta;
        }

        previousTimestamp = now;
        frameIndex++;
        MotionFrame frame = new(now, delta, frameIndex, reason, phase);
        MotionFrameResult result = Graph.Sample(frame);
        wasActiveLastTick = result.NeedsAnotherFrame || Graph.HasActiveMotion;
        if (!wasActiveLastTick)
        {
            previousTimestamp = null;
        }

        return result;
    }
}
