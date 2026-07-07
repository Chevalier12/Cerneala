using Cerneala.UI.Motion.Core;

namespace Cerneala.Tests.UI.Motion.Core;

public sealed class ManualMotionClock : IMotionClock
{
    public TimeSpan Now { get; private set; }

    public void Set(TimeSpan now)
    {
        Now = now;
    }

    public void Advance(TimeSpan delta)
    {
        Now += delta;
    }
}
