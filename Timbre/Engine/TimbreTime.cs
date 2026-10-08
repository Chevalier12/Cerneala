using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre.Engine;

internal static class TimbreTime
{
    internal static TimeSpan FromFrames(long frames) =>
        TimeSpan.FromTicks(frames * TimeSpan.TicksPerSecond / TimbreCatalog.SampleRate);

    // Nearest frame, so FromFrames/ToFrames round-trips exactly.
    internal static long ToFrames(TimeSpan time) =>
        (long)((((Int128)time.Ticks * TimbreCatalog.SampleRate) + (TimeSpan.TicksPerSecond / 2)) / TimeSpan.TicksPerSecond);
}
