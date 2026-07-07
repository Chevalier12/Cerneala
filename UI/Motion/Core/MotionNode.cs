namespace Cerneala.UI.Motion.Core;

public abstract class MotionNode
{
    internal bool IsRegistered { get; set; }

    protected internal abstract MotionNodeTickResult Tick(MotionFrame frame);

    protected internal virtual void OnRegistered(MotionGraph graph)
    {
    }

    protected internal virtual void OnUnregistered()
    {
    }
}

public readonly record struct MotionNodeTickResult(
    int ValuesChanged = 0,
    int PropertyWrites = 0,
    bool Completed = false,
    int RenderInvalidations = 0,
    int LayoutInvalidations = 0,
    int SkippedByReducedMotion = 0);
