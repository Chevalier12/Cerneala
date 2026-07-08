using Cerneala.UI.Motion.Core;

namespace Cerneala.UI.Invalidation;

public sealed class FrameStats
{
    public int InheritedElements { get; private set; }

    public int CommandStateElements { get; private set; }

    public int AspectElements { get; private set; }

    public int MeasuredElements { get; private set; }

    public int ArrangedElements { get; private set; }

    public int MeasureCalls { get; private set; }

    public int ArrangeCalls { get; private set; }

    public int RenderedElements { get; private set; }

    public int HitTestElements { get; private set; }

    public int ReusedCaches { get; private set; }

    public int NoWorkFrames { get; private set; }

    public int MotionFrames { get; private set; }

    public int MotionNodesSampled { get; private set; }

    public int MotionValuesChanged { get; private set; }

    public int MotionPropertyWrites { get; private set; }

    public int MotionCompleted { get; private set; }

    public int MotionRenderInvalidations { get; private set; }

    public int MotionLayoutInvalidations { get; private set; }

    public int MotionSkippedByReducedMotion { get; private set; }

    public bool HasWork =>
        InheritedElements > 0 ||
        CommandStateElements > 0 ||
        AspectElements > 0 ||
        MeasuredElements > 0 ||
        ArrangedElements > 0 ||
        RenderedElements > 0 ||
        HitTestElements > 0 ||
        MotionFrames > 0 ||
        MotionNodesSampled > 0 ||
        MotionValuesChanged > 0 ||
        MotionPropertyWrites > 0 ||
        MotionCompleted > 0 ||
        MotionRenderInvalidations > 0 ||
        MotionLayoutInvalidations > 0 ||
        MotionSkippedByReducedMotion > 0;

    public void Count(FramePhase phase)
    {
        switch (phase)
        {
            case FramePhase.InheritedProperties:
                InheritedElements++;
                break;
            case FramePhase.CommandState:
                CommandStateElements++;
                break;
            case FramePhase.Aspect:
                AspectElements++;
                break;
            case FramePhase.Measure:
                MeasuredElements++;
                break;
            case FramePhase.Arrange:
                ArrangedElements++;
                break;
            case FramePhase.RenderCache:
                RenderedElements++;
                break;
            case FramePhase.HitTest:
                HitTestElements++;
                break;
        }
    }

    public void CountReusedCache()
    {
        ReusedCaches++;
    }

    public void CountMeasureCall()
    {
        MeasureCalls++;
    }

    public void CountArrangeCall()
    {
        ArrangeCalls++;
    }

    public void CountNoWorkFrame()
    {
        NoWorkFrames++;
        CountReusedCache();
    }

    public void CountMotion(MotionFrameResult result)
    {
        MotionFrames += result.MotionFrames;
        MotionNodesSampled += result.MotionNodesSampled;
        MotionValuesChanged += result.MotionValuesChanged;
        MotionPropertyWrites += result.MotionPropertyWrites;
        MotionCompleted += result.MotionCompleted;
        MotionRenderInvalidations += result.MotionRenderInvalidations;
        MotionLayoutInvalidations += result.MotionLayoutInvalidations;
        MotionSkippedByReducedMotion += result.MotionSkippedByReducedMotion;
    }
}
