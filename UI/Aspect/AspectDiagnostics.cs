namespace Cerneala.UI.Aspect;

public static class AspectDiagnostics
{
    public sealed class Snapshot
    {
        public Snapshot(
            ResolvedAspect? resolvedAspect = null,
            IReadOnlyList<AspectResolutionStep>? resolutionSteps = null,
            IReadOnlyList<AspectTokenTrace>? tokenTraces = null,
            AspectEngineCounters? counters = null)
        {
            ResolvedAspect = resolvedAspect;
            ResolutionSteps = resolutionSteps ?? [];
            TokenTraces = tokenTraces ?? [];
            Counters = counters ?? new AspectEngineCounters();
        }

        public ResolvedAspect? ResolvedAspect { get; }

        public IReadOnlyList<AspectResolutionStep> ResolutionSteps { get; }

        public IReadOnlyList<AspectTokenTrace> TokenTraces { get; }

        public AspectEngineCounters Counters { get; }
    }
}
