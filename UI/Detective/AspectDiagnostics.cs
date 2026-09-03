using Cerneala.UI.Aspect;

namespace Cerneala.UI.Detective;

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
            ResolutionSteps = Array.AsReadOnly((resolutionSteps ?? []).ToArray());
            TokenTraces = Array.AsReadOnly((tokenTraces ?? []).ToArray());
            Counters = counters ?? new AspectEngineCounters();
        }

        public ResolvedAspect? ResolvedAspect { get; }

        public IReadOnlyList<AspectResolutionStep> ResolutionSteps { get; }

        public IReadOnlyList<AspectTokenTrace> TokenTraces { get; }

        public AspectEngineCounters Counters { get; }
    }
}
