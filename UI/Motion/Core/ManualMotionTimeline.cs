using Cerneala.UI.Motion.Interpolation;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Motion.Core;

public sealed class ManualMotionTimeline : MotionTimeline
{
    private readonly IUiThreadAccess threadAccess;
    private readonly MotionGraph graph;
    private float progress;

    public ManualMotionTimeline()
    {
        threadAccess = new CapturedUiThreadAccess();
        graph = new MotionGraph(threadAccess, CreateMixers(), ReducedMotionPolicy.Default);
    }

    public override float Progress => progress;

    public void SetProgress(float progress)
    {
        threadAccess.VerifyAccess();
        this.progress = Math.Clamp(progress, 0, 1);
    }

    public MotionValue<T> CreateValue<T>(T initial)
    {
        return graph.CreateValue(initial);
    }

    private static ValueMixerRegistry CreateMixers()
    {
        ValueMixerRegistry mixers = new();
        mixers.RegisterBuiltIns();
        return mixers;
    }
}
