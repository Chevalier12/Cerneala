using System.Diagnostics;

namespace Cerneala.UI.Motion.Core;

public sealed class SystemMotionClock : IMotionClock
{
    private readonly Stopwatch stopwatch = Stopwatch.StartNew();

    public TimeSpan Now => stopwatch.Elapsed;
}
