namespace Cerneala.UI.Aspect;

public sealed class AspectEngineCounters
{
    public AspectEngineCounters()
    {
    }

    private AspectEngineCounters(
        int rulesConsidered,
        int rulesMatched,
        int declarationsResolved,
        int tokenLookups,
        int cacheHits,
        int cacheMisses)
    {
        RulesConsidered = rulesConsidered;
        RulesMatched = rulesMatched;
        DeclarationsResolved = declarationsResolved;
        TokenLookups = tokenLookups;
        CacheHits = cacheHits;
        CacheMisses = cacheMisses;
    }

    public int RulesConsidered { get; internal set; }

    public int RulesMatched { get; internal set; }

    public int DeclarationsResolved { get; internal set; }

    public int TokenLookups { get; internal set; }

    public int CacheHits { get; internal set; }

    public int CacheMisses { get; internal set; }

    internal AspectEngineCounters Snapshot()
    {
        return new AspectEngineCounters(RulesConsidered, RulesMatched, DeclarationsResolved, TokenLookups, CacheHits, CacheMisses);
    }
}
