using Cerneala.Timbre.Catalog;

namespace Cerneala.Timbre;

public sealed class TimbreRuntimeOptions
{
    public ITimbreOutput? Output { get; set; }

    public int MaxVoices { get; set; } = TimbreCatalog.MaxVoices;

    public long AutoPreloadMaxBytes { get; set; } = TimbreCatalog.AutoPreloadMaxBytes;

    public long MaxPreloadBytes { get; set; } = TimbreCatalog.MaxPreloadBytesPerClip;

    public long MaxCacheBytes { get; set; } = TimbreCatalog.MaxCacheBytes;

    public long StreamingMemoryLimit { get; set; } = TimbreCatalog.StreamingMemoryLimit;

    public TimeSpan DelayTailCap { get; set; } = TimeSpan.FromSeconds(TimbreCatalog.DelayTailCapSeconds);

    public string? BaseDirectory { get; set; }
}
