using Cerneala.UI.Rendering;

namespace Cerneala.Tests.UI.Rendering;

public sealed class RenderCountersTests
{
    [Fact]
    public void CountersRecordCacheAndCompositionWork()
    {
        RenderCounters counters = new();

        counters.CountCacheHit();
        counters.CountCacheMiss();
        counters.CountLocalRebuild();
        counters.CountComposedElement();
        counters.CountEmittedCommands(3);

        Assert.Equal(1, counters.CacheHits);
        Assert.Equal(1, counters.CacheMisses);
        Assert.Equal(1, counters.LocalRebuilds);
        Assert.Equal(1, counters.ComposedElements);
        Assert.Equal(3, counters.EmittedCommands);
    }

    [Fact]
    public void EmittedCommandCountRejectsNegativeValues()
    {
        RenderCounters counters = new();

        Assert.Throws<ArgumentOutOfRangeException>(() => counters.CountEmittedCommands(-1));
    }
}
