namespace Cerneala.UI.Rendering;

public sealed class RenderCounters
{
    public int CacheHits { get; private set; }

    public int CacheMisses { get; private set; }

    public int LocalRebuilds { get; private set; }

    public int ComposedElements { get; private set; }

    public int EmittedCommands { get; private set; }

    public void CountCacheHit()
    {
        CacheHits++;
    }

    public void CountCacheMiss()
    {
        CacheMisses++;
    }

    public void CountLocalRebuild()
    {
        LocalRebuilds++;
    }

    public void CountComposedElement()
    {
        ComposedElements++;
    }

    public void CountEmittedCommands(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        EmittedCommands += count;
    }
}
