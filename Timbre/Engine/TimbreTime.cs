using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre.Engine;

internal static class TimbreTime
{
    // 128-bit, because frames * TicksPerSecond overflows long above about
    // 9.2e11 frames; a time that does not fit a TimeSpan is clamped to its range.
    internal static TimeSpan FromFrames(long frames) =>
        TimeSpan.FromTicks((long)Int128.Clamp((Int128)frames * TimeSpan.TicksPerSecond / TimbreCatalog.SampleRate, long.MinValue, long.MaxValue));

    // Nearest frame, so FromFrames/ToFrames round-trips exactly.
    internal static long ToFrames(TimeSpan time) =>
        (long)((((Int128)time.Ticks * TimbreCatalog.SampleRate) + (TimeSpan.TicksPerSecond / 2)) / TimeSpan.TicksPerSecond);
}
