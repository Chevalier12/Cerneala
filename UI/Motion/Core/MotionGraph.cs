namespace Cerneala.UI.Motion.Core;

/// <summary>
/// Phase 1 graph shell. Cross-thread animation requests must be marshaled through
/// the platform UI dispatcher before calling mutation APIs on this graph.
/// </summary>
public sealed class MotionGraph
{
    private readonly MotionThreadGuard threadGuard;
    private int nodesSampled;
    private int valuesChanged;
    private int propertyWrites;
    private int completed;
    private int renderInvalidations;
    private int layoutInvalidations;
    private int skippedByReducedMotion;

    public MotionGraph(MotionThreadGuard threadGuard)
    {
        this.threadGuard = threadGuard ?? throw new ArgumentNullException(nameof(threadGuard));
    }

    public bool HasActiveMotion { get; private set; }

    public void ActivateTestNode(
        int nodesSampled = 1,
        int valuesChanged = 0,
        int propertyWrites = 0,
        int completed = 0,
        int renderInvalidations = 0,
        int layoutInvalidations = 0,
        int skippedByReducedMotion = 0)
    {
        threadGuard.VerifyAccess();
        HasActiveMotion = true;
        this.nodesSampled = nodesSampled;
        this.valuesChanged = valuesChanged;
        this.propertyWrites = propertyWrites;
        this.completed = completed;
        this.renderInvalidations = renderInvalidations;
        this.layoutInvalidations = layoutInvalidations;
        this.skippedByReducedMotion = skippedByReducedMotion;
    }

    public void CompleteTestNode()
    {
        threadGuard.VerifyAccess();
        HasActiveMotion = false;
    }

    internal MotionFrameResult Sample(MotionFrame frame)
    {
        threadGuard.VerifyAccess();
        if (!HasActiveMotion)
        {
            return MotionFrameResult.Empty(frame);
        }

        bool remainsActive = completed == 0;
        HasActiveMotion = remainsActive;
        return new MotionFrameResult(
            frame,
            remainsActive,
            1,
            nodesSampled,
            valuesChanged,
            propertyWrites,
            completed,
            renderInvalidations,
            layoutInvalidations,
            skippedByReducedMotion);
    }
}
