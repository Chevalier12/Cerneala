namespace Cerneala.UI.Invalidation;

public sealed class FrameStats
{
    public int InheritedElements { get; private set; }

    public int StyledElements { get; private set; }

    public int MeasuredElements { get; private set; }

    public int ArrangedElements { get; private set; }

    public int MeasureCalls { get; private set; }

    public int ArrangeCalls { get; private set; }

    public int RenderedElements { get; private set; }

    public int HitTestElements { get; private set; }

    public int ReusedCaches { get; private set; }

    public int NoWorkFrames { get; private set; }

    public bool HasWork =>
        InheritedElements > 0 ||
        StyledElements > 0 ||
        MeasuredElements > 0 ||
        ArrangedElements > 0 ||
        RenderedElements > 0 ||
        HitTestElements > 0;

    public void Count(FramePhase phase)
    {
        switch (phase)
        {
            case FramePhase.InheritedProperties:
                InheritedElements++;
                break;
            case FramePhase.Style:
                StyledElements++;
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
}
