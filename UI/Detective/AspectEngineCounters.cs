namespace Cerneala.UI.Detective;

public sealed class AspectEngineCounters
{
    public AspectEngineCounters()
    {
    }

    private AspectEngineCounters(
        int rulesConsidered,
        int rulesMatched,
        int conditionEvaluations,
        int declarationsResolved,
        int tokenLookups,
        int cacheHits,
        int cacheMisses)
    {
        RulesConsidered = rulesConsidered;
        RulesMatched = rulesMatched;
        ConditionEvaluations = conditionEvaluations;
        DeclarationsResolved = declarationsResolved;
        TokenLookups = tokenLookups;
        CacheHits = cacheHits;
        CacheMisses = cacheMisses;
    }

    public int RulesConsidered { get; internal set; }

    public int RulesMatched { get; internal set; }

    public int ConditionEvaluations { get; internal set; }

    public int DeclarationsResolved { get; internal set; }

    public int TokenLookups { get; internal set; }

    public int CacheHits { get; internal set; }

    public int CacheMisses { get; internal set; }

    internal AspectEngineCounters Snapshot()
    {
        return new AspectEngineCounters(
            RulesConsidered,
            RulesMatched,
            ConditionEvaluations,
            DeclarationsResolved,
            TokenLookups,
            CacheHits,
            CacheMisses);
    }
}
