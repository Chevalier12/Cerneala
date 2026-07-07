using Cerneala.UI.Motion.Interpolation;

namespace Cerneala.UI.Motion.Core;

public sealed class ManualMotionTimeline : MotionTimeline
{
    private readonly MotionGraph graph = new(new MotionThreadGuard(Environment.CurrentManagedThreadId), CreateMixers(), ReducedMotionPolicy.Default);
    private float progress;

    public override float Progress => progress;

    public void SetProgress(float progress)
    {
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
