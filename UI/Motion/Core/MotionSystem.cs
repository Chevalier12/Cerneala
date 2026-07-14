using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Diagnostics;
using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Motion.Layout;
using Cerneala.UI.Motion.Presence;
using Cerneala.UI.Motion.Properties;
using Cerneala.UI.Motion.Specs;
using Cerneala.UI.Motion.States;
using Cerneala.UI.Motion.Transactions;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Motion.Core;

public sealed class MotionSystem
{
    public const int ActiveOpacityRenderInvalidationsPerTickBudget = 1;
    public const int SimultaneousRenderAnimationStressBudget = 100;
    public const int LayoutMotionStressBudget = 100;

    private readonly IMotionClock clock;
    private TimeSpan? previousTimestamp;
    private int frameIndex;
    private bool wasActiveLastTick;
    private TimeSpan maxDelta = TimeSpan.FromMilliseconds(100);

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
        Timelines = new MotionTimelineRegistry();
        Diagnostics = new MotionDiagnostics();
        Tokens = new MotionTokens();
        Mixers = new ValueMixerRegistry();
        Mixers.RegisterBuiltIns();
        Properties = new MotionPropertyStore();
        AnimatableProperties = new AnimatablePropertyRegistry();
        Graph = new MotionGraph(root.Relay, Mixers, ReducedMotion, Diagnostics);
        Layout = new LayoutMotionCoordinator(this);
        Presence = new PresenceCoordinator(this);
        Transactions = new MotionTransactionContext(this);
        Frames = new MotionFrameCoordinator(root, this);
    }

    public UIRoot Root { get; }

    public ReducedMotionPolicy ReducedMotion { get; }

    public MotionGraph Graph { get; }

    public MotionTimelineRegistry Timelines { get; }

    public MotionDiagnostics Diagnostics { get; }

    public MotionFrameCoordinator Frames { get; }

    public MotionTokens Tokens { get; }

    public ValueMixerRegistry Mixers { get; }

    public MotionPropertyStore Properties { get; }

    public AnimatablePropertyRegistry AnimatableProperties { get; }

    public MotionTransactionContext Transactions { get; }

    public LayoutMotionCoordinator Layout { get; }

    public PresenceCoordinator Presence { get; }

    public TimeSpan MaxDelta
    {
        get => maxDelta;
        set
        {
            VerifyAccess();
            maxDelta = value;
        }
    }

    public bool HasActiveMotion => Graph.HasActiveMotion || Properties.HasPendingWrites;

    public MotionTransactionScope BeginTransaction(MotionSpec defaultSpec)
    {
        return Transactions.Begin(defaultSpec);
    }

    public MotionTransactionScope BeginTransaction(MotionTransactionOptions options)
    {
        return Transactions.Begin(options);
    }

    public MotionTransactionScope Disable()
    {
        return Transactions.Disable();
    }

    public MotionFrameResult Tick(
        MotionFrameReason reason = MotionFrameReason.Scheduled,
        MotionFramePhase phase = MotionFramePhase.BeforeRender)
    {
        VerifyAccess();
        TimeSpan now = clock.Now;
        MotionFrame idleFrame = new(now, TimeSpan.Zero, frameIndex, reason, phase);
        if (!Graph.HasActiveMotion && !Properties.HasPendingWrites)
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
        MotionFrameResult sampled = Graph.HasActiveMotion
            ? Graph.Tick(frame)
            : new MotionFrameResult(frame, false, 1, 0, 0, 0, 0, 0, 0, 0);
        MotionPropertyFlushResult propertyFlush = Properties.Flush();
        MotionFrameResult result = new(
            sampled.Frame,
            sampled.NeedsAnotherFrame || Properties.HasPendingWrites,
            sampled.MotionFrames,
            sampled.MotionNodesSampled,
            sampled.MotionValuesChanged,
            sampled.MotionPropertyWrites + propertyFlush.PropertyWrites,
            sampled.MotionCompleted,
            sampled.MotionRenderInvalidations + propertyFlush.RenderInvalidations,
            sampled.MotionLayoutInvalidations + propertyFlush.LayoutInvalidations,
            sampled.MotionSkippedByReducedMotion);
        wasActiveLastTick = result.NeedsAnotherFrame || Graph.HasActiveMotion || Properties.HasPendingWrites;
        if (!wasActiveLastTick)
        {
            previousTimestamp = null;
        }

        return result;
    }

    internal void VerifyAccess() => Root.Relay.VerifyAccess();
}
